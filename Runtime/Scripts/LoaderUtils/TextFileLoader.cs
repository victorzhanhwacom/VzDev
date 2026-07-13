using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.IO;
using NaughtyAttributes;
using System;
using Debug = VzDev.ToolUtils.Debug;
using Cysharp.Threading.Tasks;
using System.Threading; // 使用 VzDev 的 Debug 擴展，提供更豐富的日誌功能

namespace VzDev.LoaderUtils
{
    /// <summary>
    /// 文字檔載入器，支援從 StreamingAssets、Resources、PersistentDataPath 載入文字檔。
    /// 所有載入路徑均使用 UniTask 非同步載入，0GC，並提供載入成功與失敗的事件回調。
    /// </summary>
    public class TextFileLoader : MonoBehaviour
    {
        #region Fields
        [Foldout("[Events]")] public UnityEvent<bool> onLoadingEvent;
        [Foldout("[Events]")] public UnityEvent<string> onLoaded;
        [Foldout("[Events]")] public UnityEvent<string> onFailed;
        [Foldout("[Settings]"), SerializeField] private EnumLoadPath enumLoadPath = EnumLoadPath.StreamingAssets;
        [Foldout("[Settings]"), SerializeField] private string fileName = "data.json";

        private CancellationTokenSource _cts;
        private bool _isLoading;

        #endregion

        private void Start() => SetLoadingState(_isLoading);

        /// <summary>
        /// 開始載入文字檔，根據選擇的載入路徑呼叫對應的載入方法。
        /// </summary>
        [Button, HideIf("_isLoading")]
        public void LoadFile()
        {
            StopLoading();
            _cts = new CancellationTokenSource();

            if (enumLoadPath == EnumLoadPath.Resources)
                LoadResourcesInstanceAsync(fileName, _cts.Token).Forget();
            else
                LoadWebRequestAsync(fileName, _cts.Token).Forget();
        }

        [Button, ShowIf("_isLoading")]
        public void StopLoading()
        {
            if (_isLoading)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                SetLoadingState(false);
                Debug.Log("[TextLoader] Loading stopped.");
            }
        }

        /// <summary>
        /// 從 Resources 非同步載入文字檔，使用 ResourceRequest，適用於 enumLoadPath = Resources。
        /// </summary>
        private async UniTaskVoid LoadResourcesInstanceAsync(string fileName, CancellationToken ct)
        {
            SetLoadingState(true);
            var (isSuccess, result) = await LoadFromResourcesAsync(fileName, ct);

            if (ct.IsCancellationRequested) return;

            if (isSuccess)
            {
                Debug.Log($"[TextLoader] File Loaded (Resources):\n{result}");
                onLoaded?.Invoke(result);
            }
            else
            {
                Debug.LogError($"[TextLoader] Failed to load file from Resources: {result}");
                onFailed?.Invoke(result);
            }
            SetLoadingState(false);
        }

        /// <summary>
        /// StreamingAssets / PersistentData 非同步載入，使用 UnityWebRequest。
        /// </summary>
        private async UniTaskVoid LoadWebRequestAsync(string fileName, CancellationToken ct)
        {
            SetLoadingState(true);
            var (isSuccess, result) = await LoadFromURLAsync(GetUrl(fileName), ct);

            if (ct.IsCancellationRequested) return;

            if (isSuccess)
            {
                Debug.Log($"[TextLoader] File Loaded ({enumLoadPath}):\n{result}");
                onLoaded?.Invoke(result);
            }
            else
            {
                Debug.LogError($"[TextLoader] Failed to load file from {enumLoadPath}: {result}");
                onFailed?.Invoke(result);
            }
            SetLoadingState(false);
        }

        /// <summary>
        /// 根據選擇的載入路徑組合出完整的 URL。
        /// 僅供 StreamingAssets / PersistentData 使用。
        /// </summary>
        private string GetUrl(string fileName) => enumLoadPath switch
        {
            EnumLoadPath.StreamingAssets => $"{Application.streamingAssetsPath}/{fileName}",
            EnumLoadPath.PersistentData => $"{Application.persistentDataPath}/{fileName}",
            _ => throw new ArgumentOutOfRangeException(nameof(enumLoadPath))
        };

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;
            onLoadingEvent?.Invoke(isLoading);
        }

        public enum EnumLoadPath
        {
            StreamingAssets,
            Resources,
            PersistentData,
        }

        #region Static Methods

        /// <summary>
        /// 從 Resources 非同步載入文字檔，使用 ResourceRequest，適用於 enumLoadPath = Resources。
        /// </summary>
        public static async UniTask<(bool isSuccess, string result)> LoadFromResourcesAsync(string fileName, CancellationToken ct = default)
        {
            string resourcePath = Path.GetFileNameWithoutExtension(fileName);
            ResourceRequest request = Resources.LoadAsync<TextAsset>(resourcePath);
            await request.ToUniTask(cancellationToken: ct);
            TextAsset asset = request.asset as TextAsset;
            return asset != null ? (true, asset.text) : (false, "Not found in Resources");
        }

        /// <summary>
        /// 從 StreamingAssets 非同步載入文字檔。
        /// </summary>
        public static async UniTask<(bool isSuccess, string result)> LoadFromStreamingAssetsAsync(string fileName, CancellationToken ct = default)
        => await LoadFromURLAsync($"{Application.streamingAssetsPath}/{fileName}", ct);

        /// <summary>
        /// 從 PersistentData 非同步載入文字檔。
        /// </summary>
        public static async UniTask<(bool isSuccess, string result)> LoadFromPersistentDataAsync(string fileName, CancellationToken ct = default)
        => await LoadFromURLAsync($"{Application.persistentDataPath}/{fileName}", ct);

        /// <summary>
        /// 從指定 URL 非同步載入文字檔，支援 StreamingAssets / PersistentData。
        /// </summary>
        public static async UniTask<(bool isSuccess, string result)> LoadFromURLAsync(string url, CancellationToken ct = default)
        {
            using UnityWebRequest req = UnityWebRequest.Get(url);
            try
            {
                await req.SendWebRequest().ToUniTask(cancellationToken: ct);
                return (true, req.downloadHandler.text);
            }
            catch (OperationCanceledException)
            {
                return (false, "Cancelled");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        #endregion



        /* UniTask呼叫範例
        private void Start()
        {
            _cts = new CancellationTokenSource();
            LoadDataAsync(_cts.Token).Forget();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async UniTaskVoid LoadDataAsync(CancellationToken ct)
        {
            var (isSuccess, result) = await TextFileLoader.LoadFromStreamingAssetsAsync("data.json", ct);

            if (result == "Cancelled") return; // 被取消就靜默退出

            if (isSuccess)
                ProcessData(result);
        }
         */
    }
}
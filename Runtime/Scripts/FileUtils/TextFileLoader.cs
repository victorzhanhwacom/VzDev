using System;
using System.Collections;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using VzDev.CoroutineUtils;

namespace VzDev.FileUtils
{
    /// <summary>
    /// 文字檔案載入器，支援從 Resources、StreamingAssets、PersistentData 等路徑非同步載入文字檔案。
    /// <para>+ Json / 文字檔 </para>
    /// <para>+ Static模式：用Coroutine執行 </para>
    /// <para>+ MonoBehaviour模型：用UniTask執行 </para>
    /// </summary>
    public class TextFileLoader : MonoBehaviour
    {
        #region Fields
        [Foldout("[Events]")] public UnityEvent<bool> onLoadingEvent;
        [Foldout("[Events]")] public UnityEvent<string> onLoadResultEvent;
        [Foldout("[Events]")] public UnityEvent<string> onFailedMsgEvent;
        [Foldout("[Settings]"), SerializeField] private EnumFilePath enumFilePath = EnumFilePath.streamingAssetsPath;
        [Foldout("[Settings]"), SerializeField, TextArea(1, 5)] private string filePath = "data.json";

        private CancellationTokenSource _cts;
        private bool _isLoading;
        private string fullFilePath;

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

            if (enumFilePath == EnumFilePath.resourcesPath)
                LoadResourcesInstanceAsync(filePath, _cts.Token).Forget();
            else
                LoadWebRequestAsync(filePath, _cts.Token).Forget();
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

        [Button]
        private void BrowseFile()
        {
            string fileFullPath = FileHelper.BrowseFilePanel(FileHelper.GetAssetPath(enumFilePath));
            if (string.IsNullOrEmpty(fileFullPath)) return;
            filePath = Path.GetRelativePath(Application.streamingAssetsPath, fileFullPath);
            LoadFile();
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
                onLoadResultEvent?.Invoke(result);
            }
            else
            {
                Debug.LogError($"[TextLoader] Failed to load file from Resources: {result}");
                onFailedMsgEvent?.Invoke(result);
            }
            SetLoadingState(false);
        }

        /// <summary>
        /// StreamingAssets / PersistentData 非同步載入，使用 UnityWebRequest。
        /// </summary>
        private async UniTaskVoid LoadWebRequestAsync(string fileName, CancellationToken ct)
        {
            SetLoadingState(true);
            var (isSuccess, result) = await LoadFromURLAsync(FileHelper.GetAssetPath(enumFilePath, fileName), ct);

            if (ct.IsCancellationRequested) return;

            if (isSuccess)
            {
                Debug.Log($"[TextLoader] File Loaded ({enumFilePath}):\n{result}");
                onLoadResultEvent?.Invoke(result);
            }
            else
            {
                Debug.LogError($"[TextLoader] Failed to load file from {enumFilePath}: {fileName}\n{result}");
                onFailedMsgEvent?.Invoke(result);
            }
            SetLoadingState(false);
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;
            onLoadingEvent?.Invoke(isLoading);
        }


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


        #region Static Methods
        /// <summary>
        /// 使用 Coroutine 方式讀取文字檔案，適用於 WebGL 平台下透過 UnityWebRequest 非同步讀取 StreamingAssets。
        /// </summary>
        public static Coroutine LoadTextFileCoroutine(string filePath, Action<string> onComplete, Action<string> onError = null)
        => CoroutineManager.Run(LoadCoroutine(filePath, onComplete, onError));

        private static IEnumerator LoadCoroutine(string filePath, Action<string> onComplete, Action<string> onError = null)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(filePath))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"找不到檔案: {filePath}\n\t({req.error})");
                    onComplete?.Invoke(null);
                    yield break;
                }
                onComplete?.Invoke(req.downloadHandler.text);
            }
        }


        /// <summary>
        /// 直接從指定路徑讀取文字檔案，不適用於 WebGL 平台，僅適用於 PC / Mac / Linux 平台。
        /// </summary>
        public static void LoadTextFileDirectly(string filePath, Action<string> onComplete, Action<string> onError)
        {
            if (File.Exists(filePath))
            {
                string content = System.IO.File.ReadAllText(filePath);
                onComplete?.Invoke(content);
            }
            else
            {
                onError?.Invoke($"File not found: {filePath}");
            }
        }
        #endregion
    }
}

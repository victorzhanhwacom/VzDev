using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VzDev.EditorUtils
{
    /// <summary>
    /// Runtime 版模型縮圖擷取器。
    /// 需求：
    ///  1. 在 Project Settings > Tags and Layers 新增一個專用 Layer（例如 "ThumbnailCapture"），
    ///     並確保場景中沒有其他物件常駐使用這個 Layer。
    ///  2. 在場景中放一台獨立 Camera，Component 預設 disabled（純手動 Render，不吃額外每幀花費），
    ///     透過 Inspector 指派給 _captureCamera，culling mask 會由本腳本自動設成上述 Layer。
    /// </summary>
    public class RuntimeThumbnailCapture : MonoBehaviour
    {
        [Header("相機設定")]
        [SerializeField] private Camera _captureCamera;
        [SerializeField] private int _captureLayer = 8; // 對應 Project Settings 裡新增的專用 Layer index

        [Header("輸出設定")]
        [SerializeField] private int _thumbnailSize = 256;
        [SerializeField] private bool _transparentBackground = true;
        [SerializeField] private Color _backgroundColor = Color.gray;

        [Header("視角設定")]
        [SerializeField] private float _azimuth = 45f;
        [SerializeField] private float _elevation = 30f;
        [SerializeField] private float _distanceMultiplier = 1f;
        [SerializeField] private float _fieldOfView = 30f;

        [Header("效能")]
        [Tooltip("每幀最多擷取幾個模型，WebGL 上建議 1~2 避免單幀 stall")]
        [SerializeField] private int _capturesPerFrame = 1;

        private Texture2D _readBuffer;

        /// <summary>
        /// 依序擷取 models 清單，完成後透過 onComplete 回傳對應順序的 Sprite 清單。
        /// 若某個 Transform 為 null 或找不到 Renderer，對應位置回傳 null，索引順序與輸入一致。
        /// </summary>
        public void CaptureThumbnails(List<Transform> models, Action<List<Sprite>> onComplete)
        {
            if (models == null || models.Count == 0)
            {
                onComplete?.Invoke(new List<Sprite>());
                return;
            }

            StartCoroutine(CaptureRoutine(models, onComplete));
        }

        private IEnumerator CaptureRoutine(List<Transform> models, Action<List<Sprite>> onComplete)
        {
            var results = new List<Sprite>(models.Count);

            var rt = RenderTexture.GetTemporary(_thumbnailSize, _thumbnailSize, 24, RenderTextureFormat.ARGB32);
            _captureCamera.targetTexture = rt;
            _captureCamera.enabled = false; // 手動 Render，不隨每幀自動渲染
            _captureCamera.clearFlags = CameraClearFlags.SolidColor;
            _captureCamera.backgroundColor = _transparentBackground ? new Color(0f, 0f, 0f, 0f) : _backgroundColor;
            _captureCamera.cullingMask = 1 << _captureLayer;
            _captureCamera.fieldOfView = _fieldOfView;
            _captureCamera.nearClipPlane = 0.05f;
            _captureCamera.farClipPlane = 1000f;

            EnsureReadBuffer();

            int sinceYield = 0;

            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                if (model == null)
                {
                    results.Add(null);
                    continue;
                }

                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    results.Add(null);
                    continue;
                }

                var originalLayers = SwapLayerRecursive(model, _captureLayer);

                var bounds = CalculateBounds(renderers);
                FrameCamera(_captureCamera, bounds);

                _captureCamera.Render();

                RenderTexture.active = rt;
                _readBuffer.ReadPixels(new Rect(0, 0, _thumbnailSize, _thumbnailSize), 0, 0);
                _readBuffer.Apply();
                RenderTexture.active = null;

                RestoreLayers(originalLayers);

                // 每個 Sprite 需要獨立的 Texture2D 實例，不能共用 _readBuffer
                var tex = new Texture2D(_thumbnailSize, _thumbnailSize, TextureFormat.RGBA32, false);
                tex.SetPixels32(_readBuffer.GetPixels32());
                tex.Apply();

                var sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, _thumbnailSize, _thumbnailSize),
                    new Vector2(0.5f, 0.5f));

                results.Add(sprite);

                sinceYield++;
                if (sinceYield >= _capturesPerFrame)
                {
                    sinceYield = 0;
                    yield return null;
                }
            }

            _captureCamera.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);

            onComplete?.Invoke(results);
        }

        private void EnsureReadBuffer()
        {
            if (_readBuffer != null && _readBuffer.width == _thumbnailSize && _readBuffer.height == _thumbnailSize)
                return;

            if (_readBuffer != null)
                Destroy(_readBuffer);

            _readBuffer = new Texture2D(_thumbnailSize, _thumbnailSize, TextureFormat.RGBA32, false);
        }

        private static List<(Transform t, int layer)> SwapLayerRecursive(Transform root, int layer)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originals = new List<(Transform, int)>(transforms.Length);

            for (int i = 0; i < transforms.Length; i++)
            {
                originals.Add((transforms[i], transforms[i].gameObject.layer));
                transforms[i].gameObject.layer = layer;
            }

            return originals;
        }

        private static void RestoreLayers(List<(Transform t, int layer)> originals)
        {
            for (int i = 0; i < originals.Count; i++)
            {
                var (t, layer) = originals[i];
                if (t != null)
                    t.gameObject.layer = layer;
            }
        }

        private static Bounds CalculateBounds(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private void FrameCamera(Camera camera, Bounds bounds)
        {
            float radius = bounds.extents.magnitude;
            float distance = (radius / Mathf.Sin(Mathf.Deg2Rad * camera.fieldOfView * 0.5f))
                              * _distanceMultiplier;

            float az = Mathf.Deg2Rad * _azimuth;
            float el = Mathf.Deg2Rad * _elevation;

            var direction = new Vector3(
                Mathf.Sin(az) * Mathf.Cos(el),
                Mathf.Sin(el),
                Mathf.Cos(az) * Mathf.Cos(el)
            );

            camera.transform.position = bounds.center + direction * distance;
            camera.transform.LookAt(bounds.center);
        }

        private void OnDestroy()
        {
            if (_readBuffer != null)
                Destroy(_readBuffer);
        }
    }
}
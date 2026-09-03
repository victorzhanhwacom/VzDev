#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace VzDev.EditorUtils
{
    public static class ModelThumbnailGenerator
    {
        public struct Settings
        {
            public int ThumbnailSize;
            public string OutputFolder;
            public bool TransparentBackground;
            public Color BackgroundColor;
            public float Azimuth;
            public float Elevation;
            public float DistanceMultiplier;
            public float FieldOfView;
            public float KeyLightIntensity;
            public float FillLightIntensity;
        }

        // ---- 共用：相機 / 燈光初始化 ----
        // 靜態擷圖與即時預覽都呼叫這個，確保兩邊渲染結果一致
        public static void ConfigureCamera(PreviewRenderUtility previewUtility, Settings settings)
        {
            previewUtility.cameraFieldOfView = settings.FieldOfView;
            previewUtility.camera.nearClipPlane = 0.05f;
            previewUtility.camera.farClipPlane = 1000f;
            previewUtility.camera.clearFlags = CameraClearFlags.Color;
            previewUtility.ambientColor = new Color(0.35f, 0.35f, 0.35f, 1f);

            var camData = previewUtility.camera.gameObject.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
                camData = previewUtility.camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;
            camData.renderShadows = true;
            camData.renderType = CameraRenderType.Base;

            previewUtility.lights[0].intensity = settings.KeyLightIntensity;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
            if (previewUtility.lights.Length > 1)
            {
                previewUtility.lights[1].intensity = settings.FillLightIntensity;
                previewUtility.lights[1].transform.rotation = Quaternion.Euler(-20f, -110f, 0f);
            }
        }

        public static Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        public static void FrameCamera(Camera camera, Bounds bounds, Settings settings)
        {
            float radius = bounds.extents.magnitude;
            float distance = (radius / Mathf.Sin(Mathf.Deg2Rad * camera.fieldOfView * 0.5f))
                              * settings.DistanceMultiplier;

            float az = Mathf.Deg2Rad * settings.Azimuth;
            float el = Mathf.Deg2Rad * settings.Elevation;

            var direction = new Vector3(
                Mathf.Sin(az) * Mathf.Cos(el),
                Mathf.Sin(el),
                Mathf.Cos(az) * Mathf.Cos(el)
            );

            camera.transform.position = bounds.center + direction * distance;
            camera.transform.LookAt(bounds.center);
        }

        // ---- 批次擷圖（正式輸出用） ----
        public static void GenerateThumbnails(GameObject[] objects, Settings settings)
        {
            if (!Directory.Exists(settings.OutputFolder))
                Directory.CreateDirectory(settings.OutputFolder);

            var previewUtility = new PreviewRenderUtility();
            ConfigureCamera(previewUtility, settings);

            try
            {
                foreach (var go in objects)
                    GenerateThumbnail(previewUtility, go, settings);
            }
            finally
            {
                previewUtility.Cleanup();
            }
        }

        private static void GenerateThumbnail(PreviewRenderUtility previewUtility, GameObject source, Settings settings)
        {
            var instance = Object.Instantiate(source);
            previewUtility.AddSingleGO(instance);

            var bounds = CalculateBounds(instance);
            FrameCamera(previewUtility.camera, bounds, settings);

            var texture = CaptureWithAlpha(previewUtility, settings);

            File.WriteAllBytes(
                Path.Combine(settings.OutputFolder, source.name + ".png"),
                texture.EncodeToPNG());

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(texture);
        }

        // ---- 差值去背：解決 PreviewRenderUtility 不保留真實 Alpha 的問題 ----
        private static Texture2D CaptureWithAlpha(PreviewRenderUtility previewUtility, Settings settings)
        {
            var rect = new Rect(0, 0, settings.ThumbnailSize, settings.ThumbnailSize);

            if (!settings.TransparentBackground)
            {
                previewUtility.camera.backgroundColor = settings.BackgroundColor;
                previewUtility.BeginStaticPreview(rect);
                previewUtility.Render(true);
                return previewUtility.EndStaticPreview();
            }

            // 黑底渲染
            previewUtility.camera.backgroundColor = Color.black;
            previewUtility.BeginStaticPreview(rect);
            previewUtility.Render(true);
            var blackTex = previewUtility.EndStaticPreview();
            var blackPixels = blackTex.GetPixels32();

            // 白底渲染
            previewUtility.camera.backgroundColor = Color.white;
            previewUtility.BeginStaticPreview(rect);
            previewUtility.Render(true);
            var whiteTex = previewUtility.EndStaticPreview();
            var whitePixels = whiteTex.GetPixels32();

            var result = new Color32[blackPixels.Length];
            for (int i = 0; i < result.Length; i++)
            {
                Color32 b = blackPixels[i];
                Color32 w = whitePixels[i];

                // alpha = 1 - (white - black)，三色平均以求穩定
                float diff = ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / (3f * 255f);
                float alpha = Mathf.Clamp01(1f - diff);

                if (alpha > 0.004f)
                {
                    // 還原真實顏色：黑底觀測值除以 alpha
                    float r = Mathf.Clamp01((b.r / 255f) / alpha);
                    float g = Mathf.Clamp01((b.g / 255f) / alpha);
                    float bl = Mathf.Clamp01((b.b / 255f) / alpha);

                    result[i] = new Color32(
                        (byte)(r * 255f),
                        (byte)(g * 255f),
                        (byte)(bl * 255f),
                        (byte)(alpha * 255f));
                }
                else
                {
                    result[i] = new Color32(0, 0, 0, 0);
                }
            }

            var finalTex = new Texture2D(settings.ThumbnailSize, settings.ThumbnailSize, TextureFormat.RGBA32, false);
            finalTex.SetPixels32(result);
            finalTex.Apply();

            Object.DestroyImmediate(blackTex);
            Object.DestroyImmediate(whiteTex);

            return finalTex;
        }
    }
}
#endif

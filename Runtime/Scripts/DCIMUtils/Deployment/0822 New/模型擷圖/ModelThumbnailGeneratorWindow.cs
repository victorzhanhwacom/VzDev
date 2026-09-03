using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace VzDev.EditorUtils
{
    public class ModelThumbnailGeneratorWindow : UnityEditor.EditorWindow
    {
        [MenuItem("VzDev/Tools/Thumbnail Generator/Open Window %#t")]
        private static void Open()
        {
            var window = GetWindow<ModelThumbnailGeneratorWindow>("Thumbnail Generator");
            window.minSize = new Vector2(340, 620);
        }

        // ---- 可調參數 ----
        private int _thumbnailSize = 256;
        private string _outputFolder = "Assets/Thumbnails";

        private bool _transparentBackground = true;
        private Color _backgroundColor = Color.gray;

        private float _azimuth = 45f;
        private float _elevation = 30f;
        private float _distanceMultiplier = 1f;
        private float _fieldOfView = 30f;

        private float _keyLightIntensity = 1.2f;
        private float _fillLightIntensity = 0.4f;

        private const int PreviewSize = 256;

        // ---- 即時預覽用資源 ----
        private PreviewRenderUtility _livePreview;
        private GameObject _previewInstance;
        private GameObject _previewSource;
        private Texture2D _checkerTexture;

        private Vector2 _scroll;

        private void OnEnable()
        {
            _livePreview = new PreviewRenderUtility();
            _checkerTexture = BuildCheckerTexture();
        }

        private void OnDisable()
        {
            if (_previewInstance != null)
                DestroyImmediate(_previewInstance);

            _livePreview?.Cleanup();
            _livePreview = null;

            if (_checkerTexture != null)
                DestroyImmediate(_checkerTexture);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawLivePreview();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("輸出設定", EditorStyles.boldLabel);
            _thumbnailSize = EditorGUILayout.IntSlider("解析度", _thumbnailSize, 64, 1024);
            _outputFolder = EditorGUILayout.TextField("輸出資料夾", _outputFolder);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("背景", EditorStyles.boldLabel);
            _transparentBackground = EditorGUILayout.Toggle("透明背景", _transparentBackground);
            using (new EditorGUI.DisabledScope(_transparentBackground))
            {
                _backgroundColor = EditorGUILayout.ColorField("背景顏色", _backgroundColor);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("視角", EditorStyles.boldLabel);
            _azimuth = EditorGUILayout.Slider("水平角度 (Azimuth)", _azimuth, -180f, 180f);
            _elevation = EditorGUILayout.Slider("俯仰角度 (Elevation)", _elevation, -89f, 89f);
            _distanceMultiplier = EditorGUILayout.Slider("距離倍率", _distanceMultiplier, 0.5f, 3f);
            _fieldOfView = EditorGUILayout.Slider("視野角 (FOV)", _fieldOfView, 10f, 60f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("前視角")) { _azimuth = 0f; _elevation = 0f; }
            if (GUILayout.Button("等角視角")) { _azimuth = 45f; _elevation = 30f; }
            if (GUILayout.Button("俯視角")) { _azimuth = 0f; _elevation = 89f; }
            if (GUILayout.Button("側視角")) { _azimuth = 90f; _elevation = 0f; }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("光源", EditorStyles.boldLabel);
            _keyLightIntensity = EditorGUILayout.Slider("主光強度", _keyLightIntensity, 0f, 3f);
            _fillLightIntensity = EditorGUILayout.Slider("補光強度", _fillLightIntensity, 0f, 3f);

            EditorGUILayout.Space(16);

            var selectedCount = Selection.gameObjects.Length;
            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button($"產生縮圖 ({selectedCount} 個已選取)", GUILayout.Height(32)))
                {
                    ModelThumbnailGenerator.GenerateThumbnails(Selection.gameObjects, BuildSettings());
                    AssetDatabase.Refresh();
                    Debug.Log($"[ThumbnailGenerator] 完成，共 {selectedCount} 張，輸出於 {_outputFolder}");
                }
            }

            if (selectedCount == 0)
                EditorGUILayout.HelpBox("請先在 Project 視窗選擇 Prefab。", MessageType.Info);

            EditorGUILayout.EndScrollView();

            // 拖曳滑桿時即時重繪預覽
            if (GUI.changed)
                Repaint();
        }

        private void DrawLivePreview()
        {
            EditorGUILayout.LabelField("預覽", EditorStyles.boldLabel);

            var rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));

            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorGUI.HelpBox(rect, "請選擇一個模型以顯示預覽", MessageType.Info);
                return;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            if (_transparentBackground)
                DrawChecker(rect);

            UpdatePreviewInstance(selected);
            if (_previewInstance == null)
                return;

            var settings = BuildSettings();
            ModelThumbnailGenerator.ConfigureCamera(_livePreview, settings);

            var bounds = ModelThumbnailGenerator.CalculateBounds(_previewInstance);
            ModelThumbnailGenerator.FrameCamera(_livePreview.camera, bounds, settings);

            // BeginPreview/EndPreview 走連續渲染路徑，與擷圖用的 BeginStaticPreview 不同 API，
            // 這裡不需要處理真實 Alpha（棋盤格已經模擬透明效果），維持零 GC 的即時互動即可
            _livePreview.camera.backgroundColor = _transparentBackground ? new Color(0f, 0f, 0f, 0f) : _backgroundColor;
            _livePreview.BeginPreview(rect, GUIStyle.none);
            _livePreview.Render(true);
            var tex = _livePreview.EndPreview();

            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, true);
        }

        private void UpdatePreviewInstance(GameObject selected)
        {
            if (_previewSource == selected && _previewInstance != null)
                return;

            if (_previewInstance != null)
                DestroyImmediate(_previewInstance);

            _previewSource = selected;
            _previewInstance = Instantiate(selected);
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            _livePreview.AddSingleGO(_previewInstance);
        }

        private void DrawChecker(Rect rect)
        {
            const float tile = 16f;
            var texCoordRect = new Rect(0, 0, rect.width / tile, rect.height / tile);
            GUI.DrawTextureWithTexCoords(rect, _checkerTexture, texCoordRect, false);
        }

        private static Texture2D BuildCheckerTexture()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            var light = new Color(0.82f, 0.82f, 0.82f, 1f);
            var dark = new Color(0.62f, 0.62f, 0.62f, 1f);

            tex.SetPixel(0, 0, dark);
            tex.SetPixel(1, 0, light);
            tex.SetPixel(0, 1, light);
            tex.SetPixel(1, 1, dark);
            tex.Apply();

            return tex;
        }

        private ModelThumbnailGenerator.Settings BuildSettings()
        {
            return new ModelThumbnailGenerator.Settings
            {
                ThumbnailSize = _thumbnailSize,
                OutputFolder = _outputFolder,
                TransparentBackground = _transparentBackground,
                BackgroundColor = _backgroundColor,
                Azimuth = _azimuth,
                Elevation = _elevation,
                DistanceMultiplier = _distanceMultiplier,
                FieldOfView = _fieldOfView,
                KeyLightIntensity = _keyLightIntensity,
                FillLightIntensity = _fillLightIntensity
            };
        }
    }
}
#endif

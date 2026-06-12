using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace VzDev.ToolUtils.ThemeUtils
{
    public interface IThemeSubscriber
    {
        ColorToken TargetToken { get; }
        void ApplyColor(Color color);
    }

    public abstract class ThemedGraphic : MonoBehaviour, IThemeSubscriber
    {
        [SerializeField] protected ColorToken colorToken = ColorToken.Primary;
        protected Graphic _graphic;

        public ColorToken TargetToken => colorToken;

        protected virtual void Awake()
            => _graphic = GetComponent<Graphic>();

        private void OnEnable()
        {
            ThemeManager.OnThemeChanged += HandleThemeChanged;
            if (ThemeManager.Instance?.Current != null)
                ApplyColor(ThemeManager.Instance.Current.Get(colorToken));
        }

        private void OnDisable()
            => ThemeManager.OnThemeChanged -= HandleThemeChanged;

        protected virtual void HandleThemeChanged(ThemeData theme)
            => ApplyColor(theme.Get(colorToken));

        public virtual void ApplyColor(Color color)
            => _graphic.color = color;

        // ThemeManager Editor 按鈕 與 各子類別自身按鈕 共用的入口
        public void ApplyThemeEditor(ThemeData theme)
        {
#if UNITY_EDITOR
            if (_graphic == null) _graphic = GetComponent<Graphic>();
#endif
            HandleThemeChanged(theme);
        }

#if UNITY_EDITOR
        [NaughtyAttributes.Button("Apply Theme (Editor)")]
        private void ApplyThemeInEditor()
        {
            if (Application.isPlaying)
            {
                if (ThemeManager.Instance?.Current != null)
                    HandleThemeChanged(ThemeManager.Instance.Current);
                return;
            }

            ThemeData theme = FindThemeDataInEditor();
            if (theme == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 找不到 ThemeData，請確認場景中有 ThemeManager 且已設定 Default Theme。");
                return;
            }

            theme.RebuildCache();
            ApplyThemeEditor(theme);

            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private ThemeData FindThemeDataInEditor()
        {
            var manager = FindObjectOfType<ThemeManager>();
            if (manager != null)
            {
                var so   = new SerializedObject(manager);
                var prop = so.FindProperty("_defaultTheme");
                if (prop?.objectReferenceValue is ThemeData td) return td;
            }

            string[] guids = AssetDatabase.FindAssets("t:ThemeData");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<ThemeData>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));

            return null;
        }
#endif
    }
}

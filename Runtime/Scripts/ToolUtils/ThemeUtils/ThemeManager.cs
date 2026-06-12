using System;
using NaughtyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace VzDev.ToolUtils.ThemeUtils
{
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }
        public static event Action<ThemeData> OnThemeChanged;
        public ThemeData Current { get; private set; }

        [SerializeField, Expandable] private ThemeData _defaultTheme;

        private void Awake()
        {
            Instance = this;
            ApplyTheme(_defaultTheme);
        }

        public void ApplyTheme(ThemeData theme)
        {
            Current = theme;
            Current.RebuildCache();
            OnThemeChanged?.Invoke(Current);
        }

        public void SetColor(ColorToken token, Color color)
        {
            Current.OverrideToken(token, color);
            OnThemeChanged?.Invoke(Current);
        }

#if UNITY_EDITOR
        [Button("Apply Theme To All (Editor)")]
        private void ApplyThemeToAllInEditor()
        {
            if (Application.isPlaying)
            {
                ApplyTheme(_defaultTheme);
                return;
            }

            if (_defaultTheme == null)
            {
                Debug.LogWarning("[ThemeManager] 尚未設定 Default Theme。");
                return;
            }

            _defaultTheme.RebuildCache();

            // 找場景中所有 ThemedGraphic（含 inactive 物件）
            var allGraphics = FindObjectsOfType<ThemedGraphic>(includeInactive: true);

            foreach (var g in allGraphics)
            {
                g.ApplyThemeEditor(_defaultTheme);
                EditorUtility.SetDirty(g);
            }

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[ThemeManager] 已套用 '{_defaultTheme.name}' 到 {allGraphics.Length} 個物件。");
        }
#endif
    }
}

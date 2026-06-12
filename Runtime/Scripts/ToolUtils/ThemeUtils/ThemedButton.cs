using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ToolUtils.ThemeUtils
{
    public class ThemedButton : ThemedGraphic
    {
        [SerializeField] private ColorToken _normalToken    = ColorToken.Normal;
        [SerializeField] private ColorToken _highlightToken = ColorToken.Highlight;
        [SerializeField] private ColorToken _pressedToken   = ColorToken.Pressed;
        [SerializeField] private ColorToken _selectedToken  = ColorToken.Selected;
        [SerializeField] private ColorToken _disabledToken  = ColorToken.Disabled;

        private Button    _btn;
        private ThemeData _cachedTheme;

        protected override void Awake()
        {
            base.Awake();
            InitButton();
        }

        // Awake 在 Editor 非 Play 模式不執行，所以每次需要時 lazy init
        private void InitButton()
        {
            if (_btn != null) return;
            _btn     = GetComponent<Button>();
            _graphic = _btn != null ? _btn.targetGraphic : _graphic;
        }

        protected override void HandleThemeChanged(ThemeData theme)
        {
            _cachedTheme = theme;
            InitButton();
            ApplyColor(Color.clear);
        }

        public override void ApplyColor(Color color)
        {
            InitButton();
            if (_btn == null) return;

            ThemeData theme = _cachedTheme ?? ThemeManager.Instance?.Current;
            if (theme == null) return;

            var cb = _btn.colors;
            cb.normalColor      = theme.Get(_normalToken);
            cb.highlightedColor = theme.Get(_highlightToken);
            cb.pressedColor     = theme.Get(_pressedToken);
            cb.selectedColor    = theme.Get(_selectedToken);
            cb.disabledColor    = theme.Get(_disabledToken);
            _btn.colors = cb;
        }
    }
}

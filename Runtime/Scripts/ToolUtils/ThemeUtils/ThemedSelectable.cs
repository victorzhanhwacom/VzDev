using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ToolUtils.ThemeUtils
{
    [RequireComponent(typeof(Selectable))]
    public class ThemedSelectable : ThemedGraphic
    {
     /*    public enum SelectableMode { Button, Toggle, Dropdown, Slider }

        [SerializeField] private SelectableMode _mode = SelectableMode.Button; */

        [SerializeField] private ColorToken _normalToken    = ColorToken.Normal;
        [SerializeField] private ColorToken _highlightToken = ColorToken.Highlight;
        [SerializeField] private ColorToken _pressedToken   = ColorToken.Pressed;
        [SerializeField] private ColorToken _selectedToken  = ColorToken.Selected;
        [SerializeField] private ColorToken _disabledToken  = ColorToken.Disabled;

        private Selectable _selectable;
        private ThemeData  _cachedTheme;

        protected override void Awake()
        {
            base.Awake();
            InitSelectable();
        }

        private void InitSelectable()
        {
            if (_selectable != null) return;

            if(TryGetComponent(out _selectable)) return;

          /*   _selectable = _mode switch
            {
                SelectableMode.Button   => GetComponent<Button>(),
                SelectableMode.Toggle   => GetComponent<Toggle>(),
                SelectableMode.Dropdown => GetComponent<TMP_Dropdown>(),
                SelectableMode.Slider   => GetComponent<Slider>(),
                _ => null
            }; */

            if (_selectable != null)
                _graphic = _selectable.targetGraphic;
            else
                Debug.LogWarning($"[ThemedSelectable] No Selectable component found on {gameObject.name}. Please add a Selectable component or change the mode.", this);
        }

        protected override void HandleThemeChanged(ThemeData theme)
        {
            _cachedTheme = theme;
            InitSelectable();
            ApplyColor(Color.clear);
        }

        public override void ApplyColor(Color color)
        {
            InitSelectable();
            if (_selectable == null) return;

            ThemeData theme = _cachedTheme ?? ThemeManager.Instance?.Current;
            if (theme == null) return;

            var cb = _selectable.colors;
            cb.normalColor      = theme.Get(_normalToken);
            cb.highlightedColor = theme.Get(_highlightToken);
            cb.pressedColor     = theme.Get(_pressedToken);
            cb.selectedColor    = theme.Get(_selectedToken);
            cb.disabledColor    = theme.Get(_disabledToken);
            _selectable.colors  = cb;
        }
    }
}

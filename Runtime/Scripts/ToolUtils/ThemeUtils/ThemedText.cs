using TMPro;
using UnityEngine;

namespace VzDev.ToolUtils.ThemeUtils
{
    [UnityEngine.RequireComponent(typeof(UnityEngine.UI.Graphic))]
    public class ThemedText : ThemedGraphic
    {
        [SerializeField] private FontToken _fontToken = FontToken.Body;

        private TextMeshProUGUI _tmp;

        protected override void Awake()
        {
            base.Awake();
            _tmp = GetComponent<TextMeshProUGUI>();
        }

        public override void ApplyColor(Color color)
        {
            if (_tmp != null) _tmp.color = color;
            else base.ApplyColor(color);
        }

        private void ApplyFont(ThemeData theme)
        {
            if (_tmp == null) return;
            var entry = theme.GetFont(_fontToken);
            if (entry.font != null) _tmp.font = entry.font;
            if (entry.fontSize > 0) _tmp.fontSize = entry.fontSize;
            _tmp.fontStyle = entry.fontStyle;
        }

        protected override void HandleThemeChanged(ThemeData theme)
        {
            base.HandleThemeChanged(theme);

            if(!TryGetComponent(out _tmp)){
                Debug.LogWarning($"[ThemedText] No TextMeshProUGUI component found on {gameObject.name}.", this);
                return;
            }

            ApplyFont(theme);
        }
    }
}

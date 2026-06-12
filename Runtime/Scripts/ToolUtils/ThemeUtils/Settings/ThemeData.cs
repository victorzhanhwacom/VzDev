using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace VzDev.ToolUtils.ThemeUtils
{

    [CreateAssetMenu(menuName = "UI/ThemeData")]
    public class ThemeData : ScriptableObject
    {
        #region Font Tokens
        [Serializable]
        public struct FontEntry
        {
            public FontToken token;
            public TMP_FontAsset font;
            public float fontSize;
            public FontStyles fontStyle;  // Bold / Italic / Normal
        }

        public FontEntry[] fonts;

        private Dictionary<FontToken, FontEntry> _fontMap;

        public FontEntry GetFont(FontToken token)
        {
            _fontMap ??= fonts.ToDictionary(e => e.token, e => e);
            return _fontMap.TryGetValue(token, out var f) ? f : default;
        }
        #endregion


        #region Color Tokens
        [Serializable]
        public struct TokenEntry { public ColorToken token; public Color color; }
        public static FontEntry[] fontTokens;

        public TokenEntry[] tokens;

        // 快取 Dictionary，避免每次線性搜尋
        private Dictionary<ColorToken, Color> _map;

        public Color Get(ColorToken token)
        {
            _map ??= tokens.ToDictionary(e => e.token, e => e.color);
            return _map.TryGetValue(token, out var c) ? c : Color.magenta; // magenta = 除錯警示
        }
        #endregion


        // 接在 ThemeData 裡
        public void RebuildCache()
        {
            _map = tokens.ToDictionary(e => e.token, e => e.color);
            _fontMap = fonts.ToDictionary(e => e.token, e => e);
        }

        public void OverrideToken(ColorToken token, Color color)
        {
            _map ??= tokens.ToDictionary(e => e.token, e => e.color);
            _map[token] = color;
            // 同步回 array（讓 Inspector 可見，可選）
            for (int i = 0; i < tokens.Length; i++)
                if (tokens[i].token == token) { tokens[i].color = color; return; }
        }
    }
}

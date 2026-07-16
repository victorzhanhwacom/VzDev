using System.Collections.Generic;
using UnityEngine;

namespace VzDev.RenderingUtils.Outline
{
    /// <summary>
    /// 支援多個高亮群組（例如 Hover / Selected），各自可設定不同顏色。
    /// 內部用 HashSet 避免重複 Add 造成 Mask Pass 對同一物件重複繪製。
    /// </summary>
    public enum HighlightGroup { Hover, Selected }

    public static class HighlightRegistry
    {
        private static readonly Dictionary<HighlightGroup, HashSet<Renderer>> groups = new()
        {
            { HighlightGroup.Hover,    new HashSet<Renderer>() },
            { HighlightGroup.Selected, new HashSet<Renderer>() },
        };

        public static void SetSingle(HighlightGroup group, Renderer renderer)
        {
            var set = groups[group];
            set.Clear();
            if (renderer != null) set.Add(renderer);
        }

        public static void Add(HighlightGroup group, Renderer renderer)
        {
            if (renderer != null) groups[group].Add(renderer);
        }

        public static void Remove(HighlightGroup group, Renderer renderer)
        {
            groups[group].Remove(renderer);
        }

        public static void Clear(HighlightGroup group) => groups[group].Clear();

        public static IReadOnlyCollection<Renderer> Get(HighlightGroup group) => groups[group];

        public static bool HasAny()
        {
            foreach (var kv in groups)
                if (kv.Value.Count > 0) return true;
            return false;
        }
    }
}
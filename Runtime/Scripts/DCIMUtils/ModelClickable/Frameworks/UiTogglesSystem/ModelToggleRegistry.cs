using System.Collections.Generic;
using UnityEngine;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 模型 GameObject → ModelToggleBinding 的查表註冊表。
    /// 與 AssetDataDisplayRegistry 是同一種模式：ModelToggleBinding 在 OnEnable/OnDisable
    /// 自我登記/取消，這裡完全不需要手動維護清單，也不會受樓層場景 Additive 載入/卸載影響
    /// （物件被卸載時 OnDisable 會自動觸發取消登記）。
    ///
    /// 與 AssetDataDisplayRegistry 的差異：那裡是「一個 Type 對應多個 Handler」（一對多），
    /// 這裡是「一個模型對應一個 Toggle」（一對一），所以查表結果不是 List，是單一物件。
    /// </summary>
    public static class ModelToggleRegistry
    {
        private static readonly Dictionary<GameObject, ModelToggleBinding> registry = new();

        public static void Register(GameObject model, ModelToggleBinding binding)
        {
            if (model == null) return;
            registry[model] = binding;
        }

        public static void Unregister(GameObject model, ModelToggleBinding binding)
        {
            if (model == null) return;
            if (registry.TryGetValue(model, out var existing) && existing == binding)
                registry.Remove(model);
        }

        public static bool TryGetBinding(GameObject model, out ModelToggleBinding binding)
            => registry.TryGetValue(model, out binding);
    }
}

using System;
using System.Collections.Generic;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 全域面板註冊表。每個 InfoPanelHandlerBase 在 OnEnable/OnDisable 時自我登記/取消，
    /// Dispatcher 完全不需要在 Inspector 手動拖曳清單，新增面板不會讓任何既有檔案變大。
    /// 一個 DataType 可以對應多個 Handler（例如 Fan 同時有基本資訊面板 + 趨勢圖面板）。
    /// </summary>
    public static class AssetDataDisplayRegistry
    {
        private static readonly Dictionary<Type, List<IModelSelectedHandler>> registry = new();
        private static readonly List<IModelSelectedHandler> Empty = new();

        public static void Register(IModelSelectedHandler handler)
        {
            if (!registry.TryGetValue(handler.DataType, out var list))
            {
                list = new List<IModelSelectedHandler>();
                registry[handler.DataType] = list;
            }
            if (!list.Contains(handler)) list.Add(handler);
        }

        public static void Unregister(IModelSelectedHandler handler)
        {
            if (registry.TryGetValue(handler.DataType, out var list))
                list.Remove(handler);
        }

        public static IReadOnlyList<IModelSelectedHandler> GetHandlers(Type dataType)
        {
            return registry.TryGetValue(dataType, out var list) ? list : Empty;
        }
    }
}
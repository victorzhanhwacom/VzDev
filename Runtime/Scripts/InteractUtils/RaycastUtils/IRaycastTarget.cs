using UnityEngine;

namespace VzDev.RaycastUtils
{
    /// <summary>
    /// 掛在有 Collider 的物件（或其父物件）上，接收 RaycastManager 的通知。
    /// hitIndex：在依距離排序後 hit 列表中的索引，0 = 最近（最前面）。
    /// </summary>
    public interface IRaycastTarget
    {
        void OnRaycastClick(in RaycastHit hit, int hitIndex);
        void OnRaycastHoverEnter(in RaycastHit hit, int hitIndex);
        void OnRaycastHoverExit();
    }
}

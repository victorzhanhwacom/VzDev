using System.Collections.Generic;
using UnityEngine;
using VzDev.Frameworks.LifecycleUtils;

namespace VzDev.Frameworks.UIAnchorUtils
{
   
   

    /// <summary>
    /// 集中管理所有 UIAnchorFollowerItem，單一 Update 迴圈取代逐一掛載的 Update()。
    /// 掛載方式：改為 OnEnable/OnDisable 呼叫 Register/Unregister，取代原本掛 UIAnchorFollower 元件。
    /// </summary>
    public static class UIAnchorFollowerRegistry
    {
        private static readonly List<UIAnchorFollowerItem> items = new();
        public static void Register(UIAnchorFollowerItem item) => items.Add(item);
        public static void Unregister(UIAnchorFollowerItem item) => items.Remove(item);
        public static List<UIAnchorFollowerItem> Items => items;
    }

    public class UIAnchorFollowerManager : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RectTransform canvasRect;

        private void OnEnable() => GlobalLifecycleBroadcaster.OnGlobalUpdate += Tick;
        private void OnDisable() => GlobalLifecycleBroadcaster.OnGlobalUpdate -= Tick;

        private void Tick()
        {
            Vector2 canvasSize = canvasRect.rect.size;
            Vector3 camPos = mainCamera.transform.position;
            var list = UIAnchorFollowerRegistry.Items;

            // 相機這幀完全沒動、且沒有任何 target 動過時，可以整批跳過（見下方說明）
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item.target3DObject == null) continue;

                Vector3 targetPos = item.target3DObject.position;
                bool inRange = item.isAlwaysVisible ||
                    (targetPos - camPos).sqrMagnitude <= item.visibleRangeSqr;
                if (!item.isAlwaysVisible && item.visibleReverse) inRange = !inRange;

                Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPos + item.offsetPos);
                bool visible = inRange && screenPos.z > 0 &&
                    item.target3DObject.gameObject.activeInHierarchy;

                if (visible != item.lastActive)
                {
                    item.container.SetActive(visible);
                    item.lastActive = visible;
                }
                if (!visible) continue;

                Vector2 localPos = new(
                    (screenPos.x - canvasSize.x * 0.5f),
                    (screenPos.y - canvasSize.y * 0.5f));

                if (localPos != item.lastAnchoredPos)
                {
                    item.rectTrans.anchoredPosition = localPos;
                    item.lastAnchoredPos = localPos;
                }
            }
        }
    }
}
using UnityEngine;

namespace VzDev.Frameworks.UIAnchorUtils
{
     /// <summary>
    /// 純資料容器，不繼承 MonoBehaviour，避免每個標記都是一個獨立 Component + Update。
    /// </summary>
    public class UIAnchorFollowerItem:MonoBehaviour
    {
        public Transform target3DObject;
        public RectTransform rectTrans;
        public GameObject container;
        public Vector3 offsetPos;
        public bool isAlwaysVisible;
        public float visibleRangeSqr;
        public bool visibleReverse;

        // 快取上一次狀態，避免不必要的 SetActive / anchoredPosition 寫入
        public bool lastActive;
        public Vector2 lastAnchoredPos;
    }
}

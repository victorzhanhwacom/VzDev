using UnityEngine;

namespace VzDev.Helpers
{
    public static class MouseHelper
    {
        /// <summary>
        /// 判斷滑鼠是否在螢幕外
        /// </summary>
        public static bool IsPointerOutOfScreen
        {
            get
            {
                Vector3 mousePosition = Input.mousePosition;
                return mousePosition.x < 0 || mousePosition.y < 0
                || mousePosition.x > Screen.width || mousePosition.y > Screen.height;
            }
        }
    }
}

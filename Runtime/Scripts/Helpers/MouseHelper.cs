using UnityEngine;
using UnityEngine.InputSystem;

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

        /// <summary>
        /// 取得本幀「按下」的指標位置（滑鼠 / 觸控），同時支援新舊 Input System
        /// </summary>
        public static bool TryGetPointerDown(out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            var p = Pointer.current;
            if (p != null && p.press.wasPressedThisFrame) { pos = p.position.ReadValue(); return true; }
#else
        if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
#endif
            pos = default; return false;
        }

    }
}

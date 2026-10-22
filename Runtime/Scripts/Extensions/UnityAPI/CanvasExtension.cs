using System.Collections.Generic;
using UnityEngine;

namespace VzDev.ApiExtensions
{
    public static class CanvasExtension
    {
        /// <summary>
        /// 取得 Canvas 的 UI Camera。若 Canvas 為 ScreenSpaceOverlay，則回傳 null。
        /// </summary>
        public static Camera GetUICamera(this Canvas canvas)
        {
            var root = canvas.rootCanvas;
            return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }
    }
}
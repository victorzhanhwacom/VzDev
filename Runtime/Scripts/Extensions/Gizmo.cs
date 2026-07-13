using UnityEngine;

namespace VzDev.ToolUtils
{
    public static class Gizmos
    {
         // 🎯 Debug 可視化射線（只在 Scene 視窗中可見，不會出現在 Game 視窗）
        public static void DrawRay(Ray ray, float distance, Color? rayColor = null, float duration = 0.01f) =>
            UnityEngine.Debug.DrawRay(ray.origin, ray.direction * distance, rayColor ?? Color.green, duration);
    }
}

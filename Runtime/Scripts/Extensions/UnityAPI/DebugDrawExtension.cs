using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VzDev.ApiExtensions
{
    /// <summary>DebugDraw 功能擴充</summary>
    public static class DebugDrawExtension
    {
        #region Private Fields
        const int Segments = 24;
        // 單位圓的 cos/sin 預先算好，執行時不再呼叫三角函數
        static readonly Vector2[] s_UnitCircle = BuildUnitCircle(Segments);

        private static Vector2[] BuildUnitCircle(int n)
        {
            var arr = new Vector2[n + 1];
            float step = Mathf.PI * 2f / n;
            for (int i = 0; i < n; i++)
                arr[i] = new Vector2(Mathf.Cos(step * i), Mathf.Sin(step * i));
            arr[n] = arr[0];
            return arr;
        }
        #endregion

        /// <summary>畫一個 3 軸十字點（最省：只有 3 條線）</summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawPoint(this Vector3 pos, float size, Color color,
                                     float duration = 0f, bool depthTest = true)
        {
            float h = size * 0.5f;
            Debug.DrawLine(new Vector3(pos.x - h, pos.y, pos.z), new Vector3(pos.x + h, pos.y, pos.z), color, duration, depthTest);
            Debug.DrawLine(new Vector3(pos.x, pos.y - h, pos.z), new Vector3(pos.x, pos.y + h, pos.z), color, duration, depthTest);
            Debug.DrawLine(new Vector3(pos.x, pos.y, pos.z - h), new Vector3(pos.x, pos.y, pos.z + h), color, duration, depthTest);
        }

        /// <summary>在指定法線平面上畫圓</summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawCircle(this Vector3 center, float radius, Vector3 normal, Color color,
                                      float duration = 0f, bool depthTest = true)
        {
            if (normal.sqrMagnitude < 1e-8f) normal = Vector3.up;
            else normal.Normalize();

            // 建立平面上的兩個正交軸（長度 = radius）
            Vector3 helper = Mathf.Abs(normal.y) < 0.99f ? Vector3.up : Vector3.right;
            Vector3 u = Vector3.Cross(normal, helper).normalized * radius;
            Vector3 v = Vector3.Cross(normal, u);

            Vector3 prev = center + u;
            for (int i = 1; i <= Segments; i++)
            {
                Vector2 p = s_UnitCircle[i];
                Vector3 cur = center + u * p.x + v * p.y;
                Debug.DrawLine(prev, cur, color, duration, depthTest);
                prev = cur;
            }
        }

        /// <summary>水平圓（XZ 平面），最常用的便利版</summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawCircle(this Vector3 center, float radius, Color color,
                                      float duration = 0f, bool depthTest = true)
            => DrawCircle(center, radius, Vector3.up, color, duration, depthTest);

        /// <summary>線框球：三個正交圓</summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawWireSphere(this Vector3 center, float radius, Color color,
                                          float duration = 0f, bool depthTest = true)
        {
            DrawCircle(center, radius, Vector3.up, color, duration, depthTest);
            DrawCircle(center, radius, Vector3.right, color, duration, depthTest);
            DrawCircle(center, radius, Vector3.forward, color, duration, depthTest);
        }

        /// <summary>畫線並在終點加箭頭（共 5 條線）</summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawArrow(this Vector3 start, Vector3 end, Color color,
                                     float headSize = 0.2f, float duration = 0f, bool depthTest = true)
        {
            Debug.DrawLine(start, end, color, duration, depthTest);

            Vector3 dir = end - start;
            float len = dir.magnitude;
            if (len < 1e-5f) return;
            dir /= len;

            float h = Mathf.Min(headSize, len * 0.5f);   // 線太短時箭頭不超過一半長度
            Vector3 helper = Mathf.Abs(dir.y) < 0.99f ? Vector3.up : Vector3.right;
            Vector3 side = Vector3.Cross(dir, helper).normalized * (h * 0.5f);
            Vector3 up = Vector3.Cross(dir, side);
            Vector3 back = end - dir * h;

            Debug.DrawLine(end, back + side, color, duration, depthTest);
            Debug.DrawLine(end, back - side, color, duration, depthTest);
            Debug.DrawLine(end, back + up, color, duration, depthTest);
            Debug.DrawLine(end, back - up, color, duration, depthTest);
        }

        /// <summary>起點畫十字、終點畫線框球，中間畫箭頭</summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawMarkedLine(this Vector3 start, Vector3 end,
                                          Color lineColor, Color startColor, Color endColor,
                                          float markerSize = 0.2f, float duration = 0f, bool depthTest = true)
        {
            start.DrawPoint(markerSize, startColor, duration, depthTest);
            end.DrawWireSphere(markerSize * 0.5f, endColor, duration, depthTest);
            start.DrawArrow(end, lineColor, markerSize, duration, depthTest);
        }
    }
}
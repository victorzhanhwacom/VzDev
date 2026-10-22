using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace VzDev.UnityAPI.Extensions
{
    public static class RectTransformExtension
    {
        #region 判斷座標是否落在 RectTransform 範圍內
        /// <summary>
        /// 共用暫存清單，避免每次點擊都產生 GC（UI 只在主執行緒，安全）
        /// </summary>
        private static readonly List<RectTransform> _rectBuffer = new(64);

        public static bool ContainsScreenPoint(this RectTransform rt, Vector2 screenPos, Camera uiCam, bool includeChildren = true)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy) return false;
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, uiCam)) return true;
            if (!includeChildren) return false;

            _rectBuffer.Clear();
            rt.GetComponentsInChildren(false, _rectBuffer);   // 只抓啟用中的子物件，List 版本不產生 GC
            bool hit = false;
            for (int i = 0; i < _rectBuffer.Count; i++)
            {
                var child = _rectBuffer[i];
                if (child != rt && RectTransformUtility.RectangleContainsScreenPoint(child, screenPos, uiCam))
                { hit = true; break; }
            }
            _rectBuffer.Clear();   // 不保留參照
            return hit;
        }

        public static bool ContainsScreenPoint(this RectTransform[] rts, Vector2 screenPos, Camera uiCam, bool includeChildren = true)
        {
            if (rts == null) return false;
            for (int i = 0; i < rts.Length; i++)
                if (rts[i].ContainsScreenPoint(screenPos, uiCam, includeChildren)) return true;
            return false;
        }
        #endregion
    }
}
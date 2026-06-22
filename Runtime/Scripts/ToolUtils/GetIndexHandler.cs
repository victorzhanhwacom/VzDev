using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.ToolUtils
{
    /// <summary>
    /// 取得物件在父物件中的索引位置，並透過事件傳遞結果。
    /// </summary>
    public class GetIndexHandler : MonoBehaviour
    {
        #region Fields
        [Foldout("[Events]"), SerializeField] private UnityEvent<string> OnGetSiblingIndex;
        public event Action<string> OnGetSiblingIndexEvent;

        [Foldout("[Settings]"), SerializeField] private bool isOrderByDesc = true;
        [Foldout("[Settings]"), SerializeField] private int startIndexFromValue = 1;

        // 小數字快取，避免重複 alloc（0~255 範圍內幾乎零 GC）
        private static readonly string[] _indexCache = BuildCache(256);
        #endregion

        [Button, ContextMenu("Get Sibling Index")]
        public void GetSiblingIndex()
        {
            if (transform.parent == null)
            {
                Debug.LogWarning("GetSiblingIndex: No parent found. Returning 0.", this);
                return;
            }

            int raw = transform.GetSiblingIndex();
            int result = isOrderByDesc
                ? transform.parent.childCount - raw - 1 + startIndexFromValue
                : raw + startIndexFromValue;

            string label = (uint)result < (uint)_indexCache.Length
                ? _indexCache[result]
                : result.ToString();

            OnGetSiblingIndex?.Invoke(label);   // Inspector 訂閱
            OnGetSiblingIndexEvent?.Invoke(label); // Code 訂閱
        }

        private static string[] BuildCache(int size)
        {
            var cache = new string[size];
            for (int i = 0; i < size; i++)
                cache[i] = i.ToString();
            return cache;
        }
    }
}

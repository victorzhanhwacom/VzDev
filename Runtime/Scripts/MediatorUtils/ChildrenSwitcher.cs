using UnityEngine;
using NaughtyAttributes;
using System;
using UnityEngine.Events;

namespace VzDev.MediatorUtils
{
    /// <summary>
    /// 取得所有子物件，並依照傳入的值切換子物件的顯示狀態
    /// </summary>
    public class ChildrenSwitcher : MonoBehaviour
    {
        #region Variables
        [SerializeField, OnValueChanged("OnReceiveValueChanged"), Range(0, 6)] private int receiveValue = -1;
        private void OnReceiveValueChanged() => SetValue(receiveValue);
        [SerializeField, ReadOnly] private GameObject[] children;
        [SerializeField] private GameObject[] excludeChildren;
        [Foldout("[Events]")] public UnityEvent<int> onSelectedIndex;
        private bool IsHaveChildren => children != null && children.Length > 0;
        #endregion

        #region 設定值 SetValue
        public void SetValue(Boolean value) => SetValue(value ? 1 : 0);
        public void SetValue(Single value) => SetValue((int)value);
        public void SetValue(int value)
        {
            if (receiveValue < 0 || receiveValue >= children.Length)
            {
                Debug.LogWarning($"[ChildrenSwitcher] receiveValue {receiveValue} is out of range. Children length: {children.Length}");
                return;
            }
            for (int i = 0; i < children.Length; i++)
            {
                bool isActive = i == receiveValue;
                children[i].SetActive(isActive);
            }
            onSelectedIndex?.Invoke(receiveValue);
        }
        #endregion

        #region 顯示 / 隱藏所有子物件
        [Button, ShowIf(nameof(IsHaveChildren))]
        public void SetAllChildrenActive() => SetChildrenStatus(true);
        [Button, ShowIf(nameof(IsHaveChildren))]
        public void SetAllChildrenDeactive() => SetChildrenStatus(false);
        public void SetChildrenStatus(bool isActive)
        {
            for (int i = 0; i < children.Length; i++)
            {
                children[i].SetActive(isActive);
            }
        }
        #endregion

        [Button, ContextMenu("Get Children")]
        private void GetChildren()
        {
            children = new GameObject[transform.childCount - (excludeChildren != null ? excludeChildren.Length : 0)];
            int indexCounter = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                if (excludeChildren != null && System.Array.Exists(excludeChildren, x => x == child))
                {
                    continue;
                }
                children[indexCounter++] = transform.GetChild(i).gameObject;
            }
        }
    }
}

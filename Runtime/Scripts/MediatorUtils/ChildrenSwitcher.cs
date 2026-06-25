using UnityEngine;
using NaughtyAttributes;

namespace VzDev.Mediator
{
    /// <summary>
    /// 取得所有子物件，並依照傳入的值切換子物件的顯示狀態
    /// </summary>
    public class ChildrenSwitcher : MonoBehaviour
    {
        #region Variables
        [SerializeField, ReadOnly] private int receiveValue;
        [SerializeField, ReadOnly] private GameObject[] children;
        #endregion

        public void SetValue(int value)
        {
            if (receiveValue == value) return;
            receiveValue = value;
            for (int i = 0; i < children.Length; i++)
            {
                bool isActive = i == receiveValue;
                children[i].SetActive(isActive);
            }
        }

        [Button, ContextMenu("Get Children")]
        private void GetChildren()
        {
            children = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = transform.GetChild(i).gameObject;
            }
        }
    }
}

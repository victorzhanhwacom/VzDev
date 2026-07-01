using UnityEngine;
using TMPro;

namespace VzDev.MediatorUtils
{
    public class TMP_DropdownMediator : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        /// <summary>
        /// 給 UnityEvent (Inspector) 綁定用：
        /// 設置 Dropdown 的值，並保證觸發 onValueChanged。
        /// </summary>
        public void SetValueAndNotify(int value)
        {
            if (dropdown == null) return;

            int clamped = Mathf.Clamp(value, 0, dropdown.options.Count - 1);

            if (dropdown.value == clamped)
            {
                Debug.LogWarning($"Dropdown value is already {clamped}. Forcing onValueChanged event.", this);
                dropdown.onValueChanged.Invoke(clamped);
            }
            else
            {
                dropdown.value = clamped;
            }
        }
        
        private void OnValidate()
        {
            if (dropdown == null)
            {
                dropdown = GetComponent<TMP_Dropdown>();
            }
        }
    }
}
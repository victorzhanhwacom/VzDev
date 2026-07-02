using UnityEngine;
using TMPro;

namespace VzDev.MediatorUtils
{
    public class TMP_DropdownMediator : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown target;

        public void SetValueAndNotify(int value)
        {
            if (target == null) return;

            int clamped = Mathf.Clamp(value, 0, target.options.Count - 1);

            if (target.value == clamped)
            {
                Debug.LogWarning($"Dropdown value is already {clamped}. Forcing onValueChanged event.", this);
                target.onValueChanged.Invoke(clamped);
            }
            else
            {
                target.value = clamped;
            }
        }
        
        private void OnValidate()
        {
            if (target == null)
            {
                target = GetComponent<TMP_Dropdown>();
            }
        }
    }
}
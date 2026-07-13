using UnityEngine;
using UnityEngine.UI;

namespace VzDev.MediatorUtils
{
    public class ToggleMediator : MonoBehaviour
    {
        [SerializeField] private Toggle target;

        public void SetToggleAndNotify(bool value)
        {
            if (target == null) return;

            target.isOn = value;
            target.onValueChanged.Invoke(value);
        }
        
        private void OnValidate()
        {
            if (target == null)
            {
                target = GetComponent<Toggle>();
            }
        }
    }
}
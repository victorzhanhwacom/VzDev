using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class EquipmentCOBieInfo : MonoBehaviour
    {
        [SerializeField, Label("Input Fields")] private TMP_InputField[] inputFields;
        private void OnEnable()
        {
            foreach (var inputField in inputFields)
            {
            }
        }
    }


}

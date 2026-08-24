using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils
{
    public class DcimMediator : MonoBehaviour
    {
        [Foldout("[Receive]"), SerializeField] private Transform clickedModel;
        [Foldout("[Events]")] public UnityEvent<string> invokeClickModelDeviceCode;
        public void SetClickedModel(Transform model)
        {
            if (model == null)
            {
                Debug.LogWarning("SetClickedModel: model is null");
                return;
            }
            clickedModel = model;
            invokeClickModelDeviceCode?.Invoke(clickedModel.name.GetStringBetweenMarks("[", "]"));
        }
    }
}

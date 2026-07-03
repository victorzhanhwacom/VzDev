using UnityEngine;
using UnityEngine.Events;
using VzDev.UnityAPI.Extensions;

namespace VzDev.ToolUtils
{
    public class PointTagClickMediator : MonoBehaviour
    {
        public UnityEvent<string> onClickModelDeviceId;
        public void SetClickedModel(Transform model)
        {
            if(model == null) return;
            string deviceId = model.name.GetStringBetweenMark("[", "]");
            if(string.IsNullOrEmpty(deviceId))
            {
                Debug.LogWarning($"[PointTagClickMediator] Model name '{model.name}' does not contain a valid device ID.");
                return;
            }
            Debug.Log($"[PointTagClickMediator] Model '{model.name}' clicked. Extracted Device ID: {deviceId}");
            onClickModelDeviceId?.Invoke(deviceId);
        }
    }
}

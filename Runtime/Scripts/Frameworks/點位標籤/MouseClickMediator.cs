using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VzDev.UnityAPI.Extensions;

namespace VzDev.ToolUtils
{
    public class MouseClickMediator : MonoBehaviour
    {
        #region Fields
        [Foldout("[Events]")] public UnityEvent<string> onClickModelDeviceId;
        [Foldout("[Events]")] public UnityEvent<Transform> onClickPointTagTarget;

        #endregion

        /// <summary>
        /// 當Toggle被點擊時，透過事件傳遞該Toggle對應的PointTag的FollowerTarget。
        /// </summary>
        /// <param name="toggle"></param>
        public void SetClickedPointTag(Toggle toggle)
        {
            if (toggle == null)
            {
                Debug.LogWarning("[PointTagClickMediator] Toggle is null.");
                return;
            }
            if (toggle.TryGetComponent<PointTag>(out var pointTag))
            {
                Transform model = pointTag.FollowerTarget;
                SetClickedModel(model);
            }
            else
            {
                Debug.LogWarning($"[PointTagClickMediator] Toggle '{toggle.name}' does not have a PointTag component.");
            }
        }


        /// <summary>
        /// 當模型被點擊時，透過事件傳遞該模型的Device ID。
        /// </summary>
        public void SetClickedModel(Transform model)
        {
            if (model == null) return;
            onClickPointTagTarget?.Invoke(model);

            string deviceId = model.name.GetStringBetweenMarks("[", "]");
            if (string.IsNullOrEmpty(deviceId))
            {
                Debug.LogWarning($"[PointTagClickMediator] Model name '{model.name}' does not contain a valid device ID.");
            }
            else
            {
                Debug.Log($"Device ID: {deviceId}");
                onClickModelDeviceId?.Invoke(deviceId);
            }
        }
    }
}

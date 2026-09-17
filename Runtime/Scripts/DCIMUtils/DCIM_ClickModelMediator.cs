using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils
{
    public class DCIM_ClickModelMediator : MonoBehaviour
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

            //避免JS端呼叫運鏡指定模型時，誤傳出指定模型的deviceCode給JS端            
            if(ColliderInteractionSystem.lastClickModelTrigger == ColliderInteractionSystem.ClickModelTrigger.bySimulateClick)
            {
                invokeClickModelDeviceCode?.Invoke(clickedModel.name.GetStringBetweenMarks("[", "]"));
            }
        }
    }
}

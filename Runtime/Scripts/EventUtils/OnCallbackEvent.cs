using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.EventUtils
{
    /// <summary>
    /// 處理 正在呼叫 / 呼叫失敗 的UnityEvent事件
    /// </summary>
    [Serializable]
    public class OnCallbackEvent
    {
        [Foldout("[Events]-Calling"), SerializeField] protected UnityEvent<bool> onCallingBoolEvent;
        [Foldout("[Events]-Calling"), SerializeField] protected UnityEvent onCallingStartEvent, onCallingEndEvent;
        [Foldout("[Events]"), SerializeField] protected UnityEvent onSuccessEvent;
        [Foldout("[Events]"), SerializeField] protected UnityEvent<string> onErrorEvent;

        public void InvokeOnCallingEvent(bool isCalling)
        {
            onCallingBoolEvent?.Invoke(isCalling);
            (isCalling ? onCallingStartEvent : onCallingEndEvent)?.Invoke();
        }

        public void InvokeOnSuccessEvent() => onSuccessEvent?.Invoke();
        public void InvokeOnErrorEvent(string error) => onErrorEvent?.Invoke(error);
    }
}

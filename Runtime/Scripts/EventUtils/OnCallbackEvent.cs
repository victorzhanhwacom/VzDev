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
        [Foldout("[Events]-Calling"), SerializeField] protected UnityEvent<bool> callingStatusEvent;
        [Foldout("[Events]-Calling"), SerializeField] protected UnityEvent onStartEvent, onFinishedEvent;
        [Foldout("[Events]"), SerializeField] protected UnityEvent onSuccessEvent;
        [Foldout("[Events]"), SerializeField] protected UnityEvent<string> onErrorEvent;
        [Foldout("[Events]"), SerializeField] protected UnityEvent onStopEvent;

        public void InvokeCallingStatusEvent(bool isStartCall)
        {
            callingStatusEvent?.Invoke(isStartCall);
            (isStartCall ? onStartEvent : onFinishedEvent)?.Invoke();
        }

        public void InvokeOnSuccessEvent()
        {
            InvokeCallingStatusEvent(false);
            onSuccessEvent?.Invoke();
        }
        public void InvokeOnErrorEvent(string error)
        {
            InvokeCallingStatusEvent(false);
            onErrorEvent?.Invoke(error);
        }
        public void InvokeStopCallEvent()
        {
            InvokeCallingStatusEvent(false);
            onStopEvent?.Invoke();
        }
    }
}

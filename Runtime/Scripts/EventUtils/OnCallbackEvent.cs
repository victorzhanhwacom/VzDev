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
        #region UnityEvent
        [Foldout("[Events]")] public UnityEvent onStartEvent;
        [Foldout("[Events]")] public UnityEvent onStopEvent;
        [Foldout("[Events]")] public UnityEvent onSuccessEvent;
        [Foldout("[Events]")] public UnityEvent<string> onFailureEvent;
        [Foldout("[Events]")] public UnityEvent<bool> callingStatusEvent;
        #endregion

        public void InvokeStartEvent()
        {
            InvokeCallingStatusEvent(true);
            onStartEvent?.Invoke();
        }
        public void InvokeStopCallEvent()
        {
            InvokeCallingStatusEvent(false);
            onStopEvent?.Invoke();
        }

        public void InvokeOnSuccessEvent()
        {
            InvokeCallingStatusEvent(false);
            onSuccessEvent?.Invoke();
        }
        public void InvokeOnFaliureEvent(string error)
        {
            InvokeCallingStatusEvent(false);
            onFailureEvent?.Invoke(error);
        }

        public void InvokeCallingStatusEvent(bool isStartCall) => callingStatusEvent?.Invoke(isStartCall);
    }
}

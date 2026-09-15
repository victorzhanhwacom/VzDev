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
        [Foldout("[Events]"), SerializeField] protected UnityEvent<bool> onCallingEvent;
        [Foldout("[Events]"), SerializeField] protected UnityEvent<string> onErrorEvent;

        public void InvokeOnCallingEvent(bool isCalling) => onCallingEvent?.Invoke(isCalling);
        public void InvokeOnErrorEvent(string error) => onErrorEvent?.Invoke(error);
    }
}

using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebUtils
{
    public class WebGLBridgeMediator : MonoBehaviour
    {
        public enum DataType { String, Int, Float, Bool }

        #region Fields
        [InfoBox("負責設置{JS端函式名稱}")]
        [SerializeField, ReadOnly, ShowIf(nameof(IsString))] private string receivedString;
        [SerializeField, ReadOnly, ShowIf(nameof(IsInt))] private int receivedInt;
        [SerializeField, ReadOnly, ShowIf(nameof(IsFloat))] private float receivedFloat;
        [SerializeField, ReadOnly, ShowIf(nameof(IsBool))] private bool receivedBool;

        [Foldout("[Events]"), ShowIf(nameof(IsString))] public UnityEvent<string, string> sendMessageString;
        [Foldout("[Events]"), ShowIf(nameof(IsInt))] public UnityEvent<string, int> sendMessageInt;
        [Foldout("[Events]"), ShowIf(nameof(IsFloat))] public UnityEvent<string, float> sendMessageFloat;
        [Foldout("[Events]"), ShowIf(nameof(IsBool))] public UnityEvent<string, bool> sendMessageBool;

        [Foldout("[Settings]"), SerializeField] private DataType sendDataType = DataType.String;
        [Foldout("[Settings]"), SerializeField] private string functionName;
        private bool IsString => sendDataType == DataType.String;
        private bool IsInt => sendDataType == DataType.Int;
        private bool IsFloat => sendDataType == DataType.Float;
        private bool IsBool => sendDataType == DataType.Bool;
        #endregion

        public void SetFunctionName(string name) => functionName = name;

        // 通用比對邏輯，可傳入自訂的比較函式（預設用 EqualityComparer）
        private void HandleValue<T>(DataType expectedType, T newValue, ref T storedValue,
            UnityEvent<string, T> evt, Func<T, T, bool> equalsFunc = null)
        {
            if (sendDataType != expectedType)
            {
                Debug.LogError($"Data type mismatch: current mode is {sendDataType}, but received {expectedType}.");
                return;
            }

            bool isEqual = equalsFunc != null
                ? equalsFunc(storedValue, newValue)
                : EqualityComparer<T>.Default.Equals(storedValue, newValue);

            if (isEqual) return;

            storedValue = newValue;
            evt?.Invoke(functionName, storedValue);
        }

        public void SendValue(int value) => HandleValue(DataType.Int, value, ref receivedInt, sendMessageInt);

        public void SendValue(float value) =>
            HandleValue(DataType.Float, value, ref receivedFloat, sendMessageFloat, Mathf.Approximately);

        public void SendValue(bool value) => HandleValue(DataType.Bool, value, ref receivedBool, sendMessageBool);

        public void SendValue(string value)
        {
            switch (sendDataType)
            {
                case DataType.String:
                    HandleValue(DataType.String, value, ref receivedString, sendMessageString);
                    break;
                case DataType.Int:
                    if (int.TryParse(value, out var intValue))
                        SendValue(intValue);
                    else
                        Debug.LogError($"Failed to parse '{value}' to int.");
                    break;
                case DataType.Float:
                    if (float.TryParse(value, out var floatValue))
                        SendValue(floatValue);
                    else
                        Debug.LogError($"Failed to parse '{value}' to float.");
                    break;
                case DataType.Bool:
                    if (bool.TryParse(value, out var boolValue))
                        SendValue(boolValue);
                    else
                        Debug.LogError($"Failed to parse '{value}' to bool.");
                    break;
                default:
                    Debug.LogError("Unsupported data type.");
                    break;
            }
        }
    }
}
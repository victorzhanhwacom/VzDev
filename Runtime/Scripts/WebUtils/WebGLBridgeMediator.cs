using System.Runtime.InteropServices;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebUtils
{
    /// <summary>
    /// </summary>
    public class WebGLBridgeMediator : MonoBehaviour
    {
        public enum DataType{
            String,
            Int,
            Float,
            Bool
        }

        #region Fields
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

        public void SetValue(int value)
        {
            if (sendDataType == DataType.Int && receivedInt != value)
            {
                receivedInt = value;
                sendMessageInt?.Invoke(functionName, receivedInt);
            }
            else
                Debug.LogError("Data type mismatch: Expected Int.");
        }
        public void SetValue(float value)
        {
            if (sendDataType == DataType.Float && receivedFloat != value)
            {
                receivedFloat = value;
                sendMessageFloat?.Invoke(functionName, receivedFloat);
            }
            else
                Debug.LogError("Data type mismatch: Expected Float.");
        }
        public void SetValue(bool value)
        {
            if (sendDataType == DataType.Bool && receivedBool != value)
            {
                receivedBool = value;
                sendMessageBool?.Invoke(functionName, receivedBool);
            }
            else
                Debug.LogError("Data type mismatch: Expected Bool.");
        }

        public void SetValue(string value)
        {
            switch (sendDataType)
            {
                case DataType.String:
                    if(receivedString == value) return;
                    receivedString = value;
                    sendMessageString?.Invoke(functionName, receivedString);
                    break;
                case DataType.Int:
                    if (int.TryParse(value, out var intValue))
                    {
                        if(receivedInt == intValue) return;
                        receivedInt = intValue;
                        sendMessageInt?.Invoke(functionName, receivedInt);
                    }
                    else
                        Debug.LogError($"Failed to parse '{value}' to int.");
                    break;
                case DataType.Float:
                    if (float.TryParse(value, out var floatValue))
                    {
                        if(receivedFloat == floatValue) return;
                        receivedFloat = floatValue;
                        sendMessageFloat?.Invoke(functionName, receivedFloat);
                    }
                    else
                        Debug.LogError($"Failed to parse '{value}' to float.");
                    break;
                case DataType.Bool:
                    if (bool.TryParse(value, out var boolValue))
                    {
                        if(receivedBool == boolValue) return;
                        receivedBool = boolValue;
                        sendMessageBool?.Invoke(functionName, receivedBool);
                    }
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
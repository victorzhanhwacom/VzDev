using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev
{
    /// <summary>
    /// 參數發送器：用於在Unity中發送不同類型的參數值，並觸發對應的事件。
    /// </summary>
    public class ParamSender : MonoBehaviour
    {
#if UNITY_EDITOR
        #region Fields
        [SerializeField] private EnumParamType paramType = EnumParamType.String;
        [SerializeField, ShowIf("IsString"), TextArea(0, 10)] private string stringValue;
        [SerializeField, ShowIf("IsInt")] private int intValue;
        [SerializeField, ShowIf("IsFloat")] private float floatValue;
        [SerializeField, ShowIf("IsBool")] private bool boolValue;
        [SerializeField, ShowIf("IsVector2")] private Vector2 vector2Value;
        [SerializeField, ShowIf("IsVector3")] private Vector3 vector3Value;

        private bool IsHaveEventListener
        {
            get
            {
                switch (paramType)
                {
                    case EnumParamType.String:
                        return sendStringEvent != null && sendStringEvent.GetPersistentEventCount() > 0;
                    case EnumParamType.Int:
                        return sendIntEvent != null && sendIntEvent.GetPersistentEventCount() > 0;
                    case EnumParamType.Float:
                        return sendFloatEvent != null && sendFloatEvent.GetPersistentEventCount() > 0;
                    case EnumParamType.Bool:
                        return sendBoolEvent != null && sendBoolEvent.GetPersistentEventCount() > 0;
                    case EnumParamType.Vector2:
                        return sendVector2Event != null && sendVector2Event.GetPersistentEventCount() > 0;
                    case EnumParamType.Vector3:
                        return sendVector3Event != null && sendVector3Event.GetPersistentEventCount() > 0;
                    default:
                        return false;
                }
            }
        }

        #endregion

        #region Events
        [ShowIf("IsString")] public UnityEvent<string> sendStringEvent;
        [ShowIf("IsInt")] public UnityEvent<int> sendIntEvent;
        [ShowIf("IsFloat")] public UnityEvent<float> sendFloatEvent;
        [ShowIf("IsBool")] public UnityEvent<bool> sendBoolEvent;
        [ShowIf("IsVector2")] public UnityEvent<Vector2> sendVector2Event;
        [ShowIf("IsVector3")] public UnityEvent<Vector3> sendVector3Event;
        #endregion

        #region For NaughtyAttributes
        private bool IsString => paramType == EnumParamType.String;
        private bool IsInt => paramType == EnumParamType.Int;
        private bool IsFloat => paramType == EnumParamType.Float;
        private bool IsBool => paramType == EnumParamType.Bool;
        private bool IsVector2 => paramType == EnumParamType.Vector2;
        private bool IsVector3 => paramType == EnumParamType.Vector3;
        #endregion

        [Button, ShowIf("IsHaveEventListener")]
        private void SendParam()
        {
            switch (paramType)
            {
                case EnumParamType.String:
                    sendStringEvent?.Invoke(stringValue);
                    break;
                case EnumParamType.Int:
                    sendIntEvent?.Invoke(intValue);
                    break;
                case EnumParamType.Float:
                    sendFloatEvent?.Invoke(floatValue);
                    break;
                case EnumParamType.Bool:
                    sendBoolEvent?.Invoke(boolValue);
                    break;
                case EnumParamType.Vector2:
                    sendVector2Event?.Invoke(vector2Value);
                    break;
                case EnumParamType.Vector3:
                    sendVector3Event?.Invoke(vector3Value);
                    break;
            }
        }
        private enum EnumParamType
        {
            String,
            Int,
            Float,
            Bool,
            Vector2,
            Vector3,
        }
#endif
    }
}

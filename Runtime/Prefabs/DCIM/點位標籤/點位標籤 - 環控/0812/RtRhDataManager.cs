using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DataUtils;

namespace VzDev.DCIMUtils.EnviornmentUtils
{
    public class RtRhDataManager : MonoBehaviour
    {
        [SerializeField, OnValueChanged("OnCurrentRtRhModeChanged")] private EnumRtRhMode currentRtRhMode = EnumRtRhMode.Unselect;
        [SerializeField, ReadOnly] private List<SensorData_RTRH> pointModelDataList = new List<SensorData_RTRH>();
        [Foldout("[Events]")] public UnityEvent<bool> onRtModeEvent, onRhModeEvent;

        private void OnCurrentRtRhModeChanged()
        {
            if (currentRtRhMode == EnumRtRhMode.Unselect)
            {
                Debug.LogWarning("Current RtRhMode is Unselect. No action taken.", this);
                return;
            }
            ToRtMode(currentRtRhMode == EnumRtRhMode.Rt);
            ToRhMode(currentRtRhMode == EnumRtRhMode.Rh);
        }

        public void ToRtMode(bool isOn)
        {
            onRtModeEvent?.Invoke(isOn);
            if (isOn == false) return;
            currentRtRhMode = EnumRtRhMode.Rt;
            onRtRhModeChangedAction?.Invoke(currentRtRhMode);
        }

        public void ToRhMode(bool isOn)
        {
            onRhModeEvent?.Invoke(isOn);
            if (isOn == false) return;
            currentRtRhMode = EnumRtRhMode.Rh;
            onRtRhModeChangedAction?.Invoke(currentRtRhMode);
        }

        #region Parse JSON Data
        public void ParseJsonData(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("JSON data is null or empty. Cannot parse PointModelData_RTRH list.");
                return;
            }
            try
            {
                OnGetPointModelDataListAction?.Invoke(pointModelDataList);
                isParseDataSuccessAction?.Invoke(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse JSON data: {ex.Message}");
                isParseDataSuccessAction?.Invoke(false);
            }
        }
        #endregion

        #region Static Actions
        public static Action<List<SensorData_RTRH>> OnGetPointModelDataListAction;
        public static Action<bool> isParseDataSuccessAction;
        public static Action<EnumRtRhMode> onRtRhModeChangedAction;
        #endregion

#if UNITY_EDITOR
        [SerializeField, OnValueChanged("OnUnityEventEditorModeChanged")] private bool unityEventEditorMode = false;
        private void OnUnityEventEditorModeChanged()
        {
            UnityEventModeSetter.SetTriggerMode(this, "onRtModeEvent", unityEventEditorMode ? UnityEventCallState.EditorAndRuntime : UnityEventCallState.RuntimeOnly);
            UnityEventModeSetter.SetTriggerMode(this, "onRhModeEvent", unityEventEditorMode ? UnityEventCallState.EditorAndRuntime : UnityEventCallState.RuntimeOnly);
        }
#endif

    }

    public enum EnumRtRhMode
    {
        Unselect,
        Rt,
        Rh,
    }
}

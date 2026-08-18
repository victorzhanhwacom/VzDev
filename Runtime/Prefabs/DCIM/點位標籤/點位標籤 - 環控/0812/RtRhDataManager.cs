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
        [SerializeField, OnValueChanged("OnCurrentModeChanged")] private EnumRtRhMode currentMode = EnumRtRhMode.Unselect;
        [SerializeField, ReadOnly] private List<SensorData_RTRH> pointModelDataList = new List<SensorData_RTRH>();
        [Foldout("[Events]-THS")] public UnityEvent<bool> onRtModeEvent, onRhModeEvent;
        [Foldout("[Events]-WLK")] public UnityEvent<bool> onWaterLeakModeEvent;

        private void OnCurrentModeChanged()
        {
            if (currentMode == EnumRtRhMode.Unselect)
            {
                Debug.LogWarning("Current RtRhMode is Unselect. No action taken.", this);
                return;
            }
            ToRtMode(currentMode == EnumRtRhMode.Rt);
            ToRhMode(currentMode == EnumRtRhMode.Rh);
            ToWaterLeakMode(currentMode == EnumRtRhMode.WaterLeak);
        }

        public void ToWaterLeakMode(bool isOn)
        {
            onWaterLeakModeEvent?.Invoke(isOn);
            if (isOn == false) return;
            currentMode = EnumRtRhMode.WaterLeak;
        }

        public void ToRtMode(bool isOn)
        {
            onRtModeEvent?.Invoke(isOn);
            if (isOn == false) return;
            currentMode = EnumRtRhMode.Rt;
            onRtRhModeChangedAction?.Invoke(currentMode);
        }

        public void ToRhMode(bool isOn)
        {
            onRhModeEvent?.Invoke(isOn);
            if (isOn == false) return;
            currentMode = EnumRtRhMode.Rh;
            onRtRhModeChangedAction?.Invoke(currentMode);
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
        WaterLeak
    }
}

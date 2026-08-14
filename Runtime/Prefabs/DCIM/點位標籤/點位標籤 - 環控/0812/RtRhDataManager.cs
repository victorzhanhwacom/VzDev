using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.DCIMUtils.EnviornmentUtils
{
    public class RtRhDataManager : MonoBehaviour
    {
        [SerializeField, OnValueChanged("OnCurrentRtRhModeChanged")] private EnumRtRhMode currentRtRhMode = EnumRtRhMode.Unselect;
        [SerializeField, ReadOnly] private List<SensorData_RTRH> pointModelDataList = new List<SensorData_RTRH>();

        private void OnCurrentRtRhModeChanged()
        {
            switch (currentRtRhMode)
            {
                case EnumRtRhMode.Rt:
                    ToRtMode();
                    break;
                case EnumRtRhMode.Rh:
                    ToRhMode();
                    break;
                default:
                    Debug.LogWarning($"Invalid RtRhMode: {currentRtRhMode}. No action taken.", this);
                    break;
            }
        }

        public void ToRtMode()
        {
            currentRtRhMode = EnumRtRhMode.Rt;
            onRtRhModeChanged?.Invoke(currentRtRhMode);
        }

        public void ToRhMode()
        {
            currentRtRhMode = EnumRtRhMode.Rh;
            onRtRhModeChanged?.Invoke(currentRtRhMode);
        }

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

        #region Fields
        public static Action<List<SensorData_RTRH>> OnGetPointModelDataListAction;
        public static Action<bool> isParseDataSuccessAction;
        public static Action<EnumRtRhMode> onRtRhModeChanged;
        #endregion
    }

    public enum EnumRtRhMode
    {
        Unselect,
        Rt,
        Rh,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DataUtils;
using VzDev.DCIMUtils.Extensions;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.EnviornmentUtils
{
    /// <summary>
    /// 溫濕度數據綁定器：模型
    /// </summary>
    public class DataModelBinder_RTRH : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private EnumRtRhMode rtRhMode = EnumRtRhMode.Unselect;
        [SerializeField, ReadOnly] private SensorData_RTRH pointModelData = new();
        [Foldout("[Components]"), SerializeField] private HeatSource heatSource;

        public SensorData_RTRH PointModelData => pointModelData;
        #endregion

        /// <summary>
        /// 更新HeatSource的溫度值
        /// </summary>
        private void UpdateHeatSource()
        {
            if (transform.TryGetComponentAndLog(out heatSource) == false) return;
            float value;
            string unit;
            switch (rtRhMode)
            {
                case EnumRtRhMode.Rt:
                    value = pointModelData.rtValue;
                    unit = "℃";
                    break;
                case EnumRtRhMode.Rh:
                    value = pointModelData.rhValue;
                    unit = "%";
                    break;
                default:
                    //Debug.LogWarning($"DataModelBinder_RTRH: Unselect mode. Cannot update heat source.", this);
                    return;
            }
            heatSource.SetTemperature(value);
            OnValueChangeAction?.Invoke($"{value}{unit}");
            OnRtRhModeChangedAction?.Invoke(rtRhMode);
            OnPointModelDataChangedAction?.Invoke(pointModelData);
        }


        /// <summary>
        /// 當Manager取得資料時進行deviceCode比對，並更新HeatSource
        /// </summary>
        private void OnGetPointModelDataList(List<SensorData_RTRH> list)
        {
            string deviceCode = transform.GetModelDeviceCode();
            pointModelData = list.FirstOrDefault(data => data.deviceCode == deviceCode);
            if (pointModelData == null)
            {
                Debug.LogWarning($"DataModelBinder_RTRH: No matching data found for deviceCode in the list.", this);
                return;
            }
            UpdateHeatSource();
        }

        /// <summary>
        /// 改變溫濕度模式時
        /// </summary>
        private void OnRtRhTypeChanged(EnumRtRhMode mode)
        {
            rtRhMode = mode;
            if (pointModelData == null)
            {
                Debug.LogWarning($"DataModelBinder_RTRH: pointModelData is null. Cannot update heat source.", this);
                return;
            }
            UpdateHeatSource();
        }

        #region ForDemo
        public void SetRtValue(float value)
        {
            pointModelData.rtValue = value;
            UpdateHeatSource();
        }
        public void SetRhValue(int value)
        {
            pointModelData.rhValue = value;
            UpdateHeatSource();
        }
        #endregion

        #region Event Listeners
        private void OnEnable()
        {
            RtRhDataManager.OnGetPointModelDataListAction += OnGetPointModelDataList;
            RtRhDataManager.onRtRhModeChangedAction += OnRtRhTypeChanged;
        }

        private void OnDisable()
        {
            RtRhDataManager.OnGetPointModelDataListAction -= OnGetPointModelDataList;
            RtRhDataManager.onRtRhModeChangedAction -= OnRtRhTypeChanged;
        }

        public Action<string> OnValueChangeAction;
        public Action<EnumRtRhMode> OnRtRhModeChangedAction;
        public Action<SensorData_RTRH> OnPointModelDataChangedAction;
        #endregion
    }
}

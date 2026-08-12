using System;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DCIMUtils.DataUtils
{
    public class PointModelDataHandler_RTRH: MonoBehaviour
    {
        #region Fields
        [SerializeField, OnValueChanged("OnRtRhTypeChanged")] private EnumRtRhType rtRhType = EnumRtRhType.Unselect;
        private void OnRtRhTypeChanged() => UpdateHeatSource();
        [SerializeField, ReadOnly] private PointModelData_RTRH pointModelData = new();
        [Foldout("[Components]"), SerializeField] private HeatSource heatSource;
        #endregion

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

        private void UpdateHeatSource()
        {
            if (heatSource == null) return;
            float value = rtRhType switch
            {
                EnumRtRhType.Rt => pointModelData.rtValue,
                EnumRtRhType.Rh => pointModelData.rhValue,
                _ => throw new ArgumentOutOfRangeException()
            };
            heatSource.SetTemperature(value);
        }

        public void SetRtRhType(EnumRtRhType type)
        {
            rtRhType = type;
            UpdateHeatSource();
        }
    }

    public enum EnumRtRhType
    {
        Unselect,
        Rt,
        Rh,
    }
}

using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DataUtils;
using VzDev.ObjectUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.EnviornmentUtils
{
    /// <summary>
    /// 溫濕度數據綁定器：標籤
    /// <para> 透過DataModelBinder_RTRH取得溫濕度數據與溫濕度模式 </para>
    /// </summary>
    public class DataTagBinder_RTRH : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private EnumRtRhMode rtRhMode = EnumRtRhMode.Unselect;
        [SerializeField, ReadOnly] private SensorData_RTRH pointModelData;

        [Foldout("[Events]-Value")] public UnityEvent<string> onValueChangedEvent;
        [Foldout("[Events]")] public UnityEvent<bool> OnRtModeEvent, onRhModeEvent;
        [Foldout("[Components]"), SerializeField] private DataModelBinder_RTRH dataModelBinder_RTRH;
        #endregion

        /// <summary>
        /// 值改變時
        /// </summary>
        private void OnValueChanged(string value) => onValueChangedEvent?.Invoke(value);

        /// <summary>
        /// 改變溫濕度模式時
        /// </summary>
        private void OnRtRhModeChanged(EnumRtRhMode mode)
        {
            rtRhMode = mode;
            OnRtModeEvent?.Invoke(rtRhMode == EnumRtRhMode.Rt);
            onRhModeEvent?.Invoke(rtRhMode == EnumRtRhMode.Rh);
        }
        private void OnPointModelDataChanged(SensorData_RTRH data) => pointModelData = data;


        #region Event Listeners
        private bool IsCompExistCheck()
        {
            if (dataModelBinder_RTRH == null)
            {
                if (transform.TryGetComponentAndLog(out UIAnchorFollower uiAnchorFollower) == false) return false;
                if (uiAnchorFollower.Target3DObject.TryGetComponentInChildren(out dataModelBinder_RTRH) == false) return false;
            }
            return true;
        }
        private void OnEnable()
        {
            if (IsCompExistCheck() == false) return;
            dataModelBinder_RTRH.OnValueChangeAction += OnValueChanged;
            dataModelBinder_RTRH.OnRtRhModeChangedAction += OnRtRhModeChanged;
            dataModelBinder_RTRH.OnPointModelDataChangedAction += OnPointModelDataChanged;
        }
        private void OnDisable()
        {
            if (IsCompExistCheck() == false) return;
            dataModelBinder_RTRH.OnValueChangeAction -= OnValueChanged;
            dataModelBinder_RTRH.OnRtRhModeChangedAction -= OnRtRhModeChanged;
            dataModelBinder_RTRH.OnPointModelDataChangedAction -= OnPointModelDataChanged;
        }
        #endregion
    }
}

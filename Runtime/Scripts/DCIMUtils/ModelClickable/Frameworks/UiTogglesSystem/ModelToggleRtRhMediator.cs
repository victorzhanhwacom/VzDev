using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.PointInfo;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.DCIMUtils
{
    public class ModelToggleRtRhMediator : MonoBehaviour
    {
        #region Fields
        [SerializeField, Tooltip("設定RT/RH模式"), OnValueChanged("OnRtRhModeChanged")] private EnumRtRhMode rtRhMode = EnumRtRhMode.RTMode;
        [Foldout("[Components]"), SerializeField] private ModelToggleBindingGenerator modelToggleBindingGenerator;
        private PointInfo_RTRH[] pointRtRh;
        private void OnRtRhModeChanged() => SetSwitchToRTMode(rtRhMode);
        #endregion

        /// <summary>
        /// 取得目前所有點位標籤
        /// </summary>
        public void GetPoinTags()
        {
            if (modelToggleBindingGenerator == null || modelToggleBindingGenerator.ModelToggles == null)
            {
                Debug.LogError("ModelToggleBindingGenerator is not assigned.", this);
                return;
            }

            pointRtRh = modelToggleBindingGenerator.ModelToggles
                .Select(tag => tag.GetComponent<PointInfo_RTRH>())
                .Where(comp => comp != null)
                .ToArray();
        }

        public void SwitchToRTMode() => SetSwitchToRTMode(EnumRtRhMode.RTMode);
        public void SwitchToRHMode() => SetSwitchToRTMode(EnumRtRhMode.RHMode);

        private void SetSwitchToRTMode(EnumRtRhMode mode)
        {
            if (Application.isPlaying == false) return;
            if (pointRtRh == null || pointRtRh.Length == 0) GetPoinTags();
            for (int i = 0; i < pointRtRh.Length; i++)
            {
                PointInfo_RTRH pointTagInfo = pointRtRh[i];
                pointTagInfo.SwitchMode(mode == EnumRtRhMode.RTMode);
            }
        }

        public enum EnumRtRhMode
        {
            RTMode,
            RHMode
        }
    }
}

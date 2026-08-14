using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.PointInfo;
using VzDev.ToolUtils;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.DCIMUtils
{
    public class PointTagRtRhMediator : MonoBehaviour
    {
        #region Fields
        [SerializeField, Tooltip("設定RT/RH模式"), OnValueChanged("OnRtRhModeChanged")] private EnumRtRhMode rtRhMode = EnumRtRhMode.RTMode;
        [Foldout("[Components]"), SerializeField] private PointTagGenerator pointTagGenerator;
        private PointInfo_RTRH[] pointTags;
        private void OnRtRhModeChanged() => SetSwitchToRTMode(rtRhMode);
        #endregion

        /// <summary>
        /// 取得目前所有點位標籤
        /// </summary>
        public void GetPoinTags()
        {
            if (pointTagGenerator == null || pointTagGenerator.PointTags == null)
            {
                Debug.LogError("PointTagGenerator is not assigned.", this);
                return;
            }

            pointTags = pointTagGenerator.PointTags
                .Select(tag => tag.GetComponent<PointInfo_RTRH>())
                .Where(comp => comp != null)
                .ToArray();
        }

        public void SwitchToRTMode() => SetSwitchToRTMode(EnumRtRhMode.RTMode);
        public void SwitchToRHMode() => SetSwitchToRTMode(EnumRtRhMode.RHMode);

        private void SetSwitchToRTMode(EnumRtRhMode mode)
        {
            if (Application.isPlaying == false) return;
            if (pointTags == null || pointTags.Length == 0) GetPoinTags();
            for (int i = 0; i < pointTags.Length; i++)
            {
                PointInfo_RTRH pointTagInfo = pointTags[i];
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

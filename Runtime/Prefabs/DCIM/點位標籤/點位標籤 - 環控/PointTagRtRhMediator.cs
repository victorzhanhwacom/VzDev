using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using VzDev.ToolUtils;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.DCIMUtils
{
    public class PointTagRtRhMediator : MonoBehaviour
    {
        [Foldout("[Components]"), SerializeField] private PointTagGenerator pointTagGenerator;
        private PointInfo_RTRH[] _pointInfosRtRh;

        private void GetInfoTags()
        {
            if (pointTagGenerator == null && pointTagGenerator.PointTags == null)
            {
                Debug.LogError("PointTagGenerator is not assigned.", this);
                return;
            }

            _pointInfosRtRh = pointTagGenerator.PointTags
                .Select(tag => tag.GetComponent<PointInfo_RTRH>())
                .Where(info => info != null)
                .ToArray();
        }

        public void SwitchToRTMode() => SetSwitchToRTMode(true);
        public void SwitchToRHMode() => SetSwitchToRTMode(false);

        private void SetSwitchToRTMode(bool isRTMode)
        {
            if(Application.isPlaying == false) return;

            if(_pointInfosRtRh == null || _pointInfosRtRh.Length == 0)
            {
                Debug.LogWarning("Getting PointInfo_RTRH components...", this);
                GetInfoTags();
            }
            foreach (var pointInfo in _pointInfosRtRh)
            {
                pointInfo.SwitchMode(isRTMode);
            }
        }
    }
}


using System.Linq;
using UnityEngine;
using VzDev.DCIMUtils;
using VzDev.StringUtils;

namespace VzDev.ToolUtils
{

    /// <summary>
    /// For 溫濕度管理
    /// </summary>
    public class RTRHLabelGetter : MonoBehaviour, IPointTagLabelGetter
    {
        public string GetLabel(Transform targetModel)
        {
            string deviceCode = StringHelper.GetStringFromInterval(targetModel.name, "[", "]");
            return deviceCode.Split("+").LastOrDefault().ToString();
        }
    }
}
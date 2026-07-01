
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
            string raw = StringHelper.GetStringFromInterval(targetModel.name, "(", ")");
            string index = (int.Parse(raw)+1).ToString("D2");
            return $"TH-{index}";
        }
    }
}
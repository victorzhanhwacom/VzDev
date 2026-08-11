
using System.Linq;
using UnityEngine;
using VzDev.StringUtils;

namespace VzDev.ToolUtils
{
    public class CoolerMachineLabelGetter : MonoBehaviour, IPointTagLabelGetter
    {
        public string GetLabel(Transform targetModel)
        {
            string deviceCode = StringHelper.GetStringFromInterval(targetModel.name, "[", "]");
            string raw = deviceCode.Split(":").LastOrDefault();
            return raw.Split("+").LastOrDefault();;
        }
    }
}

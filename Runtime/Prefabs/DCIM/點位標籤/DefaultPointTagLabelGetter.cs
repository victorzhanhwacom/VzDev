using System.Linq;
using UnityEngine;
using VzDev.StringUtils;
using VzDev.ToolUtils;
using Debug = VzDev.ToolUtils.Debug;

/// <summary>
/// 預設的點位標籤取得器
/// </summary>
public class DefaultPointTagLabelGetter : MonoBehaviour, IPointTagLabelGetter
{
    public string GetLabel(Transform targetModel)
    {
        string deviceCode = StringHelper.GetStringFromInterval(targetModel.name, "[", "]");
        string raw = string.IsNullOrEmpty(deviceCode) ? targetModel.name : deviceCode.Split(":").LastOrDefault();
        return raw;
    }
}
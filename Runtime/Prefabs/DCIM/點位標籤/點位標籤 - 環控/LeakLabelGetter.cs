using System.Linq;
using UnityEngine;
using VzDev.StringUtils;
using VzDev.ToolUtils;

/// <summary>
/// 漏水帶標籤獲取器 - 從模型名稱中提取漏水帶的索引作為標籤。
/// </summary>
public class LeakLabelGetter : MonoBehaviour, IPointTagLabelGetter
{
    public string GetLabel(Transform targetModel)
    {
        string deviceCode = StringHelper.GetStringFromInterval(targetModel.name, "[", "]");
        string raw = deviceCode.Split(":").LastOrDefault();
        return raw.Split("+").LastOrDefault();
    }
}
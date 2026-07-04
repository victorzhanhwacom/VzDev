using UnityEngine;
using VzDev.ToolUtils;
using VzDev.UnityAPI.Extensions;

/// <summary>
/// 漏水帶標籤獲取器 - 從模型名稱中提取漏水帶的索引作為標籤。
/// </summary>
public class LeakLabelGetter : MonoBehaviour, IPointTagLabelGetter
{
    public string GetLabel(Transform targetModel)
    {
        string index = targetModel.name.GetStringBetweenMark("(", ")");
        return index;
    }
}
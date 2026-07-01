using UnityEngine;
using VzDev.ToolUtils;

/// <summary>
/// For門禁管理
/// <para>門_AR-雙開門+參數門把_D06-雙開門-180_210_8[TG+TPE+IDC+15F++AR+AR-雙開門_參數門把: D06-雙開門-180*210+64]</para>
/// <para>AI機房-02_門禁管理_門_AR-單開門+參數門把_D03-單開門-90_210_2[TG+TPE+IDC+15F++AR+AR-單開門_參數門把: D03-單開門-90*210+60]</para>
/// </summary>
public class DoorLabelGetter : MonoBehaviour, IPointTagLabelGetter
{
    public string GetLabel(Transform targetModel)
    {
        return targetModel.name.Split("_")[0];
    }
}
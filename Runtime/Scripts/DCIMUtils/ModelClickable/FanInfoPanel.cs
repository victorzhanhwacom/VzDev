using TMPro;
using UnityEngine;
using VzDev;
using VzDev.DCIMUtils.ModelInteractUtils;

public class FanInfoPanel : AssetDataDisplayBase<Fan_Asset>
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text nameText, fanSpeedText;

    private void Start()
    {
        panelRoot.SetActive(false);
    }


    protected override void UpdateUIOnSelected()
    {
        nameText.text = data.assetInfo.assetName;
        fanSpeedText.text = $"Fan Speed: {data.fanSpeed} RPM";
        panelRoot.SetActive(true);
    }

    protected override void UpdateUIOnDeselected()
    {
        nameText.text = string.Empty;
        fanSpeedText.text = string.Empty;
        panelRoot.SetActive(false);
    }
}
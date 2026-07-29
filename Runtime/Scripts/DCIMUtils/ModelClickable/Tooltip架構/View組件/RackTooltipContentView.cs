using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIM.Deployment;

namespace VzDev.UIUtils.Tooltip.ContentViews
{
    public class RackTooltipContentView : MonoBehaviour, ITooltipContentView
    {
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI progressLabel;

        public void Bind(DCIMAsset asset, string fallbackName)
        {
            if (asset is not DCR_Asset rack)
            {
                Debug.LogWarning($"[{GetType().Name}] 收到非預期的資料型別：{asset?.GetType().Name}", this);
                return;
            }

            titleLabel.text = rack.assetInfo?.assetName;
            float ratio = rack.rackPowerInfo.power_watt_Max > 0
                ? rack.currentPowerWatt / rack.rackPowerInfo.power_watt_Max
                : 0f;
            progressFill.fillAmount = Mathf.Clamp01(ratio);
            progressLabel.text = $"{rack.currentPowerWatt:F0}W / {rack.rackPowerInfo.power_watt_Max}W";
        }
    }
}

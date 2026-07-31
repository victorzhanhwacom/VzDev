using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev.UIUtils.Tooltip.ContentViews
{
    public class RackTooltipContentView : MonoBehaviour, ITooltipContentView
    {
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI progressLabel;

        public void Bind(DCIMAsset asset, string fallbackName)
        {
            if (asset is not DCR_Asset rackAsset)
            {
                Debug.LogWarning($"[{GetType().Name}] 收到非預期的資料型別：{asset?.GetType().Name}", this);
                return;
            }
            rackAsset.RefreshUsageInfo();
            titleLabel.text = rackAsset.companyPropertyInfo?.propertyName;
            progressLabel.text = $"{rackAsset.usageInfo.totalPowerWatt:#.#}W / {rackAsset.rackCapacityInfo.power_watt_Max}W";
        }
    }
}

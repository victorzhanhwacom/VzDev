// 機櫃：文字 + 用電量進度條，排版可以自己隨意設計
using TMPro;
using UnityEngine;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev.UIUtils.Tooltip.ContentViews
{
    // 冰水主機：文字 + 圖示 + 額外的運轉時數，排版跟機櫃完全不同也沒關係
    public class ChillerTooltipContentView : MonoBehaviour, ITooltipContentView
    {
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI runtimeLabel;

        public void Bind(DCIMAsset asset, string fallbackName)
        {
            if (asset is not AcSystem_Asset chiller)
            {
                Debug.LogWarning($"[{GetType().Name}] 收到非預期的資料型別：{asset?.GetType().Name}", this);
                return;
            }

            titleLabel.text = chiller.companyPropertyInfo?.propertyName;
            runtimeLabel.text = $"輸入溫度：{chiller.inputTemperature}°C\n輸出溫度：{chiller.outputTemperature}°C";
        }
    }
}
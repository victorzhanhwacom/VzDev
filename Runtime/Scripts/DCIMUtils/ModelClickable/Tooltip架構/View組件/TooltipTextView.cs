// 機櫃：文字 + 用電量進度條，排版可以自己隨意設計
using TMPro;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev.UIUtils.Tooltip.ContentViews
{
    public class TooltipTextView : MonoBehaviour, ITooltipContentView
    {
        [SerializeField] private TextMeshProUGUI txt;

        /// <summary>
        /// 無asset時，或找不到對應資料別的TooltipContentMapping時，會呼叫這個方法
        /// </summary>
        public void Bind(DCIMAsset asset, string fallbackName)
        {
            txt.text = asset?.companyPropertyInfo?.propertyName ?? fallbackName;
        }
    }
}

using VzDev.DCIM.Deployment;

namespace VzDev.UIUtils.Tooltip
{
    /// <summary>
    /// 每一種 Tooltip 內容排版 Prefab 都要實作此介面。
    /// TooltipPresenter 殼本身完全不知道 Bind 進來的是什麼資料別、排版長什麼樣子，
    /// 只負責呼叫 Bind() 把資料丟進去。
    ///
    /// asset 不為 null：依實際型別轉型後取用欄位（例如 RackAsset 的用電量）。
    /// asset 為 null：代表目標模型沒有 DCIMAsset，應直接顯示 fallbackName
    /// （這種情況通常只會分派到 fallbackTextPrefab，但保留參數讓實作方自行決定顯示方式）。
    /// </summary>
    public interface ITooltipContentView
    {
        void Bind(DCIMAsset asset, string fallbackName);
    }
}
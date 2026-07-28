 using System.Collections.Generic;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 監聽 SelectionController 的選取結果，依 asset 型別傳給 AssetDataDisplayRegistry 裡對應的 Displayer。
    ///
    /// 【架構修正】原本監聽 ModelComponentSetterEventHub.OnAnyModelClicked（點擊事件）+
    /// ColliderInteractionSystem.OnMouseClickEmpty/OnMouseClick，本質上是在監聽「發生了什麼點擊」，
    /// 而不是「選取結果是什麼」，導致重複點擊同一模型、或點到無資料模型時，
    /// 事件本身無法反映 SelectionController 實際判斷出的結果，只能用 LateUpdate 延後猜測。
    ///
    /// 現在改成只監聽 SelectionController.OnSoleSelectionChanged——這個事件本身就是「結果」，
    /// 在 SelectionController 內部判斷完成之後才廣播，收到的當下就是最終正確狀態，
    /// 不需要任何延後查詢或事件順序上的假設。
    ///
    /// 資料存取則透過 IHasDCIMAsset（見 ModelComponentBase.cs），不再需要透過泛型 Hub 轉發。
    /// </summary>
    public class AssetDataDisplayDispatcher : MonoBehaviour
    {
        /// <summary>
        /// 目前已設置AssetData的對像
        /// </summary>
        private readonly List<IModelSelectedHandler> activeHandlers = new();

        private void OnEnable()
        {
            SelectionController.OnSoleSelectionChanged += HandleSoleSelectionChanged;
        }

        private void OnDisable()
        {
            SelectionController.OnSoleSelectionChanged -= HandleSoleSelectionChanged;
        }

        /// <summary>
        /// selected 為 null 代表：沒有任何選取、多選中沒有唯一焦點、或重複點擊造成取消選取——
        /// 這三種情況對面板來說都是同一件事：清空顯示。
        /// selected 不為 null 但沒有 IHasDCIMAsset（沒掛 ModelComponentBase）：視同沒有資料。
        /// </summary>
        private void HandleSoleSelectionChanged(Renderer selected)
        {
            DeselectAllHandlers();

            if (selected == null) return;
            if (!selected.TryGetComponent<IHasDCIMAsset>(out var provider)) return;

            DCIMAsset asset = provider.GetAsset();
            if (asset == null) return;

            var handlers = AssetDataDisplayRegistry.GetHandlers(asset.GetType());
            for (int i = 0; i < handlers.Count; i++)
            {
                handlers[i].OnModelSelected(asset); //設置AssetData給對應的Handler
                activeHandlers.Add(handlers[i]);
            }
        }

        /// <summary>
        /// 將目前已設置AssetData的對像全部取消選取，並清空activeHandlers
        /// </summary>
        private void DeselectAllHandlers()
        {
            for (int i = 0; i < activeHandlers.Count; i++)
            {
                activeHandlers[i].OnModelDeselected();
            }
            activeHandlers.Clear();
        }
    }
}
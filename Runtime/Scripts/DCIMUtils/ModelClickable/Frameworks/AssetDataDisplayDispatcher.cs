using System.Collections.Generic;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 監聽ModelComponentSetterHub的互動事件，接收到AssetData，再依 asset 型別傳給 AssetDataDisplayRegistry裡對應的Displayer
    /// </summary>
    public class AssetDataDisplayDispatcher : MonoBehaviour
    {
        /// <summary>
        /// 目前已設置AssetData的對像
        /// </summary>
        private readonly List<IModelSelectedHandler> activeHandlers = new();

        private void OnEnable()
        {
            ModelComponentSetterEventHub.OnAnyModelClicked += OnAnyModelClicked;
            ColliderInteractionSystem.OnMouseClickEmpty += OnMouseClickEmpty;
        }

        private void OnDisable()
        {
            ModelComponentSetterEventHub.OnAnyModelClicked -= OnAnyModelClicked;
            ColliderInteractionSystem.OnMouseClickEmpty -= OnMouseClickEmpty;
        }

        private void OnMouseClickEmpty() => DeselectAllHandlers();

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

        private void OnAnyModelClicked(DCIMAsset asset)
        {
            DeselectAllHandlers();

            if (asset == null) return;

            var handlers = AssetDataDisplayRegistry.GetHandlers(asset.GetType());
            for (int i = 0; i < handlers.Count; i++)
            {
                handlers[i].OnModelSelected(asset); //設置AssetData給對應的Handler
                activeHandlers.Add(handlers[i]);
            }
        }
    }
}
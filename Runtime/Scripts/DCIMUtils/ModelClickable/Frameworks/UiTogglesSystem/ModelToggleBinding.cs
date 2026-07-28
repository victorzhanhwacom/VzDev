using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 掛載於「點位標籤」Toggle 身上，代表一個模型專屬的 UI Toggle。
    /// 只負責「使用者操作 Toggle」這個方向：
    /// Toggle 被勾選 → 模擬點擊該模型，完整重用既有的 OnMouseClick 事件管線
    /// （SelectionController 高亮、AssetDataDisplayDispatcher 面板…全部自動生效，
    /// 不需要在這裡重複任何一段邏輯）。
    ///
    /// 另一個方向（模型被點擊 → 這個 Toggle 被勾選）由 ModelToggleSyncController 統一處理，
    /// 職責分離：這裡只管「Toggle 主動觸發」，不管「被動同步」。
    /// </summary>
    public class ModelToggleBinding : MonoBehaviour
    {
        #region Fields
        [SerializeField, Required, Tooltip("此 Toggle 代表的目標模型")]
        private GameObject targetModel;

        [SerializeField, Required] private Toggle toggle;
        [SerializeField, Required, Tooltip("共用的 ToggleGroup，用於同步時關閉其它 Toggle")]
        private ToggleGroup toggleGroup;

        public GameObject TargetModel => targetModel;
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            ModelToggleRegistry.Register(targetModel, this);
            toggle.onValueChanged.AddListener(HandleToggleChanged);
        }

        private void OnDisable()
        {
            ModelToggleRegistry.Unregister(targetModel, this);
            toggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }
        #endregion

        #region Handlers
        /// <summary>
        /// 只處理「被勾選」；ToggleGroup 切換造成其它 Toggle 被關閉時不需要額外動作
        /// （不代表要取消模型選取，這是單選群組下的正常行為）。
        /// </summary>
        private void HandleToggleChanged(bool isOn)
        {
            if (targetModel == null) return;
            if (isOn)
            {
                ColliderInteractionSystem.SimulateClick(targetModel);
                return;
            }
            if(toggleGroup != null && toggleGroup.AnyTogglesOn())
            {
                return;
            }
            ColliderInteractionSystem.SimulateClickEmpty();
        }
        #endregion

        #region 供 ModelToggleSyncController 呼叫，同步「模型被選取」到 Toggle
        /// <summary>
        /// 將 Toggle 設為選取狀態，但不觸發 onValueChanged（避免形成
        /// Toggle→模型→Toggle 的無限迴圈）。
        /// 呼叫前先關閉群組內其它 Toggle：因為 SetIsOnWithoutNotify 不會走
        /// ToggleGroup 原生的互斥通知路徑，必須手動確保視覺上仍然只有一個被選取。
        /// </summary>
        public void SetActiveWithoutNotify()
        {
            if (toggleGroup != null)
                toggleGroup.SetAllTogglesOff(sendCallback: false);
            toggle.SetIsOnWithoutNotify(true);
        }

        public void SetInactiveWithoutNotify()
        {
            toggle.SetIsOnWithoutNotify(false);
        }

        #endregion
    }
}

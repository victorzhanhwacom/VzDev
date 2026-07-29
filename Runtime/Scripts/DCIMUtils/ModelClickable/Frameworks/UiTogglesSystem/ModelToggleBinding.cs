using System;
using NaughtyAttributes;
using TMPro;
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
    ///
    /// 【外觀同步修正】toggle.onValueChanged 上除了本類別用 AddListener 掛的
    /// HandleToggleChanged，Inspector 上還額外掛了 BoolLogicGate.SetBoolValue、
    /// GraphicColorChanger.ChangeColor、Glow/個體資訊 的 GameObject.SetActive 等
    /// 外觀效果，這些全部都要靠 onValueChanged.Invoke() 才會執行。
    /// 原本 SetActiveWithoutNotify/SetInactiveWithoutNotify 用 SetIsOnWithoutNotify
    /// 整個跳過 Invoke，導致這些外觀效果在「被動同步」時完全沒有機會執行。
    ///
    /// 修正方式：改用一般會發出事件的 toggle.isOn 賦值，讓 Inspector 掛的外觀效果
    /// 正常執行；同時用 isSyncingFromModel 旗標讓 HandleToggleChanged 在被動同步期間
    /// 直接跳過「模擬點擊模型」那一段，避免形成 Toggle→Model→Toggle 的重入迴圈。
    /// </summary>
    public class ModelToggleBinding : MonoBehaviour
    {
        #region Fields
        [SerializeField, Required, Tooltip("此 Toggle 代表的目標模型")]
        private GameObject targetModel;

        [SerializeField, Required] private Toggle toggle, labelToggle;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField, Required, Tooltip("共用的 ToggleGroup，用於同步時關閉其它 Toggle")]
        private ToggleGroup toggleGroup;

        /// <summary>
        /// 由 ModelToggleSyncController 呼叫 SetActiveWithoutNotify/SetInactiveWithoutNotify
        /// 觸發被動同步時設為 true，讓 HandleToggleChanged 知道這不是使用者真的點擊 Toggle，
        /// 不應該再去模擬點擊/取消點擊模型，避免重入迴圈。
        /// 這段期間 toggle.onValueChanged 仍會正常 Invoke，讓 Inspector 掛的外觀效果照常執行。
        /// </summary>
        private bool isSyncingFromModel = false;

        public GameObject TargetModel => targetModel;

        public Toggle ToggleItem => toggle;

        public bool LabelVisible => labelToggle.isOn;

        #endregion

        public void SetTargetModel(Transform target)
        {
            targetModel = target.gameObject;
        }
        public void SetLabel(string txt) => labelText.text = txt;
        public void SetToggleGroup(ToggleGroup group)
        {
            toggleGroup = group;
            toggle.group = group; 
        }

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
        ///
        /// isSyncingFromModel 為 true 時代表這次觸發來自 SetActiveWithoutNotify/
        /// SetInactiveWithoutNotify 的被動同步，此時外觀效果（Inspector 上掛的那幾個
        /// listener）已經因為事件正常 Invoke 而執行完畢，這裡只需要跳過模型互動那段，
        /// 不重新模擬點擊/取消點擊，避免 Toggle→Model→Toggle 的重入迴圈。
        /// </summary>
        private void HandleToggleChanged(bool isOn)
        {
            if (isSyncingFromModel) return;

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
        /// 將 Toggle 設為選取狀態。
        ///
        /// 【不在這裡處理群組內其它 Toggle 的關閉】
        /// 舊版用 toggleGroup.SetAllTogglesOff(sendCallback:false) 圖方便一次關掉其它 Toggle，
        /// 但 sendCallback:false 會讓「被關閉的那個 Toggle」自己的 Inspector 外觀效果
        /// （GraphicColorChanger、Glow/個體資訊 SetActive 等）完全沒有機會執行，造成外觀殘留。
        /// 正確做法是由呼叫端（ModelToggleSyncController，它才知道「上一個作用中的 binding
        /// 是誰」）在切換前主動呼叫舊 binding 的 SetInactiveWithoutNotify()，走正常 Invoke 路徑。
        /// 這裡只單純負責「把自己設為選取」。
        ///
        /// 【改用 toggle.isOn 而非 SetIsOnWithoutNotify】
        /// 因為 Inspector 上掛在 onValueChanged 的外觀效果必須靠事件 Invoke 才會執行；
        /// 用 isSyncingFromModel 旗標防止 HandleToggleChanged 在這個路徑下
        /// 又跑去模擬點擊模型，形成迴圈。
        /// </summary>
        public void SetActiveWithoutNotify()
        {
            isSyncingFromModel = true;
            toggle.isOn = true;
            isSyncingFromModel = false;
        }

        /// <summary>
        /// 【AllowSwitchOff 陷阱】若此 Toggle 是目前群組裡唯一開著的一個，
        /// 且 ToggleGroup.allowSwitchOff 為 false（Unity 預設），Unity 的
        /// Toggle.Set() 會在同一次呼叫裡把 m_IsOn 偷偷改回 true 再 Invoke——
        /// 也就是完全無法把「唯一的 Toggle」關閉成全部未選取的狀態，
        /// onValueChanged 收到的還是 true，導致外觀效果沒有被關閉。
        ///
        /// 這正是 Unity 內建 ToggleGroup.SetAllTogglesOff() 也要暫時把
        /// allowSwitchOff 開起來才能強制關閉的原因，這裡採用相同做法，
        /// 差別是關閉後仍要正常 Invoke（sendCallback:true），
        /// 讓 Inspector 掛的外觀效果收到正確的 false。
        /// </summary>
        public void SetInactiveWithoutNotify()
        {
            bool previousAllowSwitchOff = false;
            if (toggleGroup != null)
            {
                previousAllowSwitchOff = toggleGroup.allowSwitchOff;
                toggleGroup.allowSwitchOff = true;
            }

            isSyncingFromModel = true;
            toggle.isOn = false;
            isSyncingFromModel = false;

            if (toggleGroup != null)
                toggleGroup.allowSwitchOff = previousAllowSwitchOff;
        }

        internal void SetLabelAlwaysVisible(bool isVisible)
        {
            labelToggle.isOn = isVisible;
        }


        #endregion
    }
}
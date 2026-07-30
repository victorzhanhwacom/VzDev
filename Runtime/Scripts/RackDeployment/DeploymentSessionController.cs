using System;
using System.Collections.Generic;
using UnityEngine;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 配置管理模組核心狀態機，管理「上架庫存設備」的完整流程（Step1~Step5）。
    /// 場景中應只有一個 instance（重複存在時自我銷毀，模式與 GlobalLifecycleBroadcaster 相同），
    /// 透過 static event 廣播狀態變化，讓 Step2 外殼變色、Step3 拖曳合法性檢查/預覽、
    /// Step4 資訊表單、UI 面板各自訂閱，彼此不直接耦合。
    ///
    /// 流程狀態機：
    ///   Idle -&gt; BeginDeployment(設備) -&gt; 等待選定機櫃/U槽
    ///        -&gt; TrySelectTargetSlot(合法) -&gt; 等待填寫資訊/確認
    ///        -&gt; ConfirmDeployment() -&gt; Idle（完成一次上架並持久化）
    /// 任何階段呼叫 CancelDeployment() 都會直接回到 Idle，不留殘留狀態。
    ///
    /// 【UI互動流程對應】清單裡的 Toggle 被選中 = 呼叫 BeginDeployment（Step1，順帶觸發Step2變色）；
    /// 使用者拖曳「已選中」的獨立預覽物件到機櫃/U槽上放開 = 呼叫 TrySelectTargetSlot（Step3）；
    /// 拖放失敗時 Session 不會自動取消，讓使用者可以再拖一次，不需要重新從清單選取。
    /// </summary>
    public class DeploymentSessionController : MonoBehaviour
    {
        #region Singleton
        public static DeploymentSessionController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[{nameof(DeploymentSessionController)}] 場景中重複存在，此instance將被銷毀：{gameObject.name}", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Static Events — 供 Step2/Step3/Step4/UI 訂閱
        /// <summary>Step1完成：使用者選定要上架的庫存設備，帶出設備需求，供Step2變色使用</summary>
        public static event Action<EquipmentAssetBase> OnEquipmentSelected;
        /// <summary>使用者主動取消本次上架流程，訂閱端應清空自己暫存的視覺狀態</summary>
        public static event Action OnSessionCancelled;
        /// <summary>Step3完成：使用者選定合法的機櫃+起始U槽，進入Step4填寫資訊</summary>
        public static event Action<DCR_Asset, int> OnTargetSlotSelected;
        /// <summary>Step5完成：上架成功，帶出最終的 DeploymentRecord 供 UI 刷新列表/提示成功</summary>
        public static event Action<DeploymentRecord> OnDeploymentCompleted;
        #endregion

        #region State
        private EquipmentAssetBase pendingEquipment;
        private DCR_Asset pendingRack;
        private int pendingStartUSlot;

        public bool IsAwaitingSlotSelection => pendingEquipment != null && pendingRack == null;
        public bool IsAwaitingConfirmation => pendingEquipment != null && pendingRack != null;

        /// <summary>目前選定設備所需的U高，Step3拖曳預覽（3D/2D）換算U槽時需要用到；未選定設備時預設1</summary>
        public int PendingUHeight => pendingEquipment?.equipmentInfo.u_height ?? 1;

        /// <summary>
        /// 目前pending設備的assetNo，供清單UI判斷「我剛才選的那張卡片還是不是目前的pending對象」
        /// （例如ToggleGroup切換到別的卡片時，用來判斷自己是否該觸發CancelDeployment）。
        /// </summary>
        public string PendingEquipmentAssetNo => pendingEquipment?.assetInfo?.assetNo;
        #endregion

        #region Step1 — 選定庫存設備
        public void BeginDeployment(EquipmentAssetBase equipment)
        {
            if (equipment == null) return;
            if (equipment.deploymentStatus == EquipmentDeploymentStatus.Deployed)
            {
                Debug.LogWarning($"[{nameof(DeploymentSessionController)}] 設備已上架，不可重複選取：{equipment.assetInfo?.assetName}", this);
                return;
            }

            ResetState();
            pendingEquipment = equipment;
            OnEquipmentSelected?.Invoke(equipment);
        }
        #endregion

        #region Step2/3 — 選定機櫃與U槽（合法性檢查）
        /// <summary>
        /// 由拖放系統（SelectedEquipmentDragHandle → RackSlotDropTarget/RackSlotCell）在放開滑鼠時呼叫，
        /// 嘗試把目前選定的設備放到指定機櫃的指定起始U槽。
        /// 回傳 false 代表不合法（電力/重量/空間不足，或U槽已被佔用），呼叫端應顯示提示並讓拖放彈回原位，
        /// 不會更動任何狀態，Session維持在等待選定的狀態；回傳 true 才進入Step4等待填寫資訊。
        /// </summary>
        public bool TrySelectTargetSlot(DCR_Asset rack, int startUSlot)
        {
            if (pendingEquipment == null || rack == null) return false;

            var capacity = RackCapacityEvaluator.Evaluate(rack, pendingEquipment.equipmentInfo);
            if (!capacity.Fits)
            {
                Debug.LogWarning($"[{nameof(DeploymentSessionController)}] 容量不足，無法上架至 {rack.assetInfo?.assetName}：{capacity.shortfall}", this);
                return false;
            }

            if (!RackCapacityEvaluator.IsSlotRangeFree(rack, startUSlot, pendingEquipment.equipmentInfo.u_height))
            {
                Debug.LogWarning($"[{nameof(DeploymentSessionController)}] U槽區段已被佔用：{rack.assetInfo?.assetName} U{startUSlot}", this);
                return false;
            }

            pendingRack = rack;
            pendingStartUSlot = startUSlot;
            OnTargetSlotSelected?.Invoke(rack, startUSlot);
            return true;
        }
        #endregion

        #region Step4 — 填寫基本資訊（選填）
        public void SetBasicInfo(string customName, string note)
        {
            if (pendingEquipment == null) return;
            pendingEquipment.customName = customName;
            pendingEquipment.note = note;
        }
        #endregion

        #region Step5 — 進行上架
        /// <summary>
        /// 確認上架：把設備直接加進機櫃的 container、更新設備狀態，並透過
        /// DeploymentPersistenceService 存一份輕量snapshot（DeploymentRecord）做 JSON 暫存
        /// ——不直接序列化整個 EquipmentAssetBase，因為它底下的 ModelInfo.modelTarget 是
        /// Transform參照，JsonUtility存出來的參照離開當前場景就沒意義，還是走DTO比較乾淨。
        /// 完成後重置狀態並廣播結果。
        /// </summary>
        public bool ConfirmDeployment()
        {
            if (!IsAwaitingConfirmation)
            {
                Debug.LogWarning($"[{nameof(DeploymentSessionController)}] 尚未選定機櫃與U槽，無法確認上架", this);
                return false;
            }

            pendingEquipment.startUSlot = pendingStartUSlot;
            pendingEquipment.deploymentStatus = EquipmentDeploymentStatus.Deployed;

            pendingRack.container ??= new List<EquipmentAssetBase>();
            pendingRack.container.Add(pendingEquipment);

            var record = new DeploymentRecord
            {
                rackAssetNo = pendingRack.assetInfo?.assetNo,
                assignment = new RackSlotAssignment
                {
                    equipmentAssetNo = pendingEquipment.assetInfo?.assetNo,
                    equipmentAssetName = pendingEquipment.assetInfo?.assetName,
                    startUSlot = pendingStartUSlot,
                    uHeight = pendingEquipment.equipmentInfo.u_height,
                    powerWatt = pendingEquipment.equipmentInfo.power_watt,
                    weightKg = pendingEquipment.equipmentInfo.weight_kg,
                },
                customName = pendingEquipment.customName,
                note = pendingEquipment.note,
                deployedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            DeploymentPersistenceService.AppendRecord(record);

            OnDeploymentCompleted?.Invoke(record);
            ResetState();
            return true;
        }

        /// <summary>使用者主動取消本次上架流程（例如清單切換到別的設備、或Step4畫面按取消）</summary>
        public void CancelDeployment()
        {
            ResetState();
            OnSessionCancelled?.Invoke();
        }

        private void ResetState()
        {
            pendingEquipment = null;
            pendingRack = null;
            pendingStartUSlot = 0;
        }
        #endregion
    }
}
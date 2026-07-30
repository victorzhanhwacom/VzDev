using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// Step4 UI殼：Step3選定機櫃+U槽（OnTargetSlotSelected）後才顯示，填寫選填的名稱/備註，
    /// 按確認呼叫 ConfirmDeployment，按取消呼叫 CancelDeployment（會退回等待重新選U槽，
    /// 不會退回庫存清單，如果要退回清單重選，另外在清單UI端訂閱 OnSessionCancelled 自行處理
    /// Toggle取消勾選即可）。
    ///
    /// 本類別只管UI顯示/按鈕轉發，完全不涉及合法性判斷——那些在Step3
    /// (DeploymentSessionController.TrySelectTargetSlot) 已經做完了，走到這裡代表一定合法。
    /// </summary>
    public class DeploymentInfoFormPresenter : MonoBehaviour
    {
        [Foldout("[Components]"), SerializeField] private GameObject root;
        [Foldout("[Components]"), SerializeField] private TMP_InputField nameInput;
        [Foldout("[Components]"), SerializeField] private TMP_InputField noteInput;
        [Foldout("[Components]"), Tooltip("選填，顯示「選中的機櫃 · U槽」，沒有就不掛"), SerializeField]
        private TMP_Text targetRackLabel;
        [Foldout("[Components]"), SerializeField] private Button confirmButton;
        [Foldout("[Components]"), SerializeField] private Button cancelButton;

        private void OnEnable()
        {
            DeploymentSessionController.OnTargetSlotSelected += HandleTargetSlotSelected;
            DeploymentSessionController.OnSessionCancelled += HandleClosed;
            DeploymentSessionController.OnDeploymentCompleted += HandleDeploymentCompleted;

            confirmButton.onClick.AddListener(HandleConfirmClicked);
            cancelButton.onClick.AddListener(HandleCancelClicked);

            SetVisible(false);
        }

        private void OnDisable()
        {
            DeploymentSessionController.OnTargetSlotSelected -= HandleTargetSlotSelected;
            DeploymentSessionController.OnSessionCancelled -= HandleClosed;
            DeploymentSessionController.OnDeploymentCompleted -= HandleDeploymentCompleted;

            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            cancelButton.onClick.RemoveListener(HandleCancelClicked);
        }

        private void HandleTargetSlotSelected(DCR_Asset rack, int startUSlot)
        {
            nameInput.text = string.Empty;
            noteInput.text = string.Empty;
            if (targetRackLabel != null)
                targetRackLabel.text = $"{rack.assetInfo?.assetName} · U{startUSlot}";

            SetVisible(true);
        }

        private void HandleClosed() => SetVisible(false);
        private void HandleDeploymentCompleted(DeploymentRecord record) => SetVisible(false);

        private void HandleConfirmClicked()
        {
            if (DeploymentSessionController.Instance == null) return;

            DeploymentSessionController.Instance.SetBasicInfo(nameInput.text, noteInput.text);
            bool success = DeploymentSessionController.Instance.ConfirmDeployment();

            if (!success)
                Debug.LogWarning($"[{nameof(DeploymentInfoFormPresenter)}] 確認上架失敗，Session狀態可能不正確", this);
        }

        private void HandleCancelClicked() => DeploymentSessionController.Instance?.CancelDeployment();

        private void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }
    }
}
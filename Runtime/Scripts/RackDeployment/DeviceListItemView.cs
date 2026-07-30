using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// 掛載於庫存清單UI每一個項目上（例如 ListItem_ICTDevice），只負責「Toggle被選中」這件事：
    /// 選中時用 EquipmentCatalogEntry（先寫死）建立一個臨時資產實體，呼叫
    /// DeploymentSessionController.BeginDeployment 進入Step1（同時觸發Step2機櫃變色）。
    ///
    /// 【為什麼不在這裡掛拖曳】清單項目上已經有 ScrollViewItemPassthrough，會把拖曳事件轉發給
    /// 父層ScrollRect讓清單能滑動；如果同一個物件上再疊加拖曳偵測，兩者會同時收到同一組拖曳事件、
    /// 互相干擾。實際的拖曳上架動作交給 SelectedEquipmentDragHandle，掛在ScrollView外面
    /// 那個獨立的「已選中」預覽物件（例如 Listitem_ICTDevice_Selected）上。
    ///
    /// 之後接上真實後端資料時，只需要把 BuildRuntimeAsset() 換成用後端回傳的資料建構，
    /// 其他系統完全不用改。
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class DeviceListItemView : MonoBehaviour
    {
        [SerializeField] private EquipmentCatalogEntry catalogEntry;

        public UnityEvent<EquipmentCatalogEntry> onToggleSelected;

        private Toggle toggle;
        private string lastGeneratedAssetNo;

        private void Awake() => toggle = GetComponent<Toggle>();

        private void OnEnable() => toggle.onValueChanged.AddListener(HandleToggleChanged);
        private void OnDisable() => toggle.onValueChanged.RemoveListener(HandleToggleChanged);

        /// <summary>
        /// isOn==false 分兩種情況：(1)使用者自己點掉 (2)ToggleGroup切到別的項目造成本項目被關閉。
        /// 兩種情況都只在「目前Session的pending設備正好是我自己剛才建立的那份」時才取消，
        /// 避免切到別的項目時，反而把新選到的那份pending資料取消掉
        /// （ToggleGroup內新舊Toggle的callback觸發順序不保證，用assetNo比對可以正確處理兩種順序）。
        /// </summary>
        private void HandleToggleChanged(bool isOn)
        {
            if (DeploymentSessionController.Instance == null) return;

            if (isOn)
            {
                var asset = catalogEntry.CreateAssetInstance();
                lastGeneratedAssetNo = asset.assetInfo.assetNo;
                EquipmentCatalogRegistry.Register(lastGeneratedAssetNo, catalogEntry);
                DeploymentSessionController.Instance.BeginDeployment(asset);
                onToggleSelected?.Invoke(catalogEntry);
            }
            else if (!string.IsNullOrEmpty(lastGeneratedAssetNo)
                     && DeploymentSessionController.Instance.PendingEquipmentAssetNo == lastGeneratedAssetNo)
            {
                DeploymentSessionController.Instance.CancelDeployment();
            }
        }
    }
}
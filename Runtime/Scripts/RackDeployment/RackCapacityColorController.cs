using System.Collections.Generic;
using UnityEngine;
using VzDev.DCIM.Deployment;
using VzDev.MaterialUtils;
using VzDev.ObjectUtils;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// Step2：使用者選定要上架的設備後，依每個機櫃目前的剩餘電力/空間/承重，
    /// 把機櫃分類到5種容量分類（符合/缺電力/缺重量/缺空間/複合缺項），
    /// 各自整批交給對應分類的 MaterialReplacer 做批次材質替換，達到篩選視覺效果。
    ///
    /// 【對應實際 MaterialReplacer API】該元件是「SetTargetModels() 設定名單 →
    /// ReplaceModelsMaterial() 整批替換／RestoreModelsMaterial() 整批還原」的批次模式，
    /// 材質本身（replaceMaterial）是每個 MaterialReplacer 元件在 Inspector 上各自固定好的，
    /// 程式碼沒辦法動態指定，所以這裡準備5個 MaterialReplacer（各配一種顏色材質），
    /// 我們只負責「這一輪哪些機櫃該分到哪一個 Replacer 的名單」。
    ///
    /// 沒有 reference counting：每次重新篩選（切換設備/取消/上架完成）都必須先呼叫
    /// RestoreModelsMaterial() 清空上一輪名單的效果，才能再設定新名單並替換。
    /// </summary>
    public class RackCapacityColorController : MonoBehaviour
    {
        [SerializeField] private MaterialReplacer fitsReplacer;
        [SerializeField] private MaterialReplacer insufficientPowerReplacer;
        [SerializeField] private MaterialReplacer insufficientWeightReplacer;
        [SerializeField] private MaterialReplacer insufficientSpaceReplacer;
        [SerializeField] private MaterialReplacer mixedShortfallReplacer;

        private bool isFiltering;

        private void OnEnable()
        {
            DeploymentSessionController.OnEquipmentSelected += HandleEquipmentSelected;
            DeploymentSessionController.OnSessionCancelled += ClearFiltering;
            DeploymentSessionController.OnDeploymentCompleted += HandleDeploymentCompleted;
        }

        private void OnDisable()
        {
            DeploymentSessionController.OnEquipmentSelected -= HandleEquipmentSelected;
            DeploymentSessionController.OnSessionCancelled -= ClearFiltering;
            DeploymentSessionController.OnDeploymentCompleted -= HandleDeploymentCompleted;
            ClearFiltering();
        }

        private void HandleEquipmentSelected(EquipmentAssetBase equipment)
        {
            ClearFiltering();
            if (equipment == null) return;

            var fits = new List<Transform>();
            var noPower = new List<Transform>();
            var noWeight = new List<Transform>();
            var noSpace = new List<Transform>();
            var mixed = new List<Transform>();

            foreach (var rackObject in RackRegistry.AllRackObjects)
            {
                if (!RackRegistry.TryGetRackAsset(rackObject, out var rack)) continue;

                var result = RackCapacityEvaluator.Evaluate(rack, equipment.equipmentInfo);
                GetBucket(result.shortfall, fits, noPower, noWeight, noSpace, mixed).Add(rackObject.transform);
            }

            Apply(fitsReplacer, fits);
            Apply(insufficientPowerReplacer, noPower);
            Apply(insufficientWeightReplacer, noWeight);
            Apply(insufficientSpaceReplacer, noSpace);
            Apply(mixedShortfallReplacer, mixed);

            isFiltering = true;
        }

        /// <summary>上架完成後容量已變動，目前流程完成一次就重置Session，直接清空篩選狀態即可</summary>
        private void HandleDeploymentCompleted(DeploymentRecord record) => ClearFiltering();

        private void ClearFiltering()
        {
            if (!isFiltering) return;

            fitsReplacer?.RestoreModelsMaterial();
            insufficientPowerReplacer?.RestoreModelsMaterial();
            insufficientWeightReplacer?.RestoreModelsMaterial();
            insufficientSpaceReplacer?.RestoreModelsMaterial();
            mixedShortfallReplacer?.RestoreModelsMaterial();

            isFiltering = false;
        }

        /// <summary>
        /// 即使 models 為空也要 SetTargetModels()，避免該 Replacer 殘留上上一輪的名單，
        /// 下次真的有名單時 RestoreModelsMaterial() 才不會還原到錯誤的目標。
        /// </summary>
        private void Apply(MaterialReplacer replacer, List<Transform> models)
        {
            if (replacer == null) return;
            replacer.SetTargetModels(models);
            if (models.Count > 0) replacer.ReplaceModelsMaterial();
        }

        private static List<Transform> GetBucket(RackCapacityShortfall shortfall,
            List<Transform> fits, List<Transform> noPower, List<Transform> noWeight,
            List<Transform> noSpace, List<Transform> mixed)
        {
            if (shortfall == RackCapacityShortfall.None) return fits;

            bool isSingleFlag = (shortfall & (shortfall - 1)) == 0; // 只有一個bit被設置，代表只缺一項
            if (!isSingleFlag) return mixed;

            switch (shortfall)
            {
                case RackCapacityShortfall.Power: return noPower;
                case RackCapacityShortfall.Weight: return noWeight;
                case RackCapacityShortfall.Space: return noSpace;
                default: return mixed;
            }
        }
    }
}
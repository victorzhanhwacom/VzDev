using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DCIM.Import;
using VzDev.DCIM.RevitAssetDataStructure;
using VzDev.DCIMUtils.ModelInteractUtils;
using VzDev.UnityAPI.Extensions;
using Random = UnityEngine.Random;

namespace VzDev.DCIMUtils.Deployment.Demo
{
    /// <summary>
    /// 產生設備群到機櫃裡 (DEMO用)。
    /// <para>
    /// 【等待時機】OnEnable 時訂閱 RackComponentSetter.onSetComponentsCompleted，
    /// 搭配外部呼叫 SetEquipmentAssets(...) 設定庫存設備清單。
    /// 兩邊資料都到齊後 (dataReadyCount == dataReadyCountMax) 才真正執行生成。
    /// </para>
    /// <para>
    /// 【DEMO 用途：不出庫】equipmentAssets 只是「範本池」，同一筆範本可以被
    /// 重複抽中、放進不同機櫃甚至同一機櫃的不同U位置，不會從池中移除。
    /// 因此每次實際部署時都會呼叫 CloneEquipmentAsset 產生獨立副本——
    /// 否則多個放置位置會共用同一個 EquipmentAsset 物件，導致 startUIndex /
    /// modelInfo.modelTarget 互相覆蓋。
    /// </para>
    /// <para>
    /// 【不重疊保證】每塞入一筆設備就立刻 Add 進 rack.container，
    /// 下一筆設備尋找空位時，UsageCaculatorOfRack.IsRackUCanFit 會把它也納入
    /// 重疊檢查範圍，不需要額外自建一份「暫存已佔用區段」的資料結構。
    /// </para>
    /// <para>
    /// 【模型生成】每筆成功放置的設備，都會用範本的 modelInfo.modelTarget 當作來源，
    /// 在對應機櫃的 Transform 底下 Instantiate 一份實體模型，掛上 EquipmentComponent
    /// 並呼叫 SetData()，讓該模型可以被 ColliderInteractionSystem / SelectionController
    /// 等既有互動系統直接偵測到（跟正式流程走的是同一條路徑，Demo 不用另開後門）。
    /// 目前只是掛在機櫃 Transform 底下，尚未依 startUIndex 換算實際世界座標
    /// （U槽高度、機櫃內部Local座標基準等專案特定數值不在這裡假設，見方法註解）。
    /// </para>
    /// </summary>
    public class DemoGenerator_EquipmentToRackContainer : MonoBehaviour
    {
        #region Fields
        [SerializeField] private bool isGenerateDemoData = false;
        [SerializeField, ReadOnly, Tooltip("本次實際被部署上架的設備 (供Inspector追蹤查看)")]
        private List<EquipmentAsset> createEquipmentAssets = new List<EquipmentAsset>();
        [SerializeField, ReadOnly, Space, Tooltip("設備範本池 (DEMO用，可重複抽取，不會被消耗)")]
        private List<EquipmentAsset> equipmentAssets = new List<EquipmentAsset>();
        [SerializeField, ReadOnly] private List<RackComponent> rackComponents = new List<RackComponent>();
        [Foldout("[Settings]"), SerializeField, Tooltip("每個機櫃要嘗試塞入的設備數量範圍 (含上下限)")]
        private Vector2Int generateAmountRange = new(1, 12);
        [Foldout("[Settings]"), SerializeField, Tooltip("機櫃模型的「正面」是Local +Z還是-Z方向。" +
            "只假設機櫃只會繞垂直Y軸旋轉(樓層平面配置常見情況)，若實際貼齊方向相反，將此值切換即可。")]
        private bool frontFaceIsLocalPositiveZ = true;
        [Foldout("[Export]"), SerializeField, Tooltip("匯出JSON的檔案路徑，相對於專案根目錄(Assets的上一層)")]
        private string exportJsonPath = "Exported/racks_export.json";
        private int dataReadyCount = 0, dataReadyCountMax = 3;

        private bool isHaveData => equipmentAssets != null && equipmentAssets.Count > 0;
        private bool isHaveRacks => rackComponents != null && rackComponents.Count > 0;
        #endregion

        public void SetEquipmentAssets(List<EquipmentAsset> assets)
        {
            equipmentAssets = assets;
            TryGenerateDemoData();
        }
        private void HandleSetComponentsCompleted(List<RackComponent> list)
        {
            rackComponents = list;
            TryGenerateDemoData();
        }

        /// <summary>
        /// 累計兩邊資料到齊的次數，未到齊前不執行生成。
        /// </summary>
        private void TryGenerateDemoData()
        {
            dataReadyCount++;
            if (dataReadyCount < dataReadyCountMax) return;

            GenerateEquipmentIntoRacks();
        }

        #region Lifycycle
        private void OnEnable()
        {
            if (isGenerateDemoData == false) return;
            RackComponentSetter.OnSetComponentsCompletedAction += HandleSetComponentsCompleted;
            RackComponentSetter.onSetDeployColliderCompleted += TryGenerateDemoData;
        }

       
        private void OnDisable()
        {
            if (isGenerateDemoData == false) return;
            RackComponentSetter.OnSetComponentsCompletedAction -= HandleSetComponentsCompleted;
            RackComponentSetter.onSetDeployColliderCompleted -= TryGenerateDemoData;
        }
        #endregion

            #region 生成邏輯
            /// <summary>
            /// 依序走過每個機櫃，隨機抽範本設備塞入，直到達到本次隨機數量或嘗試次數上限。
            /// </summary>
        private void GenerateEquipmentIntoRacks()
        {
            createEquipmentAssets.Clear();

            if (rackComponents == null || rackComponents.Count == 0)
            {
                Debug.LogWarning($"[{nameof(DemoGenerator_EquipmentToRackContainer)}] rackComponents 為空，無法生成資料。");
                return;
            }
            if (equipmentAssets == null || equipmentAssets.Count == 0)
            {
                Debug.LogWarning($"[{nameof(DemoGenerator_EquipmentToRackContainer)}] equipmentAssets 範本池為空，無法生成資料。");
                return;
            }

            for (int i = 0; i < rackComponents.Count; i++)
            {
                RackComponent rackComp = rackComponents[i];
                if (rackComp == null) continue;
                if (rackComp.GetAsset() is not DCR_Asset rack) continue;

                int amount = Random.Range(generateAmountRange.x, generateAmountRange.y + 1);
                DeployRandomEquipmentToRack(rack, rackComp.transform, amount);

                rack.RefreshUsageInfo();
            }
        }

        /// <summary>
        /// 從範本池隨機抽最多 amount 筆設備塞入指定機櫃，每筆都先確認能找到不重疊的
        /// U 區段，找不到就放棄這一筆（換下一次隨機嘗試，範本池不會被消耗）。
        /// </summary>
        private void DeployRandomEquipmentToRack(DCR_Asset rack, Transform rackTransform, int amount)
        {
            int attempts = 0;
            int maxAttempts = amount * 8; // 隨機挑候選位置比First-Fit更容易在機櫃快滿/空間破碎時撲空，倍數調高避免提早放棄
            int deployedCount = 0;

            while (deployedCount < amount && attempts < maxAttempts)
            {
                attempts++;

                EquipmentAsset template = equipmentAssets[Random.Range(0, equipmentAssets.Count)];

                int heightU = template.equipmentUsageInfo.heightU;
                if (heightU <= 0) heightU = 1;

                if (!TryFindFreeUIndex(rack, heightU, out int uIndex))
                {
                    continue; // 這個範本在目前機櫃剩餘空間放不下，換下一次隨機嘗試
                }

                EquipmentAsset equipment = CloneEquipmentAsset(template);
                equipment.startUIndex = uIndex;
                equipment.deploymentStatus = DeploymentStatus.Deployed;

                if (!TryCreateEquipmentModel(rack, template, rackTransform, equipment))
                {
                    continue; // 範本沒有可實例化的模型來源，放棄這一筆，不佔用 container
                }

                rack.container.Add(equipment);
                createEquipmentAssets.Add(equipment);
                deployedCount++;
            }
        }

        /// <summary>
        /// 依範本的 modelInfo.modelTarget 實例化一份設備模型，掛載 EquipmentComponent
        /// 並呼叫 SetData(equipment)——SetData 內部會自動把
        /// equipment.modelInfo.modelTarget 設成新模型的 Transform，這裡不用手動設定。
        /// <para>
        /// 【Y軸：U槽高度】用 rackTransform 底下 BoxCollider 的世界Bounds代表整個機櫃可上架的
        /// U槽範圍（bottom = U1底部，top = 最後一U頂部），並依此換算「每1U實際世界高度」，
        /// 找出 equipment.startUIndex ~ startUIndex+heightU-1 這段區間的世界Y中點。
        /// 若機櫃框體本身比可上架區域大（例如上下還有機殼/門板造成死空間），
        /// 這段換算會偏移，需要改成量測實際導軌區域的 Bounds。
        /// </para>
        /// <para>
        /// 【X/Z軸：水平置中+前緣貼齊】改到 rackTransform 的 Local 座標系下計算，
        /// 只假設機櫃只會繞垂直 Y 軸旋轉（樓層平面配置常見情況，不會歪斜/翻滾），
        /// 這樣不論機櫃朝哪個方向擺，都能正確量出「前緣」在世界座標的位置：
        /// X 維持水平置中；Z 用 frontFaceIsLocalPositiveZ 決定前緣是 Local +Z 還是 -Z，
        /// 取機櫃 Local Bounds 在該方向的邊界，再往內縮設備自身深度的一半，讓設備前緣貼齊機櫃前緣。
        /// </para>
        /// </summary>
        private bool TryCreateEquipmentModel(DCR_Asset rack, EquipmentAsset template, Transform rackTransform, EquipmentAsset equipment)
        {
            Transform sourceModel = template.modelInfo?.modelTarget;
            if (sourceModel == null) return false;

            Collider rackCollider = rackTransform.GetComponentInChildren<BoxCollider>();
            if (rackCollider == null)
            {
                Debug.LogWarning($"[{nameof(DemoGenerator_EquipmentToRackContainer)}] 機櫃 {rackTransform.name} 底下找不到 Collider，無法計算U槽位置。", rackTransform);
                return false;
            }
            Bounds rackBounds = rackCollider.bounds;

            GameObject newModel = Instantiate(sourceModel.gameObject, rackTransform);
            newModel.name = sourceModel.name;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localPosition = Vector3.zero; // 先歸零，才能量出目前狀態下的 Renderer Bounds

            if (!TryGetCombinedWorldBounds(newModel, out Bounds equipmentBounds))
            {
                Debug.LogWarning($"[{nameof(DemoGenerator_EquipmentToRackContainer)}] {newModel.name} 底下找不到 Renderer，無法計算 Bounds 中心補償。", newModel);
                Destroy(newModel);
                return false;
            }

            // Pivot 相對於自己 Mesh Bounds 中心的偏移量：不論原始模型 Pivot 在哪個角落，
            // 都用這個偏移量回推「Pivot該放在哪裡」，讓 Mesh 的視覺中心對到目標座標。
            Vector3 pivotOffsetFromBoundsCenter = newModel.transform.position - equipmentBounds.center;

            // Y：沿用世界座標的U槽高度計算，只跟垂直高度有關，跟水平旋轉無關
            float heightU = Mathf.Max(1, template.equipmentUsageInfo.heightU);
            float uPitchWorld = rackBounds.size.y / rack.u_height_Max;
            float slotBottomWorldY = rackBounds.min.y + (equipment.startUIndex - 1) * uPitchWorld;
            float slotCenterWorldY = slotBottomWorldY + (heightU * uPitchWorld) * 0.5f;

            // X/Z：轉換到機櫃的 Local 座標系計算「水平置中 + 前緣貼齊」
            Bounds rackLocalBounds = GetLocalBounds(rackTransform, rackBounds);
            Bounds equipmentLocalBounds = GetLocalBounds(rackTransform, equipmentBounds);

            float frontLocalZ = frontFaceIsLocalPositiveZ ? rackLocalBounds.max.z : rackLocalBounds.min.z;
            float inwardDirection = frontFaceIsLocalPositiveZ ? -1f : 1f; // 從前緣往機櫃內部縮的方向
            float desiredLocalZ = frontLocalZ + inwardDirection * equipmentLocalBounds.extents.z;
            float desiredLocalX = rackLocalBounds.center.x; // 水平置中

            Vector3 desiredWorldXZ = rackTransform.TransformPoint(new Vector3(desiredLocalX, 0f, desiredLocalZ));
            // 只假設機櫃僅繞垂直Y軸旋轉，所以Local Y代入0不會影響換算出來的世界X/Z分量，
            // 真正的世界Y高度改用上面算好的 slotCenterWorldY 覆蓋。
            Vector3 desiredBoundsCenterWorld = new Vector3(desiredWorldXZ.x, slotCenterWorldY, desiredWorldXZ.z);
            newModel.transform.position = desiredBoundsCenterWorld + pivotOffsetFromBoundsCenter;

            newModel.TryAddComponent(out EquipmentComponent comp);
            comp.SetData(equipment);

            return true;
        }

        /// <summary>
        /// 合併目標物件底下所有 Renderer 的世界座標 Bounds，用來量出模型實際的視覺中心。
        /// 沒有任何 Renderer（例如空物件或純Collider）時回傳 false。
        /// </summary>
        private static bool TryGetCombinedWorldBounds(GameObject target, out Bounds combined)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                combined = default;
                return false;
            }

            combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combined.Encapsulate(renderers[i].bounds);
            }
            return true;
        }

        /// <summary>
        /// 把一個世界座標 Bounds 的 8 個角點轉換到 reference 的 Local 座標系下，
        /// 重新求出 Local 座標系下的 AABB。用途：只假設 reference 只繞垂直 Y 軸旋轉時，
        /// 這個 Local Bounds 的 X/Z 就能正確反映「不管機櫃朝哪個方向擺」的水平置中/前緣位置，
        /// 不會受機櫃本身旋轉角度影響（若直接用世界座標的 Bounds.center/min/max 計算則會出錯）。
        /// </summary>
        private static Bounds GetLocalBounds(Transform reference, Bounds worldBounds)
        {
            Vector3 c = worldBounds.center;
            Vector3 e = worldBounds.extents;
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int xi = -1; xi <= 1; xi += 2)
                for (int yi = -1; yi <= 1; yi += 2)
                    for (int zi = -1; zi <= 1; zi += 2)
                    {
                        Vector3 worldCorner = c + Vector3.Scale(e, new Vector3(xi, yi, zi));
                        Vector3 localCorner = reference.InverseTransformPoint(worldCorner);
                        min = Vector3.Min(min, localCorner);
                        max = Vector3.Max(max, localCorner);
                    }

            Bounds result = default;
            result.SetMinMax(min, max);
            return result;
        }

        /// <summary>
        /// 收集機櫃裡所有能容納 heightU、且不與 rack.container 中任何 Deployed 設備
        /// 的 U 區段重疊、也不超出 rack.u_height_Max 的候選起始 U 索引，隨機挑一個回傳。
        /// 【改動說明】原本是由 U=1 往上找到第一個能放的位置就直接用 (First-Fit)，
        /// 導致只要下面有空間，設備永遠優先塞在最底部，視覺上會擠成一團。
        /// 改成先收集所有候選位置再隨機挑選，同樣保證不重疊，但分佈會均勻散開。
        /// 依然沿用既有的 UsageCaculatorOfRack.IsRackUCanFit 靜態檢查，
        /// 不另外自建一份佔用區段快取，確保與其餘系統的判斷邏輯永遠一致。
        /// </summary>
        private bool TryFindFreeUIndex(DCR_Asset rack, int heightU, out int uIndex)
        {
            candidateUIndexBuffer.Clear();
            for (int candidate = 1; candidate + heightU - 1 <= rack.u_height_Max; candidate++)
            {
                if (UsageCaculatorOfRack.IsRackUCanFit(rack, candidate, heightU))
                    candidateUIndexBuffer.Add(candidate);
            }

            if (candidateUIndexBuffer.Count == 0)
            {
                uIndex = 0;
                return false;
            }

            uIndex = candidateUIndexBuffer[Random.Range(0, candidateUIndexBuffer.Count)];
            return true;
        }
        /// <summary>複用的暫存清單，避免 TryFindFreeUIndex 每次呼叫都重新配置。</summary>
        private readonly List<int> candidateUIndexBuffer = new();

        /// <summary>
        /// 複製範本設備資料成獨立副本，避免多個放置位置共用同一個 EquipmentAsset
        /// 物件而互相覆蓋 startUIndex / deploymentStatus / modelInfo.modelTarget。
        /// modelInfo 不在這裡複製，交給 EquipmentComponent.SetData() 設定成新模型的 Transform。
        /// 財產編號重新產生，避免同一範本被重複抽中時，畫面/清單上出現一模一樣的編號。
        /// </summary>
        private static EquipmentAsset CloneEquipmentAsset(EquipmentAsset template)
        {
            var clone = new EquipmentAsset
            {
                deviceCode = template.deviceCode,
                category = template.category,
                equipmentUsageInfo = template.equipmentUsageInfo, // struct，直接複製值
                sizeInfo = new SizeInfo
                {
                    width_mm = template.sizeInfo.width_mm,
                    height_mm = template.sizeInfo.height_mm,
                    depth_mm = template.sizeInfo.depth_mm,
                },
                companyPropertyInfo = new CompanyPropertyInfo
                {
                    propertyName = template.companyPropertyInfo.propertyName,
                    note = template.companyPropertyInfo.note,
                },
            };
            clone.companyPropertyInfo.GenerateRandomPropertyNo();
            return clone;
        }
        #endregion

        [Button, ShowIf("isHaveData")]
        private void ClearData()
        {
            equipmentAssets.Clear();
            rackComponents.Clear();
            createEquipmentAssets.Clear();
            dataReadyCount = 0;
        }

        /// <summary>
        /// 把目前 rackComponents 裡每個機櫃的 DCR_Asset（含這次 Demo 生成、放進 container
        /// 裡的 EquipmentAsset）收集起來，轉成 DCR_Asset_DTO 後存成 json 檔。
        /// 直接沿用 RackAssetJsonConverter.ExportToJsonFile，跟 WebAPI 那邊
        /// 用同一套 DTO 格式，方便之後拿去餵給後端或給其他工具驗證資料正確性。
        /// </summary>
        [Button, ShowIf("isHaveRacks")]
        private void ExportRacksToJson()
        {
            List<DCR_Asset> racks = new List<DCR_Asset>();
            for (int i = 0; i < rackComponents.Count; i++)
            {
                if (rackComponents[i] == null) continue;
                if (rackComponents[i].GetAsset() is DCR_Asset rack) racks.Add(rack);
            }

            if (racks.Count == 0)
            {
                Debug.LogWarning($"[{nameof(DemoGenerator_EquipmentToRackContainer)}] 沒有可匯出的機櫃資料。");
                return;
            }

            string fullPath = System.IO.Path.Combine(Application.dataPath, "..", exportJsonPath);
            RackAssetJsonConverter.ExportToJsonFile(racks, fullPath);
        }
    }
}
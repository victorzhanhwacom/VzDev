using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.DebugUtils;
using VzDev.RenderingUtils.Staging;

namespace VzDev.DCIMUtils.Deployment
{
    /// <summary>
    /// 監聽 DeployEquipmentList 的選取結果，生成待部署設備的預覽模型，
    /// 並讓它跟隨滑鼠在一個固定高度的水平面上移動，作為部署前的視覺預覽。
    ///
    /// 刻意不用 Physics.Raycast 打地板 Collider：
    /// 固定高度水平面用純數學求交點，不依賴場景是否正確設置地板 Collider。
    ///
    /// 【材質策略修正】不直接替換模型本身的材質（原模型可能有多個 Renderer/
    /// 多個材質 slot，逐一替換容易漏掉或處理複雜），改成在模型底下加一個
    /// 依整體 Bounds 縮放的外框子物件，套用 StagingBoundingBox Shader
    /// 顯示合適/不合適的顏色狀態。外框大小只依賴模型整體 Bounds，
    /// 完全不受模型內部材質數量影響。
    /// </summary>
    public class DeployEquipmentPlacementController : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private Camera mainCamera;
        [Foldout("[Settings]"), SerializeField, Tooltip("預覽模型跟隨滑鼠時所在的固定世界高度(Y)")]
        private float placementHeight = 1.9f;

        [Foldout("[Settings]"), SerializeField, Tooltip("ray.direction.y 小於此絕對值時，視為幾乎平視，不更新位置避免模型飛到極遠處")]
        private float minRayDirectionY = 0.001f;

        /// <summary>
        /// 目前選擇上架的庫存設備
        /// </summary>
        private EquipmentAsset currentAsset;

        [Foldout("[Renderer]"), SerializeField, Required] private Shader stagingBoundingBoxShader;
        [Foldout("[Renderer]"), SerializeField, ColorUsage(true, true)] private Color previewColor_Deployable = new Color(0f, 1f, 0f);
        [Foldout("[Renderer]"), SerializeField, ColorUsage(true, true)] private Color previewColor_NotDeployable = new Color(1f, 0f, 0f);
        [Foldout("[Renderer]"), SerializeField, Range(0.001f, 0.2f), Tooltip("外框比實際 Bounds 稍微放大的比例，避免緊貼模型表面")]
        private float boundsPadding = 0.001f;

        private Transform previewInstance;
        private Material boxMaterial;
        private Mesh unitCubeMesh;

        private const string BoxObjectName = "__PlacementPreviewBox";

        private Vector3 previewLocalBoundsCenter;
        private Vector3 previewLocalBoundsSize; // 對齊前面時要用到深度 (z)
        private bool isSnapped;

        [Foldout("[Snap]"), SerializeField, Tooltip("機櫃模型的 local +Z 是否代表機櫃敞開的正面。若對齊方向反了，切換這個值")]
        private bool rackForwardIsPositiveZ = false;

        [Foldout("[Snap]"), SerializeField, Tooltip("設備模型的 local +Z 是否代表設備的正面。若設備裝反了，切換這個值")]
        private bool deviceForwardIsPositiveZ = false;
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            EquipmentStockList.OnEquipmentSelected += HandleSelected;
            EquipmentStockList.OnEquipmentDeselected += HandleDeselected;
            RackUSlotHoverDetector.OnRackUSlotChanged += HandleRackUSlotChanged;
        }

        private void OnDisable()
        {
            EquipmentStockList.OnEquipmentSelected -= HandleSelected;
            EquipmentStockList.OnEquipmentDeselected -= HandleDeselected;
            RackUSlotHoverDetector.OnRackUSlotChanged -= HandleRackUSlotChanged;
            ClearPreview();
        }

        private void OnDestroy()
        {
            if (boxMaterial != null) Destroy(boxMaterial);
            if (unitCubeMesh != null) Destroy(unitCubeMesh);
        }

        private void Update()
        {
            if (previewInstance == null || isSnapped) return; // 已對齊時，位置改由 SnapToSlot 決定，不再跟隨滑鼠

            if (TryGetPlacementPoint(out Vector3 point))
                previewInstance.position = point;
        }

        /// <summary>
        /// 沒有正在放置任何設備時（currentAsset/previewInstance 為 null），
        /// RackUSlotHoverDetector 只要偵測到機櫃就會持續廣播，這裡直接擋掉，
        /// 避免每次滑鼠移到任何機櫃上都白白算一次 CanFit / 換色。
        /// </summary>
        private void HandleRackUSlotChanged(DCR_Asset rackAsset, int uIndex, Collider rackCollider)
        {
            if (currentAsset == null || previewInstance == null) return;

            bool isDeployable = CheckCanDeploy(rackAsset, uIndex, currentAsset);
            SetPreviewState(isDeployable);

            if (isDeployable)
            {
                SnapToSlot(rackAsset, uIndex, rackCollider, currentAsset.equipmentUsageInfo.heightU);
                isSnapped = true;
            }
            else if (isSnapped)
            {
                //previewInstance.rotation = Quaternion.identity; // 離開對齊狀態時重置方向，避免殘留機櫃的旋轉角度
                isSnapped = false;
            }
        }

        /// <summary>
        /// 判斷這台設備從 uIndex 這個 U 槽開始往上疊 heightU 格，是否放得下。
        /// </summary>
        private bool CheckCanDeploy(DCR_Asset rackAsset, int uIndex, EquipmentAsset asset)
        {
            if (rackAsset == null || uIndex <= 0 || asset == null) return false;
            return UsageCaculatorOfRack.IsRackUCanFit(rackAsset, uIndex, asset.equipmentUsageInfo.heightU);
        }
        #endregion

        #region Handlers
        private void HandleSelected(EquipmentAsset asset)
        {
            ClearPreview();

            if (asset?.modelInfo?.modelTarget == null)
            {
                Debug.LogWarning($"[{nameof(DeployEquipmentPlacementController)}] 選取的設備沒有設定 modelTarget，無法生成預覽模型");
                return;
            }
            currentAsset = asset;
            onSelectedeEquipmentToDeploy?.Invoke(currentAsset);
            previewInstance = ObjectHelper.Instantiate(currentAsset.modelInfo.modelTarget);
            CreateBoundingBoxChild();
            SetPreviewState(isSuitable: false); // 預設狀態，實際放置合法性判斷邏輯之後再接上動態切換
        }

        private void HandleDeselected()
        {
            ClearPreview();
            currentAsset = null;
        }
        #endregion

        #region Snap To Slot
        /// <summary>
        /// 將預覽模型對齊到指定 U 區段的世界座標中心，方向與機櫃一致，並貼齊機櫃前面。
        ///
        /// 【假設，需與建模端核對】
        /// 1. 機櫃內部可用空間 Collider 相對於機櫃根物件沒有額外的相對旋轉，
        ///    否則 rackCollider.bounds 算出的中心/範圍會跟機櫃根物件實際朝向對不上。
        /// 2. 機櫃僅繞世界 Y 軸旋轉（直立擺放），forward.y 理論上應為 0，
        ///    下方仍強制清零作為防禦，避免美術/匯入誤差造成的輕微傾斜污染計算。
        /// </summary>
        private void SnapToSlot(DCR_Asset rack, int uIndex, Collider rackCollider, int heightU)
        {
            if (previewInstance == null || rack?.modelInfo?.modelTarget == null || rackCollider == null) return;
            if (rack.u_height_Max <= 0) return;

            float uHeightWorld = rackCollider.bounds.size.y / rack.u_height_Max;
            float occupiedBottomY = rackCollider.bounds.min.y + (uIndex - 1) * uHeightWorld;
            float occupiedCenterY = occupiedBottomY + (heightU * uHeightWorld) * 0.5f; // 跨多個 U 時，對齊整個佔用區段的中心

            Quaternion rackRotation = rack.modelInfo.modelTarget.rotation;

            float rackSign = rackForwardIsPositiveZ ? 1f : -1f;
            Vector3 forward = rackRotation * Vector3.forward * rackSign;
            forward.y = 0f;
            forward.Normalize();

            // AABB 沿任意方向的支撐距離公式：對「軸對齊的長方體」在任意方向上都成立，
            // 不要求 forward 剛好對齊世界 X/Z 軸。
            float depthAlongForward =
                Mathf.Abs(forward.x) * rackCollider.bounds.extents.x +
                Mathf.Abs(forward.z) * rackCollider.bounds.extents.z;

            Vector3 rackFrontFaceCenter = new Vector3(
                rackCollider.bounds.center.x,
                occupiedCenterY,
                rackCollider.bounds.center.z) + forward * depthAlongForward;

            float deviceSign = deviceForwardIsPositiveZ ? 1f : -1f;
            // Pivot 不一定在模型幾何中心，反推「設備的前面」相對於 Pivot 的本地偏移量，
            // 才能讓「設備前面」精確貼齊 rackFrontFaceCenter，而不是讓 Pivot 本身對齊過去。
            Vector3 pivotToFrontFaceLocal = previewLocalBoundsCenter + new Vector3(0f, 0f, previewLocalBoundsSize.z * 0.5f * deviceSign);

            previewInstance.rotation = rackRotation;
            previewInstance.position = rackFrontFaceCenter - rackRotation * pivotToFrontFaceLocal;
        }
        #endregion

        #region Bounding Box Child
        /// <summary>
        /// 依模型整體 Renderer Bounds 建立外框子物件，Parent 進 previewInstance 底下。
        /// 因為 previewInstance 是本 Controller 私有的暫時預覽物件，不會被場上其他
        /// 依賴 GetComponentsInChildren&lt;Renderer&gt; 遍歷子物件的系統掃描到，
        /// 不需要像 BoundingBoxHighlightController 那樣刻意避免 Parent 進目標底下。
        /// </summary>
        private void CreateBoundingBoxChild()
        {
            var renderers = previewInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[{nameof(DeployEquipmentPlacementController)}] 預覽模型底下沒有任何 Renderer，無法計算 Bounds", previewInstance);
                return;
            }

            Bounds localBounds = CalculateLocalBounds(previewInstance, renderers);
            previewLocalBoundsCenter = localBounds.center;
            previewLocalBoundsSize = localBounds.size;

            var boxObject = new GameObject(BoxObjectName);
            boxObject.transform.SetParent(previewInstance, worldPositionStays: false);
            boxObject.transform.localPosition = localBounds.center;
            boxObject.transform.localRotation = Quaternion.identity;
            boxObject.transform.localScale = localBounds.size * (1f + boundsPadding);

            unitCubeMesh ??= BoundingBoxMeshBuilder.Build(new Bounds(Vector3.zero, Vector3.one), padding: 0f);
            boxObject.AddComponent<MeshFilter>().sharedMesh = unitCubeMesh;

            boxMaterial ??= new Material(stagingBoundingBoxShader) { name = "PlacementPreviewBox_Material" };
            var renderer = boxObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = boxMaterial;
        }

        /// <summary>
        /// 用世界座標 Renderer.bounds 合併全部 Renderer，再轉回 root 的本地空間。
        /// 近似解：假設模型內部子物件不會有大幅旋轉（多數機櫃/設備模型符合此前提）。
        /// </summary>
        private Bounds CalculateLocalBounds(Transform root, Renderer[] renderers)
        {
            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = root.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = root.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            return new Bounds(localCenter, localSize);
        }

        /// <summary>
        /// 切換合適/不合適的顏色狀態。
        /// </summary>
        public void SetPreviewState(bool isSuitable)
        {
            if (boxMaterial == null) return;
            Color color = isSuitable ? previewColor_Deployable : previewColor_NotDeployable;
            boxMaterial.SetColor("_FillColor", color);
            boxMaterial.SetColor("_EdgeColor", color);
        }
        #endregion

        #region Placement Math
        private bool TryGetPlacementPoint(out Vector3 point)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Mathf.Abs(ray.direction.y) < minRayDirectionY)
            {
                point = default;
                return false;
            }

            float t = (placementHeight - ray.origin.y) / ray.direction.y;
            if (t < 0f)
            {
                point = default;
                return false;
            }

            point = ray.origin + ray.direction * t;
            return true;
        }
        #endregion

        #region Cleanup
        private void ClearPreview()
        {
            if (previewInstance != null) ObjectHelper.Destroy(previewInstance.gameObject);
            previewInstance = null;
            previewLocalBoundsCenter = default;
            previewLocalBoundsSize = default;
            isSnapped = false;
        }
        #endregion

        public static Action<EquipmentAsset> onSelectedeEquipmentToDeploy;
    }
}
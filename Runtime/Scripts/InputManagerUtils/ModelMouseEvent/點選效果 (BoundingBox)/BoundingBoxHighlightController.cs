using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VzDev.RenderingUtils.Outline;

namespace VzDev.RenderingUtils.Staging
{
    /// <summary>
    /// 監聽 HighlightRegistry 的 Hover/Selected 狀態，用物件池 + 單位立方體 Mesh 的方式
    /// 顯示 Bounding Box 外框（Inzoi 風格 Fresnel 玻璃外框）。
    ///
    /// 外框物件永遠掛在本 Controller 自己的 Transform 底下，不會 SetParent 到任何目標模型，
    /// 避免被場景中其他基於「遍歷子物件」邏輯的系統（例如 MaterialReplacer 用
    /// GetComponentsInChildren&lt;Renderer&gt; 收集要換材質的物件）誤判為目標模型的子物件而被處理。
    /// 每幀直接用目標物件的世界座標/旋轉/縮放，透過矩陣運算合成外框應該在的世界位置，
    /// 效果等同「掛在目標底下」，但完全不進入目標的 Hierarchy。
    ///
    /// 效能優化：用 Transform.hasChanged 旗標判斷目標是否真的移動/旋轉/縮放過，
    /// 靜止不動的目標每幀只做一次布林檢查，不重新執行 TransformPoint/矩陣運算。
    ///
    /// Hover 與 Selected 的 Fresnel 參數（Power、Min/Max Alpha）各自獨立設定，
    /// 讓兩種狀態的視覺強度可以拉出明顯差異（例如 Hover 較低調、Selected 較強烈）。
    /// Edge Thickness 維持共用，因為線條粗細通常不需要依狀態區分。
    ///
    /// Inspector 上調整樣式參數時，透過 OnValidate 即時套用到所有已建立的材質實例，
    /// 不需要重新 Play 或重新套用效果就能看到變化（僅限 Editor 環境，WebGL Build 不會觸發）。
    ///
    /// Hover 固定只有 1 個常駐實例；Selected 用固定大小的物件池，數量不足時動態擴充不收縮。
    /// 與既有的 OutlineRendererFeature（螢幕空間輪廓）系統完全獨立、互不影響。
    /// </summary>
    public class BoundingBoxHighlightController : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Shader boundingBoxShader;

        [Header("Hover 外框樣式")]
        [SerializeField, ColorUsage(true, true)] private Color hoverFillColor = new Color(0.4f, 0.9f, 1f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color hoverEdgeColor = new Color(0.6f, 1f, 1f, 1f);
        [SerializeField, Range(0.5f, 8f), Tooltip("Hover 狀態的 Fresnel 強度，數值越低邊緣越不明顯")]
        private float hoverFresnelPower = 5f;
        [SerializeField, Range(0f, 1f)] private float hoverFillMinAlpha = 0.02f;
        [SerializeField, Range(0f, 1f), Tooltip("Hover 狀態掠射角時的最大不透明度，建議調低讓效果較低調")]
        private float hoverFillMaxAlpha = 0.25f;

        [Header("Selected 外框樣式")]
        [SerializeField, ColorUsage(true, true)] private Color selectedFillColor = new Color(1f, 0.65f, 0f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color selectedEdgeColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField, Range(0.5f, 8f), Tooltip("Selected 狀態的 Fresnel 強度")]
        private float selectedFresnelPower = 2.5f;
        [SerializeField, Range(0f, 1f)] private float selectedFillMinAlpha = 0.05f;
        [SerializeField, Range(0f, 1f), Tooltip("Selected 狀態掠射角時的最大不透明度，建議調高讓效果更明顯")]
        private float selectedFillMaxAlpha = 0.6f;

        [Header("共用參數")]
        [SerializeField, Range(0.001f, 0.1f)] private float edgeThickness = 0.015f;
        [SerializeField, Range(0f, 0.2f)] private float boundsPadding = 0.03f;
        [SerializeField, Tooltip("Selected 物件池初始大小")] private int selectedPoolInitialSize = 8;

        private const string HoverName = "__BoundingBoxHover";
        private const string SelectedPoolName = "__BoundingBoxSelectedPool";

        private PoolItem hoverItem;
        private Renderer hoverCurrentTarget;

        private readonly List<PoolItem> selectedPool = new();
        private readonly Dictionary<Renderer, PoolItem> selectedAssignment = new();

        private Mesh unitCubeMesh;

        private bool isInitialized;

        private class PoolItem
        {
            public GameObject go;
            public MeshRenderer renderer;
            public Material material;
            public bool isSelectedStyle;
        }
        #endregion

        #region Lifecycle
        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (isInitialized) return;

            unitCubeMesh = BuildUnitCubeMesh();

            hoverItem = CreatePoolItem(HoverName, isSelectedStyle: false);
            hoverItem.go.SetActive(false);

            for (int i = 0; i < selectedPoolInitialSize; i++)
            {
                var item = CreatePoolItem($"{SelectedPoolName}_{i}", isSelectedStyle: true);
                item.go.SetActive(false);
                selectedPool.Add(item);
            }

            isInitialized = true;
        }

        private void Update()
        {
            SyncSelected();
            SyncHover();
        }

        private void OnDisable()
        {
            if (hoverItem != null) hoverItem.go.SetActive(false);
            foreach (var item in selectedPool) item.go.SetActive(false);
            selectedAssignment.Clear();
            hoverCurrentTarget = null;
        }

        private void OnDestroy()
        {
            if (unitCubeMesh != null) Object.Destroy(unitCubeMesh);
        }

        private void OnValidate()
        {
            if (!isInitialized) return;
            if (hoverItem == null) return;

            ApplyStyle(hoverItem.material, hoverFillColor, hoverEdgeColor,
                hoverFresnelPower, hoverFillMinAlpha, hoverFillMaxAlpha);

            foreach (var item in selectedPool)
            {
                ApplyStyle(item.material, selectedFillColor, selectedEdgeColor,
                    selectedFresnelPower, selectedFillMinAlpha, selectedFillMaxAlpha);
            }
        }
        #endregion

        #region Pool Setup
        private PoolItem CreatePoolItem(string name, bool isSelectedStyle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, worldPositionStays: false);

            go.AddComponent<MeshFilter>().sharedMesh = unitCubeMesh;
            var material = new Material(boundingBoxShader) { name = name + "_Material" };
            var item = new PoolItem { go = go, material = material, isSelectedStyle = isSelectedStyle };

            if (isSelectedStyle)
                ApplyStyle(material, selectedFillColor, selectedEdgeColor,
                    selectedFresnelPower, selectedFillMinAlpha, selectedFillMaxAlpha);
            else
                ApplyStyle(material, hoverFillColor, hoverEdgeColor,
                    hoverFresnelPower, hoverFillMinAlpha, hoverFillMaxAlpha);

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            item.renderer = renderer;

            return item;
        }

        private void ApplyStyle(Material mat, Color fillColor, Color edgeColor,
            float fresnelPower, float fillMinAlpha, float fillMaxAlpha)
        {
            if (mat == null) return;
            mat.SetColor("_FillColor", fillColor);
            mat.SetColor("_EdgeColor", edgeColor);
            mat.SetFloat("_EdgeThickness", edgeThickness);
            mat.SetFloat("_FresnelPower", fresnelPower);
            mat.SetFloat("_FillMinAlpha", fillMinAlpha);
            mat.SetFloat("_FillMaxAlpha", fillMaxAlpha);
        }

        private static Mesh BuildUnitCubeMesh()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            return BoundingBoxMeshBuilder.Build(bounds, padding: 0f);
        }
        #endregion

        #region Sync Logic — Hover
        private void SyncHover()
        {
            var hoverSet = HighlightRegistry.Get(HighlightGroup.Hover);
            Renderer target = null;
            foreach (var r in hoverSet) { target = r; break; }

            if (target != null && HighlightRegistry.Get(HighlightGroup.Selected).Contains(target))
                target = null;

            if (target == null)
            {
                if (hoverItem.go.activeSelf) hoverItem.go.SetActive(false);
                hoverCurrentTarget = null;
                return;
            }

            bool targetSwitched = target != hoverCurrentTarget;
            hoverCurrentTarget = target;

            if (targetSwitched || target.transform.hasChanged)
            {
                FitToTarget(hoverItem, target);
                target.transform.hasChanged = false;
            }

            if (!hoverItem.go.activeSelf) hoverItem.go.SetActive(true);
        }
        #endregion

        #region Sync Logic — Selected
        private void SyncSelected()
        {
            var selectedSet = HighlightRegistry.Get(HighlightGroup.Selected);

            List<Renderer> toRelease = null;
            foreach (var kv in selectedAssignment)
            {
                if (kv.Key == null || !selectedSet.Contains(kv.Key))
                {
                    toRelease ??= new List<Renderer>();
                    toRelease.Add(kv.Key);
                }
            }
            if (toRelease != null)
            {
                foreach (var r in toRelease)
                {
                    selectedAssignment[r].go.SetActive(false);
                    selectedAssignment.Remove(r);
                }
            }

            foreach (var r in selectedSet)
            {
                if (r == null) continue;

                if (!selectedAssignment.TryGetValue(r, out var item))
                {
                    item = GetFreePoolItem();
                    selectedAssignment[r] = item;
                    FitToTarget(item, r);
                    r.transform.hasChanged = false;
                    continue;
                }

                if (r.transform.hasChanged)
                {
                    FitToTarget(item, r);
                    r.transform.hasChanged = false;
                }
            }
        }

        private PoolItem GetFreePoolItem()
        {
            foreach (var item in selectedPool)
            {
                if (!item.go.activeSelf) return item;
            }

            var newItem = CreatePoolItem($"{SelectedPoolName}_{selectedPool.Count}", isSelectedStyle: true);
            selectedPool.Add(newItem);
            return newItem;
        }
        #endregion

        #region Transform Fitting
        private void FitToTarget(PoolItem item, Renderer target)
        {
            if (!target.TryGetComponent<MeshFilter>(out var meshFilter) || meshFilter.sharedMesh == null)
            {
                item.go.SetActive(false);
                return;
            }

            var localBounds = meshFilter.sharedMesh.bounds;
            float pad = 1f + boundsPadding;
            Transform t = target.transform;

            item.go.transform.position = t.TransformPoint(localBounds.center);
            item.go.transform.rotation = t.rotation;
            item.go.transform.localScale = Vector3.Scale(t.lossyScale, localBounds.size * pad);

            item.go.SetActive(true);
        }
        #endregion
    }
}
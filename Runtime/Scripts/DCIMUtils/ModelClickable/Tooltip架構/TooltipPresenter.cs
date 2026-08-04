using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev.UIUtils.Tooltip
{
    /// <summary>
    /// 純呈現層的「殼」：只負責淡入淡出、跟隨滑鼠位置、Canvas邊界Clamp、
    /// 以及「依資料別切換內容」這件事本身——完全不涉及任何資料別的排版細節。
    ///
    /// 內容排版交給各資料別各自實作的 ITooltipContentView Prefab（見 RackTooltipContentView、
    /// ChillerTooltipContentView 等），此類別只負責「載入哪一個、塞進 contentContainer」。
    ///
    /// 由 ModelTooltipController 透過 UnityEvent 呼叫 Show/Hide，兩者完全解耦，
    /// 邏輯層（Hover判斷/計時）與呈現層（UI排版/動畫）互不依賴。
    ///
    /// 掛載位置：需放在 Screen Space - Overlay 或 Screen Space - Camera 的 Canvas 底下，
    /// 本身建議額外掛一個獨立的 Canvas 元件（巢狀 Canvas），Sort Order 設高於主選單，
    /// 避免每帧跟隨滑鼠的位置變動拖著主選單 Canvas 一起 Rebuild。
    /// root 的 Pivot/Anchor 建議設為 (0,1)（左上角錨點），搭配 cursorOffset 往右下偏移，
    /// 避免 Tooltip 遮住滑鼠正下方的目標物件。
    ///
    /// 前提：root 的父物件（本 GameObject 的 RectTransform）必須完全撐滿並對齊 Canvas
    /// （Anchor Min (0,0) / Max (1,1) / 四邊 offset 皆為 0），
    /// 否則座標轉換算出的「相對於 canvasRect」座標，跟 root.anchoredPosition
    /// 實際「相對於直接父物件」的語意會對不上，導致視覺位置被二次偏移。
    /// </summary>
    public class TooltipPresenter : MonoBehaviour
    {
        #region Content Mapping — Inspector 設定資料別對應的排版 Prefab
        /// <summary>
        /// 資料別 → 內容 Prefab 對應表。新增一種資料別的 Tooltip 排版，
        /// 只需要在 Inspector 多拉一筆對應，不需要修改此類別任何一行程式碼。
        /// </summary>
        [System.Serializable]
        private class TooltipContentMapping
        {
            [SerializeField, Tooltip("對應 DCIMAsset 子類別名稱，例如 DCR_Asset、AcSystem_Asset…")]
            private string assetTypeName;
            [SerializeField, Tooltip("該資料別要使用的內容排版 Prefab，需掛載 ITooltipContentView 的實作")]
            private GameObject contentPrefab;

            public string AssetTypeName => assetTypeName;
            public GameObject ContentPrefab => contentPrefab;
        }
        #endregion

        #region Fields
        [SerializeField] private List<TooltipContentMapping> contentMappings = new();
        [Foldout("[Components]"), SerializeField] private RectTransform root;
        [Foldout("[Components]"), SerializeField] private CanvasGroup canvasGroup;
        [Foldout("[Components]"), Tooltip("內容Prefab實例化後放入的容器"), SerializeField]
        private RectTransform contentContainer;

        [Foldout("[Content Mapping]"), Tooltip("找不到對應資料別，或目標模型沒有DCIMAsset時使用的純文字內容Prefab")]
        [SerializeField] private GameObject fallbackTextPrefab;

        [Foldout("[Settings]"), SerializeField] private Vector2 cursorOffset = new(16f, -16f);
        [Foldout("[Settings]"), SerializeField, Range(0.05f, 0.5f)] private float fadeDuration = 0.15f;

        private readonly Dictionary<string, GameObject> mappingLookup = new();

        /// <summary>
        /// 依 Prefab 快取已實例化過的內容物件，避免每次 Show() 都 Instantiate/Destroy，
        /// 對「數量很多、Hover 頻繁切換不同資料別」的場景比較友善。
        /// </summary>
        private readonly Dictionary<GameObject, GameObject> instancePool = new();

        private GameObject activeContentInstance;

        private RectTransform canvasRect;
        private Canvas parentCanvas;
        private bool isVisible;
        #endregion

        #region Lifecycle
        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;

            RefreshMappingLookup();

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false; // Tooltip永遠不吃滑鼠事件，避免擋到底下的3D Raycast判定
            root.gameObject.SetActive(false);
        }

        [Button]
        private void RefreshMappingLookup()
        {
            mappingLookup.Clear();
            foreach (var mapping in contentMappings)
            {
                if (string.IsNullOrEmpty(mapping.AssetTypeName) || mapping.ContentPrefab == null) continue;
                mappingLookup[mapping.AssetTypeName] = mapping.ContentPrefab;
            }
        }

        private void Update()
        {
            if (!isVisible) return;
            UpdatePosition(Input.mousePosition);
        }

        private void OnDestroy() => canvasGroup.DOKill();
        #endregion

        #region Public API — 供 ModelTooltipController 的 UnityEvent 連線
        /// <summary>
        /// asset 不為 null：依其實際型別名稱查找對應的內容 Prefab，找不到則用 fallback。
        /// asset 為 null：一律使用 fallbackTextPrefab，並傳入 fallbackName 顯示。
        /// </summary>
        public void Show(DCIMAsset asset, string fallbackName)
        {

            GameObject prefab = ResolveContentPrefab(asset);
            SwapContent(prefab, asset, fallbackName);

            // 必須先讓 root 進入 active 狀態，Unity 的 Canvas 更新系統
            // 會直接跳過整個 inactive 的 Hierarchy，不管後面怎麼強制呼叫
            // Canvas.ForceUpdateCanvases()/LayoutRebuilder 都是空操作。
            // 這裡先 SetActive 不會造成視覺上的閃爍/跳動：canvasGroup.alpha
            // 目前仍是 0（Awake 或上一次 Hide 淡出完畢後的狀態），
            // 物件即使已經 active、位置還沒算好，畫面上也完全看不見。
            root.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            UpdatePosition(Input.mousePosition);
            isVisible = true;

            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, fadeDuration);
        }

        public void Hide()
        {
            isVisible = false;

            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration)
                .OnComplete(() => root.gameObject.SetActive(false));
        }
        #endregion

        #region Content Resolving & Swapping
        private GameObject ResolveContentPrefab(DCIMAsset asset)
        {
            if (asset != null && mappingLookup.TryGetValue(asset.GetType().Name, out var prefab))
                return prefab;

            return fallbackTextPrefab;
        }

        /// <summary>
        /// 切換內容時優先使用快取實例：把舊內容隱藏、換一個新內容顯示出來並呼叫 Bind，
        /// 同一種內容 Prefab 不會被重複 Instantiate/Destroy。
        /// </summary>
        private void SwapContent(GameObject prefab, DCIMAsset asset, string fallbackName)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 找不到可用的內容 Prefab（沒有對應資料別也沒有設定 fallbackTextPrefab）", this);
                return;
            }

            if (activeContentInstance != null)
                activeContentInstance.SetActive(false);

            if (!instancePool.TryGetValue(prefab, out var instance))
            {
                instance = Instantiate(prefab, contentContainer);
                instancePool[prefab] = instance;
            }

            instance.SetActive(true);
            activeContentInstance = instance;

            if (instance.TryGetComponent<ITooltipContentView>(out var contentView))
                contentView.Bind(asset, fallbackName);
        }
        #endregion

        #region Position Follow
        private void UpdatePosition(Vector3 screenPosition)
        {
            if (canvasRect == null) return;

            // 不論 Overlay 或 Camera 模式，統一用同一套轉換：
            // ScreenSpaceOverlay 底下 worldCamera 傳 null 是官方建議的正確用法。
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : parentCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, cam, out Vector2 anchoredPos);

            root.anchoredPosition = anchoredPos + cursorOffset;
            ClampToCanvas();
        }

        /// <summary>
        /// 避免 Tooltip 在畫面邊緣（尤其4K/FHD不同解析度下）被裁切到看不到內容。
        /// 假設 Canvas Pivot 在中心 (0.5, 0.5)，這是 Unity UI 預設值。
        /// </summary>
        private void ClampToCanvas()
        {
            if (canvasRect == null) return;

            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 size = root.rect.size;
            Vector2 pos = root.anchoredPosition;

            float minX = -canvasSize.x * 0.5f;
            float maxX = canvasSize.x * 0.5f - size.x;
            float minY = -canvasSize.y * 0.5f + size.y;
            float maxY = canvasSize.y * 0.5f;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            root.anchoredPosition = pos;
        }
        #endregion
    }
}
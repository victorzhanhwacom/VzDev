using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.UI
{
    /// <summary>
    /// Tooltip 顯示管理器（單例）。
    /// 掛在 Tooltip 面板的 Prefab 上，該面板需常駐 Canvas 底下（建議放最上層 sorting，
    /// 保持 GameObject Active，僅透過 CanvasGroup.alpha 控制顯隱，
    /// 這樣 RectTransform 的尺寸在顯示前就能透過 Layout 正確算出，方便做邊界夾取。
    /// </summary>
    [DisallowMultipleComponent]
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [Tooltip("Tooltip所屬Canvas的RectTransform，用來做邊界夾取與座標轉換")]
        [SerializeField] private RectTransform canvasRect;
        [Tooltip("Canvas Render Mode為Screen Space - Camera或World Space時需指定；Overlay模式留空即可")]
        [SerializeField] private Camera uiCamera;

        [Header("Behaviour")]
        [SerializeField] private Vector2 cursorOffset = new Vector2(16f, -16f);
        [SerializeField] private float fadeDuration = 0.1f;
        [SerializeField] private float screenEdgePadding = 8f;

        private bool _isVisible;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            // 只有顯示中才需要每幀更新位置，隱藏時直接跳過，避免無謂開銷
            if (!_isVisible) return;
            UpdatePosition();
        }

        public void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            label.text = text;
            _isVisible = true;

            // 先強制重建版面配置，確保這一幀就能拿到正確的panel尺寸
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            UpdatePosition();

            StartFade(1f);
        }

        public void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;
            StartFade(0f);
        }

        /// <summary>供動態內容使用，例如DCIM即時資料驅動的tooltip文字</summary>
        public void SetText(string text)
        {
            if (!_isVisible) return;
            label.text = text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            UpdatePosition();
        }

        private void StartFade(float targetAlpha)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            float start = canvasGroup.alpha;
            float t = 0f;

            if (fadeDuration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }

        private void UpdatePosition()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, uiCamera, out Vector2 localPoint);

            localPoint += cursorOffset / canvasRect.localScale.x;

            Vector2 panelSize = panelRect.rect.size;
            Vector2 canvasSize = canvasRect.rect.size;

            // 以canvasRect的pivot為基準做邊界夾取（假設pivot為0.5,0.5，最常見情況）
            float minX = -canvasSize.x * 0.5f + screenEdgePadding;
            float maxX = canvasSize.x * 0.5f - panelSize.x - screenEdgePadding;
            float maxY = canvasSize.y * 0.5f - screenEdgePadding;
            float minY = -canvasSize.y * 0.5f + panelSize.y + screenEdgePadding;

            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

            panelRect.anchoredPosition = localPoint;
        }
    }
}

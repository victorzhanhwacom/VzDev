using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DCIM.RevitAssetDataStructure;
using VzDev.DCIMUtils.ModelInteractUtils;
using VzDev.UnityAPI.Extensions;
using System.Linq;

namespace VzDev.InteractiveUtils.ModelMouseEvent
{
    /// <summary>
    /// 純邏輯層：監聽 ColliderInteractionSystem 的 Hover 事件，累計 Hover 時間，
    /// 達到 hoverDelay 秒後才廣播顯示 Tooltip；離開/切換目標/點擊/失焦時立即隱藏。
    ///
    /// 【職責邊界】這裡只負責判斷「目標模型有沒有 DCIMAsset 資料」，
    /// 完全不決定「資料要顯示哪些欄位、排版長什麼樣子」——那是 TooltipPresenter 的職責。
    /// 有資料就把整個 DCIMAsset 丟給 View 自行取用；沒有資料才提供 fallback 名稱字串。
    /// </summary>
    public class ModelTooltipController : MonoBehaviour
    {
        #region Fields
        [Foldout("[Settings]"), SerializeField, Range(0.1f, 5f), Tooltip("Hover累計時間達到此秒數後才顯示Tooltip")]
        private float hoverDelay = 1f;

        /// <summary>
        /// 有資料時傳入 DCIMAsset，View 自行決定顯示哪些欄位；
        /// 沒有資料時第一個參數為 null，第二個參數是 fallback 名稱，View 應直接顯示它。
        /// </summary>
        [Foldout("[Events]"), Tooltip("達到延遲秒數後觸發")]
        public UnityEvent<DCIMAsset, string> OnShowTooltip;

        [Foldout("[Events]"), Tooltip("需要隱藏Tooltip時觸發（離開/切換/點擊/失焦）")]
        public UnityEvent OnHideTooltip;

        private GameObject currentTarget;
        private float hoverTimer;
        private bool isShown;
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            ColliderInteractionSystem.OnMouseEnter += HandleEnter;
            ColliderInteractionSystem.OnMouseExit += HandleExit;
            ColliderInteractionSystem.OnMouseClick += HandleClick;
        }

        private void OnDisable()
        {
            ColliderInteractionSystem.OnMouseEnter -= HandleEnter;
            ColliderInteractionSystem.OnMouseExit -= HandleExit;
            ColliderInteractionSystem.OnMouseClick -= HandleClick;
            ResetState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ResetState();
        }

        private void Update()
        {
            if (currentTarget == null || isShown) return;

            hoverTimer += Time.unscaledDeltaTime;
            if (hoverTimer < hoverDelay) return;

            (var asset, string fallbackName) = ResolveTargetData(currentTarget);
            OnShowTooltip?.Invoke(asset, fallbackName);
            isShown = true;
        }
        #endregion

        #region Handlers
        private void HandleEnter(GameObject go)
        {
            currentTarget = go;
            hoverTimer = 0f;
            if (isShown)
            {
                OnHideTooltip?.Invoke();
                isShown = false;
            }
        }

        private void HandleExit(GameObject go) => ResetState();

        private void HandleClick(GameObject go)
        {
            if (!isShown) return;
            OnHideTooltip?.Invoke();
            isShown = false;
        }

        private void ResetState()
        {
            currentTarget = null;
            hoverTimer = 0f;
            if (isShown) OnHideTooltip?.Invoke();
            isShown = false;
        }
        #endregion

        #region Data Resolving
        /// <summary>
        /// 只判斷「有沒有資料」，不涉及任何顯示邏輯：
        /// 兩種情況都會回傳有效的 fallbackName，不會是 null——
        /// 因為 View 端的 contentMappings 有沒有對應到某個資料別的 Prefab，
        /// 是執行期才知道的事，Controller 沒有能力（也不該）預先判斷「這次一定會顯示成功」。
        /// 若最終真的落到 fallbackTextPrefab（asset為null，或asset型別沒有對應mapping），
        /// fallbackName 永遠是可用的，不會讓畫面空白。
        ///
        /// 有 asset 時，fallbackName 優先取 assetInfo.assetName，理由：
        /// 如果之後 View 端因為 mapping 漏設定而 fallback，顯示「資產名稱」
        /// 遠比顯示「原始 GameObject 命名字串」對使用者更有意義。
        /// </summary>
        private (DCIMAsset asset, string fallbackName) ResolveTargetData(GameObject target)
        {
            if (target.TryGetComponent<IHasDCIMAsset>(out var provider))
            {
                var asset = provider.GetAsset();
                if (asset != null)
                {
                    string nameFromAsset = !string.IsNullOrEmpty(asset.companyPropertyInfo?.propertyName)
                        ? asset.companyPropertyInfo.propertyName
                        : ResolveFallbackNameFromGameObject(target.name);

                    return (asset, nameFromAsset);
                }
            }

            return (null, ResolveFallbackNameFromGameObject(target.name));
        }

        /// <summary>
        /// 與 ModelComponentSetterBase.AssignDataToComponent 相同的命名解析慣例：
        /// 先取中括號內字串，再取冒號分隔後的最後一段，無法解析則安全 fallback 回原始名稱。
        /// </summary>
        private string ResolveFallbackNameFromGameObject(string rawName)
        {
            string bracketContent = rawName.GetStringBetweenMarks("[", "]");
            if (string.IsNullOrEmpty(bracketContent)) return rawName;

            string lastSegment = bracketContent.Split(':').LastOrDefault();
            return string.IsNullOrEmpty(lastSegment) ? rawName : lastSegment;
        }
        #endregion
    }
}
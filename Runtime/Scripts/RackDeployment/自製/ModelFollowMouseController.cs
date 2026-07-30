using UnityEngine;
using UnityEngine.EventSystems;
using NaughtyAttributes;
using VzDev.Helpers;
using VzDev.DebugUtils;

namespace VzDev.InteractiveUtils.ModelPlacement
{
    /// <summary>
    /// 讓指定的模型 Prefab 產生一個「跟隨滑鼠」的預覽實例，用於場景上放置模型
    /// （例如：新增機櫃/設備時，先顯示跟著滑鼠移動的預覽，左鍵確認放置，右鍵/ESC取消）。
    ///
    /// 移動方式：從攝影機對滑鼠位置打 Raycast，命中 groundLayer（地面/樓層Mesh）才更新
    /// 預覽物件位置；沒命中時維持最後一次有效位置，不會用任意深度亂算世界座標造成亂飄。
    ///
    /// 與 ColliderInteractionSystem 的判定完全獨立：那裡打在 interactableLayer 上判斷
    /// 「Hover/Click 到哪個既有模型」，這裡打在 groundLayer 上判斷「預覽要放在哪裡」，
    /// 兩者 Layer 務必分開設置，否則預覽物件的 Collider 會互相干擾兩套 Raycast。
    /// </summary>
    public class ModelFollowMouseController : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private Camera mainCamera;

        [Foldout("[Settings]"), SerializeField, Tooltip("預覽用的模型Prefab")]
        private GameObject previewPrefab;

        [Foldout("[Settings]"), SerializeField, Tooltip("判定地面/樓層的Layer，決定預覽物件要放在哪裡")]
        private LayerMask groundLayer;

        [Foldout("[Settings]"), SerializeField] private float maxDistance = 200f;

        [Foldout("[Settings]"), SerializeField, Tooltip("放置確認後是否自動結束預覽模式；" +
            "若要連續放置多個同類模型（不結束），關閉此選項")]
        private bool endAfterPlace = true;

        [SerializeField, ReadOnly] private bool isActive;
        private GameObject previewInstance;
        #endregion

        #region Events 供外部（例如正式生成掛資料的模型）訂閱
        public static event System.Action<GameObject, Vector3> OnPlaced; // (來源Prefab, 放置世界座標)
        public static event System.Action OnPlacementCancelled;
        #endregion

        private void OnValidate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }

        public void CreateAndBeginPlacement(GameObject prefab)
        {
            if(previewPrefab != null && previewPrefab != prefab)
            {
                ObjectHelper.Destroy(previewInstance);
            }
            previewPrefab = prefab;
            BeginPlacement();
        }

        #region Public API
        /// <summary>
        /// 開始跟隨滑鼠預覽模式。若已在預覽中，先取消目前的再重新開始，避免殘留兩個實例。
        /// </summary>
        [Button]
        public void BeginPlacement()
        {
            if (isActive) CancelPlacement();
            if (previewPrefab == null) return;

            previewInstance = Instantiate(previewPrefab);
            SetPreviewCollidersEnabled(false); // 避免預覽物件擋到自己的地面 Raycast / 既有模型的互動 Raycast
            isActive = true;
        }

        public void CancelPlacement()
        {
            if (previewInstance != null) ObjectHelper.Destroy(previewInstance);
            previewInstance = null;
            isActive = false;
            OnPlacementCancelled?.Invoke();
        }
        #endregion

        private void Update()
        {
            if (!isActive || previewInstance == null) return;

            // 與 ColliderInteractionSystem 相同的失焦/離開視窗守衛，WebGL下靠JS事件驅動 IsPointerOutOfScreen
            if (!Application.isFocused || MouseHelper.IsPointerOutOfScreen) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return; // 滑鼠在UI上時不更新位置，避免預覽物件穿到UI後方對應的3D座標

            UpdatePreviewPosition();

            if (Input.GetMouseButtonDown(0))
                ConfirmPlacement();
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                CancelPlacement();
        }

        private void UpdatePreviewPosition()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundLayer))
                previewInstance.transform.position = hit.point;
            // 沒打到地面：刻意不更新，維持最後一次有效位置
        }

        private void ConfirmPlacement()
        {
            Vector3 placedPosition = previewInstance.transform.position;
            OnPlaced?.Invoke(previewPrefab, placedPosition);

            if (!endAfterPlace) return; // 連續放置模式：保留這個「幽靈預覽」繼續跟隨滑鼠

            ObjectHelper.Destroy(previewInstance);
            previewInstance = null;
            isActive = false;
        }

        private void SetPreviewCollidersEnabled(bool enabled)
        {
            var colliders = previewInstance.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) c.enabled = enabled;
        }
    }
}
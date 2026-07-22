using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VzDev.CameraUtils
{
    /// <summary>
    /// 場景攝影機控制器（Pivot + 球座標模式）。
    /// 左鍵拖曳：沿世界水平面平移
    /// 右鍵拖曳：環繞旋轉（Orbit），按下瞬間以鼠標位置校準深度作為錨點
    /// 滾輪：動量式拉近/拉遠，以鼠標指向的世界座標點為方向偏移 pivot，抵達極限距離後轉為 Dolly
    /// WASD：水平移動；Q/E：垂直移動
    /// FocusOnTarget()：供外部呼叫，運鏡到指定 Transform
    /// </summary>
    public class CameraOrbitController : MonoBehaviour
    {
        #region Fields
        [Foldout("[Components]"), SerializeField] private Camera targetCamera;
        [Foldout("[Components]"), SerializeField, Tooltip("環繞/平移的軸心點，未指定時執行期間自動建立")]
        private Transform pivot;

        [Foldout("[Settings-Orbit]"), SerializeField] private float orbitSpeed = 3f;
        [Foldout("[Settings-Orbit]"), SerializeField] private float minPitch = -80f;
        [Foldout("[Settings-Orbit]"), SerializeField] private float maxPitch = 80f;
        [Foldout("[Settings-Orbit]"), SerializeField, Tooltip("數值越小追趕越快、越硬；越大越慢、越有慣性感")]
        private float orbitSmoothTime = 0.1f;

        [Foldout("[Settings-Pan]"), SerializeField] private float panSpeed = 1f;
        [Foldout("[Settings-Pan]"), SerializeField, Tooltip("數值越小追趕越快、越硬；越大越慢、越有慣性感")]
        private float panSmoothTime = 0.15f;
        [Foldout("[Settings-Pan]"), SerializeField, Tooltip("WASD水平移動、QE垂直移動的速度(單位/秒)")]
        private float keyboardMoveSpeed = 8f;

        [Foldout("[Settings-Zoom]"), SerializeField, Tooltip("滾輪每單位輸入轉換成的速度量")]
        private float zoomImpulse = 20f;
        [Foldout("[Settings-Zoom]"), SerializeField, Tooltip("速度衰減係數，越大停下越快、越硬；越小滑行越久")]
        private float zoomDamping = 8f;
        [Foldout("[Settings-Zoom]"), SerializeField] private float minDistance = 2f;
        [Foldout("[Settings-Zoom]"), SerializeField] private float maxDistance = 100f;
        [Foldout("[Settings-Zoom]"), SerializeField,
         Tooltip("抵達最近/最遠距離後，超出的位移量轉換成 Dolly 的比例。0=硬牆，1=完全轉換")]
        private float dollyConversionRatio = 1f;

        [Foldout("[Settings-Focus]"), SerializeField] private float focusDuration = 0.6f;
        [Foldout("[Settings-Focus]"), SerializeField] private float defaultFocusDistance = 10f;
        [Foldout("[Settings-Focus]"), SerializeField] private Ease focusEase = Ease.OutCubic;

        [Foldout("[Settings-Interaction]"), SerializeField,
         Tooltip("左鍵按下瞬間、右鍵校準、滾輪取鼠標世界座標點時共用的判定Layer（建議設為與 ColliderInteractionSystem.interactableLayer 相同）")]
        private LayerMask blockPanLayer;
        [Foldout("[Settings-Interaction]"), SerializeField] private float maxRaycastDistance = 200f;

        [Foldout("[Debug]"), SerializeField, ReadOnly] private float currentDistance;
        [Foldout("[Debug]"), SerializeField, ReadOnly] private float zoomVelocity; // 動量模型：distance/sec
        [Foldout("[Debug]"), SerializeField, ReadOnly] private float yaw;
        [Foldout("[Debug]"), SerializeField, ReadOnly] private float pitch;
        [Foldout("[Debug]"), SerializeField, ReadOnly] private float targetYaw;
        [Foldout("[Debug]"), SerializeField, ReadOnly] private float targetPitch;

        private Vector3 targetPivotPosition;
        private Vector3 panVelocity;
        private float yawVelocity;
        private float pitchVelocity;

        private Vector3 lastMousePosition;
        private bool isPanning;
        private bool isOrbiting;
        private bool isFocusing;

        private Tween pivotMoveTween;
        private Tween distanceTween;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            OnValidate();

            if (pivot == null)
            {
                pivot = new GameObject("CameraPivot(Runtime)").transform;
            }

            InitializeFromCurrentTransform();
        }

        private void OnValidate()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Update()
        {
            if (!Application.isFocused) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                isPanning = false;
                isOrbiting = false;
                return;
            }

            HandleOrbitInput();
            HandlePanInput();
            HandleZoomInput();
            HandleKeyboardMoveInput();
        }

        private void LateUpdate()
        {
            if (!isFocusing)
            {
                pivot.position = Vector3.SmoothDamp(pivot.position, targetPivotPosition, ref panVelocity, panSmoothTime);

                yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, orbitSmoothTime);
                pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, orbitSmoothTime);
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // 防止 SmoothDamp overshoot 造成翻轉

                UpdateZoomMomentum();
            }

            ApplyTransform();
        }

        private void OnDestroy()
        {
            KillFocusTweens();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 依攝影機目前實際的 position/rotation，反推出 pivot 座標與初始 yaw/pitch/distance，
        /// 避免執行瞬間鏡頭跳動。
        /// </summary>
        private void InitializeFromCurrentTransform()
        {
            Vector3 forward = targetCamera.transform.forward;
            currentDistance = Mathf.Clamp(defaultFocusDistance, minDistance, maxDistance);
            pivot.position = targetCamera.transform.position + forward * currentDistance;

            Vector3 offset = targetCamera.transform.position - pivot.position;
            currentDistance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);

            Quaternion lookRot = targetCamera.transform.rotation;
            Vector3 euler = lookRot.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);

            targetPivotPosition = pivot.position;
            targetYaw = yaw;
            targetPitch = pitch;

            ApplyTransform();
        }

        private float NormalizePitch(float rawPitch)
        {
            // Unity euler X 超過90度時會以 (180 - x) 形式表示，換算回 -90~90 區間
            if (rawPitch > 180f) rawPitch -= 360f;
            return Mathf.Clamp(rawPitch, minPitch, maxPitch);
        }
        #endregion

        #region Input Handling
        private void HandleOrbitInput()
        {
            if (Input.GetMouseButtonDown(1))
            {
                KillFocusTweens();
                RecalibrateOrbitPivot();
                isOrbiting = true;
                lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(1) && isOrbiting)
            {
                Vector2 delta = (Vector2)Input.mousePosition - (Vector2)lastMousePosition;

                targetYaw = Mathf.Repeat(targetYaw + delta.x * orbitSpeed * Time.deltaTime, 360f);
                targetPitch = Mathf.Clamp(targetPitch - delta.y * orbitSpeed * Time.deltaTime, minPitch, maxPitch);

                lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(1))
            {
                isOrbiting = false;
            }
        }

        /// <summary>
        /// 右鍵按下瞬間重新校準 pivot 的「深度」（僅深度，不含左右偏移——
        /// 因為此模型要求 pivot 必須落在攝影機正前方軸線上，否則 ApplyTransform 換算的
        /// 攝影機位置會跳動）。用途：消除滾輪 Dolly 造成的 pivot 漂移，
        /// 讓 Orbit 半徑對齊「目前實際看到的場景深度」，使近/遠距離操作手感一致。
        /// </summary>
        private void RecalibrateOrbitPivot()
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            float newDistance = currentDistance;

            if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, blockPanLayer))
            {
                newDistance = Vector3.Dot(hit.point - targetCamera.transform.position, targetCamera.transform.forward);
            }

            newDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);

            Vector3 newPivot = targetCamera.transform.position + targetCamera.transform.forward * newDistance;

            pivot.position = newPivot;
            targetPivotPosition = newPivot;
            currentDistance = newDistance;
        }

        private void HandlePanInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverBlockingObject())
                {
                    isPanning = false;
                }
                else
                {
                    KillFocusTweens();
                    isPanning = true;
                    lastMousePosition = Input.mousePosition;
                }
            }

            if (Input.GetMouseButton(0) && isPanning)
            {
                Vector2 delta = (Vector2)Input.mousePosition - (Vector2)lastMousePosition;

                Vector3 flatForward = targetCamera.transform.forward;
                flatForward.y = 0f;
                flatForward.Normalize();

                Vector3 flatRight = targetCamera.transform.right;
                flatRight.y = 0f;
                flatRight.Normalize();

                float scale = currentDistance * panSpeed * 0.002f;
                Vector3 move = (-delta.x * flatRight) + (-delta.y * flatForward);
                move *= scale;

                targetPivotPosition += move;
                lastMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isPanning = false;
            }
        }

        /// <summary>
        /// WASD：沿世界水平面移動；Q/E：沿世界 Y 軸垂直移動。
        /// 直接累加進 targetPivotPosition，交由既有的 SmoothDamp 統一平滑。
        /// </summary>
        private void HandleKeyboardMoveInput()
        {
            Vector3 flatForward = targetCamera.transform.forward;
            flatForward.y = 0f;
            flatForward.Normalize();

            Vector3 flatRight = targetCamera.transform.right;
            flatRight.y = 0f;
            flatRight.Normalize();

            Vector3 moveDir = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) moveDir += flatForward;
            if (Input.GetKey(KeyCode.S)) moveDir -= flatForward;
            if (Input.GetKey(KeyCode.D)) moveDir += flatRight;
            if (Input.GetKey(KeyCode.A)) moveDir -= flatRight;
            if (Input.GetKey(KeyCode.E)) moveDir += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) moveDir -= Vector3.up;

            if (moveDir == Vector3.zero) return;

            KillFocusTweens();
            targetPivotPosition += moveDir.normalized * keyboardMoveSpeed * Time.deltaTime;
        }

        /// <summary>
        /// 滾輪只負責「灌入速度」，不直接改變距離，避免離散輸入造成跳點頓感。
        /// </summary>
        private void HandleZoomInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;

            KillFocusTweens();
            zoomVelocity -= scroll * zoomImpulse;
        }

        /// <summary>
        /// 動量式縮放：速度每帧指數衰減，用速度積分距離。
        /// 拉近/拉遠時，讓 pivot 依「本帧位移量佔目前距離的比例」朝鼠標指向的世界座標點偏移，
        /// 使縮放方向跟隨鼠標指向而非固定朝畫面正中央。抵達 min/maxDistance 後轉為 Dolly。
        /// </summary>
        private void UpdateZoomMomentum()
        {
            if (Mathf.Approximately(zoomVelocity, 0f)) return;

            zoomVelocity *= Mathf.Exp(-zoomDamping * Time.deltaTime);
            if (Mathf.Abs(zoomVelocity) < 0.001f)
            {
                zoomVelocity = 0f;
                return;
            }

            float delta = zoomVelocity * Time.deltaTime; // 負值=拉近, 正值=拉遠
            float proposed = currentDistance + delta;

            if (TryGetCursorWorldPoint(out Vector3 cursorPoint))
            {
                float driftFraction = Mathf.Clamp01(Mathf.Abs(delta) / Mathf.Max(currentDistance, 0.001f));
                targetPivotPosition = Vector3.Lerp(targetPivotPosition, cursorPoint, driftFraction);
            }

            if (proposed < minDistance)
            {
                float overflow = minDistance - proposed;
                currentDistance = minDistance;
                DollyPivot(overflow);
            }
            else if (proposed > maxDistance)
            {
                float overflow = proposed - maxDistance;
                currentDistance = maxDistance;
                DollyPivot(-overflow);
            }
            else
            {
                currentDistance = proposed;
            }
        }

        /// <summary>
        /// 取得鼠標指向的世界座標點：優先用 Raycast 命中點；
        /// 沒打到任何物件時（看向天花板/空地），退回用目前距離在該射線上取一點，
        /// 至少維持方向感。denom 過小（視線幾乎與畫面平行）時放棄，避免除以極小值震盪。
        /// </summary>
        private bool TryGetCursorWorldPoint(out Vector3 worldPoint)
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, blockPanLayer))
            {
                worldPoint = hit.point;
                return true;
            }

            float denom = Vector3.Dot(ray.direction, targetCamera.transform.forward);
            if (denom < 0.05f)
            {
                worldPoint = default;
                return false;
            }

            float t = currentDistance / denom;
            worldPoint = ray.origin + ray.direction * t;
            return true;
        }

        /// <summary>
        /// amount > 0：沿視線方向前進（鑽進場景）；amount < 0：沿視線方向後退。
        /// </summary>
        private void DollyPivot(float amount)
        {
            if (Mathf.Approximately(amount, 0f) || dollyConversionRatio <= 0f) return;

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 forward = rotation * Vector3.forward;
            targetPivotPosition += forward * (amount * dollyConversionRatio);
        }

        private bool IsPointerOverBlockingObject()
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out _, maxRaycastDistance, blockPanLayer);
        }
        #endregion

        #region Transform Application
        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = pivot.position + rotation * new Vector3(0f, 0f, -currentDistance);
            targetCamera.transform.SetPositionAndRotation(position, rotation);
        }
        #endregion

        #region Public API
        /// <summary>
        /// 運鏡至指定目標。維持目前 yaw/pitch 視角角度，將 pivot 移動至目標位置並調整距離。
        /// </summary>
        public void FocusOnTarget(Transform target, float? distance = null, float? duration = null)
        {
            if (target == null) return;

            KillFocusTweens();

            float finalDistance = Mathf.Clamp(distance ?? defaultFocusDistance, minDistance, maxDistance);
            float tweenDuration = duration ?? focusDuration;

            isPanning = false;
            isOrbiting = false;
            isFocusing = true;

            pivotMoveTween = pivot.DOMove(target.position, tweenDuration)
                .SetEase(focusEase)
                .OnComplete(SyncTargetsToCurrentAndReleaseFocus);

            float startDistance = currentDistance;
            distanceTween = DOTween.To(
                () => startDistance,
                x => { startDistance = x; currentDistance = x; },
                finalDistance,
                tweenDuration
            ).SetEase(focusEase);
        }

        /// <summary>
        /// Focus 完成後，把所有連續輸入系統的 target 與速度同步回當前值，
        /// 避免交還控制權瞬間因殘留速度/落後 target 造成鏡頭跳動。
        /// </summary>
        private void SyncTargetsToCurrentAndReleaseFocus()
        {
            targetPivotPosition = pivot.position;
            targetYaw = yaw;
            targetPitch = pitch;
            panVelocity = Vector3.zero;
            yawVelocity = 0f;
            pitchVelocity = 0f;
            zoomVelocity = 0f;
            isFocusing = false;
        }

        private void KillFocusTweens()
        {
            bool wasFocusing = isFocusing;
            if (pivotMoveTween != null && pivotMoveTween.IsActive()) pivotMoveTween.Kill();
            if (distanceTween != null && distanceTween.IsActive()) distanceTween.Kill();
            pivotMoveTween = null;
            distanceTween = null;

            if (wasFocusing) SyncTargetsToCurrentAndReleaseFocus();
        }
        #endregion
    }
}
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VzDev.CameraUtils
{
    /// <summary>
    /// 場景攝影機控制器（Pivot + 球座標模式）。
    /// 左鍵拖曳：沿世界水平面平移
    /// 右鍵拖曳：環繞旋轉（Orbit）
    /// 滾輪：動量式拉近/拉遠，抵達極限距離後轉為 Dolly，避免硬牆卡頓感
    /// FocusOnTarget()：供外部呼叫，運鏡到指定 Transform
    ///
    /// 平滑策略：
    ///   - Pan / Orbit：滑鼠拖曳每帧都有連續輸入，用 targetXxx 累積、currentXxx 用 SmoothDamp 追趕即可平滑。
    ///   - Zoom：滾輪輸入是「離散事件」（滾一下才有一次非零值），SmoothDamp 追一個跳動的 target 仍會頓；
    ///     改為「動量模型」：滾輪只灌入速度，速度每帧指數衰減、用速度積分距離，
    ///     不論滾動頻率快慢都是連續運算，且只需幾個浮點數運算，效能開銷極低。
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
         Tooltip("左鍵按下瞬間若打到此Layer的物件，視為要與模型互動，本次不觸發平移（建議設為與 ColliderInteractionSystem.interactableLayer 相同）")]
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
        /// 右鍵按下瞬間重新校準 pivot：對游標位置做 Raycast，取得場景深度後，
        /// 將 pivot 重新定位到「攝影機沿視線方向到該深度」的點。
        /// 用途：消除滾輪 Dolly 造成的 pivot 漂移，讓 Orbit 半徑永遠對齊「目前實際看到的場景深度」，
        /// 使近距離/遠距離操作時的 Orbit 手感一致。
        /// 數學上 newPivot = camera.position + forward * newDistance，
        /// 之後 ApplyTransform() 算出的攝影機位置會等於校準前的位置，鏡頭不會有跳動。
        /// </summary>
        private void RecalibrateOrbitPivot()
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            float newDistance = currentDistance;

            if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, blockPanLayer))
            {
                // 投影到攝影機前方軸線上的深度，而非直接用 hit.distance，
                // 避免游標不在畫面正中央時，pivot 偏移到視線軸線之外
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
        /// 滾輪只負責「灌入速度」，不直接改變距離。
        /// 實際距離變化由 UpdateZoomMomentum() 每帧用速度積分完成，
        /// 因此不管滾輪滾動頻率快慢，運算都是連續的，不會有跳點頓感。
        /// </summary>
        private void HandleZoomInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;

            KillFocusTweens();
            zoomVelocity -= scroll * zoomImpulse;
        }

        /// <summary>
        /// 動量式縮放：速度每帧指數衰減（framerate-independent），
        /// 再用速度積分出距離變化。抵達 min/maxDistance 時，
        /// 超出的位移轉換成 Dolly（pivot 沿視線方向前進/後退），避免硬牆卡頓。
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

            float proposed = currentDistance + zoomVelocity * Time.deltaTime;

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
using System;
using VzDev.DebugUtils;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace VzDev.CameraUtils
{
    /// RTS攝影機控制器
    /// https://chatgpt.com/share/67fcb5ab-b03c-8012-b685-28ab8ee23da5
    public class RTSCameraController : SingletonMonoBehaviour<RTSCameraController>
    {
        [Header(">>> LookAt對像")] [SerializeField] private Transform lookAtTarget;

        [Header(">>> 移動邊界BoxCollider Trigger")]
        public BoxCollider boundsCollider;

        [Header("Target Movement Bounds")] public float distance = 10f;
        public Vector2 distanceLimits = new Vector2(1f, 50f);
        public float zoomSpeed = 0.5f, zoomSpeedAdjust = 0;
        public float zoomSpeedMultiplier = 7f;
        public float zoomDampTime = 0.2f;

        [Header("Zoom Fine-Tuning")]
        [Tooltip("開啟後，滾輪縮放速度不受影格率(FPS)影響，建議保持開啟")]
        public bool frameRateIndependentZoom = true; // 修正: 解決滾輪縮放手感隨FPS漂移的問題
        private const float ReferenceFrameDelta = 1f / 60f;

        [Header("Rotation")] public float xSpeed = 7f;
        public float ySpeed = 7f;
        public float yMinLimit = -10f;
        public float yMaxLimit = 90f;
        public float rotationDampTime = 0.2f;

        [Header("Movement")] public float moveSpeed = 3f;
        public float moveDampTime = 0.2f;
        public float flyDampTime = 0.5f;
        public float edgeSizePercent = 0.02f;
        public bool enableEdgeMovement = false;

        private float _movementDampTime;

        [Header("Movement Speed Adjustment Based on Distance")] // 🔄
        public float
            moveSpeedMultiplier = 0.15f; // 🔄 Adjust move speed based on distance (lower values for faster movement)

        private Vector3 currentTargetPosition;
        private Vector3 targetMoveVelocity;
        private float x, y;
        private float currentDistance;
        private float distanceVelocity;
        private Vector3 rotationVelocity;
        private Bounds moveBounds;
        private bool isRotating = false;
        private bool isShiftPressed = false;

        // 修正: 不再直接覆寫序列化欄位 yMinLimit，改用獨立的執行期變數保存「動態下限」，
        // 這樣 Inspector 上填的 yMinLimit 永遠是使用者的原始設定，不會被跑起來後的邏輯吃掉。
        private float _effectiveYMinLimit;

        [field: SerializeField, ReadOnly, Label("是否可移動")] public bool IsEnableMove { get; private set; } = true;
        [field: SerializeField, ReadOnly, Label("是否可旋轉角度")] public bool IsEnableRotate { get; private set; } = true;
        [field: SerializeField, ReadOnly, Label("是否可拉近拉遠")] public bool IsEnableZoom { get; private set; } = true;

        /// 設定是否可移動鏡頭
        public void SetIsEnableMove(bool isEnableMove) => this.IsEnableMove = isEnableMove;

        /// 設定是否可旋轉鏡頭
        public void SetIsEnableRotate(bool isEnableRotate)
        {
            IsEnableRotate = isEnableRotate;
            // 修正: 關閉旋轉時強制重置 isRotating，避免它卡在 true 導致 HandleEdgeMovement 被永久鎖住
            if (!isEnableRotate) isRotating = false;
        }

        /// 設定是否可拉近拉遠
        public void SetIsEnableZoom(bool isEnableZoom) => IsEnableZoom = isEnableZoom;

        public void SetZoomSpeedMultiplier(float multiplier) => zoomSpeedMultiplier = multiplier;


        private Transform _originalLookAtParent;
        private bool isRecoveringLookAtParent = true;

        // 修正: 新增追蹤目標的參考。原版只在 SetFollowTarget 呼叫的那一刻用 FlyToPosition
        // 設定一次 currentTargetPosition，之後如果目標移動，currentTargetPosition 完全不會再更新，
        // 而 ApplyPosition() 每一幀都會把 lookAtTarget.position 強制拉回這個「過去某一刻」的固定世界座標，
        // 導致 parenting（lookAtTarget.parent = target）完全沒有機會發揮作用——鏡頭因此看起來像是「跟丟了」。
        private Transform _followTarget;

        public void SetFollowTarget(Transform target)
        {
            if (target != null)
            {
                Debug.Log($"RTSCameraController: SetFollowTarget to {target.name}");
                SetIsEnableMove(false);
                FlyToPosition(target);
                if (isRecoveringLookAtParent)
                {
                    isRecoveringLookAtParent = false;
                    _originalLookAtParent = lookAtTarget.parent;
                    Debug.Log($"RTSCameraController: Recovering LookAtParent to {_originalLookAtParent?.name}");
                }

                lookAtTarget.parent = target;
                _followTarget = target; // 修正: 記錄目標，讓 Update() 每一幀持續追蹤
            }
        }

        public void CancelFollowTarget()
        {
            SetIsEnableMove(true);
            isRecoveringLookAtParent = true;
            lookAtTarget.parent = _originalLookAtParent;
            _followTarget = null; // 修正: 停止追蹤
        }

        /// 修正: Follow 期間每一幀持續把 currentTargetPosition 更新成目標的最新位置，
        /// 而不是只在 SetFollowTarget 呼叫當下設定一次。用 SetTarget(pos, -1f) 是因為
        /// setDistance <= 0 時 SetTarget 不會去動 distance，只更新位置，這樣才不會每幀重置縮放距離。
        private void UpdateFollowTarget()
        {
            if (_followTarget == null) return;

            Vector3 targetPos = _followTarget.TryGetComponent(out Renderer render)
                ? render.bounds.center
                : _followTarget.position;

            SetTarget(targetPos, -1f);
        }

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;

            if (lookAtTarget != null)
            {
                currentTargetPosition = lookAtTarget.position;
            }

            currentDistance = distance;
            _effectiveYMinLimit = yMinLimit; // 修正: 初始值採用 Inspector 設定
            _movementDampTime = moveDampTime; // 修正: 給一個合理初始值，避免第一次移動時 smoothTime=0

            if (boundsCollider != null)
            {
                moveBounds = boundsCollider.bounds;
            }
        }

        void Update()
        {
            if (lookAtTarget == null) return;
            if (EventHelper.IsUsingInputField) return;

            HandleMovementInput();
            if (!isRotating) HandleEdgeMovement();
            if (IsEnableZoom) HandleZoom(); // 🔄 Zoom 開關
            if (IsEnableRotate) HandleRotation(); // 🔄 Rotation 開關
            else isRotating = false; // 修正: 旋轉被關閉時同步重置 isRotating（雙重保險，對應 SetIsEnableRotate）

            UpdateFollowTarget(); // 修正: Follow 模式下持續追蹤目標最新位置

            ApplyPosition();
        }

        void HandleMovementInput()
        {
            if (IsEnableMove == false) return;

            var kb = Keyboard.current;
            if (kb == null) return; // 修正: 沒有鍵盤裝置時直接跳過，避免 NRE

            Vector3 input = Vector3.zero;

            // 檢查Shift鍵是否被按下
            isShiftPressed = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            if (kb.wKey.isPressed) input += new Vector3(0, 0, 1);
            if (kb.sKey.isPressed) input += new Vector3(0, 0, -1);
            if (kb.aKey.isPressed) input += new Vector3(-1, 0, 0);
            if (kb.dKey.isPressed) input += new Vector3(1, 0, 0);
            if (kb.eKey.isPressed) input += new Vector3(0, 1, 0);
            if (kb.qKey.isPressed) input += new Vector3(0, -1, 0);

            if (input != Vector3.zero)
            {
                // 修正: 移除原本每幀 new 陣列 + LINQ.Any() 的寫法（GC Alloc 熱點），
                // 直接在「確定有輸入」時設定阻尼時間即可，效果相同且無配置成本。
                _movementDampTime = moveDampTime;

                // 🔄 Adjust move speed based on distance
                float adjustedMoveSpeed = moveSpeed * (1f + (currentDistance * moveSpeedMultiplier));

                // 如果按住Shift鍵，加速移動
                if (isShiftPressed)
                {
                    adjustedMoveSpeed *= 3f; // Shift按下時加倍速度
                }

                Vector3 move = Quaternion.Euler(0, x, 0) * input.normalized;
                currentTargetPosition += move * adjustedMoveSpeed * Time.deltaTime;
            }
        }

        void HandleEdgeMovement()
        {
            if (!enableEdgeMovement) return;
            if (IsEnableMove == false) return; // 修正: 原版沒檢查這個旗標，導致 Follow 模式（SetIsEnableMove(false)）時，
                                                // 若滑鼠恰好停在螢幕邊緣，仍會偷偷移動 currentTargetPosition 跟 Follow 打架。

            var mouse = Mouse.current;
            if (mouse == null) return; // 修正: 沒有滑鼠裝置時直接跳過，避免 NRE

            Vector2 mousePos = mouse.position.ReadValue();
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            float borderX = screenWidth * edgeSizePercent;
            float borderY = screenHeight * edgeSizePercent;

            Vector3 dir = Vector3.zero;

            if (mousePos.x < borderX) dir.x = -1;
            else if (mousePos.x > screenWidth - borderX) dir.x = 1;

            if (mousePos.y < borderY) dir.z = -1;
            else if (mousePos.y > screenHeight - borderY) dir.z = 1;

            if (dir != Vector3.zero)
            {
                // 修正: 邊緣移動觸發時同樣要設定阻尼時間，
                // 原版只有 HandleMovementInput 會設，導致純滑鼠邊緣移動時 _movementDampTime 停在初始值(0)。
                _movementDampTime = moveDampTime;

                // 🔄 Adjust move speed based on distance
                float adjustedMoveSpeed = moveSpeed * (1f + (currentDistance * moveSpeedMultiplier));

                // 如果按住Shift鍵，加速移動
                if (isShiftPressed)
                {
                    adjustedMoveSpeed *= 2f; // Shift按下時加倍速度
                }

                Vector3 move = Quaternion.Euler(0, x, 0) * dir.normalized;
                currentTargetPosition += move * adjustedMoveSpeed * Time.deltaTime;
            }
        }

        void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return; // 修正: 沒有滑鼠裝置時直接跳過，避免 NRE

            float scroll = mouse.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                // 修正: zoomSpeedAdjust 現在儲存「比例」，每次用當前 zoomSpeed 即時計算，
                // 不會因為之後 zoomSpeed 被改變而失準（原版是把 adjustValue*zoomSpeed 的結果值烤進去）。
                float zoomSpeedFinal = Mathf.Max(zoomSpeed * (1f + zoomSpeedAdjust), 0.0000001f);
                float adjustedZoomSpeed = zoomSpeedFinal + (currentDistance * zoomSpeedMultiplier);

                // 修正: 滾輪是離散事件，不應該用 Time.deltaTime 縮放（否則縮放手感隨FPS改變）。
                // frameRateIndependentZoom 開啟時用固定的參考幀時間，讓手感在任何FPS下一致。
                float deltaFactor = frameRateIndependentZoom ? ReferenceFrameDelta : Time.deltaTime;

                distance -= scroll * adjustedZoomSpeed * deltaFactor;
                distance = Mathf.Clamp(distance, distanceLimits.x, distanceLimits.y);
            }

            currentDistance = Mathf.SmoothDamp(currentDistance, distance, ref distanceVelocity, zoomDampTime);

            // 🔄 Check if the distance is 1 and adjust Y Min Limit accordingly
            // 修正: 不再覆寫序列化欄位 yMinLimit，改寫獨立的 _effectiveYMinLimit，
            // 這樣 Inspector 上使用者填的 yMinLimit 值永遠保留，不會被跑起來的邏輯永久蓋掉。
            _effectiveYMinLimit = Mathf.Approximately(currentDistance, 1f) ? -90f : yMinLimit;
        }

        void HandleRotation()
        {
            var mouse = Mouse.current;
            if (mouse == null) return; // 修正: 沒有滑鼠裝置時直接跳過，避免 NRE

            if (mouse.rightButton.isPressed)
            {
                isRotating = true;
                Vector2 delta = mouse.delta.ReadValue();
                x += delta.x * xSpeed * 0.02f;
                y -= delta.y * ySpeed * 0.02f;
                y = Mathf.Clamp(y, _effectiveYMinLimit, yMaxLimit); // 修正: 改用動態下限變數
            }
            else
            {
                isRotating = false;
            }

            Quaternion targetRotation = Quaternion.Euler(y, x, 0);
            Quaternion smoothRotation =
                Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime / rotationDampTime);
            transform.rotation = smoothRotation;
        }

        void ApplyPosition()
        {
            if (boundsCollider != null)
            {
                moveBounds = boundsCollider.bounds;
                currentTargetPosition.x = Mathf.Clamp(currentTargetPosition.x, moveBounds.min.x, moveBounds.max.x);
                currentTargetPosition.y = Mathf.Clamp(currentTargetPosition.y, moveBounds.min.y, moveBounds.max.y);
                currentTargetPosition.z = Mathf.Clamp(currentTargetPosition.z, moveBounds.min.z, moveBounds.max.z);
            }

            Vector3 dampedTarget = Vector3.SmoothDamp(lookAtTarget.position, currentTargetPosition, ref targetMoveVelocity,
                _movementDampTime);
            lookAtTarget.position = dampedTarget;

            Vector3 offset = transform.rotation * new Vector3(0, 0, -currentDistance);
            transform.position = lookAtTarget.position + offset;
        }

        public void SetTarget(Transform target) => SetTarget(target.position, defaultFlyDistance);

        public void SetTarget(Vector3 position, float setDistance = 1.5f)
        {
            position.y += 0.2f;
            currentTargetPosition = position;
            if (setDistance > 0f)
            {
                distance = Mathf.Clamp(setDistance, distanceLimits.x, distanceLimits.y);
            }
        }

        /// 設定Zoom的調整速度（比例值，例如 0.5 代表在基礎 zoomSpeed 上增加 50%）
        // 修正: 改成單純儲存比例，不再乘上呼叫當下的 zoomSpeed，避免之後 zoomSpeed 改變時失準
        public void SetZoomSpeedAdjust(float adjustValue) => zoomSpeedAdjust = adjustValue;

        public static void CameraToPosition(Transform target, float? setDistance = null) =>
            Instance.FlyToPosition(target, setDistance); // 修正: 統一走 FlyToPosition(Transform)，確保跟 instance 版本一樣優先用 Renderer bounds 對焦

        public void FlyToPosition(Transform target, float? setDistance = null)
        {
            if (target.TryGetComponent(out Renderer render))
                FlyToPosition(render.bounds.center, setDistance ?? defaultFlyDistance);
            else
                FlyToPosition(target.position, setDistance ?? defaultFlyDistance);
        }

        public void FlyToPosition(Transform target) => FlyToPosition(target, defaultFlyDistance);

        public void FlyToPosition(Vector3 position, float? setDistance = null)
        {
            _movementDampTime = flyDampTime;
            SetTarget(position, setDistance ?? defaultFlyDistance);
        }

        [SerializeField] private float defaultFlyDistance = 2f;
        public void SetDefaultFlyDistance(float distance) => defaultFlyDistance = distance;
    }
}
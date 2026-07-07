using System;
using System.Linq;
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
        public float zoomSpeed = 0.5f, zoomSpeedAdjust=0;
        public float zoomSpeedMultiplier = 7f;
        public float zoomDampTime = 0.2f;


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

        [field: SerializeField, ReadOnly, Label("是否可移動")] public bool IsEnableMove { get; private set; } = true;
        [field: SerializeField, ReadOnly, Label("是否可旋轉角度")] public bool IsEnableRotate { get; private set; } = true;
        [field: SerializeField, ReadOnly, Label("是否可拉近拉遠")] public bool IsEnableZoom { get; private set; } = true;

        /// 設定是否可移動鏡頭
        public void SetIsEnableMove(bool isEnableMove) => this.IsEnableMove = isEnableMove;
        /// 設定是否可旋轉鏡頭
        public void SetIsEnableRotate(bool isEnableRotate) => IsEnableRotate = isEnableRotate;
        /// 設定是否可拉近拉遠
        public void SetIsEnableZoom(bool isEnableZoom) => IsEnableZoom = isEnableZoom;
        
        public void SetZoomSpeedMultiplier(float multiplier) => zoomSpeedMultiplier = multiplier;
        
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

            ApplyPosition();
        }

        void HandleMovementInput()
        {
            if (IsEnableMove == false) return;
            
            Vector3 input = Vector3.zero;
            var kb = Keyboard.current;

            // 檢查Shift鍵是否被按下
            isShiftPressed = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            if (kb.wKey.isPressed) input += new Vector3(0, 0, 1);
            if (kb.sKey.isPressed) input += new Vector3(0, 0, -1);
            if (kb.aKey.isPressed) input += new Vector3(-1, 0, 0);
            if (kb.dKey.isPressed) input += new Vector3(1, 0, 0);
            if (kb.eKey.isPressed) input += new Vector3(0, 1, 0);
            if (kb.qKey.isPressed) input += new Vector3(0, -1, 0);
            
            var keys = new[]
            {
                kb.wKey, kb.aKey, kb.sKey,
                kb.dKey, kb.qKey, kb.eKey
            };

            if (keys.Any(k => k.wasPressedThisFrame))
            {
                _movementDampTime = moveDampTime;
            }

            if (input != Vector3.zero)
            {
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

            Vector2 mousePos = Mouse.current.position.ReadValue();
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
            float scroll = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                float zoomSpeedFinal = Mathf.Max(zoomSpeed + zoomSpeedAdjust, 0.0000001f);
                float adjustedZoomSpeed = zoomSpeedFinal + (currentDistance * zoomSpeedMultiplier);
                distance -= scroll * adjustedZoomSpeed * Time.deltaTime;
                distance = Mathf.Clamp(distance, distanceLimits.x, distanceLimits.y);
            }

            currentDistance = Mathf.SmoothDamp(currentDistance, distance, ref distanceVelocity, zoomDampTime);

            // 🔄 Check if the distance is 1 and adjust Y Min Limit accordingly
            if (Mathf.Approximately(currentDistance, 1f))
            {
                yMinLimit = -90f;
            }
            else
            {
                yMinLimit = -20f; // Default value
            }
        }

        void HandleRotation()
        {
            if (Mouse.current.rightButton.isPressed)
            {
                isRotating = true;
                Vector2 delta = Mouse.current.delta.ReadValue();
                x += delta.x * xSpeed * 0.02f;
                y -= delta.y * ySpeed * 0.02f;
                y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
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

        public void SetTarget(Vector3 position, float setDistance = 1.5f)
        {
            position.y += 0.2f;
            currentTargetPosition = position;
            if (setDistance > 0f)
            {
                distance = Mathf.Clamp(setDistance, distanceLimits.x, distanceLimits.y);
            }
        }
        /// 設定Zoom的調整速度
        public void SetZoomSpeedAdjust(float adjustValue) => zoomSpeedAdjust = adjustValue * zoomSpeed;
        
        public static void CameraToPosition(Transform target, float? setDistance=null) => Instance.FlyToPosition(target.position, setDistance);

        public void FlyToPosition(Transform target)
        {
            if (target.TryGetComponent(out Renderer render))
                FlyToPosition(render.bounds.center, defaultFlyDistance);
            else
                FlyToPosition(target.position, defaultFlyDistance);
        }

        public void FlyToPosition(Vector3 position, float? setDistance=null)
        {
            _movementDampTime = flyDampTime;
            SetTarget(position, setDistance ?? defaultFlyDistance);
        }

        [SerializeField]private float defaultFlyDistance = 2f;
        public void SetDefaultFlyDistance(float distance) => defaultFlyDistance = distance;
    }
}
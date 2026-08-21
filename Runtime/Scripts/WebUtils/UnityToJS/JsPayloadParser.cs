using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DebugUtils;
using VzDev.Helpers;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.WebGLUtils
{
    public class JsPayloadParser : MonoBehaviour
    {
        #region Events
        [SerializeField, ReadOnly] private DCIM_JsPayload receivedPayload;
        [SerializeField, ReadOnly, ShowIf("IsUserTokenPayload")] private UserTokenPayload userTokenPayload;
        [SerializeField, ReadOnly, ShowIf("IsSwitchSystemMenuPayload")] private SwitchSystemMenuPayload switchSystemMenuPayload;
        [SerializeField, ReadOnly, ShowIf("IsSwitchFloorPayload")] private SwitchFloorPayload switchFloorPayload;
        [SerializeField, ReadOnly, ShowIf("IsClickModelPayload")] private ClickModelPayload clickModelPayload;
        [SerializeField, ReadOnly, ShowIf("IsClickModelPayload")] private Transform clickModelTarget;
        [Foldout("[Events]-取得UserToken")] public UnityEvent<string> OnGetUserTokenEvent;
        [Foldout("[Events]-切換系統選單")] public UnityEvent<EnumSystemMenu> OnSwitchSystemMenuEvent;
        [Foldout("[Events]-切換樓層")] public UnityEvent<EnumFloor> OnSwitchFloorEvent;

        private bool IsUserTokenPayload => receivedPayload.action == EnumJsAction.UserToken && !string.IsNullOrEmpty(receivedPayload.payload);
        private bool IsSwitchSystemMenuPayload => receivedPayload.action == EnumJsAction.SwitchSystemMenu && !string.IsNullOrEmpty(receivedPayload.payload);
        private bool IsSwitchFloorPayload => receivedPayload.action == EnumJsAction.SwitchToFloor && !string.IsNullOrEmpty(receivedPayload.payload);
        private bool IsClickModelPayload => receivedPayload.action == EnumJsAction.SimulateClickModel && !string.IsNullOrEmpty(receivedPayload.payload);

        #endregion

        /*JSON解析格式
        {
            action: "{EnumJsAction}",       //動作
            deviceCode: "{deviceCode}",     //模型deviceCode (For點擊模型Action)
            systemMenu: "{EnumSystemMenu}"  //系統選單 (For切換系統選單Action)
            floor: "{EnumFloor}"            //樓層 (For切換樓層Action)
        }
        */
        /// <summary>
        /// JSON解析
        /// </summary>
        public void ParseJsPayload(string json)
        {
            receivedPayload = new DCIM_JsPayload();
            switchSystemMenuPayload = null;
            switchFloorPayload = null;
            clickModelPayload = null;

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException e)
            {
                Debug.LogError($"[{GetType().Name}] JSON 解析失敗: {e.Message}");
                return;
            }

            // 先拿 action，用 Enum.TryParse 避免炸掉
            string actionStr = root["action"]?.ToString();
            if (!Enum.TryParse(actionStr, out EnumJsAction action))
            {
                Debug.LogWarning($"[{GetType().Name}] 未知的 action: {actionStr}");
                return;
            }

            receivedPayload.action = action;

            JObject payload = root["payload"] as JObject;
            if (payload == null)
            {
                Debug.LogWarning($"[{GetType().Name}] {action} 缺少 payload");
                return;
            }

            receivedPayload.payload = root["payload"].ToString();
            CheckAction(receivedPayload.action, payload);
        }

        private void CheckAction(EnumJsAction action, JObject payload)
        {
            switch (action)
            {
                case EnumJsAction.UserToken:
                    OnUserTokenAction(payload);
                    break;
                case EnumJsAction.SwitchSystemMenu:
                    OnSwitchSystemMenuAction(payload);
                    break;
                case EnumJsAction.SwitchToFloor:
                    OnSwitchFloorAction(payload);
                    break;
                case EnumJsAction.SimulateClickModel:
                    OnSimulateClickModelAction(payload);
                    break;
                case EnumJsAction.SimulateClickEmpty:
                    Debug.Log($"SimulateClickEmpty action received");
                    ColliderInteractionSystem.SimulateClickEmpty();
                    break;

                default:
                    Debug.LogWarning($"[WebGLBridge] 沒有對應的 handler: {action}");
                    break;
            }
        }
        private void OnUserTokenAction(JObject payload)
        {
            userTokenPayload = payload.ToObject<UserTokenPayload>();
            if (userTokenPayload == null)
            {
                Debug.LogWarning($"[{GetType().Name}] UserToken 缺少 payload");
                return;
            }
            OnGetUserTokenEvent?.Invoke(userTokenPayload.userToken);
        }
        private void OnSwitchSystemMenuAction(JObject payload)
        {
            switchSystemMenuPayload = payload.ToObject<SwitchSystemMenuPayload>();
            if (switchSystemMenuPayload == null)
            {
                Debug.LogWarning($"[{GetType().Name}] SwitchSystemMenu 缺少 payload");
                return;
            }
            OnSwitchSystemMenuEvent?.Invoke(switchSystemMenuPayload.systemMenu);
        }
        private void OnSwitchFloorAction(JObject payload)
        {
            switchFloorPayload = payload.ToObject<SwitchFloorPayload>();
            if (switchFloorPayload == null)
            {
                Debug.LogWarning($"[{GetType().Name}] SwitchToFloor 缺少 payload");
                return;
            }
            OnSwitchFloorEvent?.Invoke(switchFloorPayload.floor);
        }
        private void OnSimulateClickModelAction(JObject payload)
        {
            clickModelPayload = payload.ToObject<ClickModelPayload>();
            if (clickModelPayload == null)
            {
                Debug.LogWarning($"[{GetType().Name}] SimulateClickModel 缺少 payload");
                return;
            }

            List<Transform> foundObjects = ObjectHelper.FindObjectsByDeviceCode(clickModelPayload.deviceCode, NameSearchMode.Exact, true);

            clickModelTarget = foundObjects.Count > 0
             ? foundObjects[0]
             : null;
            if (clickModelTarget == null)
            {
                Debug.LogWarning($"No GameObject found with device code: {clickModelPayload.deviceCode}");
                return;
            }
            Debug.Log($"SimulateClickModel action received for device: {clickModelPayload.deviceCode}");
            ColliderInteractionSystem.SimulateClick(clickModelTarget.gameObject);
        }
    }
}

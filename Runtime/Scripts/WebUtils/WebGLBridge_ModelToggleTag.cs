using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.ApiExtensions;
using VzDev.InteractiveUtils.ModelMouseEvent;

namespace VzDev.WebGLUtils
{
        /// <summary>
        /// 接收JS端訊息 - 處理ModelToggleBinding的Toggle切換，透過DeviceCode找到對應的模型
        /// </summary>
        public class WebGLBridge_ModelToggleTag : WebGLBridge
        {
                [field: SerializeField, ReadOnly]
                public string ReceiveDeviceCode { get; private set; } = string.Empty;

                [ReadOnly, SerializeField] private ModelToggleBinding foundModelByDeviceCode;
                [ReadOnly, SerializeField] private ModelToggleBinding[] modelToggleBindingsAcSystem;

                [Foldout("[Components]"), SerializeField] private ModelToggleBindingGenerator modelToggleCCTV, modelToggleDoor;
                [Foldout("[Components]"), SerializeField] private ModelToggleBindingGenerator[] modelToggleAcSystem;

                public void SetCctvToggleOn(string deviceCode) => FindObjectByDeviceCode(modelToggleCCTV.ModelToggles, deviceCode, true);
                public void SetCctvToggleOff(string deviceCode) => FindObjectByDeviceCode(modelToggleCCTV.ModelToggles, deviceCode, false);
                public void SetDoorToggleOn(string deviceCode) => FindObjectByDeviceCode(modelToggleDoor.ModelToggles, deviceCode, true);
                public void SetDoorToggleOff(string deviceCode) => FindObjectByDeviceCode(modelToggleDoor.ModelToggles, deviceCode, false);
                public void SetAcSystemToggleOn(string deviceCode) => SetAcSystemToggleOn(deviceCode, true);
                public void SetAcSystemToggleOff(string deviceCode) => SetAcSystemToggleOn(deviceCode, false);
                private void SetAcSystemToggleOn(string deviceCode, bool isOn)
                {
                        if (modelToggleBindingsAcSystem == null || modelToggleBindingsAcSystem.Length == 0)
                        {
                                modelToggleBindingsAcSystem = new ModelToggleBinding[modelToggleAcSystem.Length];
                                for (int i = 0; i < modelToggleAcSystem.Length; i++)
                                {
                                        modelToggleBindingsAcSystem = modelToggleBindingsAcSystem.Combine(modelToggleAcSystem[i].ModelToggles);
                                }
                        }
                        FindObjectByDeviceCode(modelToggleBindingsAcSystem, deviceCode, isOn);
                }

                /// <summary>
                /// 透過DeviceCode找到對應的模型，並觸發事件傳遞該模型的Transform。
                /// </summary>
                private void FindObjectByDeviceCode(ModelToggleBinding[] targetList, string deviceCode, bool isOn)
                {
                        ReceiveDeviceCode = deviceCode;
                        foundModelByDeviceCode = Array.Find(targetList, modelToggle => modelToggle.TargetModel.name.Contains(deviceCode, StringComparison.OrdinalIgnoreCase));

                        if (foundModelByDeviceCode == null)
                        {
                                Debug.LogWarning($"[WebGLBridge_ModelToggleTag]  找不到對應的物件，DeviceCode: {deviceCode}");
                                return;
                        }
                        else
                        {
                                foundModelByDeviceCode.GetComponent<ModelToggleBinding>().ToggleItem.isOn = isOn;
                        }
                }
        }
}
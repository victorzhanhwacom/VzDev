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

                public void SetCctvToggleOn(string deviceCode, string isOn) => FindObjectByDeviceCode(modelToggleCCTV.ModelToggles, deviceCode, isOn);
                public void SetDoorToggleOn(string deviceCode, string isOn) => FindObjectByDeviceCode(modelToggleDoor.ModelToggles, deviceCode, isOn);
                public void SetAcSystemToggleOn(string deviceCode, string isOn)
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
                private void FindObjectByDeviceCode(ModelToggleBinding[] targetList, string deviceCode, string isOn)
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
                                foundModelByDeviceCode.GetComponent<ModelToggleBinding>().ToggleItem.isOn = bool.TryParse(isOn, out bool result) ? result : false;
                        }
                }


                public string testDeviceCode = string.Empty;
                public string boolString = "false";
                [Button]
                private void Test_CCTV() => SetCctvToggleOn(testDeviceCode, boolString);

                [Button]
                private void Test_Door() => SetDoorToggleOn(testDeviceCode, boolString);
                [Button]
                private void Test_AC() => SetAcSystemToggleOn(testDeviceCode, boolString);

        }
}
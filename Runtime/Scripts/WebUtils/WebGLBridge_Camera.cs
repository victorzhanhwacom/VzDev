using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.ObjectUtils;

namespace VzDev.WebGLUtils
{
        /// <summary>
        /// 接收JS端訊息 - 處理攝影機焦點切換，透過DeviceCode找到對應的模型，並觸發事件傳遞該模型的Transform。
        /// </summary>
        public class WebGLBridge_Camera : WebGLBridge
        {
                [field: SerializeField, ReadOnly]
                public string ReceiveDeviceCode { get; private set; } = string.Empty;

                [ReadOnly, SerializeField] private Transform foundModelByDeviceCode;

                [Foldout("[Events-Extend]")] public UnityEvent<Transform> OnDeviceFocus;
                [Foldout("[Events-Extend]")] public UnityEvent<string> OnFloorFocus;

                [Foldout("[Components]"), SerializeField] private ModelFinder modelFinderCCTV, modelFinderDoor;

                /// <summary>
                /// 樓層焦點切換，透過樓層名稱觸發事件傳遞該樓層的Transform。
                /// </summary>
                /// <param name="floorName"></param>
                public void SetCameraFocus_Floor(string floorName) => OnFloorFocus?.Invoke(floorName.Trim());
                
                public void SetCameraFocus_CCTV(string deviceCode) => FindObjectByDeviceCode(modelFinderCCTV.FoundModels, deviceCode);
                public void SetCameraFocus_Door(string deviceCode) => FindObjectByDeviceCode(modelFinderDoor.FoundModels, deviceCode);

                /// <summary>
                /// 透過DeviceCode找到對應的模型，並觸發事件傳遞該模型的Transform。
                /// </summary>
                private void FindObjectByDeviceCode(List<Transform> targetList, string deviceCode)
                {
                        ReceiveDeviceCode = deviceCode;
                        foundModelByDeviceCode = targetList.Find(model => model.name.Contains(deviceCode, StringComparison.OrdinalIgnoreCase));

                        if (foundModelByDeviceCode == null)
                        {
                                Debug.LogWarning($"[WebGLBridge_Camera] SetCameraFocus_CCTV: 找不到對應的物件，DeviceCode: {deviceCode}");
                                return;
                        }
                        else
                        {
                                OnDeviceFocus?.Invoke(foundModelByDeviceCode);
                        }
                }
                

                public string testDeviceCode = string.Empty;
                [Button]    
                private void TestSetCameraFocus_CCTV() => SetCameraFocus_CCTV(testDeviceCode);
                
                [Button]    
                private void TestSetCameraFocus_Door() => SetCameraFocus_Door(testDeviceCode);
        }
}
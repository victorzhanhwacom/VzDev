using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.ObjectUtils;

namespace VzDev.WebGLUtils
{
        /// <summary>
        /// 接收JS端訊息 (For環控)
        /// </summary>
        public class WebGLBridge_Camera : WebGLBridge
        {
                [field: SerializeField, ReadOnly]
                public string ReceiveDeviceCode { get; private set; } = string.Empty;

                [ReadOnly, SerializeField] private Transform foundModelByDeviceCode;

                [Foldout("[Events-Custom]")] public UnityEvent<Transform> OnDeviceFocus;

                [Foldout("[Components]"), SerializeField] private ModelFinder modelFinderCCTV, modelFinderDoor;

                public void SetCameraFocus_CCTV(string deviceCode) => FindObjectByDeviceCode(modelFinderCCTV.FoundModels, deviceCode);
                public void SetCameraFocus_Door(string deviceCode) => FindObjectByDeviceCode(modelFinderDoor.FoundModels, deviceCode);

                private void FindObjectByDeviceCode(List<Transform> list, string deviceCode)
                {
                        ReceiveDeviceCode = deviceCode;
                        foundModelByDeviceCode = list.Find(model => model.name.Contains(deviceCode, StringComparison.OrdinalIgnoreCase));

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
        }
}
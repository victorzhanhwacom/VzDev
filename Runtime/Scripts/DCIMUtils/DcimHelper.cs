using System;
using UnityEngine;
using VzDev.DCIMUtils.DataUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils
{
    /// <summary>
    /// DCIM Helper：提供機櫃、設備、模型相關的工具方法
    /// </summary>
    public static class DCIM_Helper
    {
        #region 設備模型名稱String比對
        /// <summary>
        /// 設備模型比對邏輯, 處理名稱裡有空格或底線的情況
        /// </summary>
        public static bool CompareEquipmentModelName(string modelName, string equipmentModelName)
        {
            bool result = modelName.IndexOf(equipmentModelName, StringComparison.OrdinalIgnoreCase) >= 0;
            if (result) return result;
            modelName = modelName.Replace(" ", "_");
            //equipmentModelName = equipmentModelName.Replace(" ", "_");
            result = modelName.IndexOf(equipmentModelName, StringComparison.OrdinalIgnoreCase) >= 0;
            return result;
        }
        #endregion

        #region 從DeviceName / DeviceCode取得模型名稱
        /// <summary>
        /// 從DeviceName取得模型名稱
        /// </summary>
        public static string GetModelNameFromDeviceName(string deviceName)
        => GetModelNameFromDeviceCode(deviceName.GetStringBetweenMarks("[", "]"));

        /// <summary>
        /// 從DeviceCode取得模型名稱
        /// </summary>
        public static string GetModelNameFromDeviceCode(string deviceCode)
        {
            if (string.IsNullOrEmpty(deviceCode)) return string.Empty;

            string[] parts = deviceCode.Split(":");
            if (parts.Length < 2)
            {
                Debug.LogWarning($"Device code '{deviceCode}' does not contain a model name.");
                return string.Empty;
            }
            return parts[1].Split("+")[0].Trim();
        }
        #endregion

        #region 設備模型對齊到機櫃槽位的工具方法
        /// <summary>
        /// 將設備模型對齊到機櫃的指定槽位
        /// </summary>
        public static void SetEquipmentSnapToRackSlot(Transform equipmentModel, DCR_Asset rackAsset, Collider rackCollider, int uIndex, int heightU)
        {
            if (equipmentModel == null || rackAsset?.modelInfo?.modelTarget == null || rackCollider == null) return;
            if (rackAsset.u_height_Max <= 0) return;

            float uHeightWorld = rackCollider.bounds.size.y / rackAsset.u_height_Max; //依照機櫃Collider的實際高度與U槽最大數，計算每個U的世界座標高度
            float occupiedBottomY = rackCollider.bounds.min.y + (uIndex - 1) * uHeightWorld;
            float occupiedCenterY = occupiedBottomY + (heightU * uHeightWorld) * 0.5f; // 跨多個 U 時，對齊整個佔用區段的中心

            Quaternion rackRotation = rackAsset.modelInfo.modelTarget.rotation;
            bool rackForwardIsPositiveZ = false;
            bool deviceForwardIsPositiveZ = false;

            float rackSign = rackForwardIsPositiveZ ? 1f : -1f;
            Vector3 forward = rackRotation * Vector3.forward * rackSign;
            forward.y = 0f;
            forward.Normalize();

            // AABB 沿任意方向的支撐距離公式：對「軸對齊的長方體」在任意方向上都成立，
            // 不要求 forward 剛好對齊世界 X/Z 軸。
            float depthAlongForward =
                Mathf.Abs(forward.x) * rackCollider.bounds.extents.x +
                Mathf.Abs(forward.z) * rackCollider.bounds.extents.z;

            Vector3 rackFrontFaceCenter = new Vector3(rackCollider.bounds.center.x, occupiedCenterY, rackCollider.bounds.center.z) + forward * depthAlongForward;

            Bounds localBounds = CalculateLocalBounds(equipmentModel, equipmentModel.GetComponentsInChildren<Renderer>(true));
            Vector3 previewLocalBoundsCenter = localBounds.center;
            Vector3 previewLocalBoundsSize = localBounds.size;  // 對齊前面時要用到深度 (z)

            float deviceSign = deviceForwardIsPositiveZ ? 1f : -1f;
            // Pivot 不一定在模型幾何中心，反推「設備的前面」相對於 Pivot 的本地偏移量，
            // 才能讓「設備前面」精確貼齊 rackFrontFaceCenter，而不是讓 Pivot 本身對齊過去。
            Vector3 pivotToFrontFaceLocal = previewLocalBoundsCenter + new Vector3(0f, 0f, previewLocalBoundsSize.z * 0.5f * deviceSign);

            equipmentModel.rotation = rackRotation;
            equipmentModel.position = rackFrontFaceCenter - rackRotation * pivotToFrontFaceLocal;
        }
        /// <summary>
        /// 用世界座標 Renderer.bounds 合併全部 Renderer，再轉回 root 的本地空間。
        /// 近似解：假設模型內部子物件不會有大幅旋轉（多數機櫃/設備模型符合此前提）。
        /// </summary>
        private static Bounds CalculateLocalBounds(Transform root, Renderer[] renderers)
        {
            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = root.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = root.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            return new Bounds(localCenter, localSize);
        }
        #endregion

        /////////// 20260902//////////////////////////
        #region Enum
        public enum EnumAlignType
        {
            Center,
            Top,
            Bottom,
            Left,
            Right,
            Front,
            Back
        }
        #endregion
        /// <summary>
        /// [Extension] 將Transform對齊到指定的EnumAlignType位置，並加上偏移量
        /// </summary>
        public static void SetEquipmentSnapToU(Transform equipmentModel, Transform rackModel, EnumAlignType alignType = EnumAlignType.Front, Vector3 offset = new Vector3())
        {
            Bounds equipmentBounds = equipmentModel.GetComponent<Renderer>().bounds;
            Bounds rackBounds = rackModel.GetComponent<Renderer>().bounds;


            Vector3 newPosition = Vector3.zero;
            newPosition.z = rackBounds.max.z - equipmentBounds.size.z; // 對齊到機櫃前方
            // equipmentModel.localPosition = newPosition;
            equipmentModel.position = new Vector3(0, 0, rackBounds.max.z);
        }

        /// <summary>
        /// 計算物件「相對於自身 Pivot」的 local bounds（涵蓋所有子物件 Renderer）。
        /// 作法：暫時把旋轉歸零，此時 world AABB 等同「未旋轉狀態下的 local 外框」，
        /// 再減去 pivot 世界座標，得到不受 Pivot 位置影響的相對外框。
        /// </summary>
        private static Bounds GetLocalBoundsRelativeToPivot(Transform target)
        {
            Quaternion originalRotation = target.rotation;
            target.rotation = Quaternion.identity;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[SetEquipmentPositionAlign] {target.name} 找不到任何 Renderer，無法計算外框");
                target.rotation = originalRotation;
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 min = worldBounds.min - target.position;
            Vector3 max = worldBounds.max - target.position;

            target.rotation = originalRotation;

            Bounds localBounds = new Bounds();
            localBounds.SetMinMax(min, max);
            return localBounds;
        }





        //////////////////////////////////////////////////////////////////////////// 20260825 //////////////////////////////////////////



        /// 機櫃單一RackUnit模型尺吋
        public static Vector3 RackUnitSize => new(0.4826f, 0.0445f, 0.9f);

        #region 模型deviceId相關處理

        /// 從模型名稱 取得deviceId
        public static string GetDeviceId(string modelName)
        {
            modelName = modelName.Trim();
            int start = modelName.IndexOf('[');
            int end = modelName.LastIndexOf(']');

            if (start >= 0 && end > start)
            {
                return modelName.Substring(start + 1, end - start - 1);
            }

            return modelName;
        }

        /// 從deviceId 取得專案名稱
        public static string GetProjectName(string deviceId) => deviceId.Split('+')[0];

        /// 從deviceId 取得專案地點
        public static string GetProjectLocation(string deviceId) => deviceId.Split('+')[1];

        /// 從deviceId 取得樓層
        public static string GetRoomFloor(string deviceId) => deviceId.Split('+')[3];

        /// 從deviceId 取得機房代號
        public static string GetRoomCode(string deviceId) => deviceId.Split('+')[4];

        /// 從deviceId 取得設備類型 (DCR、DCS、DCN)
      /*   public static EnumDeviceType GetDeviceType(string deviceId)
            => EnumHelper.GetEnumByString<EnumDeviceType>(deviceId); */

        /// 從deviceId 取得設備名稱 (是否包含流水號)
        public static string GetDeviceName(string deviceId, bool isIncludeCode = false)
        {
            if (deviceId.Contains(":") == false) return deviceId;
            string deviceNameAndCode = deviceId.Split(":")[1].Trim();
            if (isIncludeCode) return deviceNameAndCode;
            return deviceNameAndCode.Split("+")[0];
        }

        /// 從deviceId 取得設備類型 (Rack、Server、Router、Switch)
/*         public static EnumRevitAssetKind GetDeviceKind(string deviceId)
            => EnumHelper.GetEnumByString<EnumRevitAssetKind>(deviceId);
 */
        /// 從deviceId 取得設備類型 中文
    /*     public static string GetDeviceKindZh(string deviceId)
            => GetDeviceKindZh(EnumHelper.GetEnumByString<EnumRevitAssetKind>(deviceId)); */
        /// 從deviceId 取得設備類型 中文
        public static string GetDeviceKindZh(EnumRevitAssetKind revitAssetKind)
            => revitAssetKind switch
            {
                EnumRevitAssetKind.Rack => "機櫃",
                EnumRevitAssetKind.Server => "伺服主機",
                EnumRevitAssetKind.Router => "路由器",
                EnumRevitAssetKind.Switch => "交換機",
                EnumRevitAssetKind.ODF => "光纖配線架",
                EnumRevitAssetKind.DF => "網路配線架",
                EnumRevitAssetKind.RackStation => "NAS網路儲存伺服器",
                _ => "未知"
            };
        #endregion


    }

    #region Enum

    [Serializable]
    public enum EnumDeviceType
    {
        DCR,
        DCN,
        DCS
    }

    [Serializable]
    public enum EnumRevitAssetKind
    {
        Unknown,

        /// 機櫃
        Rack,
        /// 伺服主機
        Server,
        /// 路由器
        Router,
        /// 交換器
        Switch,
        /// 光纖配線架
        ODF,
        /// 網路配線架
        DF,
        /// NAS
        RackStation,
    }

    #endregion
}
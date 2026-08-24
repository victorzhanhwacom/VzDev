using System;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class WebAPIManager : MonoBehaviour
    {
        #region Fields

        [SerializeField, Label("設備模型")] private List<GameObject> equipmentModels = new List<GameObject>();
        #endregion

        /// <summary>
        /// 取得機房內機櫃群資訊
        /// </summary>
        [Button]
        public void GetRackListInformation()
        {
            OnGetRackListInformationAction?.Invoke("");
        }
        /// <summary>
        /// 取得上架庫存設備列表
        /// </summary>
        [Button]
        public void GetEquipmentAssetInStock()
        {
            OnGetEquipmentAssetInStockAction?.Invoke("");
        }


        /// <summary>
        /// 取得設備模型列表
        /// </summary>
        [Button]
        public void GetEquipmentModels()
        {
            OnGetEquipmentModelsAction?.Invoke(equipmentModels);
        }

        #region Static Events
        /// <summary>
        /// 取得機櫃資料(json字串)
        /// </summary>
        public static Action<string> OnGetRackListInformationAction;
        /// <summary>
        /// 取得設備模型列表
        /// </summary>
        public static Action<List<GameObject>> OnGetEquipmentModelsAction;
        /// <summary>
        /// 設得庫存設備資料列表(json字串)
        /// </summary>
        public static Action<string> OnGetEquipmentAssetInStockAction;
        
        #endregion
    }
}

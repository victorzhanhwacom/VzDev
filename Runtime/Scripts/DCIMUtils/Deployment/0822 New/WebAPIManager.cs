using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.FileUtils;

namespace VzDev.DCIMUtils.DeploymentUtils
{
    public class WebAPIManager : MonoBehaviour
    {
        #region Fields



        #endregion

        /// <summary>
        /// 取得機房內機櫃群資訊
        /// </summary>
        [Button]
        public void GetRackListInformation()
        {
            string result = "";
            #region For Demo
            result = FileHelper.LoadTextFileDirectly("機房一.json", EnumFilePath.streamingAssetsPath);
            #endregion
            OnGetRackListInformationAction?.Invoke(result);
        }


        /// <summary>
        /// 取得上架庫存設備列表
        /// </summary>
        [Button]
        public void GetEquipmentAssetInStock()
        {
            #region For Demo
            #endregion
            //OnGetEquipmentAssetInStockAction?.Invoke("");
        }

        /// <summary>
        /// 取得設備模型列表
        /// </summary>
        [Button]
        public void GetEquipmentModels()
        {
            #region For Demo
            #endregion
            //OnGetEquipmentModelsAction?.Invoke(new List<Transform>());
        }


        #region Static Events
        /// <summary>
        /// 取得機櫃資料(json字串)
        /// </summary>
        public static Action<string> OnGetRackListInformationAction;
        /// <summary>
        /// 設得庫存設備資料列表(json字串)
        /// </summary>
        public static Action<string> OnGetEquipmentAssetInStockAction;
         /// <summary>
        /// 取得設備模型列表
        /// </summary>
        public static Action<List<Transform>> OnGetEquipmentModelsAction;
        #endregion
    }
}

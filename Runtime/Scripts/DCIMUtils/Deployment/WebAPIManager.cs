using System;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.Deployment
{
    public class WebAPIManager : MonoBehaviour
    {
        #region Fields
        [SerializeField] private EnumSource source = EnumSource.WebAPI;
        [SerializeField, ShowIf("isFromJsonFile")] private string jsonFileName = "機房一.json";

        [SerializeField] private List<GameObject> equipmentModels = new List<GameObject>();

        [Foldout("[Events]"), SerializeField, HideIf("isPlaying")] private UnityEvent<bool, List<GameObject>> OnGetEquipmentModelsEvent;
        [Foldout("[Events]"), SerializeField, HideIf("isPlaying")] private UnityEvent<bool, string> OnGetEquipmentAssetInStockEvent;
        [Foldout("[Events]"), SerializeField, HideIf("isPlaying")] private UnityEvent<bool, string> OnGetRackListInformationEvent;

        private bool isPlaying => Application.isPlaying;
        private bool isFromJsonFile => source == EnumSource.JsonFile;
        #endregion

        private void Awake()
        {
            if (isPlaying)
            {
                OnGetRackListInformationEvent.RemoveAllListeners();
                OnGetEquipmentAssetInStockEvent.RemoveAllListeners();
                OnGetEquipmentModelsEvent.RemoveAllListeners();
            }
        }

        /// <summary>
        /// 取得設備模型列表
        /// </summary>
        [Button]
        public void GetEquipmentModels()
        {
            OnGetEquipmentModelsEvent?.Invoke(true, equipmentModels);
        }

        /// <summary>
        /// 取得上架庫存設備列表
        /// </summary>
        [Button]
        public void GetEquipmentAssetInStock()
        {
            OnGetEquipmentAssetInStockEvent?.Invoke(true, "");
            OnGetEquipmentAssetInStockAction?.Invoke(true, "");
        }

        /// <summary>
        /// 取得機房內機櫃群資訊
        /// </summary>
        [Button]
        public void GetRackListInformation()
        {
            string jsonData = "";
            if (isFromJsonFile)
            {
                string path = Path.Combine(Application.streamingAssetsPath, jsonFileName.Trim());
                jsonData = File.ReadAllText(path); //假設為WebAPI回傳的JSON字串
                Debug.Log($"從Json檔案取得機櫃群資訊，檔案路徑: {jsonFileName} \n{jsonData.ToJsonFormat()}");
            }
            else
            {
                Debug.LogWarning("WebAPIManager.GetRackListInformation() 尚未實作 WebAPI 取得機櫃群資訊的功能，請自行補齊。");
            }
            OnGetRackListInformationEvent?.Invoke(true, jsonData);
            OnGetRackListInformationAction?.Invoke(true, jsonData);
        }

        #region Static Events
        public static Action<bool, string> OnGetRackListInformationAction;
        public static Action<bool, string> OnGetEquipmentAssetInStockAction;
        public static Action<bool, List<GameObject>> OnGetEquipmentModelsAction;
        #endregion

        private enum EnumSource
        {
            WebAPI, JsonFile
        }
    }
}

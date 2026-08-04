using System;
using System.Collections.Generic;
using System.IO;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.Import;
using VzDev.DCIM.RevitAssetDataStructure;

public class WebAPI_GetRackList : MonoBehaviour
{
    #region Fields
    [SerializeField, ReadOnly] private List<DCR_Asset> dcrAssetList = new List<DCR_Asset>();
    [Foldout("[Settings]"), SerializeField] private string jsonFileName = "機房一.json"; // 放在 Assets/StreamingAssets/ 底下
    private bool isHaveAssets => (dcrAssetList != null && dcrAssetList.Count > 0);
    private string path = null;
    private string jsonData = null;
    #endregion

    [Button]
    public void GetDataFromWebAPI()
    {
        path ??= Path.Combine(Application.streamingAssetsPath, jsonFileName);
        jsonData = File.ReadAllText(path); //假設為WebAPI回傳的JSON字串

        dcrAssetList = RackAssetJsonConverter.ParseFromJson(File.ReadAllText(path));
        OnGetRackAssetsEvent?.Invoke(dcrAssetList);
    }

    [Button, ShowIf("isHaveAssets")]
    private void ClearData() => dcrAssetList?.Clear();

    public static Action<List<DCR_Asset>> OnGetRackAssetsEvent;
}
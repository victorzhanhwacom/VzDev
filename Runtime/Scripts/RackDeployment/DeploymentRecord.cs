using System;
using System.Collections.Generic;

namespace VzDev.DCIM.Deployment
{
    /// <summary>
    /// 一次完整上架動作的紀錄，Step5確認上架後產生，交給 DeploymentPersistenceService 存檔。
    /// </summary>
    [Serializable]
    public class DeploymentRecord
    {
        public string rackAssetNo;
        public RackSlotAssignment assignment;
        public string customName;
        public string note;
        public long deployedAtUnixSeconds;
    }

    /// <summary>
    /// JsonUtility 序列化用的容器：JsonUtility 不支援直接把 List&lt;T&gt; 當作序列化根物件，
    /// 必須包一層帶欄位的類別才能正確存取。
    /// </summary>
    [Serializable]
    public class DeploymentSaveData
    {
        public List<DeploymentRecord> records = new();
    }
}
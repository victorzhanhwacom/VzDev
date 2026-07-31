using VzDev.DCIM.RevitAssetDataStructure;
using VzDev.DCIMUtils.ModelInteractUtils;
using VzDev.DCIMUtils.RackDeployment;

namespace VzDev
{
    /// <summary>
    /// 【配置管理模組修改】新增 OnEnable/OnDisable 與 SetData 時自我登記/取消到 RackRegistry，
    /// 供 Step2 外殼變色需要「列舉場景中所有機櫃」時查詢（IHasDCIMAsset 只能反查單一物件，
    /// 沒辦法反過來列舉「有哪些機櫃」）。
    /// </summary>
    public class RackComponent : ModelComponentBase<DCR_Asset>
    {
      /*   public override void SetData(DCR_Asset assetData)
        {
            base.SetData(assetData);
            if (isActiveAndEnabled) RackRegistry.Register(gameObject, assetData);
        }

        private void OnEnable()
        {
            if (GetAsset() is DCR_Asset rack) RackRegistry.Register(gameObject, rack);
        }

        private void OnDisable() => RackRegistry.Unregister(gameObject); */
    }
}
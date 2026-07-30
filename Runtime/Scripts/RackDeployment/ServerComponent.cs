using VzDev.DCIM.Deployment;
using VzDev.DCIMUtils.ModelInteractUtils;

namespace VzDev
{
    /// <summary>
    /// 伺服主機模型專屬的 ModelComponent 閉包子類別，命名對稱既有的 RackComponent。
    /// 掛在 DeployedModelSpawner 生成出來的DCS實體模型上，讓它具備點擊/Hover事件
    /// 與 IHasDCIMAsset 資料存取管線（例如之後點擊已上架設備要跳出資訊面板/Tooltip）。
    /// </summary>
    public class ServerComponent : ModelComponentBase<DCS_Asset> { }
}
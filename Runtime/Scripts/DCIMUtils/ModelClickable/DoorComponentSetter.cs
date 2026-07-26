using System;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils
{
    public class DoorComponentSetter : ModelComponentSetterBase<DoorAsset, DoorComponent>
    {
    }

    [Serializable]
    public class DoorAsset: DCIMAsset
    {
    }
}

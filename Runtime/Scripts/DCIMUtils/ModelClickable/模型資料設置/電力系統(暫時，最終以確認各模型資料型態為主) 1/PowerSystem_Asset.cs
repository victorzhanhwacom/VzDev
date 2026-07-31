using System;
using VzDev.DCIM.RevitAssetDataStructure;

namespace VzDev
{
    [Serializable]
    public class PowerSystem_Asset : DCIMAsset
    {
        public float capacity, voltage, current, power, powerFactor, frequency, temperature, soc;
    }
}

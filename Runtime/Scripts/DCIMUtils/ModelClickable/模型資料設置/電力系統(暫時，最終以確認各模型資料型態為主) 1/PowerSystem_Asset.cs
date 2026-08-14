using System;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev
{
    [Serializable]
    public class PowerSystem_Asset : DCIMAsset
    {
        public float capacity, voltage, current, power, powerFactor, frequency, temperature, soc;
    }
}

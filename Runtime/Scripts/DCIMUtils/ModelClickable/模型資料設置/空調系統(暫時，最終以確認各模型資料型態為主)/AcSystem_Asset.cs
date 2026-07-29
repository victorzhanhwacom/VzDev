using System;
using VzDev.DCIM.Deployment;

namespace VzDev
{
    [Serializable]
    public class AcSystem_Asset : DCIMAsset
    {
        public float outputPressure, outputFlowRate, outputTemperature;
        public float inputPressure, inputFlowRate, inputTemperature;
    }
}

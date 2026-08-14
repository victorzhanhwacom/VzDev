using System;
using VzDev.DCIMUtils.DataUtils;

namespace VzDev
{
    [Serializable]
    public class AcSystem_Asset : DCIMAsset
    {
        public float outputPressure, outputFlowRate, outputTemperature;
        public float inputPressure, inputFlowRate, inputTemperature = 32.7f;
    }
}

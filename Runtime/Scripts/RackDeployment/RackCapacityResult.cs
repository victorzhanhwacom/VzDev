using System;

namespace VzDev.DCIM.Deployment
{
    [Flags]
    public enum RackCapacityShortfall
    {
        None = 0,
        Power = 1 << 0,
        Weight = 1 << 1,
        Space = 1 << 2,
    }

    /// <summary>
    /// 單一機櫃對某設備的容量評估結果，供 Step2 外殼變色 / Step3 拖曳合法性檢查共用，
    /// 兩處都只呼叫 RackCapacityEvaluator.Evaluate 取得這個結果，不重複計算邏輯。
    /// </summary>
    public readonly struct RackCapacityResult
    {
        public readonly int remainingPowerWatt;
        public readonly float remainingWeightKg;
        public readonly int remainingUSlots;
        public readonly RackCapacityShortfall shortfall;

        public bool Fits => shortfall == RackCapacityShortfall.None;

        public RackCapacityResult(int remainingPower, float remainingWeight, int remainingU, RackCapacityShortfall shortfall)
        {
            remainingPowerWatt = remainingPower;
            remainingWeightKg = remainingWeight;
            remainingUSlots = remainingU;
            this.shortfall = shortfall;
        }
    }
}
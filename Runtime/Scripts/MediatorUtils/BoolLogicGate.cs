using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev.MediatorUtils
{
    /// Bool邏輯閘
    public class BoolLogicGate : MonoBehaviour
    {
        #region Variables

        [Label("[邏輯設定]"), SerializeField] private List<BoolLogic> boolLogics;
        [Label("[Toggle對像群]"), SerializeField] private List<Toggle> toggles;

        #endregion

        public void SetBoolValue(bool value)
        {
            bool toggleIsOn = toggles.Any(target=>target.isOn);
            
            for (int i = 0; i < boolLogics.Count; i++)
            {
                var result = Evaluate(value, toggleIsOn, boolLogics[i].boolGateType);
                boolLogics[i].onResultEvent?.Invoke(result);
                (result? boolLogics[i].onTrueEvent: boolLogics[i].onFalseEvent)?.Invoke();
            }
        }
        
        /// 統一邏輯閘計算方法
        public static bool Evaluate(bool a, bool b, BoolGateType type)
        {
            switch (type)
            {
                case BoolGateType.AND:  return a && b;
                case BoolGateType.OR:   return a || b;
                case BoolGateType.NOT:  return !a;
                case BoolGateType.NAND: return !(a && b);
                case BoolGateType.NOR:  return !(a || b);
                case BoolGateType.XOR:  return a ^ b;
                case BoolGateType.XNOR: return !(a ^ b);
            }
            return false;
        }
        

        [Serializable]
        public struct BoolLogic
        {
            public BoolGateType boolGateType;
            public UnityEvent<bool> onResultEvent;
            public UnityEvent onTrueEvent, onFalseEvent;
        }
        
        public enum BoolGateType
        {
            /// AND：兩個都為 true 才 true
            AND,
            /// OR：只要有一個為 true 就 true
            OR,
            /// NOT：輸入反相（true → false / false → true）
            NOT,
            /// NAND：AND 的反相；只有兩個都 true 才 false
            NAND,
            /// NOR：OR 的反相；只有兩個都 false 才 true
            NOR,
            /// XOR：互斥；兩個輸入「不同」才 true
            XOR,
            /// XNOR：互斥相等；兩個輸入「相同」才 true
            XNOR,
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VzDev.ApiExtensions
{
    /// 供像Dictionary需要Inspector視覺化的使用
    /// <para>+ 夾帶Key與其值 </para>
    /// <para>+ T, V: 可以任意Class </para>
    [Serializable]
    public class KeyValueData<TKey, TValue>
    {
        [field:SerializeField]
        public TKey Key { get; set; }
        [field:SerializeField]
        public TValue Value{ get; set; }

        public KeyValueData(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
        
        public KeyValueData(KeyValuePair<TKey, TValue> kvp)
        {
            Key = kvp.Key;
            Value = kvp.Value;
        }
        
        public override string ToString() => $"[KeyValueItemData] key:{Key}, value:{Value}";
    }
}
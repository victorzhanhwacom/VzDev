using System;
using System.Collections.Generic;
using UnityEngine;

namespace VzDev.Frameworks
{
    /// <summary>
    /// 泛型靜態註冊表。每個封閉型別（例如 Registry&lt;string, DeviceModel&gt;）各自擁有獨立的名冊。
    /// 物件在 OnEnable 登記、OnDisable 註銷；使用端以 key O(1) 查詢，或用 for 迴圈零 GC 遍歷。
    /// 注意：移除採 swap-remove，遍歷順序不保證等於登記順序。
    /// </summary>
    public static class Registry<TKey, TValue> where TValue : class
    {
        #region Static Action
        public static event Action<TValue> OnRegistered;
        public static event Action<TValue> OnUnregistered;
        #endregion

        #region Fields
        static readonly Dictionary<TKey, int> _index = new();
        static readonly List<TKey> _keys = new();
        static readonly List<TValue> _values = new();
        public static int Count => _values.Count;
        #endregion

        /// <summary>
        /// 建構子
        /// </summary>
        static Registry() => RegistryReset.Add(Clear);

        #region 查詢是否已登記 / 取值
        /// <summary>
        /// 以 key 查詢是否已登記，O(1)
        /// </summary>
        public static bool Contains(TKey key) => _index.ContainsKey(key);

        /// <summary>
        /// 以 key 查詢物件，O(1)
        /// </summary>
        public static bool TryGet(TKey key, out TValue value)
        {
            if (_index.TryGetValue(key, out int i))
            {
                value = _values[i];
                return true;
            }
            value = null;
            return false;
        }
        /// <summary>
        /// 供外部可用Count()來以索引遍歷：for (int i = 0; i &lt; Count; i++) Get(i)，不產生 GC。
        /// </summary>
        public static TValue GetAt(int i) => _values[i];
        #endregion

        #region 登記 / 註銷 物件
        /// <summary>
        /// 登記物件，O(1)
        /// </summary>
        public static bool Register(TKey key, TValue value)
        {
            if (value == null)
            {
                Debug.LogWarning($"[Registry<{typeof(TValue).Name}>] 嘗試登記 null，key = {key}");
                return false;
            }
            if (_index.TryGetValue(key, out int existing))
            {
                if (ReferenceEquals(_values[existing], value)) return false; // 重複登記同一個實例
                Debug.LogWarning($"[Registry<{typeof(TValue).Name}>] key 重複：{key}，已被其他物件佔用");
                return false;
            }

            _index.Add(key, _values.Count);
            _keys.Add(key);
            _values.Add(value);
            OnRegistered?.Invoke(value);
            return true;
        }

        /// <summary>
        /// 只有同一個實例才能註銷，避免 key 被別人佔用時誤刪。
        /// </summary>
        public static bool Unregister(TKey key, TValue value)
        {
            if (!_index.TryGetValue(key, out int i)) return false;
            if (!ReferenceEquals(_values[i], value)) return false;

            // swap-remove：把最後一筆搬到被刪的位置，O(1)
            int last = _values.Count - 1;
            if (i != last)
            {
                TKey lastKey = _keys[last];
                _keys[i] = lastKey;
                _values[i] = _values[last];
                _index[lastKey] = i;
            }
            _keys.RemoveAt(last);
            _values.RemoveAt(last);
            _index.Remove(key);

            OnUnregistered?.Invoke(value);
            return true;
        }
        #endregion

        #region 觀察者模式 - 進行函式訂閱，當有新物件登記時會呼叫 onAdded
        /// <summary>
        /// 先對「已登記」的每個物件呼叫 onAdded，再訂閱之後新登記的物件。
        /// 不論訂閱者比物件早或晚載入，都不會漏掉。
        /// </summary>
        public static void Observe(Action<TValue> onAdded)
        {
            int count = _values.Count;
            for (int i = 0; i < count; i++) onAdded(_values[i]);
            OnRegistered += onAdded;
        }

        /// <summary>
        /// 取消訂閱 onAdded，之後新登記的物件就不會再呼叫。
        /// </summary>
        public static void Unobserve(Action<TValue> onAdded) => OnRegistered -= onAdded;
        #endregion

        public static void Clear()
        {
            _index.Clear();
            _keys.Clear();
            _values.Clear();
            OnRegistered = null;
            OnUnregistered = null;
        }
    }

    /// <summary>
    /// 泛型類別無法使用 [RuntimeInitializeOnLoadMethod]，因此由這個非泛型類別統一重置。
    /// 關閉 Domain Reload（Enter Play Mode Options）時，避免上一次 Play 的資料殘留。
    /// </summary>
    internal static class RegistryReset
    {
        static readonly List<Action> _clears = new();

        internal static void Add(Action clear) => _clears.Add(clear);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll()
        {
            for (int i = 0; i < _clears.Count; i++) _clears[i]();
        }
    }

    /*
            使用方式：
            public static class RackRegistry
            {
                public static bool Register(RackController r)   => Registry<string, RackController>.Register(r.Id, r);
                public static bool Unregister(RackController r) => Registry<string, RackController>.Unregister(r.Id, r);
                public static bool TryGet(string id, out RackController r) => Registry<string, RackController>.TryGet(id, out r);
            }

            public class SensorModel : MonoBehaviour
            {
                [SerializeField] string id;
                public string Id => id;

                void OnEnable()
                {
                    SensorRegistry.Register(id, this);          // 物件：登記自己，讓場景4 找得到
                    WebAPIManager.OnSensorUpdated += Apply;     // 資料：訂閱 API
                    if (WebAPIManager.TryGetSensor(id, out var d)) Apply(d);
                }
                void OnDisable()
                {
                    SensorRegistry.Unregister(id, this);
                    WebAPIManager.OnSensorUpdated -= Apply;
                }
            }

            https://claude.ai/share/03d984eb-d175-42f3-b208-8b0b179ca79d
    */
}

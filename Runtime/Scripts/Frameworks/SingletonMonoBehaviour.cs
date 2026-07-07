using UnityEngine;

namespace VzDev.DebugUtils
{
    /// 單例模式，可static呼叫，可掛載於GameObject上
    /// <para>偵測Instance是否存在並自動新建於場景上</para>
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component, new()
    {
        private static T instance { get; set; }

        protected static string objName => $"[Singleton] - {typeof(T).Name}";

        protected static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();

                    if (instance == null)
                    {
                        GameObject obj = new GameObject();
                        instance = obj.AddComponent<T>();
                        instance.name = objName;
                    }
                }

                return instance;
            }
        }

        protected virtual void Awake() => instance ??= this as T;

        protected virtual void OnValidate() => name = objName;
    }
}

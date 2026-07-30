using System.Collections.Generic;
using UnityEngine;
using VzDev.DCIM.Deployment;

namespace VzDev.DCIMUtils.RackDeployment
{
    /// <summary>
    /// GameObject → DCR_Asset 的查表註冊表，模式與 ModelToggleRegistry / AssetDataDisplayRegistry 相同：
    /// 掛載 RackComponent 的機櫃模型在 OnEnable/OnDisable 自我登記/取消，不受樓層 Additive
    /// 場景載入/卸載影響。
    /// </summary>
    public static class RackRegistry
    {
        private static readonly Dictionary<GameObject, DCR_Asset> registry = new();
        private static readonly List<GameObject> allRackObjects = new();

        public static void Register(GameObject rackObject, DCR_Asset asset)
        {
            if (rackObject == null || asset == null) return;
            if (!registry.ContainsKey(rackObject)) allRackObjects.Add(rackObject);
            registry[rackObject] = asset;
        }

        public static void Unregister(GameObject rackObject)
        {
            if (rackObject == null) return;
            registry.Remove(rackObject);
            allRackObjects.Remove(rackObject);
        }

        public static bool TryGetRackAsset(GameObject rackObject, out DCR_Asset asset)
            => registry.TryGetValue(rackObject, out asset);

        public static IReadOnlyList<GameObject> AllRackObjects => allRackObjects;
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace VzDev.MaterialUtils
{
    /// 材質替換的共享狀態權威，處理多個 Requester 對同一 Transform 的重疊需求
    public class MaterialStateService : MonoBehaviour
    {
        private static MaterialStateService _instance;
        public static MaterialStateService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<MaterialStateService>();
                return _instance;
            }
        }

        private class Entry
        {
            public Material[] originalMaterials; // 只在第一次被請求時快取
            public Dictionary<object, Material> requesters = new();
        }

        private readonly Dictionary<Transform, Entry> _entries = new();

        /// 模組要求替換材質（Open 時呼叫）
        public void Request(object requester, List<Transform> targets, Material material, List<Transform> exclude = null)
        {
            var excludeSet = exclude != null ? new HashSet<Transform>(exclude) : null;

            foreach (var target in targets)
            {
                if (target == null || (excludeSet != null && excludeSet.Contains(target))) continue;

                if (!_entries.TryGetValue(target, out var entry))
                {
                    entry = new Entry { originalMaterials = CacheOriginal(target) };
                    _entries[target] = entry;
                }

                entry.requesters[requester] = material;
                ApplyEffective(target, entry);
            }
        }

        /// 模組取消替換需求（Close 時呼叫）
        public void Release(object requester, List<Transform> targets)
        {
            foreach (var target in targets)
            {
                if (target == null || !_entries.TryGetValue(target, out var entry)) continue;

                entry.requesters.Remove(requester);

                if (entry.requesters.Count == 0)
                {
                    RestoreOriginal(target, entry.originalMaterials);
                    _entries.Remove(target);
                }
                else
                {
                    ApplyEffective(target, entry); // 還有其他人要求，改套用剩下需求中的材質
                }
            }
        }

        private Material[] CacheOriginal(Transform target)
        {
            var renderer = target.GetComponent<MeshRenderer>();
            return renderer != null ? renderer.sharedMaterials : null;
        }

        private void RestoreOriginal(Transform target, Material[] original)
        {
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null && original != null)
            {
                renderer.sharedMaterials = original;
                if(target.TryGetComponent(out Collider collider)) collider.enabled = true;
            }
        }

        private void ApplyEffective(Transform target, Entry entry)
        {
            // 衝突策略：目前是「最後一個要求者優先」，若需要可改成 priority 欄位排序
            Material effective = null;
            foreach (var kv in entry.requesters) effective = kv.Value;

            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null || effective == null) return;

            var mats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = effective;
            renderer.sharedMaterials = mats;
            if(target.TryGetComponent(out Collider collider)) collider.enabled = false;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VzDev.ApiExtensions;
using VzDev.UnityAPI.Extensions;
using static VzDev.UnityAPI.Extensions.TransformExtension;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.ObjectUtils
{
    /// <summary>
    /// 集中式模型搜尋器，透過關鍵字或 Transform 陣列搜尋目標物件，並觸發事件
    /// </summary>
    public class ModelFinder : MonoBehaviour
    {
        #region Fields
        [SerializeField] private bool logEnabled = false;
        [SerializeField] private string[] keywords;
        [SerializeField] private List<Transform> keyModels;
        [SerializeField] private List<Transform> foundModels;
        [Foldout("[Events]")] public UnityEvent<List<Transform>> onFoundModels;
        [Foldout("[Components]"), SerializeField] private Transform targetModelsParent;
        [Foldout("[Settings]"), SerializeField] private EnumSearchType searchType = EnumSearchType.Include;
        [Foldout("[Settings]"), SerializeField] private bool isIncludeInactive = true;
        [Foldout("[Settings]"), SerializeField] private EnumComponentType enumComponentType = EnumComponentType.MeshRenderer;

        public List<Transform> FoundModels => foundModels;
        public EnumSearchType SearchType => searchType;

        public enum EnumComponentType
        {
            None,
            MeshRenderer,
            Collider,
        }

        private bool IsHaveKeywords => keywords != null && keywords.Length > 0;
        private bool IsHaveModels => keyModels != null && keyModels.Count > 0;
        private bool IsFoundModels => foundModels != null && foundModels.Count > 0;

        #endregion

        public void AddKeyModels(List<Transform> models)
        {
            if (keyModels == null)
            {
                keyModels = new List<Transform>();
            }
            keyModels.AddRangeWithDistinct(models);
        }

        public void SetKeyModels(List<Transform> models) => keyModels = models;

        public void FindeModelsExceptModels(List<Transform> models)
        {
            keyModels = models;
            FindModelsByTransforms();
        }

        [Button, ShowIf(nameof(IsHaveKeywords))]
        public void FindModelsByKeywords()
        {
            foundModels?.Clear();
            switch (enumComponentType)
            {
                case EnumComponentType.None:
                    targetModelsParent.FindChildrenByKeywords(searchType: searchType, keywords: keywords, results: foundModels, includeInactive: isIncludeInactive);
                    break;
                case EnumComponentType.MeshRenderer:
                    targetModelsParent.FindChildrenByKeywords<MeshRenderer>(searchType: searchType, keywords: keywords, results: foundModels, includeInactive: isIncludeInactive);

                    break;
                case EnumComponentType.Collider:
                    targetModelsParent.FindChildrenByKeywords<Collider>(searchType: searchType, keywords: keywords, results: foundModels, includeInactive: isIncludeInactive);

                    break;
            }
            onFoundModels?.Invoke(foundModels);
            Debug.Assert(logEnabled, $"Found {foundModels.Count} target objects.", this);
        }

        [Button, ShowIf(nameof(IsHaveModels))]
        public void FindModelsByTransforms()
        {
            if (targetModelsParent == null || keyModels == null) return;

            foundModels?.Clear();

            // 1. 取得 targetModelsParent 底下所有的子孫 MeshRenderer 物件的 Transform
            // (這步驟模擬你原本擴充函式內部抓取子物件的行為)
            List<Transform> allChildren = targetModelsParent.GetComponentsInChildren<MeshRenderer>(true)
                                                .Select(mr => mr.transform)
                                                .ToList();

            // 將面板上的 keyModels「自身 + 所有子孫」都攤平進 HashSet，
            // 避免只比對 keyModel 本身的 Transform 參照，導致其子物件被漏判。
            // (Bug 根因：allChildren 是 targetModelsParent 底下所有 MeshRenderer 子孫，
            //  其中包含 keyModel 的子物件；但舊版 modelSet 只裝了 keyModel 自己，
            //  Exclude 模式下 !modelSet.Contains(child) 會誤判為 true，把子物件也留下來)
            HashSet<Transform> modelSet = new HashSet<Transform>();
            for (int i = 0; i < keyModels.Count; i++)
            {
                Transform key = keyModels[i];
                if (key == null) continue;
                modelSet.Add(key);
                Transform[] descendants = key.GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < descendants.Length; j++)
                {
                    modelSet.Add(descendants[j]);
                }
            }

            // 2. 根據 searchType 進行過濾
            IEnumerable<Transform> filtered;
            if (searchType == EnumSearchType.Include)
            {
                // 包含：留下來的物件必須屬於某個 keyModel 的子樹（含自身）
                filtered = allChildren.Where(t => modelSet.Contains(t));
            }
            else
            {
                // 排除：留下來的物件必須不屬於任何 keyModel 的子樹（含自身）
                filtered = allChildren.Where(t => !modelSet.Contains(t));
            }

            // 3. 將結果寫入並觸發事件
            foundModels.AddRange(filtered);
            InvokeFoundModels();
        }

        [Button, ShowIf(nameof(IsFoundModels))]
        private void InvokeFoundModels()
        {
            if (foundModels != null && foundModels.Count > 0)
            {
                onFoundModels?.Invoke(foundModels);
            }
        }


#if UNITY_EDITOR
        [Button, ShowIf(nameof(IsFoundModels))]
        public void SelectObjects() => Selection.objects = foundModels.Select(t => t.gameObject).ToArray<Object>();
#endif
    }
}
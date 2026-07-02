using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VzDev.UnityAPI.Extensions;
using static VzDev.UnityAPI.Extensions.TransformExtension;

namespace VzDev.ObjectUtils
{
    /// 依關鍵字尋找模型
    public class ModelFinder : MonoBehaviour
    {
        #region Fields
        [SerializeField] private string[] keywords;
        [SerializeField] private List<Transform> keyModels;
        [SerializeField] private List<Transform> foundModels;
        [Foldout("[Events]")] public UnityEvent<List<Transform>> onFoundModels;
        [Foldout("[Components]"), SerializeField] private Transform targetModelsParent;
        [Foldout("[Settings]"), SerializeField] private EnumSearchType searchType = EnumSearchType.Include;
        [Foldout("[Settings]"), SerializeField] private bool isIncludeInactive = true;
        
        private bool IsHaveKeywords => keywords != null && keywords.Length > 0;
        private bool IsHaveModels => keyModels != null && keyModels.Count > 0;
        private bool IsFoundModels  => foundModels != null && foundModels.Count > 0;

        #endregion

        [Button, ShowIf(nameof(IsHaveKeywords))]
        public void FindModelsByKeywords()
        {
            foundModels?.Clear();
            targetModelsParent.FindChildrenByKeywords<MeshRenderer>(searchType:searchType, keywords: keywords, results: foundModels, includeInactive: isIncludeInactive);
            onFoundModels?.Invoke(foundModels);
            Debug.Log($"Found {foundModels.Count} target objects.", this);
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

            // 將面板上的 models 轉為 HashSet 以提升比對效能
            HashSet<Transform> modelSet = new HashSet<Transform>(keyModels);

            // 2. 根據 searchType 進行過濾
            IEnumerable<Transform> filtered;
            if (searchType == EnumSearchType.Include)
            {
                // 包含：留下來的物件必須存在於 models 陣列中
                filtered = allChildren.Where(t => modelSet.Contains(t));
            }
            else
            {
                // 排除：留下來的物件必須不存在於 models 陣列中
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
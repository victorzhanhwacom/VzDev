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
        #region Variables

        [SerializeField] private string[] keywords;
        [SerializeField] private List<Transform> foundModels;
        [Foldout("[Events]")] public UnityEvent<List<Transform>> onFoundModels;
        [Foldout("[Components]"), SerializeField] private EnumSearchType searchType = EnumSearchType.Include;
        [Foldout("[Components]"), SerializeField] private Transform targetModelsParent;
        
        #endregion

        [Button]
        public void FindModelsByKeywords()
        {
            foundModels?.Clear();
            targetModelsParent.FindChildrenByKeywords<MeshRenderer>(searchType:searchType, keywords: keywords, results: foundModels);
            onFoundModels?.Invoke(foundModels);
            Debug.Log($"Found {foundModels.Count} target objects.", this);
        }
        
        
#if UNITY_EDITOR
        [Button]
        public void SelectObjects() => Selection.objects = foundModels.Select(t => t.gameObject).ToArray<Object>();
#endif

    }
}
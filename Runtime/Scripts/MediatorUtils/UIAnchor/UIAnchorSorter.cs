using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace _VictorDev.Framework
{
    /// 對UIAnchor進行前後排序
    public class UIAnchorSorter : MonoBehaviour
    {
        [Label("[資料項 - UIAnchor]"), SerializeField] private List<UIAnchorFollower> uiAnchorList;

        private void Update()
        {
            // 根据攝影機距离对Landmark进行排序并调整Sibling Index
            uiAnchorList.Sort((a, b) => b.DistanceFromCamera.CompareTo(a.DistanceFromCamera));
            for (int i = 0; i < uiAnchorList.Count; i++)
            {
                uiAnchorList[i].transform.SetSiblingIndex(i);
            }
        }
        
      /*   public void AddToSortList(UIAnchorFollower uiAnchorFollower) 
            => uiAnchorList.ClearMissingTargets().TryAdd(uiAnchor); */
        
        [Button]
        private void GetLandmarksFromThisContainer() => uiAnchorList = GetComponentsInChildren<UIAnchorFollower>(true).ToList();
    }
}
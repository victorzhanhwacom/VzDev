using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ObjectUtils
{
    /// <summary>
    /// 根據攝影機距離對UI Anchor進行排序，確保UI元素的渲染順序正確。
    /// </summary>
    public class UIAnchorSorter : MonoBehaviour
    {
        [SerializeField] private bool isIncludeInactive = false;

        [Label("[資料項 - UIAnchor]"), SerializeField] private List<UIAnchorFollower> uiAnchorList;

        public void SetAnchorList(List<UIAnchorFollower> list) => uiAnchorList = list;


        private void Update()
        {
            // 根据攝影機距离对Landmark进行排序并调整Sibling Index
            // 使用預先定義好的市場比較函式，完全 0 GC
            uiAnchorList?.Sort(CompareUiAnchors);

            // 2. 調整階層（只動 active 的物件）
            int siblingIndex = 0;
            for (int i = 0; i < uiAnchorList?.Count; i++)
            {
                if (uiAnchorList[i] != null && uiAnchorList[i].gameObject.activeInHierarchy)
                {
                    uiAnchorList[i].transform.SetSiblingIndex(siblingIndex);
                    siblingIndex++;
                }
            }
        }

        // 2. 抽出變成獨立的靜態或類別方法，避免每幀產生匿名委派 (Delegate)
        private int CompareUiAnchors(UIAnchorFollower a, UIAnchorFollower b)
        {
            // 預防萬一的空值檢查
            if (a == null) return 1;
            if (b == null) return -1;

            bool aActive = a.gameObject.activeInHierarchy;
            bool bActive = b.gameObject.activeInHierarchy;

            // 優先把沒啟動的 (Active = false) 丟到 List 的最後面
            if (aActive && !bActive) return -1;
            if (!aActive && bActive) return 1;
            if (!aActive && !bActive) return 0;

            // 都啟動的情況下，比較相機距離（由大到小排序）
            return b.DistanceFromCamera.CompareTo(a.DistanceFromCamera);
        }
        private void Start() => GetLandmarksFromThisContainer();
        [Button]
        public void GetLandmarksFromThisContainer() => uiAnchorList = GetComponentsInChildren<UIAnchorFollower>(isIncludeInactive).ToList();
    }
}
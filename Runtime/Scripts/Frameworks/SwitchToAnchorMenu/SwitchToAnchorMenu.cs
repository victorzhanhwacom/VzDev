using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.Frameworks.SwitchToAnchorMenuUtils
{
    public class SwitchToAnchorMenu : MonoBehaviour
    {
        #region Fileds
        [SerializeField] private List<AnchorItem> anchors;
        [Foldout("[Events]")] public UnityEvent<Transform, float> onAnchorSelected;
        #endregion

        public void SwitchToFollow(bool isSelected, string anchorName)
        {
            anchorName = anchorName.Trim();
            bool isTargetFloor;
            AnchorItem item;
            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i] == null)
                {
                    Debug.LogWarning($"AnchorItem at index {i} is null. Please check the anchors list in the SwitchToAnchorMenu component.");
                    continue;
                }
                item = anchors[i];

                isTargetFloor = item.anchorName == anchorName;
                if (isTargetFloor)
                {
                    item.InvokeEvents(isSelected);
                    onAnchorSelected?.Invoke(item.anchor, item.camDistance);
                }
            }
        }

        [Serializable]
        public class AnchorItem
        {
            public string anchorName;
            public Transform anchor;
            public float camDistance;

            public UnityEvent<bool> isSelected;
            public UnityEvent onSelected;
            public UnityEvent onDeselected;

            public void InvokeEvents(bool selected)
            {
                isSelected?.Invoke(selected);
                if (selected)
                {
                    onSelected?.Invoke();
                }
                else
                {
                    onDeselected?.Invoke();
                }
            }
        }
    }
}

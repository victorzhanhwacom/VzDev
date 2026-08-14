using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VzDev.DebugUtils;

namespace VzDev.ToolUtils
{
    public class PointTagGenerator : MonoBehaviour
    {
        #region Fields
        [SerializeField, Tooltip("是否顯示點位標籤"), OnValueChanged("OnShowPointTagsChanged")] private bool showPointTags = false;
        [SerializeField, Tooltip("是否總是顯示標籤文字"), OnValueChanged("OnAlwaysShowLabelChanged")] private bool alwaysShowLabel = false;
        [SerializeField, ReadOnly] private PointTag selectedPointTag;
        [SerializeField] private List<Transform> targetModels;
        [SerializeField, ReadOnly] private List<PointTag> pointTags;
        [Foldout("[Events]"), Tooltip("當點位標籤被選中時觸發")] public UnityEvent<Transform> onPointTagSelectedTransform;
        [Foldout("[Prefabs]"), SerializeField] private PointTag pointTagPrefab;
        [Foldout("[Components]"), SerializeField] private Transform pointsContainer;
        [Foldout("[Components]"), SerializeField] private ToggleGroup toggleGroup;
        [Foldout("[Components]"), SerializeField] private MonoBehaviour labelGetter;
        #endregion

        /// <summary>
        /// 清除所有已生成的點位標籤
        /// </summary>
        [Button]
        private void ClearTags()
        {
            pointTags ??= new List<PointTag>();
            if (pointTags.Count == 0) return;
            for (int i = pointTags.Count - 1; i >= 0; i--)
            {
                pointTags[i].OnToggleChangedAction -= OnToggleChanged;
                ObjectHelper.Destroy(pointTags[i].gameObject);
            }
            selectedPointTag = null;
        }

        #region Generate Point Tags
        /// <summary>
        /// 設定目標模型列表，並生成對應的點位標籤
        /// </summary>
        public void GeneratePointTags(List<Transform> models)
        {
            targetModels = models;
            GeneratePointTags();
        }

        /// <summary>
        /// 生成點位標籤，並將其與目標模型綁定
        /// </summary>
        [Button]
        private void GeneratePointTags()
        {
            IPointTagLabelGetter _labelGetter = GetLabelGetter();

            if (targetModels == null || targetModels.Count == 0)
            {
                Debug.LogWarning("沒有指定目標模型，無法生成點位標籤。", this);
                return;
            }
            ClearTags();

            for (int i = 0; i < targetModels.Count; i++)
            {
                Transform targetModel = targetModels[i];
                PointTag pointTag = Instantiate(pointTagPrefab, pointsContainer);
                pointTag.SetFollowerTarget(targetModel);
                pointTag.OnToggleChangedAction += OnToggleChanged;
                pointTags.Add(pointTag);

                string label = _labelGetter != null ? _labelGetter.GetLabel(targetModel) : targetModel.name;
                pointTag.name = label;
                pointTag.SetLabel(label);

                if (toggleGroup != null) pointTag.SetToggleGroup(toggleGroup);

                //更新顯示狀態
                SetTagVisible(showPointTags);
            }
            alwaysShowLabel = pointTagPrefab.LabelVisible;
        }
        private void OnToggleChanged(bool isOn, PointTag pointTag)
        {
            if (isOn)
            {
                selectedPointTag = pointTag;
                onPointTagSelectedTransform?.Invoke(selectedPointTag.FollowerTarget);
            }
        }
        #endregion

        private IPointTagLabelGetter GetLabelGetter()
        {
            if (labelGetter != null && labelGetter is IPointTagLabelGetter getter)
            {
                return getter;
            }
            return null;
        }

        #region Set Visible
        private void OnShowPointTagsChanged() => SetTagVisible(showPointTags);
        private void OnAlwaysShowLabelChanged() => SetLabelAlwaysVisible(alwaysShowLabel);
        /// <summary>
        /// 設定點位標籤的顯示與隱藏
        /// </summary>
        public void SetTagVisible(bool isVisible)
        {
            showPointTags = isVisible;
            if (pointTags == null || pointTags.Count == 0) return;
            for (int i = 0; i < pointTags.Count; i++)
            {
                pointTags[i].gameObject.SetActive(isVisible);
                if (isVisible)
                {
                    pointTags[i].OnToggleChangedAction += OnToggleChanged;
                }
                else
                {
                    pointTags[i].OnToggleChangedAction -= OnToggleChanged;
                }
            }
        }

        /// <summary>
        /// 設定Label是否永遠可見
        /// </summary>
        public void SetLabelAlwaysVisible(bool isVisible)
        {
            alwaysShowLabel = isVisible;
            if (pointTags == null || pointTags.Count == 0) return;
            for (int i = 0; i < pointTags.Count; i++)
            {
                pointTags[i].SetLabelAlwaysVisible(isVisible);
            }
        }
        #endregion

        private void OnValidate()
        {
            if (labelGetter != null && !(labelGetter is IPointTagLabelGetter))
            {
                Debug.LogWarning($"{labelGetter.name} 沒有實作 IPointTagLabelGetter,請重新指定。", this);
                labelGetter = null;
            }
        }
#if UNITY_EDITOR
        [Button]
        private void SelectAndFocusPointTags() => ObjectHelper.SelectAndFocus(pointTags.ConvertAll(tag => tag.gameObject).ToArray());
#endif
    }


    /// <summary>
    /// 提供一個介面，讓使用者可以自訂如何取得標籤文字
    /// </summary>
    public interface IPointTagLabelGetter
    {
        public string GetLabel(Transform targetModel);
    }
}

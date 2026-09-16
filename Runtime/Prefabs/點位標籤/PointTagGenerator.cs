using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using VzDev.DCIMUtils;
using VzDev.DebugUtils;

namespace VzDev.LandmarkUtils
{
    public class PointTagGenerator : MonoBehaviour
    {
        #region Fields
        [SerializeField, Tooltip("是否Dot簡易模式"), OnValueChanged("OnDotModeChanged")] private bool isDotMode = false;
        [SerializeField, Tooltip("是否顯示"), OnValueChanged("OnShowPointTagsChanged")] private bool showPointTags = false;
        [SerializeField] private List<Transform> targetModels;
        [SerializeField, ReadOnly] private List<PointTag> pointTags;
        [Foldout("[Prefabs]"), SerializeField] private PointTag pointTagPrefab;
        [Foldout("[Components]"), SerializeField] private Transform pointsContainer;
        [Foldout("[Components]"), SerializeField] private ToggleGroup toggleGroupDotMode, toggleGroupViewMode;
        #endregion

        private void OnDotModeChanged() => SetDotMode(isDotMode);
        private void OnShowPointTagsChanged() => SetTagVisible(showPointTags);
        public void SetDotMode(bool value)
        {
            isDotMode = value;
            foreach (var pointTag in pointTags)
            {
                if (pointTag != null) pointTag.SetIsDotMode(isDotMode);
            }
        }
        public void SetTagVisible(bool value)
        {
            showPointTags = value;
            foreach (var pointTag in pointTags)
            {
                if (pointTag != null) pointTag.gameObject.SetActive(showPointTags);
            }
        }

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
                if (pointTags[i] == null) continue;
                ObjectHelper.Destroy(pointTags[i].gameObject);
            }
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
                pointTag.SetModelName(DCIM_Helper.GetModelNameFromDeviceName(targetModel.name, true));
                pointTag.SetToggleViewModeGroup(toggleGroupViewMode);
                pointTag.SetToggleDotModeGroup(toggleGroupDotMode);
                pointTag.SetIsDotMode(isDotMode);
                pointTags.Add(pointTag);

                SetTagVisible(showPointTags);
                SetDotMode(isDotMode);
            }
        }
        #endregion


#if UNITY_EDITOR
        [Button]
        private void SelectAndFocusPointTags() => ObjectHelper.SelectAndFocus(pointTags.ConvertAll(tag => tag.gameObject).ToArray());
#endif
    }
}

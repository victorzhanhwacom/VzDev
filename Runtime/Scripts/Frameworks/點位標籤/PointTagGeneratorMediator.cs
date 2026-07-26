using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev.ToolUtils
{
    public class PointTagGeneratorMediator : MonoBehaviour
    {
        #region Fields
        [SerializeField] private PointTagGenerator[] pointTagGenerators;
        [SerializeField, OnValueChanged("OnShowPointTagsChanged")] private bool showPointTags = false;
        [SerializeField, OnValueChanged("OnAlwaysShowLabelChanged")] private bool alwaysShowLabel = false;
        private void OnShowPointTagsChanged() => SetVisible(showPointTags);
        private void OnAlwaysShowLabelChanged() => SetLabelAlwaysVisible(alwaysShowLabel);
        #endregion

        /// <summary>
        /// 設定點位標籤的顯示與隱藏
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            showPointTags = isVisible;
            if (pointTagGenerators == null || pointTagGenerators.Length == 0) return;
            for (int i = 0; i < pointTagGenerators.Length; i++)
            {
                pointTagGenerators[i].SetVisible(isVisible);
            }
        }

        /// <summary>
        /// 設定Label是否永遠可見
        /// </summary>
        public void SetLabelAlwaysVisible(bool isVisible)
        {
            alwaysShowLabel = isVisible;
            if (pointTagGenerators == null || pointTagGenerators.Length == 0) return;
            for (int i = 0; i < pointTagGenerators.Length; i++)
            {
                pointTagGenerators[i].SetLabelAlwaysVisible(isVisible);
            }
        }

        [Button]
        private void GetPointTagGeneratorsInChildren() => pointTagGenerators = GetComponentsInChildren<PointTagGenerator>(true);

        private void OnValidate()
        {
            if (pointTagGenerators == null || pointTagGenerators.Length == 0)
            {
                GetPointTagGeneratorsInChildren();
            }
        }
    }
}

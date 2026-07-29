using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace VzDev
{
    public class ModelToggleBindingGeneratorMediator : MonoBehaviour
    {
        #region Fields
        [SerializeField, Tooltip("是否顯示點位標籤"), OnValueChanged("OnShowPointTagsChanged"), ShowIf("isHaveModelToggleGenerators")]
        private bool showPointTags = false;
        [SerializeField, Tooltip("是否總是顯示標籤文字"), OnValueChanged("OnAlwaysShowLabelChanged"), ShowIf("isHaveModelToggleGenerators")]
        private bool alwaysShowLabel = false;
        [SerializeField, ReadOnly] private ModelToggleBindingGenerator[] modelToggleGenerators = new ModelToggleBindingGenerator[0];
        private bool isHaveModelToggleGenerators => modelToggleGenerators != null && modelToggleGenerators.Length > 0;

        private void OnShowPointTagsChanged() => SetVisible(showPointTags);
        private void OnAlwaysShowLabelChanged() => SetLabelAlwaysVisible(alwaysShowLabel);
        #endregion

        // <summary>
        /// 設定點位標籤的顯示與隱藏
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            showPointTags = isVisible;
            if (modelToggleGenerators == null || modelToggleGenerators.Length == 0) return;
            for (int i = 0; i < modelToggleGenerators.Length; i++)
            {
                modelToggleGenerators[i].SetVisible(isVisible);
            }
        }

        /// <summary>
        /// 設定Label是否永遠可見
        /// </summary>
        public void SetLabelAlwaysVisible(bool isVisible)
        {
            alwaysShowLabel = isVisible;
            if (modelToggleGenerators == null || modelToggleGenerators.Length == 0) return;
            for (int i = 0; i < modelToggleGenerators.Length; i++)
            {
                modelToggleGenerators[i].SetLabelAlwaysVisible(isVisible);
            }
        }

        [Button]
        private void GetAllModelToggleGenerators()
        {
            modelToggleGenerators = GetComponentsInChildren<ModelToggleBindingGenerator>(true);
        }
    }
}
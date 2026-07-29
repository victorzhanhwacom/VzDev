using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VzDev.DebugUtils;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.ObjectUtils;
using VzDev.ToolUtils;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev
{
    public class ModelToggleBindingGenerator : MonoBehaviour
    {
        #region Fields
        [SerializeField, Tooltip("是否顯示點位標籤"), OnValueChanged("OnShowPointTagsChanged")] private bool showPointTags = false;
        [SerializeField, Tooltip("是否總是顯示標籤文字"), OnValueChanged("OnAlwaysShowLabelChanged")] private bool alwaysShowLabel = false;

        private void OnShowPointTagsChanged() => SetVisible(showPointTags);
        private void OnAlwaysShowLabelChanged() => SetLabelAlwaysVisible(alwaysShowLabel);


        [SerializeField, ReadOnly] private List<Transform> targetModels;
        [SerializeField, ReadOnly] private ModelToggleBinding[] modelToggleBindings;
        public ModelToggleBinding[] ModelToggles => modelToggleBindings;

        [Foldout("[Components]"), SerializeField] private ModelToggleBinding modelTogglePrefab;
        [Foldout("[Components]"), SerializeField] private Transform container;
        [Foldout("[Components]"), SerializeField] private ToggleGroup toggleGroup;

        [Foldout("[Components]"), SerializeField, Required] private MonoBehaviour labelGetter;

        private ModelToggleBinding currentSelectedTag;
        //  [Foldout("[Events]")] public UnityEvent<ModelToggleBinding> onPointTagSelected;
        [Foldout("[Events]")] public UnityEvent<Transform> onPointTagSelectedTransform;

        /// <summary>
        /// 用於取得標籤文字的介面
        /// </summary>
        private IPointTagLabelGetter _labelGetter;
        #endregion

        // <summary>
        /// 設定點位標籤的顯示與隱藏
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            showPointTags = isVisible;
            if (modelToggleBindings == null || modelToggleBindings.Length == 0) return;
            for (int i = 0; i < modelToggleBindings.Length; i++)
            {
                modelToggleBindings[i].gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// 設定Label是否永遠可見
        /// </summary>
        public void SetLabelAlwaysVisible(bool isVisible)
        {
            alwaysShowLabel = isVisible;
            if (modelToggleBindings == null || modelToggleBindings.Length == 0) return;
            for (int i = 0; i < modelToggleBindings.Length; i++)
            {
                modelToggleBindings[i].SetLabelAlwaysVisible(isVisible);
            }
        }



        public void GenerateModelToggles(List<Transform> models)
        {
            targetModels = models;
            GenerateModelToggles();
        }

        /// <summary>
        /// 生成點位標籤，並將其與目標模型綁定
        /// </summary>
        public void GenerateModelToggles()
        {
            if (_labelGetter == null) SetLabelGetter();
            ClearExistingModelToggles();
            string labelText;

            modelToggleBindings = new ModelToggleBinding[targetModels.Count];
            for (int i = 0; i < targetModels.Count; i++)
            {
                Transform targetModel = targetModels[i];
                // 在每個目標模型的位置生成一個UI Anchor作為Tag
                ModelToggleBinding modelToggleBinding = Instantiate(modelTogglePrefab, targetModel.position, Quaternion.identity, container);
                modelToggleBindings[i] = modelToggleBinding;
                modelToggleBinding.SetTargetModel(targetModel);
                if (modelToggleBinding.TryGetComponent(out UIAnchorFollower follower))
                {
                    follower.SetTargetObject(targetModel);
                }

                // 使用Label Getter來決定Tag的顯示文字，如果沒有提供Label Getter，則使用模型名稱
                labelText = _labelGetter?.GetLabel(targetModel) ?? "unknown";
                modelToggleBinding.name = labelText;
                modelToggleBinding.SetLabel(labelText);
                if (toggleGroup != null) modelToggleBinding.SetToggleGroup(toggleGroup);

                modelToggleBinding.ToggleItem.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        currentSelectedTag = modelToggleBinding;
                        //onPointTagSelected?.Invoke(currentSelectedTag);
                        onPointTagSelectedTransform?.Invoke(currentSelectedTag.TargetModel.transform);
                    }
                    // if (toggleGroup != null && toggleGroup.AnyTogglesOn()) onPointTagDeselected?.Invoke();
                });

                //更新顯示狀態
                alwaysShowLabel = modelToggleBinding.LabelVisible;
                modelToggleBinding.gameObject.SetActive(showPointTags);
            }
        }

        private void ClearExistingModelToggles()
        {
            if (modelToggleBindings == null || modelToggleBindings.Length == 0) return;
            for (int i = modelToggleBindings.Length - 1; i >= 0; i--)
            {
                ObjectHelper.Destroy(modelToggleBindings[i].gameObject);
            }
            modelToggleBindings = new ModelToggleBinding[0];
        }


        private void SetLabelGetter()
        {
            if (labelGetter != null && labelGetter is IPointTagLabelGetter getter) _labelGetter = getter;
            else Debug.LogWarning("Label Getter does not implement IPointTagLabelGetter. Defaulting to model name.", this);
        }


        private void OnValidate()
        {
            if (labelGetter != null && !(labelGetter is IPointTagLabelGetter))
            {
                Debug.LogWarning($"{labelGetter.name} 沒有實作 IPointTagLabelGetter,請重新指定。", this);
                labelGetter = null;
            }
        }
    }
}

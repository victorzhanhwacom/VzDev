using System.Collections.Generic;
using NaughtyAttributes;
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
        [SerializeField] private List<Transform> targetModels;
        [Foldout("[Events]"), Tooltip("當點位標籤被選中時觸發")] public UnityEvent<Transform> onPointTagSelectedTransform;
        [Foldout("[Events]"), Tooltip("當點位標籤被選中時觸發")] public UnityEvent<PointTag> onPointTagSelected;
        [Foldout("[Events]"), Tooltip("當點位標籤被取消選中時觸發")] public UnityEvent onPointTagDeselected;
        [Foldout("[Components]"), SerializeField] private PointTag pointTagPrefab;
        [Foldout("[Components]"), SerializeField] private Transform pointsContainer;
        [Foldout("[Components]"), SerializeField] private ToggleGroup toggleGroup;

        // 這裡使用 MonoBehaviour 以便在 Inspector 中拖拽任何實現了 IPointTagLabelGetter 的組件
        [Foldout("[Components]"), SerializeField, Required] private MonoBehaviour labelGetter;

        /// <summary>
        /// 用於取得標籤文字的介面
        /// </summary>
        private IPointTagLabelGetter _labelGetter;

        public PointTag[] PointTags { get; private set; }

        private bool IsHaveData => Application.isPlaying && pointTagPrefab != null
            && pointsContainer != null && targetModels != null && targetModels.Count > 0;

        private PointTag currentSelectedTag;
        #endregion
        private void SetLabelGetter()
        {
            if (labelGetter != null && labelGetter is IPointTagLabelGetter getter) _labelGetter = getter;  
            else Debug.LogWarning("Label Getter does not implement IPointTagLabelGetter. Defaulting to model name.", this);
        }

        private void OnShowPointTagsChanged() => SetVisible(showPointTags);
        private void OnAlwaysShowLabelChanged() => SetLabelAlwaysVisible(alwaysShowLabel);

        /// <summary>
        /// 設定點位標籤的顯示與隱藏
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            showPointTags = isVisible;
            if (PointTags == null || PointTags.Length == 0) return;
            for (int i = 0; i < PointTags.Length; i++)
            {
                PointTags[i].gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// 設定Label是否永遠可見
        /// </summary>
        public void SetLabelAlwaysVisible(bool isVisible)
        {
            alwaysShowLabel = isVisible;
            if (PointTags == null || PointTags.Length == 0) return;
            for (int i = 0; i < PointTags.Length; i++)
            {
                PointTags[i].SetLabelAlwaysVisible(isVisible);
            }
        }


        /// <summary>
        /// 設定目標模型列表，並生成對應的點位標籤
        /// </summary>
        public void SetTargetModels(List<Transform> models) => targetModels = models;
        public void GeneratePointTags(List<Transform> models)
        {
            targetModels = models;
            GeneratePointTags();
        }

        /// <summary>
        /// 生成點位標籤，並將其與目標模型綁定
        /// </summary>
        [Button, ShowIf(nameof(IsHaveData))]
        public void GeneratePointTags()
        {
            if(_labelGetter == null) SetLabelGetter();
            ClearExistingTags();

            PointTags = new PointTag[targetModels.Count];
            for (int i = 0; i < targetModels.Count; i++)
            {
                Transform targetModel = targetModels[i];
                // 在每個目標模型的位置生成一個UI Anchor作為Tag
                PointTag pointTag = Instantiate(pointTagPrefab, targetModel.position, Quaternion.identity, pointsContainer);
                PointTags[i] = pointTag;

                // 可以在這裡對tag進行額外的設定，例如顯示點的座標等
                pointTag.SetFollowerTarget(targetModel); // 假設PointTag有這樣的方法來設定目標位置

                // 使用Label Getter來決定Tag的顯示文字，如果沒有提供Label Getter，則使用模型名稱
                pointTag.name = _labelGetter?.GetLabel(targetModel) ?? "unknown";
                pointTag.SetLabel(pointTag.name);
                if (toggleGroup != null) pointTag.ToggleItem.group = toggleGroup;

                pointTag.ToggleItem.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        currentSelectedTag = pointTag;
                        onPointTagSelected?.Invoke(currentSelectedTag);
                        onPointTagSelectedTransform?.Invoke(currentSelectedTag.FollowerTarget);
                    }
                    if (toggleGroup != null && toggleGroup.AnyTogglesOn()) onPointTagDeselected?.Invoke();
                });

                //更新顯示狀態
                alwaysShowLabel = pointTag.LabelVisible;
                pointTag.gameObject.SetActive(showPointTags);
            }
        }

        [Button, ShowIf(nameof(IsHaveData))]
        private void ClearExistingTags()
        {
            if (PointTags == null || PointTags.Length == 0) return;
            for (int i = PointTags.Length - 1; i >= 0; i--)
            {
                ObjectHelper.Destroy(PointTags[i].gameObject);
            }
            PointTags = new PointTag[0];
            currentSelectedTag = null;
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


    /// <summary>
    /// 提供一個介面，讓使用者可以自訂如何取得標籤文字
    /// </summary>
    public interface IPointTagLabelGetter
    {
        public string GetLabel(Transform targetModel);
    }
}

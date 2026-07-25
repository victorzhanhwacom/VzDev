using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev.ToolUtils
{
    public class PointTagGeneratorNEW : MonoBehaviour
    {
        #region Fields

        [SerializeField] private bool removeExistingTagsOnGenerate = true;
        [SerializeField] private List<Transform> targetModels;
        [Foldout("[Events]")] public UnityEvent onTagesInitialized;
        [Foldout("[Events]")] public UnityEvent<bool> onToggleValueChanged;
        [Foldout("[Components]"), SerializeField] private PointTag pointTagPrefab;
        [Foldout("[Components]"), SerializeField] private Transform pointsContainer;
        [Foldout("[Components]"), SerializeField] private ToggleGroup toggleGroup;

        // 這裡使用 MonoBehaviour 以便在 Inspector 中拖拽任何實現了 IPointTagLabelGetter 的組件
        [Foldout("[Components]"), SerializeField, Required] private MonoBehaviour labelGetter;
        
        private IPointTagLabelGetter _labelGetter;

        public PointTag[] PointTags { get; private set; }

        private bool IsHaveData => Application.isPlaying && pointTagPrefab != null
            && pointsContainer != null && targetModels != null && targetModels.Count > 0;
        #endregion

        private void Start()
        {
            if (labelGetter != null && labelGetter is IPointTagLabelGetter getter)
            {
                _labelGetter = getter;
            }
            else
            {
                Debug.LogWarning("Label Getter does not implement IPointTagLabelGetter. Defaulting to model name.", this);
            }

        }

        [Button, ShowIf(nameof(IsHaveData))]
        public void GeneratePointTags()
        {
            if (removeExistingTagsOnGenerate) ClearExistingTags();

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
                if(toggleGroup != null) pointTag.ToggleItem.group = toggleGroup;
                pointTag.ToggleItem.onValueChanged.AddListener(onToggleValueChanged.Invoke);
                /* pointTag.ToggleItem.onValueChanged.AddListener(isOn =>
                {
                    if (isOn) onToggleValueChanged?.Invoke(pointTag.FollowerTarget);
                }); */
            }
            onTagesInitialized?.Invoke();
        }

        [Button, ShowIf(nameof(IsHaveData))]
        private void ClearExistingTags()
        {
            foreach (Transform child in pointsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        public void SetTargetModels(List<Transform> models) => targetModels = models;

        /// <summary>
        /// 設定Label是否永遠可見
        /// </summary>
        public void SetLabelAlwaysVisible(bool isAlwaysVisible)
        {
            if (PointTags == null || PointTags.Length == 0) return;
            foreach (var pointTag in PointTags)
            {
                pointTag.SetLabelAlwaysVisible(isAlwaysVisible);
            }
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

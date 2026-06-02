using System.Collections.Generic;
using VzDev.ObjectUtils;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VzDev
{
    public class PointTagGenerator : MonoBehaviour
    {
        #region Variables

        [SerializeField] private bool isGenerateOnStart = true;
        [SerializeField] private List<Transform> targetModels;
        [Foldout("[Events]")] public UnityEvent<Transform> onTagClicked;
        [Foldout("[Components]"), SerializeField] private UIAnchorFollower pointTagPrefab;
        [Foldout("[Components]"), SerializeField] private Transform pointsContainer;
        [Foldout("[Components]"), SerializeField] private ToggleGroup toggleGroup;
        [Foldout("[Settings]"), SerializeField] private string labelPrefix;

        private bool IsHaveData => Application.isPlaying && pointTagPrefab != null
            && pointsContainer != null && targetModels != null && targetModels.Count > 0;
        #endregion

        private void Start()
        {
            if (isGenerateOnStart) GeneratePointTags();
        }

        [Button, ShowIf(nameof(IsHaveData))]
        public void GeneratePointTags()
        {
            ClearExistingTags();

            for (int i = 0; i < targetModels.Count; i++)
            {
                Transform targetModel = targetModels[i];
                // 在每個目標模型的位置生成一個UI Anchor作為Tag
                UIAnchorFollower uiAnchorFollower = Instantiate(pointTagPrefab, targetModel.position, Quaternion.identity, pointsContainer);
                // 可以在這裡對tag進行額外的設定，例如顯示點的座標等
                uiAnchorFollower.SetTargetObject(targetModel); // 假設UIAnchorFollower有這樣的方法來設定目標位置

                // 如果tag prefab有TextMesh組件，可以設定顯示的文字
                TextMeshProUGUI textMesh = uiAnchorFollower.GetComponentInChildren<TextMeshProUGUI>(true);
                if (textMesh != null && !string.IsNullOrEmpty(labelPrefix))
                {
                    textMesh.SetText($"{labelPrefix}{i + 1:D2}"); // 例如顯示 "Tag01", "Tag02" 等等
                    uiAnchorFollower.name += textMesh.text; // 同步物件名稱與顯示文字，方便在Hierarchy中識別
                }
                else
                {
                    uiAnchorFollower.name += $"_{i + 1:D2}"; // 如果沒有TextMesh，至少在名稱上區分
                }

                // 為tag添加點擊事件，當tag被點擊時觸發onTagClicked事件並傳遞對應的目標模型
                Toggle toggle = uiAnchorFollower.GetComponentInChildren<Toggle>(true);
                if (toggle != null)
                {
                    toggle.group = toggleGroup; // 如果有ToggleGroup，將Toggle加入其中以實現互斥選擇
                    toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn) onTagClicked?.Invoke(uiAnchorFollower.Target3DObject);
                    });
                }
            }
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
    }
}

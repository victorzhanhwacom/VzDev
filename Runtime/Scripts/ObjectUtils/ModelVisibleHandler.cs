using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class ModelVisibleHandler : MonoBehaviour
{
    [SerializeField] private List<Transform> targetModels;
    private bool isHaveModels => targetModels != null && targetModels.Count > 0;

    public void SetTargetModels(List<Transform> models) => targetModels = models;
    public void HideModels(List<Transform> models)
    {
        SetTargetModels(models);
        SetVisible(false);
    }

    [Button, ShowIf("isHaveModels")]
    public void ShowModels() => SetVisible(true);
    [Button, ShowIf("isHaveModels")]
    public void HideModels() => SetVisible(false);

    public void SetVisible(bool isVisible)
    {
        if (targetModels == null || targetModels.Count == 0) return;

        foreach (var model in targetModels)
        {
            if (model != null)
            {
                model.gameObject.SetActive(isVisible);
            }
        }
    }

}

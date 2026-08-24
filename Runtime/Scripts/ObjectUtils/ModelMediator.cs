using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.ObjectUtils
{
    public class ModelMediator : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent<List<Transform>> InvokeModelsEvent;
        [SerializeField] private List<Transform> targetModels;
        private bool IsHaveModels => targetModels != null && targetModels.Count > 0;

        public void SetTargetModels(List<Transform> models)
        {
            targetModels = models;
            InvokeModels();
        }

        [Button,  ShowIf(nameof(IsHaveModels))]
        public void InvokeModels() => InvokeModelsEvent?.Invoke(targetModels);
    }
}

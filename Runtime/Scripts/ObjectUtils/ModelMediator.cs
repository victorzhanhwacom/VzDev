using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.ObjectUtils
{
    public class ModelMediator : MonoBehaviour
    {
        [Foldout("[Events]")] public UnityEvent<List<Transform>> OnReceiveModel;
        [SerializeField] private List<Transform> targetModels;
        private bool IsHaveModels => targetModels != null && targetModels.Count > 0;

        public void SetTargetModels(List<Transform> models)
        {
            targetModels = models;
            InvokeReceiveModel();
        }

        [Button]
        public void GetChildrenModels()
        {
            targetModels = transform.GetComponentsInChildren<MeshRenderer>().Select(renderer => renderer.transform).ToList();
            InvokeReceiveModel();
        }

        [Button,  ShowIf(nameof(IsHaveModels))]
        public void InvokeReceiveModel() => OnReceiveModel?.Invoke(targetModels);
    }
}

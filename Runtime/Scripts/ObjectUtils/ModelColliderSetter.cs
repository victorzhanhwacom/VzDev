using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.UnityAPI.Extensions;
using static VzDev.UnityAPI.Extensions.TransformExtension;

namespace VzDev.ObjectUtils
{
    /// <summary>
    /// 自動為模型對像添加碰撞器的工具類別，支援 BoxCollider 和 MeshCollider。
    /// </summary>
    public class ModelColliderSetter : MonoBehaviour
    {
        public enum ColliderType
        {
            BoxCollider,
            MeshCollider
        }

        #region Fields
        [SerializeField] private ColliderType colliderType = ColliderType.BoxCollider;
        [SerializeField, ReadOnly] private List<Transform> models;
        private bool isHaveModels => models != null && models.Count > 0;
        #endregion

        [Button, ShowIf("isHaveModels")]
        private void RemoveAndClear()
        {
            RemoveColliders();
            models = new List<Transform>();
        }

        public void GenerateColliders(List<Transform> modelList)
        {
            models = modelList;
            SetColliders();
        }

        [Button, ShowIf("isHaveModels")]
        public void RemoveColliders()
        {
            if (!isHaveModels)
            {
                Debug.LogWarning("No models found to remove colliders from.", this);
                return;
            }

            foreach (var model in models)
            {
                if (model == null) continue;

                var boxCollider = model.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    DestroyImmediate(boxCollider);
                }

                var meshCollider = model.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    DestroyImmediate(meshCollider);
                }
            }
        }

        [Button, ShowIf("isHaveModels")]
        private void SetColliders()
        {
            if (!isHaveModels)
            {
                Debug.LogWarning("No models found to set colliders on.", this);
                return;
            }

            foreach (var model in models)
            {
                if (model == null) continue;

                switch (colliderType)
                {
                    case ColliderType.BoxCollider:
                        model.gameObject.TryAddComponent<BoxCollider>();
                        break;
                    case ColliderType.MeshCollider:
                        var meshFilter = model.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            model.gameObject.TryAddComponent<MeshCollider>(out MeshCollider meshCollider);
                            meshCollider.sharedMesh = meshFilter.sharedMesh;
                        }
                        else
                        {
                            Debug.LogWarning($"Model {model.name} does not have a MeshFilter with a valid mesh.", this);
                        }
                        break;
                }
            }
        }
    }
}
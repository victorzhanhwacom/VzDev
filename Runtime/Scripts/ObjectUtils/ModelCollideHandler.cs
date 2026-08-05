using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using VzDev.UnityAPI.Extensions;
using Debug = VzDev.ToolUtils.Debug;
using static VzDev.UnityAPI.Extensions.TransformExtension;
using VzDev.DebugUtils;

namespace VzDev.ObjectUtils
{
    /// <summary>
    /// 自動為模型對像添加碰撞器的工具類別，支援 BoxCollider 和 MeshCollider。
    /// </summary>
    public class ModelCollideHandler : MonoBehaviour
    {
        public enum ColliderType
        {
            BoxCollider,
            MeshCollider
        }

        #region Fields
        [SerializeField, OnValueChanged("OnColliderEnabledChanged"), ShowIf("isCreatedColliders")] private bool isColliderEnabled = true;
        private void OnColliderEnabledChanged() => SetEnable(isColliderEnabled);
        [SerializeField, ReadOnly] private List<Transform> models;
        [Foldout("[Settings]"), SerializeField] private ColliderType colliderType = ColliderType.BoxCollider;
        private List<Collider> colliders = new List<Collider>();
        private bool isCreatedColliders => colliders != null && colliders.Count > 0;
        #endregion

        public void GenerateColliders(List<Transform> modelList)
        {
            models = modelList;
            RemoveAndClear();
            SetColliders();
        }

        private void SetColliders()
        {
            foreach (var model in models)
            {
                if (model == null) continue;

                switch (colliderType)
                {
                    case ColliderType.BoxCollider:
                        model.gameObject.TryAddComponent(out BoxCollider boxCollider);
                        colliders.Add(boxCollider);
                        break;
                    case ColliderType.MeshCollider:
                        var meshFilter = model.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            model.gameObject.TryAddComponent<MeshCollider>(out MeshCollider meshCollider);
                            meshCollider.sharedMesh = meshFilter.sharedMesh;
                            colliders.Add(meshCollider);
                        }
                        else
                        {
                            Debug.LogWarning($"Model {model.name} does not have a MeshFilter with a valid mesh.", this);
                        }
                        break;
                }
            }
        }

        public void Clickable() => SetEnable(true);
        public void Unclickable() => SetEnable(false);

        public void SetEnable(bool isEnable)
        {
            isColliderEnabled = isEnable;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                if (collider == null) continue;
                collider.enabled = isColliderEnabled;
            }
        }

        [Button, ShowIf("isCreatedColliders")]
        public void RemoveAndClear()
        {
            foreach (var collider in colliders)
            {
                if (collider == null) continue;
                ObjectHelper.Destroy(collider);
            }
            colliders.Clear();
        }
    }
}
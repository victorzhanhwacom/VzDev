using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.DebugUtils;

namespace VzDev.ObjectUtils
{
    public class PrefabInstantiater : MonoBehaviour
    {
        #region Fields
        [SerializeField, OnValueChanged("OnVisibleChanged")] private bool visible;
        [SerializeField] private List<Transform> targetModels;
        [SerializeField, ReadOnly] private List<GameObject> _instantiatedModels;
        [Foldout("[Events]")] public UnityEvent<List<GameObject>> OnPrefabsInstantiated;
        [Foldout("[Components]"), SerializeField] private GameObject prefab;
        private bool isInstantiated => _instantiatedModels != null && _instantiatedModels.Count > 0;
        private bool isHaveTargetModels => targetModels != null && targetModels.Count > 0;
        private void OnVisibleChanged() => SetVisible(visible);
        #endregion

        #region Visible
        public void SetVisible(bool visible)
        {
            if (!isInstantiated) return;

            foreach (var model in _instantiatedModels)
            {
                if (model != null)
                {
                    model.gameObject.SetActive(visible);
                }
            }
        }
        public void ShowPrefabs() => SetVisible(true);
        public void HidePrefabs() => SetVisible(false);
        #endregion

        #region Generate & Remove
        public void GeneratePrefabs(List<Transform> models)
        {
            targetModels = models;
            if(!NullCheck()) return;
            GeneratePrefabs();
        }
        [Button, ShowIf("isHaveTargetModels")]
        public void GeneratePrefabs()
        {
            if(!NullCheck()) return;
            if (isInstantiated) RemovePrefabs();
            _instantiatedModels = new List<GameObject>();

            for (int i = 0; i < targetModels.Count; i++)
            {
                var model = targetModels[i];
                var instance = ObjectHelper.Instantiate(prefab.transform, model).gameObject;
                instance.SetActive(visible);
                _instantiatedModels.Add(instance);
            }
            OnPrefabsInstantiated?.Invoke(_instantiatedModels);
        }

        [Button, ShowIf("isInstantiated")]
        public void RemovePrefabs()
        {
            if (!isInstantiated) return;

            foreach (var model in _instantiatedModels)
            {
                if (model != null)
                {
                    ObjectHelper.Destroy(model.gameObject);
                }
            }
            _instantiatedModels = null;
        }
        #endregion

        private bool NullCheck()
        {
            if (prefab == null)
            {
                Debug.LogError("PrefabInstantiater: Prefab is null.");
                return false;
            }
            if (!isHaveTargetModels)
            {
                Debug.LogError("PrefabInstantiater: Target models list is null or empty.");
                return false;
            }
            return true;
        }
    }
}

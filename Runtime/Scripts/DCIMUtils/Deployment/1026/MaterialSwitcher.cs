using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev
{
    public class MaterialSwitcher : MonoBehaviour
    {
        #region Fields
        [SerializeField, ReadOnly] private int receiveValue = -1;
        [SerializeField] private List<MaterialSet> materialSet;
        #endregion

        public void SetValue(bool isOn) => SetValue(isOn ? 1 : 0);
        public void SetValue(int value)
        {
            receiveValue = value;
            materialSet.ForEach(ms => ms.CheckAndApply(receiveValue));
        }
        public void Restore() => materialSet.ForEach(ms => ms.Restore());



        [Serializable]
        public struct MaterialSet
        {
            public int index;
            public Material mat;
            public Renderer target;

            private Material[] _originalMaterials, _replaceMaterials;

            public void Restore()
            {
                if (target != null && _originalMaterials != null)
                {
                    target.materials = _originalMaterials;
                }
            }

            public void CheckAndApply(int num)
            {
                if (num == index && target != null)
                {
                    _originalMaterials ??= target.materials; // 儲存原始材質
                    if (_replaceMaterials == null)
                    {
                        _replaceMaterials = new Material[_originalMaterials.Length];
                        for (int i = 0; i < _originalMaterials.Length; i++) _replaceMaterials[i] = mat;
                    }
                    target.materials = _replaceMaterials;
                }
            }
        }
    }
}

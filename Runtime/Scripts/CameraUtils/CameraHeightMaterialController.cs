using System.Collections.Generic;
using UnityEngine;

namespace VzDev.CameraUtils
{
    /// <summary>
    /// 根據Camera高度來改變目標模型的材質
    /// </summary>
    public class CameraHeightMaterialController : MonoBehaviour
    {
        [System.Serializable]
        public class MaterialGroup
        {
            public string groupName;
            public List<GameObject> rootObjects;
            public float showBelowHeight = 3f;
            public Material transparentMaterial;

            [HideInInspector] public List<Renderer> renderers = new();
            [HideInInspector] public bool forceOriginal = false;
        }

        #region Fields
        [SerializeField] private List<MaterialGroup> groups;
        private Dictionary<int, bool> _lastState = new();
        private Dictionary<Renderer, Material[]> _originalMaterials = new(); // Material[] 改為陣列
        #endregion

        void Awake()
        {
            for (int i = 0; i < groups.Count; i++)
            {
                _lastState[i] = true;

                foreach (var root in groups[i].rootObjects)
                {
                    if (root == null) continue;

                    var renderers = root.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        groups[i].renderers.Add(rend);
                        _originalMaterials[rend] = rend.sharedMaterials; // 存整個陣列
                    }
                }
            }
        }

        public void SetForceOriginal(string groupName, bool force)
        {
            int index = groups.FindIndex(g => g.groupName == groupName);
            if (index == -1) return;

            groups[index].forceOriginal = force;
            _lastState[index] = !force;
        }

        public void SetForceOriginalAll(bool force)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].forceOriginal = force;
                _lastState[i] = !force;
            }
        }

        void Update()
        {
            float camY = transform.position.y;

            for (int i = 0; i < groups.Count; i++)
            {
                bool shouldShow = groups[i].forceOriginal || camY < groups[i].showBelowHeight;
                if (shouldShow == _lastState[i]) continue;
                _lastState[i] = shouldShow;

                foreach (var rend in groups[i].renderers)
                {
                    if (rend == null) continue;

                    if (shouldShow)
                    {
                        rend.sharedMaterials = _originalMaterials[rend]; // 還原整個陣列
                    }
                    else
                    {
                        // 建立與 slot 數量相同的透明材質陣列
                        var transparentArray = new Material[rend.sharedMaterials.Length];
                        for (int j = 0; j < transparentArray.Length; j++)
                            transparentArray[j] = groups[i].transparentMaterial;

                        rend.sharedMaterials = transparentArray;
                    }
                }
            }
        }
    }
}
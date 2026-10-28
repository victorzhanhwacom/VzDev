using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev
{
    public class ParentSizeFitter : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Vector3 expendSize = new Vector3(0.03f, 0.01f, 0.03f);
        [SerializeField] private Transform parent;
        #endregion

        private void OnValidate()
        {
            parent = transform.parent;
        }

    }
}

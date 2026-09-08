using System.Collections.Generic;
using UnityEngine;
using VzDev.DebugUtils;

namespace VzDev.ObjectUtils
{
    public class ModelDeleteHandler : MonoBehaviour
    {
        public void DeleteTargetModels(List<Transform> models)
        {
            for (int i = models.Count - 1; i >= 0; i--)
            {
                Transform model = models[i];
                if (model != null)
                {
                    ObjectHelper.Destroy(model.gameObject);
                }
            }
        }
    }
}
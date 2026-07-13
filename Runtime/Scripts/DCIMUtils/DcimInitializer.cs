using UnityEngine;
using VzDev.ObjectUtils;

namespace VzDev.DCIMUtils
{
    /// <summary>
    /// 初始化處理器，負責管理模型尋找器和碰撞器設置器。
    /// </summary>
    public class DcimInitializer : MonoBehaviour
    {
        [SerializeField] private bool logEnabled = true;
        [SerializeField] private ModelFinder[] modelFinders;
        [SerializeField] private ModelColliderSetter[] modelColliderSetters;

        public void Awake()
        {
            FindModels();
            SetColliders();
        }

        private void FindModels()
        {
            if(logEnabled)
                Debug.Log("Finding models using ModelFinder components.", this);
            for (int i = 0; i < modelFinders.Length; i++)
            {
                if (modelFinders[i] == null)
                {
                    Debug.LogWarning($"ModelFinder at index {i} is not assigned.", this);
                    continue;
                }
                modelFinders[i].FindModelsByKeywords();
            }
        }

        private void SetColliders()
        {
            for (int i = 0; i < modelColliderSetters.Length; i++)
            {
                if (modelColliderSetters[i] == null)
                {
                    Debug.LogWarning($"ModelColliderSetter at index {i} is not assigned.", this);
                    continue;
                }
                modelColliderSetters[i].SetColliders();
            }
        }
    }
}

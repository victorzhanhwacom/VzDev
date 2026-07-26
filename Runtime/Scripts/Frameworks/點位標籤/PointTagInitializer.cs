using NaughtyAttributes;
using UnityEngine;

namespace VzDev.ToolUtils
{
    public class PointTagInitializer : MonoBehaviour
    {
        #region Fields
        [SerializeField] private PointTagGeneratorMediator[] pointTagGeneratorMediators;
        #endregion

        /// <summary>
        /// 設定點位標籤的顯示與隱藏
        /// </summary>
        public void SetVisible(int mainmenuIndex)
        {
            for(int i = 0; i < pointTagGeneratorMediators.Length; i++)
            {
                pointTagGeneratorMediators[i].SetVisible(i == mainmenuIndex);
            }
        }

        public void HideAll() => SetVisible(-1);

        [Button]
        private void GetPointTagGeneratorMediators()
        {
            pointTagGeneratorMediators = GetComponentsInChildren<PointTagGeneratorMediator>(true);
        }
        private void OnValidate() => GetPointTagGeneratorMediators();

    }
}

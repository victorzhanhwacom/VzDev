using UnityEngine;

namespace VzDev.Helpers
{
    public static class LayerMaskHelper
    {
        /// <summary>
        /// 將LayerIndex轉換為對應的LayerMask
        /// </summary>
        public static LayerMask IndexToLayerMask(int layerIndex) => 1 << layerIndex;
        public static LayerMask IndexToLayerMask(Transform target) => IndexToLayerMask(target.gameObject.layer);

        /// <summary>
        /// 判斷指定的 GameObject 是否在給定的 LayerMask 中。
        /// </summary>
        public static bool IsLayerInMask(GameObject obj, LayerMask layerMask)
        {
            return (layerMask.value & (1 << obj.layer)) != 0;
        }

        /// 以LayerMask名稱取得LayerIndex
        public static int LayerMaskToIndex(LayerMask targetLayerMask) => Mathf.RoundToInt(Mathf.Log(targetLayerMask.value, 2));
    }
}
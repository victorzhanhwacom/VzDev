using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VzDev.MaterialUtils
{
    public static class MaterialHelper
    {
        #region Replace Material

        /// 存儲每個物件及其原始材質的字典 {物件Transform, 材質陣列}
        private static readonly Dictionary<Transform, Material[]> originalMaterials = new();

        /// <summary>
        /// 替換物件(陣列)為指定材質 {排除的對像(選填)}
        /// </summary>
        public static void ReplaceMaterial(List<Transform> targets, Material replaceMaterial, List<Transform> excludeTargets) =>
            targets.ForEach(target => ReplaceMaterialRecursively(target, replaceMaterial, excludeTargets));

        /// 替換物件及其底下每層所有子物件的材質 {排除的對像(選填)}
        public static void ReplaceMaterialRecursively(Transform target, Material material,
            List<Transform> excludeTargets = null)
        {
            if (excludeTargets != null)
            {
                //當目標對像不在排除名單內時
                if (excludeTargets.Contains(target) == false)
                {
                    ReplaceMaterial(target, material, excludeTargets);
                    // 遞迴處理所有子物件
                    foreach (Transform child in target)
                    {
                        ReplaceMaterialRecursively(child, material, excludeTargets);
                    }
                }
            }
            else
            {
                //當沒有排除名單時，直接替換 
                ReplaceMaterial(target, material);
                // 遞迴處理所有子物件
                foreach (Transform child in target)
                {
                    ReplaceMaterialRecursively(child, material);
                }
            }
        }

        /// 替換Targets(陣列)為指定材質
        public static void ReplaceMaterial(List<Transform> targets, Material replaceMaterial) =>
            targets.ForEach(target => ReplaceMaterial(target, replaceMaterial));

        /// 將目前模型的材質，替換為指定材質
        public static void ReplaceMaterial(Transform target, Material replaceMaterial, List<Transform> excludeTargets = null)
        {
            if (target == null) return;
            // 尋找所有子物件身上的 Renderer（包含 inactive）
            Renderer[] result = target.GetComponentsInChildren<Renderer>(includeInactive: true);

            foreach (Renderer childRenderer in result)
            {
                Transform childTrans = childRenderer.transform;
                // 若有排除名單，且當前子物件在排除名單內，則跳過
                if (excludeTargets != null && excludeTargets.Contains(childTrans))
                {
                    continue;
                }
                
                // 若沒有存過原始材質，才存（避免重複覆蓋）
                originalMaterials.TryAdd(childTrans, childRenderer.sharedMaterials);
                
                if (childRenderer.sharedMaterials.Length > 1)
                {
                    //若有多個Material
                    Material[] newMaterials = new Material[childRenderer.sharedMaterials.Length];
                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        newMaterials[i] = replaceMaterial;
                    }
                    childRenderer.materials = newMaterials;
                }
                else
                    childRenderer.material = replaceMaterial;
            }
        }

        #endregion

        #region Restore Material

        /// 復原全部對像的原始材質
        public static void RestoreAllMaterials()
        {
            foreach (var kvp in originalMaterials)
            {
                RestoreMaterial(kvp.Key);
            }
        }

        /// 復原對像(陣列)的原始材質，並從Dictionary裡移除
        public static void RestoreMaterial(List<Transform> targets) => targets.ForEach(RestoreMaterial);

        /// 復原對像的原始材質，並從Dictionary裡移除
        public static void RestoreMaterial(Transform target)
        {
            if(target == null) return;
            // 尋找所有子物件身上的 Renderer（包含 inactive）
            Renderer[] childRenderer = target.GetComponentsInChildren<Renderer>(includeInactive: true);

            foreach (Renderer child in childRenderer)
            {
                Transform childTrans = child.transform;

                if (originalMaterials.TryGetValue(childTrans, out Material[] mats))
                {
                    child.materials = mats;
                    originalMaterials.Remove(childTrans);   // 若要記憶體乾淨，也可以移除
                }
            }
        }
        #endregion

        #region 設定Material屬性

        /// 将材质设置为透明模式
        public static void SetTransparentMode(Material targetMaterial)
        {
            targetMaterial.SetFloat("_Mode", 3); // 设置模式为 Transparent
            targetMaterial.SetOverrideTag("RenderType", "Transparent");
            targetMaterial.EnableKeyword("_ALPHABLEND_ON");
            targetMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            targetMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            targetMaterial.SetInt("_ZWrite", 0); // 关闭深度写入
            targetMaterial.renderQueue = (int)RenderQueue.Transparent; // 设置渲染队列为透明层
        }

        /// 将材质设置为不透明模式
        public static void SetOpaqueMode(Material targetMaterial)
        {
            targetMaterial.SetFloat("_Mode", 0); // 设置模式为 Opaque
            targetMaterial.SetOverrideTag("RenderType", "Opaque");
            targetMaterial.DisableKeyword("_ALPHABLEND_ON");
            targetMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
            targetMaterial.SetInt("_DstBlend", (int)BlendMode.Zero);
            targetMaterial.SetInt("_ZWrite", 1); // 开启深度写入
            targetMaterial.renderQueue = (int)RenderQueue.Geometry; // 设置渲染队列为几何层
        }

        #endregion
    }
}
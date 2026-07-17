#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VzDev.EditorUtils
{
    /// <summary>
    /// 將 Hierarchy 中複選的多個物件(含其子物件)上的 MeshFilter 合併(Bake)成單一 Mesh Asset，
    /// 存至指定資料夾。
    ///
    /// 適用場景：靜態場景裝飾物、無需個別互動/變色的幾何物件，用於減少 Draw Call。
    /// 不適用場景：機櫃(RackComponent)等需要 GPU Instancing + MaterialPropertyBlock
    ///            做「個別變色/告警狀態」的物件 —— 合併後將無法個別控制材質屬性。
    ///
    /// 放置位置：此檔案必須位於任一名為 "Editor" 的資料夾內(例如 Assets/Editor/)，
    /// 才能被 Unity 正確歸入 Editor Assembly。#if UNITY_EDITOR 為額外保險。
    /// </summary>
    public static class MeshBakeTool
    {
        private const string DefaultFolder = "Assets/BakedMeshes";

        [MenuItem("VzDev/Tools/Bake Selected Meshes To Asset")]
        private static void BakeSelectedMeshes()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Bake Mesh", "請先在 Hierarchy 選取至少一個含有 Mesh 的物件。", "OK");
                return;
            }

            // Step 0：蒐集所有選取物件（含子物件）上的 MeshFilter
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            foreach (var go in selected)
            {
                meshFilters.AddRange(go.GetComponentsInChildren<MeshFilter>());
            }

            if (meshFilters.Count == 0)
            {
                EditorUtility.DisplayDialog("Bake Mesh", "選取的物件（含子物件）中找不到任何 MeshFilter。", "OK");
                return;
            }

            // Step 1：依材質分組，蒐集 (mesh, subMeshIndex) -> material 的對應
            // 分組原因：確保輸出 Mesh 每個 subMesh 對應唯一材質，避免材質陣列與 subMesh 錯位
            List<Material> materials = new List<Material>();
            Dictionary<Material, List<CombineInstance>> materialGroups = new Dictionary<Material, List<CombineInstance>>();

            int skippedCount = 0;
            int totalVertexCount = 0;

            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) { skippedCount++; continue; }
                var renderer = mf.GetComponent<MeshRenderer>();
                if (renderer == null) { skippedCount++; continue; }

                Mesh mesh = mf.sharedMesh;
                totalVertexCount += mesh.vertexCount;
                Material[] sharedMats = renderer.sharedMaterials;

                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    Material mat = sub < sharedMats.Length
                        ? sharedMats[sub]
                        : (sharedMats.Length > 0 ? sharedMats[sharedMats.Length - 1] : null);
                    if (mat == null) continue;

                    if (!materialGroups.TryGetValue(mat, out var list))
                    {
                        list = new List<CombineInstance>();
                        materialGroups[mat] = list;
                        materials.Add(mat);
                    }

                    list.Add(new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = sub,
                        transform = mf.transform.localToWorldMatrix // 烘焙到世界座標
                    });
                }
            }

            if (materials.Count == 0)
            {
                EditorUtility.DisplayDialog("Bake Mesh", "找不到任何有效材質的 Mesh 可供合併。", "OK");
                return;
            }

            // 頂點數超過 UInt16 上限(65535)時，改用 UInt32 IndexFormat
            IndexFormat indexFormat = totalVertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

            // Step 2：每個材質分組各自合併成一個 subMesh（mergeSubMeshes:true, useMatrices:true）
            List<CombineInstance> finalCombines = new List<CombineInstance>();
            List<Mesh> intermediateMeshes = new List<Mesh>();

            foreach (var mat in materials)
            {
                var group = materialGroups[mat];
                Mesh subMesh = new Mesh { indexFormat = indexFormat };
                subMesh.CombineMeshes(group.ToArray(), mergeSubMeshes: true, useMatrices: true);
                intermediateMeshes.Add(subMesh);

                finalCombines.Add(new CombineInstance
                {
                    mesh = subMesh,
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity // 已在 Step2 烘焙至世界座標，不再二次轉換
                });
            }

            // Step 3：合併各材質的 subMesh 成最終 Mesh，保留 subMesh 分離結構
            Mesh finalMesh = new Mesh { indexFormat = indexFormat };
            finalMesh.CombineMeshes(finalCombines.ToArray(), mergeSubMeshes: false, useMatrices: false);
            finalMesh.name = "BakedMesh_" + selected[0].name;

            // 釋放中繼 Mesh（僅為記憶體物件，未存為 Asset，須手動釋放避免洩漏）
            foreach (var m in intermediateMeshes) Object.DestroyImmediate(m);

            // Step 4：存成 Asset
            if (!Directory.Exists(DefaultFolder)) Directory.CreateDirectory(DefaultFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultFolder}/{finalMesh.name}.asset");
            AssetDatabase.CreateAsset(finalMesh, path);
            AssetDatabase.SaveAssets();

            // Step 5：在場景中建立一個掛載合併結果的物件，方便立即檢視（世界座標原點為 pivot）
            GameObject bakedGO = new GameObject(finalMesh.name);
            bakedGO.AddComponent<MeshFilter>().sharedMesh = finalMesh;
            bakedGO.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();

            Selection.activeObject = finalMesh;
            EditorUtility.FocusProjectWindow();

            Debug.Log(
                $"[MeshBakeTool] 合併完成：來源 MeshFilter={meshFilters.Count}（略過 {skippedCount}），" +
                $"材質數={materials.Count}，總頂點數={totalVertexCount}，IndexFormat={indexFormat}，已存於 {path}");
        }
    }
}
#endif
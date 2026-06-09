using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VzDev.EditorUtils
{
    public class MeshCombinerEditor : EditorWindow
    {
        private string savePath = "Assets/CombinedMeshes";

        [MenuItem("VzDev Tools/BIM Pipelines/Force Mesh Combiner")]
        public static void ShowWindow()
        {
            GetWindow<MeshCombinerEditor>("Mesh Combiner");
        }

        private void OnGUI()
        {
            Rect contentRect = EditorGUILayout.BeginVertical();

            GUILayout.Label("Force Combine Selected Meshes By Material", EditorStyles.boldLabel);
            GUILayout.Label("BIM/Revit 最佳化版本：支援多選、修正鏡像反轉與多材質缺塊", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Mesh Asset Save Path:");
            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField(savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    // Convert absolute path to relative Assets/... path
                    if (selected.StartsWith(Application.dataPath))
                        savePath = "Assets" + selected.Substring(Application.dataPath.Length);
                    else
                        EditorUtility.DisplayDialog("Error", "Please select a folder inside the Assets directory.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Combine Selected Parent's Children", GUILayout.Height(30)))
            {
                CombineSelected();
            }

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                float fixedWidth = 480f;
                float paddingHeight = 25f;
                float targetHeight = contentRect.height + paddingHeight;
                Vector2 calculatedSize = new Vector2(fixedWidth, targetHeight);
                if (this.minSize != calculatedSize)
                {
                    this.minSize = calculatedSize;
                    this.maxSize = calculatedSize;
                }
            }
        }

        private void CombineSelected()
        {
            GameObject[] selectedGroups = Selection.gameObjects;

            if (selectedGroups == null || selectedGroups.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please select at least one parent GameObject in Hierarchy.", "OK");
                return;
            }

            // Ensure save directory exists
            if (!AssetDatabase.IsValidFolder(savePath))
            {
                CreateFolderRecursive(savePath);
            }

            int processedCount = 0;

            foreach (GameObject selectedGroup in selectedGroups)
            {
                Dictionary<Material, List<(MeshFilter filter, int subMeshIndex)>> materialToMeshMap =
                    new Dictionary<Material, List<(MeshFilter, int)>>();

                MeshRenderer[] renderers = selectedGroup.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0) continue;

                foreach (var renderer in renderers)
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;

                    Material[] sharedMaterials = renderer.sharedMaterials;
                    int subMeshCount = filter.sharedMesh.subMeshCount;

                    for (int i = 0; i < subMeshCount; i++)
                    {
                        if (i >= sharedMaterials.Length) continue;
                        Material mat = sharedMaterials[i];
                        if (mat == null) continue;

                        if (!materialToMeshMap.ContainsKey(mat))
                            materialToMeshMap[mat] = new List<(MeshFilter, int)>();

                        materialToMeshMap[mat].Add((filter, i));
                    }
                }

                if (materialToMeshMap.Count == 0) continue;

                GameObject combinedRoot = new GameObject($"{selectedGroup.name}_CombinedRoot");
                combinedRoot.transform.position = selectedGroup.transform.position;
                combinedRoot.transform.rotation = selectedGroup.transform.rotation;
                combinedRoot.transform.localScale = selectedGroup.transform.localScale;

                foreach (var pair in materialToMeshMap)
                {
                    Material mat = pair.Key;
                    var meshDataList = pair.Value;

                    List<CombineInstance> combineInstances = new List<CombineInstance>();
                    List<Mesh> temporaryMirroredMeshes = new List<Mesh>();

                    foreach (var data in meshDataList)
                    {
                        CombineInstance combine = new CombineInstance();
                        Matrix4x4 relativeMatrix = combinedRoot.transform.worldToLocalMatrix * data.filter.transform.localToWorldMatrix;
                        combine.transform = relativeMatrix;
                        combine.subMeshIndex = data.subMeshIndex;

                        if (relativeMatrix.determinant < 0)
                        {
                            Mesh invertedMesh = Instantiate(data.filter.sharedMesh);
                            InvertMeshTriangles(invertedMesh, data.subMeshIndex);
                            temporaryMirroredMeshes.Add(invertedMesh);
                            combine.mesh = invertedMesh;
                        }
                        else
                        {
                            combine.mesh = data.filter.sharedMesh;
                        }

                        combineInstances.Add(combine);
                    }

                    GameObject combinedObj = new GameObject($"Combined_{mat.name}");
                    combinedObj.transform.SetParent(combinedRoot.transform, false);

                    MeshFilter newFilter = combinedObj.AddComponent<MeshFilter>();
                    MeshRenderer newRenderer = combinedObj.AddComponent<MeshRenderer>();

                    Mesh newMesh = new Mesh();
                    newMesh.indexFormat = IndexFormat.UInt32;
                    newMesh.CombineMeshes(combineInstances.ToArray(), true, true);
                    newMesh.name = $"{selectedGroup.name}_{mat.name}";

                    // ── 核心改動：儲存 Mesh 為 .asset 檔 ──
                    SaveMeshAsset(newMesh, selectedGroup.name, mat.name);

                    newFilter.sharedMesh = newMesh;
                    newRenderer.sharedMaterial = mat;

                    GameObjectUtility.SetStaticEditorFlags(combinedObj,
                        StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);

                    foreach (var tmpMesh in temporaryMirroredMeshes)
                        DestroyImmediate(tmpMesh);
                }

                Undo.RegisterCreatedObjectUndo(combinedRoot, "Combine Meshes Revit Optimized");
                processedCount++;
            }

            // 一次性寫入所有 asset 至磁碟
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success",
                $"Successfully processed {processedCount} Revit model roots.\nMesh assets saved to: {savePath}", "OK");
        }

        /// <summary>
        /// 將 Mesh 儲存為 .asset 檔，若同名已存在則覆寫（避免重複建立）。
        /// </summary>
        private void SaveMeshAsset(Mesh mesh, string groupName, string matName)
        {
            // Sanitize filename：移除路徑非法字元
            string safeName = string.Join("_",
                $"{groupName}_{matName}".Split(Path.GetInvalidFileNameChars()));
            string assetPath = $"{savePath}/{safeName}.asset";

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                // 覆寫既有 asset 的資料，保留原本的 GUID（不會讓 Scene 引用斷掉）
                EditorUtility.CopySerialized(mesh, existing);
                mesh = existing; // 讓 MeshFilter 指向已存在的 asset
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, assetPath);
            }
        }

        /// <summary>
        /// 遞迴建立 Assets 資料夾路徑（AssetDatabase.CreateFolder 只支援一層）。
        /// </summary>
        private void CreateFolderRecursive(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private void InvertMeshTriangles(Mesh mesh, int subMeshIndex)
        {
            int[] triangles = mesh.GetTriangles(subMeshIndex);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            mesh.SetTriangles(triangles, subMeshIndex);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
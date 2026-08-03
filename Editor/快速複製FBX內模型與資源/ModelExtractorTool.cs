using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 將目前選取的模型（FBX 內的 GameObject 或場景中的物件）提取成獨立資產：
/// - 複製其 MeshFilter.sharedMesh 為獨立 .asset
/// - 複製其 MeshRenderer.sharedMaterials 為獨立 .mat
/// - 重新綁定後另存為 Prefab
///
/// 用途：避免 Prefab 一直依賴 FBX 內部的 sub-asset（Mesh/Material 綁死在 FBX 檔案裡），
/// 方便你之後獨立編輯 Mesh 或替換材質。
///
/// 使用方式：此腳本需放在名為 "Editor" 的資料夾內（例如 Assets/Editor/）。
/// 選取物件後，執行選單 Tools > Model Extractor > Extract Selected Model To Prefab
/// </summary>
public static class ModelExtractorTool
{
    private const string RootFolder = "Assets/ExtractedModels";

    /// <summary>
    /// 是否在清理名稱時，額外移除以 "_" 分隔後重複出現的片段，
    /// 以及結尾的純數字流水號（例如 "A_B_B_87" -> "A_B"）。
    /// </summary>
    private const bool RemoveDuplicateTokensAndTrailingIndex = true;

    [MenuItem("VzDev/Tools/Model Extractor/Extract Selected Model To Prefab %#e")]
    private static void ExtractSelectedModel()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Model Extractor", "請先選擇至少一個模型物件（Project 或 Hierarchy 皆可，支援多選）。", "OK");
            return;
        }

        var createdPrefabs = new System.Collections.Generic.List<Object>();
        var failedNames = new System.Collections.Generic.List<string>();
        int totalMesh = 0;
        int totalMat = 0;

        // 用一個統一的資料夾名稱作為集合（Batch）名稱：多選時各自建立分類子資料夾，
        // 單選時直接沿用原本的行為。
        foreach (GameObject selected in selectedObjects)
        {
            bool ok = ExtractSingle(selected, out int meshCount, out int matCount, out GameObject prefab);
            if (ok)
            {
                createdPrefabs.Add(prefab);
                totalMesh += meshCount;
                totalMat += matCount;
            }
            else
            {
                failedNames.Add(selected.name);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"完成！共處理 {selectedObjects.Length} 個物件。\n\n" +
                          $"成功: {createdPrefabs.Count}\n" +
                          $"Mesh 總數: {totalMesh}\n" +
                          $"Material 總數: {totalMat}";

        if (failedNames.Count > 0)
            message += $"\n\n失敗: {string.Join(", ", failedNames)}";

        EditorUtility.DisplayDialog("Model Extractor", message, "OK");

        if (createdPrefabs.Count > 0)
        {
            Selection.objects = createdPrefabs.ToArray();
            EditorGUIUtility.PingObject(createdPrefabs[0]);
        }
    }

    [MenuItem("VzDev/Tools/Model Extractor/Extract Selected Model To Prefab %#e", true)]
    private static bool ValidateExtractSelectedModel()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    /// <summary>
    /// 處理單一物件：建立獨立資料夾、複製 Mesh / Material、輸出 Prefab。
    /// </summary>
    private static bool ExtractSingle(GameObject selected, out int meshCount, out int matCount, out GameObject prefab)
    {
        meshCount = 0;
        matCount = 0;
        prefab = null;

        // 建立一份場景中的工作副本，避免直接改動原始 FBX / 場景物件
        string cleanName = SanitizeName(selected.name);
        GameObject instance = Object.Instantiate(selected);
        instance.name = cleanName;

        // 每個物件各自建立獨立資料夾：Assets/ExtractedModels/<ModelName>_N/
        string targetFolder = CreateUniqueFolder(RootFolder, cleanName);
        string meshFolder = EnsureSubFolder(targetFolder, "Meshes");
        string matFolder = EnsureSubFolder(targetFolder, "Materials");

        // 遍歷所有子物件（含自己）的 MeshFilter / MeshRenderer
        MeshFilter[] meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            // 複製 Mesh 為獨立資產
            Mesh meshCopy = Object.Instantiate(mf.sharedMesh);
            meshCopy.name = SanitizeName(mf.sharedMesh.name);
            string meshPath = AssetDatabase.GenerateUniqueAssetPath($"{meshFolder}/{meshCopy.name}.asset");
            AssetDatabase.CreateAsset(meshCopy, meshPath);
            mf.sharedMesh = meshCopy;
            meshCount++;

            // 複製對應的 MeshRenderer 材質
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material[] originalMats = mr.sharedMaterials;
                Material[] newMats = new Material[originalMats.Length];

                for (int i = 0; i < originalMats.Length; i++)
                {
                    if (originalMats[i] == null)
                    {
                        newMats[i] = null;
                        continue;
                    }

                    Material matCopy = new Material(originalMats[i]);
                    matCopy.name = SanitizeName(originalMats[i].name);
                    string matPath = AssetDatabase.GenerateUniqueAssetPath($"{matFolder}/{matCopy.name}.mat");
                    AssetDatabase.CreateAsset(matCopy, matPath);
                    newMats[i] = matCopy;
                    matCount++;
                }

                mr.sharedMaterials = newMats;
            }
        }

        // 另存為 Prefab
        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{cleanName}.prefab");
        prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool success);

        Object.DestroyImmediate(instance);

        return success;
    }

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// 清理名稱，依序執行：
    /// 1. 移除 [ ] 符號以及其中包含的內容（例如 "Rock[LOD0]" -> "Rock"）
    /// 2. （可選）以 "_" 拆分後，移除重複出現的片段、以及結尾的純數字流水號
    ///    （例如 "A_B_B_87" -> "A_B"）
    /// 3. 將任何系統不合法的檔名字元（: * ? " &lt; &gt; | \ /）換成底線
    /// 4. 清掉多餘空白／底線
    /// 若清理後變成空字串，會回傳一個保底名稱以避免產生空路徑。
    /// </summary>
    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unnamed";

        // 1. 移除 [ ] 及其中內容
        string cleaned = Regex.Replace(name, @"\[.*?\]", string.Empty);

        // 2. 移除重複片段與結尾流水號
        if (RemoveDuplicateTokensAndTrailingIndex)
        {
            cleaned = RemoveDuplicateSegmentsAndTrailingIndex(cleaned);
        }

        // 3. 取代不合法的檔名字元
        foreach (char c in InvalidFileNameChars)
        {
            cleaned = cleaned.Replace(c, '_');
        }

        // 4. 清除多餘空白與底線
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        cleaned = cleaned.Trim('_', ' ');
        cleaned = Regex.Replace(cleaned, @"_{2,}", "_");

        return string.IsNullOrEmpty(cleaned) ? "Unnamed" : cleaned;
    }

    /// <summary>
    /// 以 "_" 拆分名稱後，移除重複出現的片段（只保留第一次出現），
    /// 並移除結尾連續的純數字片段（視為流水號）。
    /// 例如 "電氣設備_Schneider-ER8222_Schneider-ER8222_87" -> "電氣設備_Schneider-ER8222"
    /// </summary>
    private static string RemoveDuplicateSegmentsAndTrailingIndex(string name)
    {
        string[] tokens = name.Split('_');
        var seen = new System.Collections.Generic.HashSet<string>();
        var result = new System.Collections.Generic.List<string>();

        foreach (string token in tokens)
        {
            if (string.IsNullOrEmpty(token)) continue;
            if (seen.Add(token))
            {
                result.Add(token);
            }
        }

        // 移除結尾連續的純數字片段（流水號），但至少保留一個片段
        while (result.Count > 1 && Regex.IsMatch(result[result.Count - 1], @"^\d+$"))
        {
            result.RemoveAt(result.Count - 1);
        }

        return string.Join("_", result);
    }

    /// <summary>
    /// 在 parent 底下建立一個以 baseName 命名的資料夾，若已存在則自動加上流水號避免覆蓋。
    /// </summary>
    private static string CreateUniqueFolder(string parent, string baseName)
    {
        EnsureFolderPath(parent);

        string folderPath = $"{parent}/{baseName}";
        int suffix = 1;
        while (AssetDatabase.IsValidFolder(folderPath))
        {
            folderPath = $"{parent}/{baseName}_{suffix}";
            suffix++;
        }

        string newFolderName = Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parent, newFolderName);
        return folderPath;
    }

    private static string EnsureSubFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
        return path;
    }

    /// <summary>
    /// 確保多層資料夾路徑存在（例如 Assets/ExtractedModels），逐層建立。
    /// </summary>
    private static void EnsureFolderPath(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0]; // 應為 "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
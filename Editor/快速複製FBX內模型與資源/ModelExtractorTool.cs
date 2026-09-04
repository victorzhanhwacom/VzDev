using System.IO;
using System.Linq;
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

    #region Pivot Alignment 設定
    /// <summary>
    /// 設定值透過 EditorPrefs 存取，跨場景/跨 Session 都會記住，
    /// 不需要每次使用前重新設定一次。
    /// </summary>
    private const string PrefKeyHorizontal = "VzDev.ModelExtractorTool.HorizontalAlign";
    private const string PrefKeyVertical = "VzDev.ModelExtractorTool.VerticalAlign";

    private enum HorizontalPivotAlign { KeepOriginal, Center }
    private enum VerticalPivotAlign { KeepOriginal, Bottom, Center, Top }

    /// <summary>預設：水平置中，符合大多數機房設備需要左右對稱擺放的慣例。</summary>
    private static HorizontalPivotAlign horizontalAlign
    {
        get => (HorizontalPivotAlign)EditorPrefs.GetInt(PrefKeyHorizontal, (int)HorizontalPivotAlign.Center);
        set => EditorPrefs.SetInt(PrefKeyHorizontal, (int)value);
    }

    /// <summary>預設：底部貼齊，符合機房設備上架時「底部對齊 U 槽底部」的慣例。</summary>
    private static VerticalPivotAlign verticalAlign
    {
        get => (VerticalPivotAlign)EditorPrefs.GetInt(PrefKeyVertical, (int)VerticalPivotAlign.Bottom);
        set => EditorPrefs.SetInt(PrefKeyVertical, (int)value);
    }

    private const string PivotMenuRoot = "VzDev/Tools/Model Extractor/Pivot Alignment/";

    [MenuItem(PivotMenuRoot + "Horizontal (X_Z)/置中 Center")]
    private static void SetHorizontalCenter() => horizontalAlign = HorizontalPivotAlign.Center;
    [MenuItem(PivotMenuRoot + "Horizontal (X_Z)/置中 Center", true)]
    private static bool ValidateSetHorizontalCenter()
    {
        Menu.SetChecked(PivotMenuRoot + "Horizontal (X_Z)/置中 Center", horizontalAlign == HorizontalPivotAlign.Center);
        return true;
    }

    [MenuItem(PivotMenuRoot + "Horizontal (X_Z)/維持原本 Keep Original")]
    private static void SetHorizontalKeep() => horizontalAlign = HorizontalPivotAlign.KeepOriginal;
    [MenuItem(PivotMenuRoot + "Horizontal (X_Z)/維持原本 Keep Original", true)]
    private static bool ValidateSetHorizontalKeep()
    {
        Menu.SetChecked(PivotMenuRoot + "Horizontal (X_Z)/維持原本 Keep Original", horizontalAlign == HorizontalPivotAlign.KeepOriginal);
        return true;
    }

    [MenuItem(PivotMenuRoot + "Vertical (Y)/底部貼齊 Bottom")]
    private static void SetVerticalBottom() => verticalAlign = VerticalPivotAlign.Bottom;
    [MenuItem(PivotMenuRoot + "Vertical (Y)/底部貼齊 Bottom", true)]
    private static bool ValidateSetVerticalBottom()
    {
        Menu.SetChecked(PivotMenuRoot + "Vertical (Y)/底部貼齊 Bottom", verticalAlign == VerticalPivotAlign.Bottom);
        return true;
    }

    [MenuItem(PivotMenuRoot + "Vertical (Y)/垂直置中 Center")]
    private static void SetVerticalCenter() => verticalAlign = VerticalPivotAlign.Center;
    [MenuItem(PivotMenuRoot + "Vertical (Y)/垂直置中 Center", true)]
    private static bool ValidateSetVerticalCenter()
    {
        Menu.SetChecked(PivotMenuRoot + "Vertical (Y)/垂直置中 Center", verticalAlign == VerticalPivotAlign.Center);
        return true;
    }

    [MenuItem(PivotMenuRoot + "Vertical (Y)/頂部貼齊 Top")]
    private static void SetVerticalTop() => verticalAlign = VerticalPivotAlign.Top;
    [MenuItem(PivotMenuRoot + "Vertical (Y)/頂部貼齊 Top", true)]
    private static bool ValidateSetVerticalTop()
    {
        Menu.SetChecked(PivotMenuRoot + "Vertical (Y)/頂部貼齊 Top", verticalAlign == VerticalPivotAlign.Top);
        return true;
    }

    [MenuItem(PivotMenuRoot + "Vertical (Y)/維持原本 Keep Original")]
    private static void SetVerticalKeep() => verticalAlign = VerticalPivotAlign.KeepOriginal;
    [MenuItem(PivotMenuRoot + "Vertical (Y)/維持原本 Keep Original", true)]
    private static bool ValidateSetVerticalKeep()
    {
        Menu.SetChecked(PivotMenuRoot + "Vertical (Y)/維持原本 Keep Original", verticalAlign == VerticalPivotAlign.KeepOriginal);
        return true;
    }
    #endregion

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
        int skippedCount = 0;

        // 用一個統一的資料夾名稱作為集合（Batch）名稱：多選時各自建立分類子資料夾，
        // 單選時直接沿用原本的行為。
        foreach (GameObject selected in selectedObjects)
        {
            bool ok = ExtractSingle(selected, out int meshCount, out int matCount, out GameObject prefab, out bool skipped);
            if (ok)
            {
                createdPrefabs.Add(prefab);
                totalMesh += meshCount;
                totalMat += matCount;
            }
            else if (skipped)
            {
                skippedCount++;
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
                          $"略過重複: {skippedCount}\n" +
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
    /// 若同名模型（cleanName）已經處理過，直接跳過，不重新複製任何資產。
    /// </summary>
    private static bool ExtractSingle(GameObject selected, out int meshCount, out int matCount, out GameObject prefab, out bool skipped)
    {
        meshCount = 0;
        matCount = 0;
        prefab = null;
        skipped = false;

        // 建立一份場景中的工作副本，避免直接改動原始 FBX / 場景物件
        // 【命名一致性】改用 ExtractCanonicalModelName，邏輯與正式流程
        // ModelComponentSetterBase.AssignDataToComponent / ModelTooltipController
        // 完全一致（取 [ ] 內字串，再取 ':' 分隔後最後一段），確保輸出的
        // Prefab 名稱能跟 JSON 資料裡的 modelName 逐字比對，不會因為額外的
        // 去重複片段/砍尾碼清理而跟原始命名產生落差。
        string cleanName = SanitizeForFileSystem(ExtractCanonicalModelName(selected.name));

        // 【跳過重複】場景中同一種模型常被重複擺放多次（例如 RJ45-UTP-24Panel-1U+1、
        // +3、+9…），砍掉流水號後名稱會撞在一起——這種情況代表本質上是同一個模型，
        // 只要處理過一次即可，重新複製 Mesh/Material 只是浪費時間，直接跳過。
        string targetFolder = $"{RootFolder}/{cleanName}";
        if (AssetDatabase.IsValidFolder(targetFolder))
        {
            skipped = true;
            return false;
        }

        GameObject instance = Object.Instantiate(selected);
        instance.name = cleanName;

        // 每個物件各自建立獨立資料夾：Assets/ExtractedModels/<ModelName>/
        EnsureFolderPath(RootFolder);
        AssetDatabase.CreateFolder(RootFolder, cleanName);
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

        // 【Pivot對齊】依目前的 horizontalAlign / verticalAlign 設定，把模型包一層新的
        // 根節點，讓根節點原點對齊到指定位置（例如 水平置中 + 底部貼齊）。
        // 兩軸都設為「維持原本」時，直接回傳 instance 本身，不會多包一層。
        GameObject prefabRoot = ApplyPivotAlignment(instance);
        if (prefabRoot != instance) instance.name = "Model"; // 讓 Hierarchy 好辨識：外層是Pivot根節點，內層才是實際模型

        // 另存為 Prefab（targetFolder 一定是全新建立的，不會有舊檔衝突）
        string prefabPath = $"{targetFolder}/{cleanName}.prefab";
        prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath, out bool success);

        Object.DestroyImmediate(prefabRoot);

        return success;
    }

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    #region Pivot Alignment 實作
    /// <summary>
    /// 依 horizontalAlign / verticalAlign 設定，重新調整模型的 Pivot 位置。
    /// <para>
    /// 不改動任何 Mesh 頂點資料：而是額外包一層空的 GameObject 當作新的 Prefab 根節點，
    /// 把 instance 變成它的子物件，並把 instance 的 localPosition 扣掉「該當作 Pivot
    /// 的世界座標點」，讓那個點剛好落在新根節點的原點 (0,0,0) 上。
    /// 只涉及位移運算，不會動到旋轉/縮放，模型外觀、朝向完全不變。
    /// </para>
    /// <para>
    /// 若水平與垂直都設定為「維持原本」，或模型底下找不到任何 Renderer（沒有 Bounds
    /// 可以對齊），直接回傳 instance 本身，不會多包一層空節點。
    /// </para>
    /// </summary>
    private static GameObject ApplyPivotAlignment(GameObject instance)
    {
        if (horizontalAlign == HorizontalPivotAlign.KeepOriginal && verticalAlign == VerticalPivotAlign.KeepOriginal)
            return instance;

        if (!TryGetCombinedWorldBounds(instance, out Bounds worldBounds))
            return instance;

        Vector3 originalWorldPos = instance.transform.position;

        float pivotX = horizontalAlign == HorizontalPivotAlign.Center ? worldBounds.center.x : originalWorldPos.x;
        float pivotZ = horizontalAlign == HorizontalPivotAlign.Center ? worldBounds.center.z : originalWorldPos.z;
        float pivotY = verticalAlign switch
        {
            VerticalPivotAlign.Bottom => worldBounds.min.y,
            VerticalPivotAlign.Center => worldBounds.center.y,
            VerticalPivotAlign.Top => worldBounds.max.y,
            _ => originalWorldPos.y,
        };

        Vector3 pivotWorldPos = new Vector3(pivotX, pivotY, pivotZ);

        GameObject pivotRoot = new GameObject(instance.name);
        pivotRoot.transform.position = Vector3.zero;
        pivotRoot.transform.rotation = Quaternion.identity;

        // worldPositionStays: true，先讓 instance 維持目前世界座標不變地變成子物件，
        // 再手動扣掉 pivotWorldPos，把「該當作 Pivot 的世界座標點」平移到 (0,0,0)，
        // 也就是 pivotRoot 的原點。
        instance.transform.SetParent(pivotRoot.transform, worldPositionStays: true);
        instance.transform.localPosition -= pivotWorldPos;

        return pivotRoot;
    }

    /// <summary>
    /// 合併目標物件底下所有 Renderer 的世界座標 Bounds，用來量出模型實際的視覺範圍。
    /// 沒有任何 Renderer（例如空物件或純 Collider）時回傳 false。
    /// </summary>
    private static bool TryGetCombinedWorldBounds(GameObject target, out Bounds combined)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            combined = default;
            return false;
        }

        combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combined.Encapsulate(renderers[i].bounds);
        }
        return true;
    }
    #endregion

    /// <summary>
    /// 取得用於比對 JSON 資料的「規範化模型名稱」。
    /// <para>
    /// 邏輯與正式流程完全一致（見 ModelComponentSetterBase.AssignDataToComponent、
    /// ModelTooltipController.ResolveFallbackNameFromGameObject）：
    /// 取 GameObject 名稱裡 [ ] 中間的字串，再取 ':' 分隔後的最後一段，
    /// 這一段字串才是真正用來跟 JSON modelName 比對的識別碼。
    /// </para>
    /// <para>
    /// 若名稱裡沒有 [ ] 標記（不符合正式命名慣例），直接回傳原始名稱，
    /// 不做任何額外清理——避免自作聰明的清理規則（去重複片段、砍尾碼數字等）
    /// 悄悄改變了識別身份，導致跟 JSON 對不起來。
    /// </para>
    /// </summary>
    private static string ExtractCanonicalModelName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "Unnamed";

        string bracketContent = GetStringBetweenMarks(rawName, "[", "]");
        if (string.IsNullOrEmpty(bracketContent)) return rawName;

        string lastSegment = bracketContent.Split(':').LastOrDefault();
        if (string.IsNullOrEmpty(lastSegment)) return rawName;

        // 【流水號清理】場景中同一種模型會被重複擺放多次，每個實例的名稱尾端
        // 會被加上 "+數字" 的流水號以區分（例如 "RJ45-UTP-24Panel-1U+1"、
        // "...+3"、"...+9"），這個數字只是「這是第幾個實例」，不是模型識別碼
        // 的一部分，也不會出現在 JSON 的 modelName 裡，所以要砍掉。
        return RemoveTrailingSerialNumber(lastSegment);
    }

    /// <summary>
    /// 砍掉名稱尾端的 "+數字" 流水號（例如 "RJ45-UTP-24Panel-1U+182" -> 
    /// "RJ45-UTP-24Panel-1U"）。若尾端沒有這個格式則原樣回傳。
    /// </summary>
    private static string RemoveTrailingSerialNumber(string name)
    {
        return Regex.Replace(name, @"\+\d+$", string.Empty);
    }

    /// <summary>
    /// 取出 startMark 與 endMark 之間的字串（不含標記本身）。
    /// 找不到任一標記時回傳空字串。
    /// </summary>
    private static string GetStringBetweenMarks(string source, string startMark, string endMark)
    {
        int startIndex = source.IndexOf(startMark);
        if (startIndex < 0) return string.Empty;
        startIndex += startMark.Length;

        int endIndex = source.IndexOf(endMark, startIndex);
        if (endIndex < 0) return string.Empty;

        return source.Substring(startIndex, endIndex - startIndex);
    }

    /// <summary>
    /// 只做「檔案系統合法性」層面的最小清理（把不合法的檔名字元換成底線），
    /// 不改動任何字元的意義——這是唯一可以套用在「已經是規範化識別碼」上的清理，
    /// 因為它不會影響後續跟 JSON modelName 的比對結果（合法檔名字元本身也不該出現在
    /// modelName 裡）。
    /// </summary>
    private static string SanitizeForFileSystem(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unnamed";

        foreach (char c in InvalidFileNameChars)
        {
            name = name.Replace(c, '_');
        }
        name = name.Trim();

        return string.IsNullOrEmpty(name) ? "Unnamed" : name;
    }

    /// <summary>
    /// 清理名稱（僅供 Mesh / Material 子資產命名使用，這些名稱不需要跟 JSON 比對，
    /// 只需要是合法且易讀的檔名），依序執行：
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
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
        // 【世界座標保留】Object.Instantiate(selected) 在沒有指定 parent 時，
        // 複製出來的新物件會直接照抄 selected 的「Local」Position/Rotation/Scale。
        // 如果 selected 在場景 Hierarchy 裡本來就巢狀在某個有旋轉/縮放的父物件底下
        // （例如場景佈局用的容器物件），複製出來的 instance 因為沒有父物件，
        // Local 值會直接變成世界值，跟原本「肉眼看到」的視覺朝向/位置對不上，
        // 導致抽出來的模型無故轉了角度。這裡強制對齊回 selected 原本真正的世界座標，
        // 確保不論 selected 巢狀在哪一層，抽出來的結果都跟原本視覺呈現一致。
        instance.transform.SetPositionAndRotation(selected.transform.position, selected.transform.rotation);
        instance.transform.localScale = selected.transform.lossyScale;
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

        // 【Pivot對齊】依目前的 horizontalAlign / verticalAlign 設定，直接把
        // instance 本身的世界座標移動到計算出來的 Pivot 位置——不會額外包一層
        // GameObject，輸出的 Prefab 不會有 "Model" 子物件，instance 自己就是根節點。
        ApplyPivotAlignment(instance, meshFilters);

        // 另存為 Prefab（targetFolder 一定是全新建立的，不會有舊檔衝突）
        string prefabPath = $"{targetFolder}/{cleanName}.prefab";
        prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool success);

        Object.DestroyImmediate(instance);

        return success;
    }

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    #region Pivot Alignment 實作
    /// <summary>
    /// 依 horizontalAlign / verticalAlign 設定，直接把 instance 本身的世界座標
    /// 移動到計算出來的 Pivot 位置，<b>不會額外包一層 GameObject</b>。
    /// <para>
    /// 【做法】不用「加一層父物件再位移」的方式，而是先把 instance 即將位移的
    /// 向量 (deltaP)，反向轉換進每一個 MeshFilter 的 Local 空間後烘焙進 Mesh
    /// 頂點資料，再把 instance.transform.position 直接設成新的 Pivot 世界座標。
    /// 烘焙前後，所有頂點的「世界座標」完全不變，只是 instance 自己的 Local
    /// 原點換到了新的位置——最終輸出的 Prefab 就是 instance 自己，不會有額外的
    /// "Model" 子物件。
    /// </para>
    /// <para>
    /// 只處理位移（Translation），不涉及旋轉/縮放，法線/切線方向不受影響，
    /// 不需要另外重算。
    /// </para>
    /// <para>
    /// 若水平與垂直都設定為「維持原本」、模型底下找不到任何 Renderer（沒有
    /// Bounds 可以對齊），或算出來剛好不需要移動，則不做任何事。
    /// </para>
    /// </summary>
    private static void ApplyPivotAlignment(GameObject instance, MeshFilter[] meshFilters)
    {
        if (horizontalAlign == HorizontalPivotAlign.KeepOriginal && verticalAlign == VerticalPivotAlign.KeepOriginal)
            return;

        if (!TryGetCombinedWorldBounds(instance, out Bounds worldBounds))
            return;

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
        Vector3 deltaP = pivotWorldPos - originalWorldPos;

        if (deltaP == Vector3.zero) return; // 剛好就在目標位置上，不需要搬動

        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            // 把「instance 即將位移的向量」轉換到這個 MeshFilter 的 Local 空間，
            // 頂點要反向位移，才能在 instance 真正移動之後，世界座標維持不變。
            // 用 MultiplyVector（只套用矩陣的旋轉/縮放部分，忽略位移），
            // 因為 deltaP 是一段位移量，不是世界座標中的某個點。
            Vector3 localOffset = mf.transform.worldToLocalMatrix.MultiplyVector(deltaP);

            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] -= localOffset;
            mesh.vertices = vertices;

            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
        }

        instance.transform.position = pivotWorldPos;
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

    #region Axis Correction — 事後校正已提取模型的軸向
    /// <summary>
    /// 選取一個或多個已提取的模型（Project 視窗裡的 Prefab，或場景中的物件皆可），
    /// 執行此選單即可讓整個模型繞著自己的 Pivot 旋轉指定角度。
    /// <para>
    /// 用途：少數模型的 Mesh 本身（源自 Revit/Blender 匯出）帶著跟其他模型不一致的
    /// 軸向慣例（例如深度方向實際存在 Local X 而不是 Local Z），肉眼看起來就是
    /// 「轉了 90 度」。這個工具直接把校正旋轉烘焙進 Mesh 頂點/法線/切線資料，
    /// Transform 本身的 Rotation 完全不動（維持 Identity），不需要重新從 FBX 提取。
    /// </para>
    /// </summary>
    private const string CorrectionMenuRoot = "VzDev/Tools/Model Extractor/Axis Correction/";

    [MenuItem(CorrectionMenuRoot + "旋轉校正 +90° (Y軸)")]
    private static void CorrectRotationPlus90() => ApplyAxisCorrectionToSelection(90f);

    [MenuItem(CorrectionMenuRoot + "旋轉校正 +90° (Y軸)", true)]
    private static bool ValidateCorrectRotationPlus90() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

    [MenuItem(CorrectionMenuRoot + "旋轉校正 -90° (Y軸)")]
    private static void CorrectRotationMinus90() => ApplyAxisCorrectionToSelection(-90f);

    [MenuItem(CorrectionMenuRoot + "旋轉校正 -90° (Y軸)", true)]
    private static bool ValidateCorrectRotationMinus90() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

    [MenuItem(CorrectionMenuRoot + "旋轉校正 180° (Y軸)")]
    private static void CorrectRotation180() => ApplyAxisCorrectionToSelection(180f);

    [MenuItem(CorrectionMenuRoot + "旋轉校正 180° (Y軸)", true)]
    private static bool ValidateCorrectRotation180() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

    /// <summary>
    /// 對目前選取的所有物件執行旋轉校正。會先跳出確認對話框，
    /// 因為這個動作會直接改寫 Mesh 資產的頂點資料，不在 Undo 系統的追蹤範圍內。
    /// </summary>
    private static void ApplyAxisCorrectionToSelection(float yAngleDegrees)
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Axis Correction", "請先選擇要校正的模型（Project 視窗中的 Prefab，或場景中的物件皆可，支援多選）。", "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Axis Correction",
            $"即將把選取的 {selectedObjects.Length} 個模型繞 Y 軸旋轉 {yAngleDegrees}°，\n" +
            $"直接烘焙進 Mesh 頂點資料（無法透過 Ctrl+Z 復原）。\n\n確定要繼續嗎？",
            "確定", "取消");
        if (!confirmed) return;

        Quaternion correction = Quaternion.Euler(0f, yAngleDegrees, 0f);
        int fixedCount = 0;
        int skippedMeshCount = 0;

        foreach (GameObject root in selectedObjects)
        {
            if (BakeRotationCorrection(root, correction, ref skippedMeshCount))
                fixedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"完成！已校正 {fixedCount} 個模型（旋轉 {yAngleDegrees}°）。";
        if (skippedMeshCount > 0)
            message += $"\n\n有 {skippedMeshCount} 個 Mesh 因為不是 {RootFolder} 底下的獨立資產而被跳過" +
                       "（避免誤改到原始 FBX 內建的 Mesh），詳情請看 Console 的警告訊息。";

        EditorUtility.DisplayDialog("Axis Correction", message, "OK");
    }

    /// <summary>
    /// 把 <paramref name="correction"/> 這個旋轉，繞著 <paramref name="root"/>
    /// 目前的世界座標為中心，烘焙進它底下所有 MeshFilter 參照的 Mesh 頂點資料。
    /// <para>
    /// 【數學原理】對每個 MeshFilter，先用它目前（不變動）的
    /// localToWorldMatrix 算出頂點現在的世界座標，套用「以 root 為中心的旋轉」，
    /// 再用 worldToLocalMatrix 換算回它自己的 Local 空間存回去——因為 Unity
    /// 的 Transform 矩陣鏈本身完全沒有被更動，這個換算對任意巢狀深度都成立，
    /// 不需要另外遞迴處理子物件。
    /// </para>
    /// <para>
    /// 【安全性】只允許處理路徑在 <see cref="RootFolder"/> 底下的 Mesh 資產
    /// （也就是本工具自己複製出來的獨立資產），避免誤改到原始 FBX 內建的
    /// sub-asset，或被多個模型共用而牽連到不相關的模型。
    /// </para>
    /// </summary>
    private static bool BakeRotationCorrection(GameObject root, Quaternion correction, ref int skippedMeshCount)
    {
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters == null || meshFilters.Length == 0) return false;

        Vector3 pivotWorldPos = root.transform.position;
        Matrix4x4 worldRotationAroundPivot =
            Matrix4x4.Translate(pivotWorldPos) * Matrix4x4.Rotate(correction) * Matrix4x4.Translate(-pivotWorldPos);

        bool anyBaked = false;

        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            string assetPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(RootFolder + "/"))
            {
                Debug.LogWarning($"[Axis Correction] 跳過 {mf.name}：Mesh「{mesh.name}」不是 {RootFolder} 底下的獨立資產，" +
                                  "為避免誤改原始 FBX 資料，不會處理。", mf);
                skippedMeshCount++;
                continue;
            }

            // 換算回這個 MeshFilter 自己的 Local 空間；Transform 完全不動，
            // 只有 Mesh 頂點資料被改寫。
            Matrix4x4 finalBake = mf.transform.worldToLocalMatrix * worldRotationAroundPivot * mf.transform.localToWorldMatrix;

            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = finalBake.MultiplyPoint3x4(vertices[i]);
            mesh.vertices = vertices;

            Vector3[] normals = mesh.normals;
            if (normals != null && normals.Length == vertices.Length)
            {
                Matrix4x4 normalMatrix = finalBake.inverse.transpose;
                for (int i = 0; i < normals.Length; i++)
                    normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
                mesh.normals = normals;
            }

            Vector4[] tangents = mesh.tangents;
            if (tangents != null && tangents.Length == vertices.Length)
            {
                for (int i = 0; i < tangents.Length; i++)
                {
                    Vector3 t = finalBake.MultiplyVector(new Vector3(tangents[i].x, tangents[i].y, tangents[i].z)).normalized;
                    tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
                }
                mesh.tangents = tangents;
            }

            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            anyBaked = true;
        }

        return anyBaked;
    }
    #endregion
}
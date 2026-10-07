using UnityEditor;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 在 Hierarchy 選取物件（支援單選或複選）後，按下熱鍵可將物件名稱中
/// 「[」與「]」之間的文字複製到剪貼簿。
///
/// 使用方式：
/// 1. 這個腳本必須放在名為 "Editor" 的資料夾底下
///    （例如 Assets/Editor/CopyNameBetweenBrackets.cs），
///    否則 Unity 打包時會出錯。
/// 2. 在 Hierarchy 選取一個或多個物件，例如名稱為 "Enemy[Boss_01]"
///    （可用 Ctrl/Cmd 或 Shift 進行複選）
/// 3. 按下熱鍵 Ctrl+Shift+C（Mac 為 Cmd+Shift+C）
/// 4. 所有符合的 [] 內文字會依 Hierarchy 順序，
///    以「"內容",」的格式、每項一行（並縮排對齊）複製到剪貼簿，
///    最後一項不含逗號，可直接貼上作為陣列/清單使用
///    （單選時只會複製單獨一行，例如 "內容"，不含逗號）
///
/// 5. 選單「VzDev/Tools/複製時加上雙引號」可切換是否用 "" 包住每一項
///    （熱鍵 Ctrl+Shift+Alt+Q，勾選狀態會以 EditorPrefs 保存）。
///    關閉時輸出格式為「內容,」，其餘格式不變。
///
/// 若想更改熱鍵，修改下方 MenuItem 路徑字串裡的組合鍵代碼即可：
/// % = Ctrl(Win)/Cmd(Mac)，# = Shift，& = Alt，無符號 = 一般英數字鍵
/// </summary>
public static class CopyNameBetweenBrackets
{
    private const string MenuPath = "VzDev/Tools/複製物件名稱中的 [] 內文字 %#&c";
    private const string ToggleQuoteMenuPath = "VzDev/Tools/複製時加上雙引號 %#&q";
    private const string WrapWithQuotesPrefKey = "VzDev.CopyNameBetweenBrackets.WrapWithQuotes";

    /// <summary>
    /// 複製時是否以 "" 包住每一項內容（預設為 true，以 EditorPrefs 保存）。
    /// </summary>
    private static bool WrapWithQuotes
    {
        get => EditorPrefs.GetBool(WrapWithQuotesPrefKey, true);
        set => EditorPrefs.SetBool(WrapWithQuotesPrefKey, value);
    }

    [MenuItem(MenuPath)]
    private static void CopyBracketText()
    {
        // Selection.gameObjects 依 Hierarchy 順序排列，單選時陣列長度為 1，
        // 因此同一段邏輯可同時處理單選與複選。
        GameObject[] objs = Selection.gameObjects;
        if (objs == null || objs.Length == 0)
        {
            Debug.LogWarning("[CopyNameBetweenBrackets] 沒有選取任何物件。");
            return;
        }

        var matchedContents = new System.Collections.Generic.List<string>();
        var unmatchedNames = new System.Collections.Generic.List<string>();

        foreach (GameObject obj in objs)
        {
            string objName = obj.name;
            Match match = Regex.Match(objName, @"\[(.*?)\]");

            if (match.Success)
            {
                matchedContents.Add(match.Groups[1].Value);
            }
            else
            {
                unmatchedNames.Add(objName);
            }
        }

        if (matchedContents.Count == 0)
        {
            Debug.LogWarning($"[CopyNameBetweenBrackets] 選取的 {objs.Length} 個物件名稱中皆找不到 [] 內容。");
            return;
        }

        bool wrapWithQuotes = WrapWithQuotes;
        var formattedContents = new System.Collections.Generic.List<string>();
        foreach (string c in matchedContents)
        {
            formattedContents.Add(wrapWithQuotes ? $"\"{c}\"" : c);
        }
        string result = string.Join(",\n    ", formattedContents);
        EditorGUIUtility.systemCopyBuffer = result;

        var log = new StringBuilder();
        log.Append($"[CopyNameBetweenBrackets] 已複製 {matchedContents.Count} 項內容到剪貼簿：{result}");
        if (unmatchedNames.Count > 0)
        {
            log.Append($"（略過 {unmatchedNames.Count} 個無 [] 內容的物件：{string.Join("、", unmatchedNames)}）");
        }
        Debug.Log(log.ToString());
    }

    // 驗證函式：沒有選取物件時，選單項目會變成灰色（不可點擊）
    [MenuItem(MenuPath, true)]
    private static bool ValidateCopyBracketText()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    // 切換是否加上雙引號，選單項目前會顯示勾選狀態
    [MenuItem(ToggleQuoteMenuPath)]
    private static void ToggleWrapWithQuotes()
    {
        WrapWithQuotes = !WrapWithQuotes;
        Menu.SetChecked(ToggleQuoteMenuPath, WrapWithQuotes);
        Debug.Log($"[CopyNameBetweenBrackets] 複製時加上雙引號：{(WrapWithQuotes ? "開啟" : "關閉")}");
    }

    // 驗證函式：開啟選單時同步勾選狀態（確保重開 Editor 後也正確顯示）
    [MenuItem(ToggleQuoteMenuPath, true)]
    private static bool ValidateToggleWrapWithQuotes()
    {
        Menu.SetChecked(ToggleQuoteMenuPath, WrapWithQuotes);
        return true;
    }
}
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

/// <summary>
/// 在 Hierarchy 選取物件後，按下熱鍵可將物件名稱中
/// 「[」與「]」之間的文字複製到剪貼簿。
///
/// 使用方式：
/// 1. 這個腳本必須放在名為 "Editor" 的資料夾底下
///    （例如 Assets/Editor/CopyNameBetweenBrackets.cs），
///    否則 Unity 打包時會出錯。
/// 2. 在 Hierarchy 選取一個物件，例如名稱為 "Enemy[Boss_01]"
/// 3. 按下熱鍵 Ctrl+Shift+C（Mac 為 Cmd+Shift+C）
/// 4. "Boss_01" 就會被複製到系統剪貼簿，可直接貼上使用
///
/// 若想更改熱鍵，修改下方 MenuItem 路徑字串裡的組合鍵代碼即可：
/// % = Ctrl(Win)/Cmd(Mac)，# = Shift，& = Alt，無符號 = 一般英數字鍵
/// </summary>
public static class CopyNameBetweenBrackets
{
    private const string MenuPath = "VzDev Tools/複製物件名稱中的 [] 內文字 %#&c";

    [MenuItem(MenuPath)]
    private static void CopyBracketText()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogWarning("[CopyNameBetweenBrackets] 沒有選取任何物件。");
            return;
        }

        string objName = obj.name;
        Match match = Regex.Match(objName, @"\[(.*?)\]");

        if (!match.Success)
        {
            Debug.LogWarning($"[CopyNameBetweenBrackets] 物件名稱「{objName}」中找不到 [] 內容。");
            return;
        }

        string content = match.Groups[1].Value;
        EditorGUIUtility.systemCopyBuffer = content;
        Debug.Log($"[CopyNameBetweenBrackets] 已複製「{content}」到剪貼簿（來源物件：{objName}）");
    }

    // 驗證函式：沒有選取物件時，選單項目會變成灰色（不可點擊）
    [MenuItem(MenuPath, true)]
    private static bool ValidateCopyBracketText()
    {
        return Selection.activeGameObject != null;
    }
}

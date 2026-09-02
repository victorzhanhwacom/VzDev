using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace VzDev.EditorUtils
{
    /// <summary>
    /// 讀取系統剪貼簿內容（例如來自 Unity Editor 之外的其他 App 已複製的字串），
    /// 套用到所有選取物件的名稱上：
    /// <para>【已有 [ 與 ] 時】取代兩者之間的字串，保留 [ ] 本身與名稱其餘部分。</para>
    /// <para>【沒有 [ 與 ] 時】在名稱後面附加 " [{剪貼簿字串}]"。</para>
    /// 與既有「複製 [ 與 ] 之間字串到剪貼簿」功能互為逆向操作。
    /// 支援多選、支援 Undo（Ctrl+Z 可還原名稱變更）。
    /// </summary>
    public static class AppendClipboardToNameTool
    {
        // 避開 Unity 內建 Ctrl+Shift+V (Paste As Child)，改用 Ctrl+Alt+V。
        // 若日後仍與其他工具衝突，可透過 Edit > Shortcuts 視窗搜尋
        // "快速設定所選物件的DeviceCode(從剪貼簿)" 自行重新綁定，不需要改程式碼。
        private const string MenuPath = "VzDev/Tools/快速設定所選物件的DeviceCode(從剪貼簿) %&v";

        // 只取代第一組 [ ] 之間的內容（非貪婪比對，避免名稱裡有多組中括號時吃過頭）
        private static readonly Regex BracketPattern = new Regex(@"\[.*?\]", RegexOptions.Compiled);

        [MenuItem(MenuPath)]
        private static void ApplyClipboardToSelectedNames()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("[AppendClipboardToNameTool] 目前沒有選取任何物件。");
                return;
            }

            // Trim：剪貼簿字串常見會夾帶來源App附加的換行/空白字元（\r\n、尾端空格），
            // 若不清除，套用進中括號後，"]" 會被推到看不見的下一行，視覺上像是消失了。
            string clipboard = GUIUtility.systemCopyBuffer?.Trim();
            if (string.IsNullOrEmpty(clipboard))
            {
                Debug.LogWarning("[AppendClipboardToNameTool] 剪貼簿內容為空，取消操作。");
                return;
            }

            Undo.RecordObjects(selected, "Apply Clipboard To Name");

            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i];
                if (go == null) continue;

                go.name = BracketPattern.IsMatch(go.name)
                    ? BracketPattern.Replace(go.name, $"[{clipboard}]", 1) // 只取代第一組
                    : $"{go.name} [{clipboard}]";
            }

            Debug.Log($"[AppendClipboardToNameTool] 已將剪貼簿字串 \"{clipboard}\" 套用到 {selected.Length} 個物件名稱。");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateApplyClipboardToSelectedNames()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }
    }
}
using UnityEditor;
using UnityEngine;

namespace VzDev.EditorUtils
{
    /// <summary>
    /// 切換目前選取物件（Hierarchy / Scene）的「可選取（Pickable）」狀態。
    ///
    /// 機制：呼叫 SceneVisibilityManager 的 Picking API，
    /// 這正是 Hierarchy 每一列最前面「游標圖示」對應的功能
    /// （與眼睛圖示的 Visibility 是獨立的兩套狀態）：
    ///   - 停用 Picking 後，游標圖示會顯示為打X狀態
    ///   - 物件無法在 Scene 視圖中被滑鼠點選（Hierarchy 仍可點選/顯示）
    ///   - 不影響 Play Mode 邏輯、不影響 Inspector 編輯、不影響 Transform/Component 資料
    ///   - 純 Editor-only 狀態，不會被存進 Scene 檔案或影響 Build
    ///
    /// 每個選取物件依「自身目前狀態」獨立切換（非全部套同一狀態），
    /// 若混合選取已停用與未停用 Picking 的物件，各自反轉。
    ///
    /// includeDescendants = true：連同子物件一起切換，符合大量 Rack/設備物件多為
    /// prefab 結構（父物件掛 Collider，子物件為 Mesh）的場景習慣。
    ///
    /// 熱鍵：Ctrl+Shift+L（Windows）/ Cmd+Shift+L（Mac）
    /// 語法對照：% = Ctrl/Cmd, # = Shift, & = Alt
    /// </summary>
    public static class ToggleSelectableTool
    {
        private const string MenuPath = "VzDev/Toggle Selectable (Pickable) %#l";
        private const bool IncludeDescendants = true;

        [MenuItem(MenuPath, priority = 1)]
        private static void ToggleSelectable()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("[ToggleSelectableTool] 沒有選取任何物件，操作取消。");
                return;
            }

            var svm = SceneVisibilityManager.instance;
            int disabledCount = 0;
            int enabledCount = 0;

            foreach (GameObject go in selected)
            {
                if (go == null) continue;

                bool isPickingDisabled = svm.IsPickingDisabled(go);

                if (isPickingDisabled)
                {
                    svm.EnablePicking(go, IncludeDescendants);
                    enabledCount++;
                }
                else
                {
                    svm.DisablePicking(go, IncludeDescendants);
                    disabledCount++;
                }
            }

            EditorApplication.RepaintHierarchyWindow();
            SceneView.RepaintAll();

            Debug.Log($"[ToggleSelectableTool] 停用選取 {disabledCount} 個，恢復選取 {enabledCount} 個。");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggleSelectable()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }
    }
}

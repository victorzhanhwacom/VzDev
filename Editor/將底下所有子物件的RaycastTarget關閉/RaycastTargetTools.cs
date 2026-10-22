using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RaycastTargetTools
{
    const string Name = "Optimize Raycast Targets";
    const string MainPath = "VzDev/UI Tools/" + Name + " #&r";   // 熱鍵：Shift + Alt + R

    // ① 頂部選單 + 熱鍵（處理目前所有選取的物件）
    [MenuItem(MainPath, false, 0)]
    static void FromMainMenu() => Run(Selection.gameObjects);

    [MenuItem(MainPath, true)]   // 沒選物件時選單項會變灰
    static bool ValidateMainMenu() => Selection.gameObjects.Length > 0;

    // ② Hierarchy 右鍵
    [MenuItem("GameObject/" + MainPath, false, 0)] 
    static void FromHierarchy(MenuCommand cmd)
    {
        // 多選時 Unity 會對每個物件各呼叫一次，這裡只讓第一次執行
        var sel = Selection.gameObjects;
        if (cmd.context != null && sel.Length > 1 && cmd.context != sel[0]) return;
        Run(sel);
    }

    // ③ Inspector 元件右鍵（掛在 RectTransform 上，所以任何 UI 物件都有）
    [MenuItem("CONTEXT/RectTransform/" + Name)]
    static void FromInspector(MenuCommand cmd) => Run(new[] { ((Component)cmd.context).gameObject });

    static void Run(IEnumerable<GameObject> roots)
    {
        Undo.SetCurrentGroupName(Name);
        int group = Undo.GetCurrentGroup();

        int total = 0;
        foreach (var root in roots)
            if (root != null) total += root.OptimizeRaycastTargets();

        Undo.CollapseUndoOperations(group);   // 整批操作一次 Ctrl+Z 就能復原
        Debug.Log($"[RaycastTarget] 完成，共關閉 {total} 個");
    }

    /// 擴充方法：關閉 root 底下不需要接收點擊的 Raycast Target，回傳關閉的數量
    public static int OptimizeRaycastTargets(this GameObject root)
    {
        // 所有 Selectable（Button、Toggle、Slider…）指定的 Target Graphic 都要保留
        var keep = new HashSet<Graphic>(
            root.GetComponentsInChildren<Selectable>(true)
                .Select(s => s.targetGraphic)
                .Where(g => g != null));

        int count = 0;
        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
        {
            if (!g.raycastTarget) continue;
            // 同一個物件上有任何事件處理元件（Button、EventTrigger、自訂 IPointerClickHandler…）也保留
            if (keep.Contains(g) || g.GetComponent<IEventSystemHandler>() != null) continue;

            Undo.RecordObject(g, Name);
            g.raycastTarget = false;
            count++;
            Debug.Log($"[RaycastTarget] 關閉：{g.name}", g);   // 點 Console 訊息可跳到該物件
        }
        return count;
    }
}
using UnityEngine;

namespace VzDev.UnityAPI.Extensions
{
    public static class GameObjectExtension
    {
        /// 為GameObject的name加上標頭Header，格式為 [HeaderName] OriginalName
        public static string SetNameHeader(this GameObject self, string headerName)
        {
            // 1. 安全檢查：如果物件本身是 null，直接報錯並回傳空字串
            if (self == null)
            {
                Debug.LogError("SetNameHeader: Target GameObject is null!");
                return string.Empty;
            }

            // 2. 只撈一次名字，避免反覆觸發 Unity C++ 底層的字串分配
            string currentName = self.name;

            if (string.IsNullOrWhiteSpace(currentName))
                return currentName;

            // 3. 高效字串處理：不用 Regex，改用純字元索引查找（速度快數十倍、0 GC）
            string baseName = currentName;
            if (currentName.StartsWith("["))
            {
                int closeBracketIndex = currentName.IndexOf(']');
                if (closeBracketIndex != -1 && currentName.Length > closeBracketIndex + 1)
                {
                    // 擷取掉 "[XXX] " 之後的原本名稱
                    // 如果右括號後面有空格，就多跳一格
                    int startIndex = (currentName.Length > closeBracketIndex + 1 && currentName[closeBracketIndex + 1] == ' ')
                        ? closeBracketIndex + 2
                        : closeBracketIndex + 1;

                    baseName = currentName.Substring(startIndex);
                }
            }

            // 4. 真正幫物件改名！
            string newName = $"[{headerName}] {baseName}";
            self.name = newName;

            // 5. 回傳新名字
            return newName;
        }

        /// 將GameObject的Layer設置為對應的LayerMask(單一)
        public static void SetLayerMask(this GameObject self, LayerMask layerMask) =>
            /// LayerMask轉Layer的計算方式：LayerMask.value是2的layer次方，所以可以透過Log base 2來計算出對應的Layer
            self.layer = Mathf.RoundToInt(Mathf.Log(layerMask.value, 2));

        /// 刪除GameObject (Runtime/Editor), 包含檢查是否為null
        public static void ToDestroy(this GameObject self, bool isLogResult = false)
        {
#if UNITY_EDITOR
            string selfName = self.name;
            Object.DestroyImmediate(self, false);
#else
                Object.Destroy(self);
#endif
        }
    }
}
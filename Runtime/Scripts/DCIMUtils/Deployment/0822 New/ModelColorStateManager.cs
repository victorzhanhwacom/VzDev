using System.Collections.Generic;
using UnityEngine;

namespace VzDev.ColorUtils.ColorState
{
    /// <summary>
    /// 通用模型變色管理器。將傳入的目標（Renderer / Transform / GameObject）
    /// 透過 MaterialPropertyBlock 設定指定顏色，並可還原成設定前的原始顏色。
    ///
    /// 【為何用 MaterialPropertyBlock，而不是 renderer.material】
    /// 專案內 200+ 機櫃/設備 Renderer 走 GPU Instancing + MaterialPropertyBlock 做
    /// 逐物件變色（見 DCIM Shader 套件：alert pulse、status glow 等），存取
    /// renderer.material 會強制建立獨立材質實體，直接打斷 GPU Instancing／
    /// SRP Batcher，因此這裡統一走 SetPropertyBlock，不觸碰 sharedMaterial 本身。
    ///
    /// 【原始顏色只快取一次】同一個 Renderer 被要求變色多次（例如先設紅色再設黃色），
    /// 只在「目前沒有快取原始顏色」時才寫入 originalColors，避免把「已經被蓋過的
    /// 顏色」誤存成原始值，導致最終 Restore 還原到錯誤的顏色。
    ///
    /// 【重要假設】原始顏色一律讀取自 renderer.sharedMaterial 的 colorProperty 值，
    /// 假設在第一次呼叫 SetColor 之前，沒有其他系統已經用 MaterialPropertyBlock
    /// 覆寫過同一個顏色屬性（MaterialPropertyBlock 本身無法查詢「這個屬性目前是否
    /// 被覆寫過」，只能整體判斷是否為空）。如果告警系統與這個管理器需要同時作用在
    /// 同一顆 Renderer 上，建議用不同的 Shader 顏色屬性名稱區分基礎色與告警色，
    /// 或協調好呼叫順序，避免互相覆蓋對方要還原的基準值。
    ///
    /// 【colorProperty 需與 Shader 對應】目標 Shader 必須把該顏色屬性宣告在
    /// UNITY_INSTANCING_BUFFER 內（專案 DCIM Shader 套件皆遵循此規範），才能維持
    /// GPU Instancing 相容；預設 "_BaseColor" 對應 URP Lit/SimpleLit，若目標用的是
    /// 專案自訂Shader，呼叫時自行帶入對應的屬性名稱。
    ///
    /// 【靜態字典的生命週期風險】與已知的 MaterialStateService 風險相同：
    /// Additive 場景卸載時，若沒有先呼叫 RestoreColor/RestoreAll，
    /// originalColors 會殘留已銷毀 Renderer 的 dangling entry。
    /// Prune() 用 Unity 覆寫過的 == null 判斷清除，建議在樓層場景卸載流程呼叫一次。
    /// </summary>
    public static class ModelColorStateManager
    {
        private const string DefaultColorProperty = "_BaseColor";

        private static readonly Dictionary<Renderer, Color> originalColors = new();
        private static readonly MaterialPropertyBlock sharedBlock = new();

        /// <summary>
        /// 收集子物件Renderer用的共用暫存清單，避免每次呼叫都重新配置一份 List。
        /// 僅在同一次呼叫的同步流程內使用，不會有重入/併發問題（Unity 主執行緒單線程）。
        /// </summary>
        private static readonly List<Renderer> childRendererBuffer = new();

        #region SetColor — Renderer / Transform / GameObject 多種輸入型態
        /// <param name="includeChildren">
        /// false（預設）：只變 targets 清單裡指定的那些 Renderer 本身。
        /// true：以每個 Renderer 所在的 GameObject 為根，連同子物件（包含未啟用）的
        /// Renderer 一起變色——適合 targets 裡放的是「父層代表物件」的情況。
        /// </param>
        public static void SetColor(IReadOnlyList<Renderer> targets, Color color, string colorProperty = DefaultColorProperty, bool includeChildren = false)
        {
            if (targets == null) return;

            if (!includeChildren)
            {
                for (int i = 0; i < targets.Count; i++)
                    SetColorSingle(targets[i], color, colorProperty);
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;
                CollectRenderers(targets[i].gameObject, includeChildren: true, childRendererBuffer);
                for (int j = 0; j < childRendererBuffer.Count; j++)
                    SetColorSingle(childRendererBuffer[j], color, colorProperty);
            }
        }

        /// <param name="includeChildren">
        /// false（預設）：只抓 targets 自己身上的 Renderer，抓不到就跳過（例如目標是純 Transform 節點）。
        /// true：連同所有子物件（包含未啟用的 GameObject）的 Renderer 一起變色，
        /// 適合「機櫃外殼」這種父層節點本身沒有 Renderer、視覺上是由子模型組成的情況。
        /// </param>
        public static void SetColor(IReadOnlyList<Transform> targets, Color color, string colorProperty = DefaultColorProperty, bool includeChildren = false)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;
                CollectRenderers(targets[i].gameObject, includeChildren, childRendererBuffer);
                for (int j = 0; j < childRendererBuffer.Count; j++)
                    SetColorSingle(childRendererBuffer[j], color, colorProperty);
            }
        }

        /// <param name="includeChildren">說明同 Transform 版本的 includeChildren。</param>
        public static void SetColor(IReadOnlyList<GameObject> targets, Color color, string colorProperty = DefaultColorProperty, bool includeChildren = false)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;
                CollectRenderers(targets[i], includeChildren, childRendererBuffer);
                for (int j = 0; j < childRendererBuffer.Count; j++)
                    SetColorSingle(childRendererBuffer[j], color, colorProperty);
            }
        }

        public static void SetColorSingle(Renderer renderer, Color color, string colorProperty = DefaultColorProperty)
        {
            if (renderer == null) return;

            // 只在第一次變色時快取原始顏色，避免多次變色把中間顏色誤存成「原始值」
            if (!originalColors.ContainsKey(renderer))
                originalColors[renderer] = ReadBaseColor(renderer, colorProperty);

            renderer.GetPropertyBlock(sharedBlock);
            sharedBlock.SetColor(colorProperty, color);
            renderer.SetPropertyBlock(sharedBlock);
        }
        #endregion

        #region RestoreColor — 還原成設定前的原始顏色
        /// <param name="includeChildren">必須與當初呼叫 SetColor 時的設定一致，理由同下方 Transform 版本的說明。</param>
        public static void RestoreColor(IReadOnlyList<Renderer> targets, string colorProperty = DefaultColorProperty, bool includeChildren = false)
        {
            if (targets == null) return;

            if (!includeChildren)
            {
                for (int i = 0; i < targets.Count; i++)
                    RestoreColorSingle(targets[i], colorProperty);
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;
                CollectRenderers(targets[i].gameObject, includeChildren: true, childRendererBuffer);
                for (int j = 0; j < childRendererBuffer.Count; j++)
                    RestoreColorSingle(childRendererBuffer[j], colorProperty);
            }
        }

        /// <param name="includeChildren">
        /// 必須與當初呼叫 SetColor 時的設定一致，否則子物件變色後可能找不到對應的
        /// Renderer 而還原不到（因為 originalColors 的 Key 是實際的 Renderer，不是父層 Transform）。
        /// </param>
        public static void RestoreColor(IReadOnlyList<Transform> targets, string colorProperty = DefaultColorProperty, bool includeChildren = false)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;
                CollectRenderers(targets[i].gameObject, includeChildren, childRendererBuffer);
                for (int j = 0; j < childRendererBuffer.Count; j++)
                    RestoreColorSingle(childRendererBuffer[j], colorProperty);
            }
        }

        /// <param name="includeChildren">說明同 Transform 版本的 includeChildren。</param>
        public static void RestoreColor(IReadOnlyList<GameObject> targets, string colorProperty = DefaultColorProperty, bool includeChildren = false)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;
                CollectRenderers(targets[i], includeChildren, childRendererBuffer);
                for (int j = 0; j < childRendererBuffer.Count; j++)
                    RestoreColorSingle(childRendererBuffer[j], colorProperty);
            }
        }

        public static void RestoreColorSingle(Renderer renderer, string colorProperty = DefaultColorProperty)
        {
            if (renderer == null) return;
            if (!originalColors.TryGetValue(renderer, out Color original)) return; // 沒被變過色，不需處理

            renderer.GetPropertyBlock(sharedBlock);
            sharedBlock.SetColor(colorProperty, original);
            renderer.SetPropertyBlock(sharedBlock);

            originalColors.Remove(renderer);
        }

        /// <summary>
        /// 還原目前快取中「所有」被變色過的 Renderer，通常用於整批清除
        /// （例如告警全部解除、切換樓層場景前的保險清理）。
        /// </summary>
        public static void RestoreAll(string colorProperty = DefaultColorProperty)
        {
            if (originalColors.Count == 0) return;

            // 先複製一份 Key 清單，避免在 foreach 過程中修改字典造成例外
            var renderers = new List<Renderer>(originalColors.Keys);
            for (int i = 0; i < renderers.Count; i++)
                RestoreColorSingle(renderers[i], colorProperty);
        }
        #endregion

        #region 依 includeChildren 收集目標身上（或連同子物件）的 Renderer
        /// <summary>
        /// includeChildren = false：只抓 go 自己身上的 Renderer，抓不到就回傳空清單。
        /// includeChildren = true：連同未啟用的子物件一起收集（GetComponentsInChildren 的
        /// includeInactive 傳 true），避免例如「暫時關閉的告警子模型」被漏掉。
        /// 使用 GetComponentsInChildren(bool, List&lt;T&gt;) 的 List 版本寫入 buffer，
        /// 該 API 會先清空傳入的 List 再填入結果，這裡不需要另外手動 Clear。
        /// </summary>
        private static void CollectRenderers(GameObject go, bool includeChildren, List<Renderer> buffer)
        {
            if (!includeChildren)
            {
                buffer.Clear();
                if (go.TryGetComponent<Renderer>(out var r)) buffer.Add(r);
                return;
            }

            go.GetComponentsInChildren(true, buffer);
        }
        #endregion

        #region 讀取原始顏色（以 sharedMaterial 為基準，見類別註解的假設說明）
        private static Color ReadBaseColor(Renderer renderer, string colorProperty)
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(colorProperty))
                return renderer.sharedMaterial.GetColor(colorProperty);

            Debug.LogWarning(
                $"[{nameof(ModelColorStateManager)}] {renderer.name} 的材質沒有屬性 \"{colorProperty}\"，" +
                $"將以白色作為原始顏色的 Fallback，Restore 後可能與預期不符。", renderer);
            return Color.white;
        }
        #endregion

        #region 清理已銷毀 Renderer 的殘留快取（建議於 Additive 場景卸載流程呼叫）
        /// <summary>
        /// 清除 originalColors 中已經被銷毀（Missing Reference）的 Renderer 項目。
        /// 建議在樓層 Additive 場景卸載流程中呼叫一次，避免字典無限增長
        /// （與已知的 MaterialStateService 記憶體洩漏風險同一個成因）。
        /// </summary>
        public static void Prune()
        {
            List<Renderer> toRemove = null;
            foreach (var kv in originalColors)
            {
                if (kv.Key == null)
                {
                    toRemove ??= new List<Renderer>();
                    toRemove.Add(kv.Key);
                }
            }
            if (toRemove == null) return;
            for (int i = 0; i < toRemove.Count; i++)
                originalColors.Remove(toRemove[i]);
        }
        #endregion
    }
}
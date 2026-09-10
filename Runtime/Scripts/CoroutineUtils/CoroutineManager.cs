using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.CoroutineUtils
{
    /// <summary>
    /// 全域 Coroutine 執行器。任何地方（包含非 MonoBehaviour 的純 C# 類別）
    /// 都可以透過 static 方法把 IEnumerator 丟進來執行，不需要自己掛 MonoBehaviour。
    ///
    /// 【自動建立】第一次呼叫任何 static API 時，若場景上還沒有 instance，
    /// 會自動建立一個隱藏的 GameObject 並 DontDestroyOnLoad，
    /// 因此不受樓層 Additive 場景載入/卸載影響，永遠可用，Inspector 不需要手動擺放。
    /// 若場景上已手動擺放一份（例如想在 Inspector 觀察 runningCount），
    /// 則沿用該 instance，不會重複建立。
    ///
    /// 【Owner 分組】呼叫端可傳入 owner（通常是 this），讓多筆 coroutine 綁在同一個
    /// 邏輯擁有者身上，在該物件 OnDisable/OnDestroy 時呼叫 StopAll(owner) 一次清空，
    /// 避免 Toggle/面板等會反覆開關的物件累積殘留的 coroutine 呼叫已銷毀物件的方法。
    /// </summary>
    public class CoroutineManager : MonoBehaviour
    {
        #region Fields
        [InfoBox("全域 Coroutine 執行器，供外部透過 static 方式委派 Coroutine 執行")]
        [SerializeField, ReadOnly, Tooltip("目前追蹤中的 Owner 數量（僅供 Inspector 觀察，不影響邏輯）")]
        private int trackedOwnerCount;
        #endregion

        private static CoroutineManager instanceRef;
        private static bool isQuitting;

        /// <summary>
        /// owner → 該 owner 名下所有尚在執行的 Coroutine。
        /// 用 Coroutine 完成時自動從清單移除（見 TrackedRoutine），避免無限增長。
        /// </summary>
        private readonly Dictionary<object, List<Coroutine>> ownedCoroutines = new();

        /// <summary>
        /// 用來讓 TrackedRoutine 在完成時能找到「自己對應的 Coroutine 物件」的可變容器。
        /// StartCoroutine 呼叫時會同步跑到第一個 yield 之前，此時 Coroutine 物件尚未回傳，
        /// 因此無法在 TrackedRoutine 內直接取得自己的 Coroutine 參照；
        /// 用這個 box 延後賦值，等外層拿到回傳值後才填入，TrackedRoutine 真正用到它時
        /// （inner 執行完畢之後）一定已經被填好。
        /// </summary>
        private class CoroutineHandle
        {
            public Coroutine coroutine;
        }

        #region Lifecycle — Singleton
        private void Awake()
        {
            if (instanceRef != null && instanceRef != this)
            {
                Debug.LogWarning(
                    $"{nameof(CoroutineManager)} 場景上重複存在，此 instance 將被銷毀：{gameObject.name}", this);
                Destroy(gameObject);
                return;
            }
            instanceRef = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit() => isQuitting = true;

        private void OnDestroy()
        {
            if (instanceRef != this) return;
            instanceRef = null;
            ownedCoroutines.Clear();
        }
        #endregion

        /// <summary>
        /// 取得（必要時自動建立）唯一的 instance。
        /// 應用程式結束時（isQuitting）不再建立新的 GameObject，避免在退出流程中
        /// 產生「先建立又立刻被場景卸載銷毀」的無意義物件。
        /// </summary>
        private static CoroutineManager Instance
        {
            get
            {
                if (instanceRef != null) return instanceRef;
                if (isQuitting) return null;

                var go = new GameObject($"[{nameof(CoroutineManager)}]");
                instanceRef = go.AddComponent<CoroutineManager>();
                return instanceRef;
            }
        }

        #region Public Static API — 執行
        /// <summary>
        /// 執行一個 Coroutine，不綁定任何 owner（呼叫端自行保管回傳的 Coroutine 以便手動 Stop）。
        /// </summary>
        public static Coroutine Run(IEnumerator routine)
        {
            if (routine == null) return null;
            CoroutineManager inst = Instance;
            return inst != null ? inst.StartCoroutine(routine) : null;
        }

        /// <summary>
        /// 執行一個 Coroutine 並綁定到 owner。owner 通常傳 this（呼叫端的物件），
        /// 之後可用 StopAll(owner) 一次停掉這個 owner 名下所有尚在執行的 coroutine。
        /// owner 傳 null 等同呼叫 Run(routine)。
        /// </summary>
        public static Coroutine Run(IEnumerator routine, object owner)
        {
            if (routine == null) return null;
            if (owner == null) return Run(routine);

            CoroutineManager inst = Instance;
            if (inst == null) return null;

            var handle = new CoroutineHandle();
            handle.coroutine = inst.StartCoroutine(inst.TrackedRoutine(routine, owner, handle));

            if (!inst.ownedCoroutines.TryGetValue(owner, out var list))
            {
                list = new List<Coroutine>();
                inst.ownedCoroutines[owner] = list;
                inst.trackedOwnerCount = inst.ownedCoroutines.Count;
            }
            list.Add(handle.coroutine);

            return handle.coroutine;
        }

        /// <summary>
        /// 停止單一個 Coroutine（透過 Run 回傳的 handle）。
        /// </summary>
        public static void Stop(Coroutine coroutine)
        {
            if (coroutine == null || instanceRef == null) return;
            instanceRef.StopCoroutine(coroutine);
        }

        /// <summary>
        /// 停止某個 owner 名下所有尚在執行的 Coroutine，並清空該 owner 的追蹤紀錄。
        /// 建議在該 owner 的 OnDisable/OnDestroy 呼叫，避免殘留 coroutine 之後
        /// 呼叫到已停用/已銷毀物件的方法。
        /// </summary>
        public static void StopAll(object owner)
        {
            if (owner == null || instanceRef == null) return;
            if (!instanceRef.ownedCoroutines.TryGetValue(owner, out var list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null) instanceRef.StopCoroutine(list[i]);
            }
            list.Clear();
            instanceRef.ownedCoroutines.Remove(owner);
            instanceRef.trackedOwnerCount = instanceRef.ownedCoroutines.Count;
        }
        #endregion

        #region Public Static API — 常用便捷封裝
        /// <summary>
        /// delaySeconds 秒後執行 action。
        /// </summary>
        public static Coroutine RunDelayed(float delaySeconds, Action action, object owner = null)
            => Run(DelayedRoutine(delaySeconds, action), owner);

        /// <summary>
        /// 下一幀執行 action（等同 yield return null 後執行）。
        /// </summary>
        public static Coroutine RunNextFrame(Action action, object owner = null)
            => Run(NextFrameRoutine(action), owner);

        /// <summary>
        /// 經過 frameCount 幀後執行 action。
        /// </summary>
        public static Coroutine RunAfterFrames(int frameCount, Action action, object owner = null)
            => Run(AfterFramesRoutine(frameCount, action), owner);

        /// <summary>
        /// 等到 condition 成立後執行 onConditionMet（等同 yield return new WaitUntil(condition)）。
        /// </summary>
        public static Coroutine RunUntil(Func<bool> condition, Action onConditionMet, object owner = null)
            => Run(UntilRoutine(condition, onConditionMet), owner);
        #endregion

        #region Routine 包裝
        /// <summary>
        /// 用巢狀 StartCoroutine 等待 inner 執行完畢後，把自己從 ownedCoroutines 移除，
        /// 避免長時間運作下 owner 的清單無限增長（尤其大量短生命週期的 Toggle/預覽物件）。
        /// </summary>
        private IEnumerator TrackedRoutine(IEnumerator inner, object owner, CoroutineHandle handle)
        {
            yield return StartCoroutine(inner);

            if (ownedCoroutines.TryGetValue(owner, out var list))
            {
                list.Remove(handle.coroutine);
                if (list.Count == 0)
                {
                    ownedCoroutines.Remove(owner);
                    trackedOwnerCount = ownedCoroutines.Count;
                }
            }
        }

        private static IEnumerator DelayedRoutine(float delaySeconds, Action action)
        {
            if (delaySeconds > 0f) yield return new WaitForSeconds(delaySeconds);
            action?.Invoke();
        }

        private static IEnumerator NextFrameRoutine(Action action)
        {
            yield return null;
            action?.Invoke();
        }

        private static IEnumerator AfterFramesRoutine(int frameCount, Action action)
        {
            for (int i = 0; i < frameCount; i++) yield return null;
            action?.Invoke();
        }

        private static IEnumerator UntilRoutine(Func<bool> condition, Action onConditionMet)
        {
            yield return new WaitUntil(condition);
            onConditionMet?.Invoke();
        }
        #endregion
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = VzDev.ToolUtils.Debug;
using VzDev.Frameworks;

namespace VictorDev.Managers
{
    /// Task管理器，在Coroutine裡面執行Task，以維持主執行緒的同步
    public class TaskManager : SingletonMonoBehaviour<TaskManager>
    {
        /// 暫存執行中的Task
        private readonly Dictionary<string, TrackedTask> _runningTasks = new();

        /// 執行帶有 Tag 的 Task，如果同 Tag 的任務已存在，將會先取消並移除舊的
        /// <para>+ action 代入  async Task RunTask(CancellationToken token) </para>
        public static void Run(string tag, Func<CancellationToken, Task> taskAction, float timeoutSeconds = 60)
        {
            Cancel(tag);

            TrackedTask newTask = new TrackedTask(tag, timeoutSeconds);
            Instance._runningTasks[tag] = newTask;

            // Editor模式下無法執行Coroutine，所以直接執行Task
            if (Application.isPlaying) Instance.StartCoroutine(ExecuteCoroutine(newTask, taskAction));
            else ExecuteTask(newTask, taskAction);
        }

        /// 在Runtime下執行Coroutine + Task
        private static IEnumerator ExecuteCoroutine(TrackedTask newTask, Func<CancellationToken, Task> taskAction)
        {
            try
            {
                newTask.SetTask(taskAction(newTask.Cts.Token));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TaskManager] Failed to start task with tag '{newTask.Tag}': {ex}");
                Complete(newTask);
                yield break;
            }

            while (!newTask.Task.IsCompleted)
                yield return null;

            Complete(newTask, newTask.Task.Exception?.GetBaseException());
        }

        /// 在Editor下執行純Task
        private static async void ExecuteTask(TrackedTask newTask, Func<CancellationToken, Task> action)
        {
            Exception exception = null;
            try
            {
                newTask.SetTask(action(newTask.Cts.Token));
                await newTask.Task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Complete(newTask, exception);
            }
        }

        /// 任務結束後的統一處理：判斷結束原因、輸出Log、移除記錄並釋放資源
        private static void Complete(TrackedTask task, Exception exception = null)
        {
            if (task.IsManuallyCancelled)
            {
                Debug.LogWarning(
                    $"[TaskManager] Task '{task.Tag}' 被手動取消或被同 Tag 的新任務取代 (經過 {task.Elapsed:F1}s)");
            }
            else if (task.Cts.IsCancellationRequested)
            {
                Debug.LogWarning(
                    $"[TaskManager] Task '{task.Tag}' 逾時，超過 {task.TimeoutSeconds}s (經過 {task.Elapsed:F1}s)");
            }
            else if (task.Task?.IsCanceled == true || exception is OperationCanceledException)
            {
                Debug.LogWarning(
                    $"[TaskManager] Task '{task.Tag}' 被內部操作取消，非 TaskManager 逾時 (經過 {task.Elapsed:F1}s)" +
                    (exception != null ? $": {exception.Message}" : ""));
            }
            else if (exception != null)
            {
                Debug.LogWarning(
                    $"[TaskManager] Task '{task.Tag}' 發生例外 (經過 {task.Elapsed:F1}s): {exception}");
            }

            if (Instance._runningTasks.TryGetValue(task.Tag, out var current) && current == task)
                Instance._runningTasks.Remove(task.Tag);

            task.Dispose();
        }

        /// 取消指定 Tag 的任務
        public static void Cancel(string tag)
        {
            if (Instance._runningTasks.TryGetValue(tag, out var task))
            {
                try
                {
                    task.CancelManually();
                }
                catch (ObjectDisposedException ex)
                {
                    Debug.LogWarning($"CancellationTokenSource for tag [{tag}] was already disposed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unexpected error during cancel for tag [{tag}]: {ex.Message}");
                }
                finally
                {
                    Instance._runningTasks.Remove(tag);
                }
            }
        }

        /// 取消所有任務
        public static void CancelAll()
        {
           /*  Instance._runningTasks.CloneValuesAsList().ForEach(trackedTask =>
            {
                if (trackedTask == null)
                {
                    Debug.LogWarning("[TaskManager] CancelAll 中發現 null TrackedTask.");
                }
                else
                {
                    Cancel(trackedTask.Tag);
                }
            }); */
        }

        /// 指定的 Tag 是否正在執行任務
        public static bool IsRunning(string tag) => Instance._runningTasks.ContainsKey(tag);

        /// Task記錄資料結構
        private class TrackedTask : IDisposable
        {
            public string Tag { get; }
            public float TimeoutSeconds { get; }
            public Task Task { get; private set; }
            public CancellationTokenSource Cts { get; }

            /// 是否由 Cancel() 手動取消（包含被同 Tag 新任務取代）
            public bool IsManuallyCancelled { get; private set; }

            /// 開始時間 (不受 Time.timeScale 影響)
            public float StartTime { get; } = Time.realtimeSinceStartup;

            /// 已經過時間 (秒)
            public float Elapsed => Time.realtimeSinceStartup - StartTime;

            private bool _isDisposed;

            public TrackedTask(string tag, float timeoutSeconds = 10)
            {
                Tag = tag;
                TimeoutSeconds = timeoutSeconds;
                // Debug.Log($"[TaskManager] Task '{Tag}' 開始執行 (逾時 {TimeoutSeconds}s)");
                Cts = new CancellationTokenSource(Mathf.RoundToInt(timeoutSeconds * 1000));
            }

            public void SetTask(Task task) => Task = task;

            /// 手動取消，並標記取消原因
            public void CancelManually()
            {
                IsManuallyCancelled = true;
                if (!_isDisposed && !Cts.IsCancellationRequested) Cts.Cancel();
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                Cts.Dispose();
            }
        }
    }
}
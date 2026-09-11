using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VzDev.DebugUtils;
using UnityEngine;
using Debug = VzDev.ToolUtils.Debug;

namespace VictorDev.Managers
{
    /// Task管理器，在Coroutine裡面執行Task，以維持主執行緒的同步
    public class TaskManager : SingletonMonoBehaviour<TaskManager>
    {
        /// 暫存執行中的Task
        private readonly Dictionary<string, TrackedTask> _runningTasks = new();

        /// 執行帶有 Tag 的 Task，如果同 Tag 的任務已存在，將會先取消並移除舊的
        /// <para>+ action 代入  async Task RunTask(CancellationToken token) </para>
        public static void Run(string tag, Func<CancellationToken, Task> taskAction, float timeoutSeconds = 10)
        {
            Cancel(tag);

            TrackedTask newTask = new TrackedTask(tag, timeoutSeconds);
            Instance._runningTasks[tag] = newTask;

            // Editor模式下無法執行Coroutine，所以直接執行Task
            if (Application.isPlaying) Instance.StartCoroutine(ExecuteCoroutine(newTask, taskAction));
            else ExecuteTask(newTask, taskAction, timeoutSeconds);
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
                Instance._runningTasks.Remove(newTask.Tag);
                yield break;
            }

            while (!newTask.Task.IsCompleted)
                yield return null;

            if (newTask.Task.IsCanceled || newTask.Cts.IsCancellationRequested)
            {
                Debug.LogWarning($"[TaskManager] Task with tag '{newTask.Tag}' was cancelled by timeout.");
            }
            else if (newTask.Task.IsFaulted)
            {
                Debug.LogWarning($"[TaskManager] Task with tag '{newTask.Tag}' threw exception: {newTask.Task.Exception}");
            }

            if (Instance._runningTasks.TryGetValue(newTask.Tag, out var current) && current == newTask)
                Instance._runningTasks.Remove(newTask.Tag);
        }

        /// 在Editor下執行純Task
        private static async void ExecuteTask(TrackedTask newTask, Func<CancellationToken, Task> action, float timeoutSeconds)
        {
            try
            {
                newTask.SetTask(action(newTask.Cts.Token));
                await newTask.Task;
            }
            catch (OperationCanceledException) when (newTask.Cts.IsCancellationRequested)
            {
                Debug.LogWarning(
                    $"[TaskManager] Task with tag '{newTask.Tag}' was cancelled by timeout {timeoutSeconds} seconds.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TaskManager] Task with tag '{newTask.Tag}' threw exception: {ex}");
            }
            finally
            {
                if (Instance._runningTasks.TryGetValue(newTask.Tag, out var current) && current == newTask)
                    Instance._runningTasks.Remove(newTask.Tag);
            }
        }

        /// 取消指定 Tag 的任務
        public static void Cancel(string tag)
        {
            if (Instance._runningTasks.TryGetValue(tag, out var task))
            {
                try
                {
                    if (!task.Cts.IsCancellationRequested) task.Cts.Cancel();
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
        private class TrackedTask
        {
            public string Tag { get; private set; }
            public Task Task { get; private set; }
            public CancellationTokenSource Cts{ get; private set; }

            public TrackedTask(string tag, float timeoutSeconds = 10)
            {
                Tag = tag;
                Cts = new CancellationTokenSource(Mathf.RoundToInt(timeoutSeconds * 1000));
            }

            public void SetTask(Task task) => Task = task;
        }
    }
}
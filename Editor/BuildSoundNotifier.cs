using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VzDev
{
    public class BuildSoundNotifier : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result == BuildResult.Succeeded)
            {
                PlayBuildSound();
                Debug.Log("Build 完成!");
            }
            else
            {
                Debug.LogWarning("Build 失敗或被取消。");
            }
        }
        private void PlayBuildSound() => EditorApplication.Beep(); // 最簡單的提示音
    }
}

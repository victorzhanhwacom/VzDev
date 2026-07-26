using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebGLUtils
{
        /// <summary>
        /// 接收JS端訊息 (For主選單控制)
        /// </summary>
        public class WebGLBridge_MainMenu : WebGLBridge
        {
                [field: SerializeField, ReadOnly]
                public string MainMenuIndex { get; private set; } = string.Empty;

                [Foldout("[Events-Custom]")] public UnityEvent<int> OnMainMenuChanged;
                [Foldout("[Events-Custom]")] public UnityEvent GoToHomePage;

                public void SetMainMenu(string mainMenuIndex)
                {
                        MainMenuIndex = mainMenuIndex;
                        if (int.TryParse(MainMenuIndex, out int index))
                        {
                                Debug.Log($"[WebGLBridge_MainMenu] MainMenuIndex 已更新為: {index}");
                                if(index == -1)
                                {
                                        GoToHomePage?.Invoke();
                                }
                                else
                                {
                                        OnMainMenuChanged?.Invoke(index);
                                }
                        }
                        else
                        {
                                Debug.LogWarning($"[WebGLBridge_MainMenu] 無法解析 MainMenuIndex: {MainMenuIndex} 為整數");
                        }
                }
        }
}
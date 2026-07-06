using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.WebGLUtils
{
        /// <summary>
        /// 接收JS端訊息 (For環控)
        /// </summary>
        public class WebGLBridge_Env : WebGLBridge
        {
                [field: SerializeField, ReadOnly]
                public string SubMenuIndex { get; private set; } = string.Empty;

                [Foldout("[Events-Custom]")] public UnityEvent<int> OnSubMenuChanged;
                [Foldout("[Components]"), SerializeField] private GameObject pageTarget;

                public void SetSubMenu(string subMenuIndex)
                {
                        if (pageTarget.activeSelf == false)
                        {
                                Debug.LogWarning($"[WebGLBridge_Env] 無法設定 SubMenuIndex，因為 pageTarget 尚未啟用");
                                return;
                        }
                        SubMenuIndex = subMenuIndex;
                        if (int.TryParse(SubMenuIndex, out int index))
                        {
                                Debug.Log($"[WebGLBridge_Env] SubMenuIndex 已更新為: {index}");
                                OnSubMenuChanged?.Invoke(index);
                        }
                        else
                        {
                                Debug.LogWarning($"[WebGLBridge_Env] 無法解析 SubMenuIndex: {SubMenuIndex} 為整數");
                        }
                }
        }
}
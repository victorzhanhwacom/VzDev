using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VzDev.WebGLUtils;

namespace VzDev
{
    public class MainMenuSwitcher : MonoBehaviour
    {
        [SerializeField] private SystemMenuEventSetting[] systemMenuEventSettings;

        public void SwitchMenu(EnumSystemMenu systemMenu)
        {
            Debug.Log($"MainMenuSwitcher.SwitchMenu: {systemMenu}");
            foreach (var item in systemMenuEventSettings)
            {
                if (item.systemMenu == systemMenu)
                {
                    Debug.Log($"MainMenuSwitcher.SwitchMenu: Found matching system menu: {systemMenu}. Invoking event.");
                    item.onEvent?.Invoke();
                    return; //只會有一個符合的系統選單，找到後就直接返回
                }
            }
        }

        [Serializable]
        public class SystemMenuEventSetting
        {
            public EnumSystemMenu systemMenu;
            public UnityEvent onEvent;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SystemMenuEventSetting))]
        public class SystemMenuEventSettingDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty systemMenuProp = property.FindPropertyRelative("systemMenu");

                // 取得目前 enum 選項顯示的文字（例如 "電力DCP"）
                string displayName = systemMenuProp.enumDisplayNames[systemMenuProp.enumValueIndex];

                // 把原本的 "Element 0" 換成該值
                GUIContent newLabel = new GUIContent(displayName);

                EditorGUI.PropertyField(position, property, newLabel, true);
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }
#endif 
    }
}

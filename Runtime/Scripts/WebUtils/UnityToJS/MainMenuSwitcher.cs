using System;
using UnityEngine;
using UnityEngine.UI;
using VzDev.WebGLUtils;

namespace VzDev
{
    public class MainMenuSwitcher : MonoBehaviour
    {
        public MenuItem[] menuItems;

        public void SwitchMenu(EnumSystemMenu systemMenu)
        {
            foreach (var item in menuItems)
            {
                if (item.systemMenu == systemMenu)
                {
                    foreach (var toggle in item.toggle)
                    {
                        toggle.isOn = true;
                    }
                }
            }
        }

        [Serializable]
        public class MenuItem
        {
            public EnumSystemMenu systemMenu;
            public Toggle[] toggle;
        }
    }
}

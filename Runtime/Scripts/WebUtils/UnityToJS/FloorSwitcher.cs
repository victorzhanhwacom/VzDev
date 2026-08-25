using System;
using UnityEngine;
using UnityEngine.UI;
using VzDev.WebGLUtils;

namespace VzDev
{
    public class FloorSwitcher : MonoBehaviour
    {
        public FlooItem[] menuItems;

        public void SwitchFloor(EnumFloor floor)
        {
            foreach (var item in menuItems)
            {
                if (item.floor == floor)
                {
                    foreach (var toggle in item.toggle)
                    {
                        toggle.isOn = true;
                    }
                }
            }
        }

        [Serializable]
        public class FlooItem
        {
            public EnumFloor floor;
            public Toggle[] toggle;
        }
    }
}

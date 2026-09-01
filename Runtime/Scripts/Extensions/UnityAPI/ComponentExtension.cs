using System.Collections.Generic;
using UnityEngine;

namespace VzDev.ApiExtensions
{
    
    public static class ComponentExtension
    {
       public static void SetIfNull(this Component self, Component value)
        {
            if(self == null)
            {
                self = value;
            }
        }
    }
}
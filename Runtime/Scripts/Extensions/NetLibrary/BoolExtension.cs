using System;
using UnityEngine;

namespace VzDev.NetLibrary.Extensions
{
    public static class BoolExtension
    {
        
        public static void CheckAndAction(this bool self, Action actionOnTrue, Action actionOnFalse)
        {
            if (self) actionOnTrue?.Invoke();
            else actionOnFalse?.Invoke();
        }
    }
}
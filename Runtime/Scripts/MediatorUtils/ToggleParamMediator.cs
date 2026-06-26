using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.MediatorUtils
{
    /// <summary>
    /// 根據設定，加上相對應的參數進行Invoke
    /// </summary>
    public class ToggleParamMediator : MonoBehaviour
    {
        #region Fields
        [Foldout("[Events]"), ShowIf(nameof(IsStringType))] public UnityEvent<bool, string> onToggleString;
        [Foldout("[Events]"), ShowIf(nameof(IsIntType))] public UnityEvent<bool, int> onToggleInt;
        [Foldout("[Settings]"), SerializeField] private EnumParamType paramType;
        [Foldout("[Settings]"), SerializeField, ShowIf(nameof(IsStringType))] private string paramString;
        [Foldout("[Settings]"), SerializeField, ShowIf(nameof(IsIntType))] private int paramInt;

        private bool IsStringType => paramType == EnumParamType.String;
        private bool IsIntType => paramType == EnumParamType.Int;
        #endregion

        public void SetBoolParam(bool value)
        {
            switch (paramType)
            {
                case EnumParamType.String:
                    onToggleString?.Invoke(value, paramString);
                    break;
                case EnumParamType.Int:
                    onToggleInt?.Invoke(value, paramInt);
                    break;
            }
        }

         public enum EnumParamType
        {
            String, Int
        }
    }
}

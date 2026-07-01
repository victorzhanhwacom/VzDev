using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace VzDev.InputUtils
{
    /// <summary>
    /// 監聽鍵盤輸入，當按下數字鍵 (0-9 / Numpad 0-9) 或字母鍵 (A-Z) 時，觸發對應的事件。
    /// </summary>
    public class KeyInputTrigger : MonoBehaviour
    {
        #region Fields
        [SerializeField] private bool isActive = true;

        [Foldout("數字鍵事件 (0-9 / Numpad 0-9)")]
        public UnityEvent<int> OnNumberKeyPressed;

        [Foldout("文字鍵事件 (A-Z)")]
        public UnityEvent<string> OnLetterKeyPressed;
        #endregion

        private void Update()
        {
            if (!isActive || Keyboard.current == null) return;

            foreach (var key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    HandleKey(key);
                }
            }
        }

        private void HandleKey(KeyControl key)
        {
            Key code = key.keyCode;

            // 主鍵盤數字鍵 + Numpad 數字鍵，統一轉成 0-9
            int? digit = GetDigitValue(code);
            if (digit.HasValue)
            {
                OnNumberKeyPressed?.Invoke(digit.Value);
                OnLetterKeyPressed?.Invoke(digit.Value.ToString());
                return;
            }

            // 字母鍵 A-Z (enum 本身就是照 A-Z 順序排列，範圍比較沒問題)
            if (code >= Key.A && code <= Key.Z)
            {
                OnLetterKeyPressed?.Invoke(code.ToString());
                return;
            }
        }

        /// <summary>
        /// 將主鍵盤數字鍵或 Numpad 數字鍵轉換為 0-9 的數值。
        /// 主鍵盤數字鍵 enum 順序為 Digit1~Digit9, Digit0（非數值順序），故不可用範圍比較。
        /// </summary>
        private int? GetDigitValue(Key code)
        {
            switch (code)
            {
                case Key.Digit0: return 0;
                case Key.Digit1: return 1;
                case Key.Digit2: return 2;
                case Key.Digit3: return 3;
                case Key.Digit4: return 4;
                case Key.Digit5: return 5;
                case Key.Digit6: return 6;
                case Key.Digit7: return 7;
                case Key.Digit8: return 8;
                case Key.Digit9: return 9;
                case Key.Numpad0: return 0;
                case Key.Numpad1: return 1;
                case Key.Numpad2: return 2;
                case Key.Numpad3: return 3;
                case Key.Numpad4: return 4;
                case Key.Numpad5: return 5;
                case Key.Numpad6: return 6;
                case Key.Numpad7: return 7;
                case Key.Numpad8: return 8;
                case Key.Numpad9: return 9;
                default: return null;
            }
        }

        private void OnValidate() => name = $"{GetType().Name} (DevBuild Only)";
    }
}
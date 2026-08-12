using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VzDev.UI
{
    /// <summary>
    /// 掛在任意uGUI元件（Image / Button / RawImage...）上即可提供Tooltip。
    /// 透過IPointerEnter/Exit走EventSystem，WebGL滑鼠與觸控都能正常觸發。
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [TextArea]
        [SerializeField] private string tooltipText;
        [SerializeField] private float showDelay = 0.3f;

        /// <summary>
        /// 動態文字來源（選用）。設定後Show時會優先呼叫此委派取得最新文字，
        /// 適合像DCIM即時資料這種內容會變動的情境，避免每次都要重新SetText整個元件。
        /// </summary>
        public Func<string> DynamicTextProvider;

        private WaitForSeconds _delayWait;
        private Coroutine _delayRoutine;

        private void Awake()
        {
            _delayWait = new WaitForSeconds(showDelay);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_delayRoutine != null) StopCoroutine(_delayRoutine);
            _delayRoutine = StartCoroutine(ShowAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelPendingShow();
            TooltipManager.Instance?.Hide();
        }

        private void OnDisable()
        {
            CancelPendingShow();
            TooltipManager.Instance?.Hide();
        }

        private void CancelPendingShow()
        {
            if (_delayRoutine == null) return;
            StopCoroutine(_delayRoutine);
            _delayRoutine = null;
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return _delayWait;
            string text = DynamicTextProvider != null ? DynamicTextProvider() : tooltipText;
            TooltipManager.Instance?.Show(text);
            _delayRoutine = null;
        }

        /// <summary>Inspector外部/runtime想改固定文字時用這個</summary>
        public void SetTooltipText(string text) => tooltipText = text;
    }
}

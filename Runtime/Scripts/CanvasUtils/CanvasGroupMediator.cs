using UnityEngine;

namespace VzDev.CanvasUtils
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupMediator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public void ToShow() => SetCanvasGroupAlpha(1f);
        public void ToHide() => SetCanvasGroupAlpha(0f);

        public void SetCanvasGroupAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
            SetCanvasGroupInteractable(alpha > 0f);
        }

        public void SetCanvasGroupInteractable(bool interactable)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        private void OnValidate() => Awake();
    }
}

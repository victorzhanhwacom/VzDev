using DG.Tweening;
using UnityEngine;
using VzDev.RaycastUtils;
using VzDev.UnityAPI.Extensions;

namespace VzDev
{
    public class RaycastObject : MonoBehaviour, IRaycastTarget
    {
        [SerializeField, ColorUsage(true, true)] private Color hoverColor = Color.yellow;
        [SerializeField] private float jumpDistance = 2f;
        [SerializeField] private float scaleOut = 1.2f;
        [SerializeField] private float clickDuration = 0.5f;
        [SerializeField] private float hoverDuration = 0.2f;
        [SerializeField] private Ease ease = Ease.OutQuad;
        private Vector3 originalScale, originalPosition;
        private Renderer _renderer;

        private void Start()
        {
            originalScale = transform.localScale;
            originalPosition = transform.position;
            _renderer = GetComponent<Renderer>();
        }


        public void OnRaycastClick(in RaycastHit hit, int hitIndex)
        {
            //跳一下，最後要回到原始位置
            transform.DOJump(originalPosition, jumpDistance, 1, clickDuration).SetEase(ease);

        }

        public void OnRaycastHoverEnter(in RaycastHit hit, int hitIndex)
        {
            //設定DOScale Offset
            transform.DOScale(scaleOut, hoverDuration).SetRelative().SetEase(ease);

            //改變顏色
            if (_renderer != null)
            {
                _renderer.DOEmission(hoverColor, hoverDuration).SetEase(ease);
            }
        }

        public void OnRaycastHoverExit()
        {
            transform.DOScale(originalScale, hoverDuration).SetEase(ease);
            //恢復顏色
            if (_renderer != null)
            {
                _renderer.DOEmission(Color.black, hoverDuration).SetEase(ease);
            }
        }
    }
}

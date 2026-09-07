using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DOTweenUtils
{
    public class DOTweenMover : DOTweenBase
    {
        private enum EnumPosType
        {
            OffsetPos, AnchorPos
        }
        private enum EnumType
        {
            From, To
        }

        #region Fields

        [SerializeField, ReadOnly] private Vector2 originalPos, destinationPos;

        [Foldout("[Tween Move]"), SerializeField] private EnumPosType posType = EnumPosType.OffsetPos;
        private bool isOffsetPos => posType == EnumPosType.OffsetPos;
        [Foldout("[Tween Move]"), SerializeField] private EnumType enumType = EnumType.From;
        private bool isFromOffset => isOffsetPos && enumType == EnumType.From;
        private bool isToOffset => isOffsetPos && enumType == EnumType.To;
        private bool isFromPos => !isOffsetPos && enumType == EnumType.From;
        private bool isToPos => !isOffsetPos && enumType == EnumType.To;

        [Foldout("[Tween Offset]"), SerializeField, ShowIf("isFromOffset")] private Vector2 fromOffset;
        [Foldout("[Tween Offset]"), SerializeField, ShowIf("isToOffset")] private Vector2 toOffset;

        [Foldout("[Tween Pos]"), SerializeField, ShowIf("isFromPos")] private Vector2 fromPos;
        [Foldout("[Tween Pos]"), SerializeField, ShowIf("isToPos")] private Vector2 toPos;

        [Foldout("[Components]"), SerializeField] private RectTransform rectTransform;

        #endregion

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            Vector2 basePos = rectTransform.anchoredPosition;

            if (enumType == EnumType.From)
            {
                // From 由使用者指定，To(destination) 用目前的實際位置
                originalPos = isOffsetPos ? basePos + (Vector2)fromOffset : (Vector2)fromPos;
                destinationPos = basePos;
            }
            else // EnumType.To
            {
                // To 由使用者指定，From(original) 用目前的實際位置
                destinationPos = isOffsetPos ? basePos + (Vector2)toOffset : (Vector2)toPos;
                originalPos = basePos;
            }
        }

        public override void SetVisible(bool isVisible)
        {
            StopTween();
            tween = rectTransform.DOAnchorPos(isVisible ? destinationPos : originalPos, Duration);
            tween = SetTweenParams(tween, isVisible);
        }

        private void OnValidate() => Awake();
        private void OnDisable() => tween?.Kill();
    }
}

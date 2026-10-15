using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VzDev
{
    public class GateMoverTest : MonoBehaviour
    {
        public Transform leftDoor, rightDoor;
        public Vector3 posOffsetLeftDoor, posOffsetRightDoor;
        public float speed = 1f;
        public float holdSec = 2f;
        public Ease ease = Ease.InQuad;

        public Collider gateCollider;

        private Camera mainCamera;

        private Sequence leftGateSequence, rightGateSequence;

        private void Start()
        {
            mainCamera = Camera.main;
            // 預先建立序列，供後續呼叫使用
            leftGateSequence = SetSequence(leftDoor, posOffsetLeftDoor, leftDoor.localPosition);
            rightGateSequence = SetSequence(rightDoor, posOffsetRightDoor, rightDoor.localPosition);
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // 點在 UI 上時不觸發（場景中沒有 EventSystem 可省略）
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit) && hit.collider == gateCollider)
            {
                OpenGate();
            }
        }


        private Sequence SetSequence(Transform gate, Vector3 offsetPos, Vector3 initialPos)
            => DOTween.Sequence()
                .Append(gate.DOLocalMove(offsetPos, speed).SetEase(ease).SetRelative())
                .AppendInterval(holdSec)
                .Append(gate.DOLocalMove(initialPos, speed).SetEase(ease))
                .SetAutoKill(false)   // 播完保留，才能重複使用
                .Pause();             // 統一在這裡暫停

        [Button]
        public void MoveLeftGate() => PlayIfIdle(leftGateSequence);

        [Button]
        public void MoveRightGate() => PlayIfIdle(rightGateSequence);

        [Button]
        public void OpenGate()
        {
            MoveLeftGate();
            MoveRightGate();
        }

        private void PlayIfIdle(Sequence seq)
        {
            if (seq == null || seq.IsPlaying()) return;
            seq.Restart();
        }

        private void OnDestroy()
        {
            leftGateSequence?.Kill();
            rightGateSequence?.Kill();
        }
    }
}

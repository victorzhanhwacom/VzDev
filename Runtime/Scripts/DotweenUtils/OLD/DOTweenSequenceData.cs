using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    /// 「動畫序列」的設定檔。可同時播放多個 DOTweenBaseData。
    [CreateAssetMenu(fileName = "DOTweenSequence", menuName = "VzDev/DOTween/Sequence")]
    public class DOTweenSequenceData : DOTweenBaseData
    {
        // 同時拖入你做好的 MoverData 和 FaderData 資產！
        public List<DOTweenBaseData> subAnimations;

        public override ITweenWorker CreateWorker(GameObject target)
        {
            List<ITweenWorker> workers = new List<ITweenWorker>();
            foreach (DOTweenBaseData anim in subAnimations)
            {
                workers.Add(anim.CreateWorker(target));
            }
            return new SequenceWorker(workers);
        }
    }

    public class SequenceWorker : ITweenWorker
    {
        #region Variables
        private List<ITweenWorker> _workers;
        public SequenceWorker(List<ITweenWorker> workers) => _workers = workers;

        private int loopCounter;
        #endregion

        public void Play(UnityEvent onStart, UnityEvent onComplete)
        {
            loopCounter = 0;
            UnityEvent subAnimComplete = new();
            subAnimComplete.AddListener(() =>
            {
                loopCounter++;
                if (loopCounter >= _workers.Count) onComplete?.Invoke();
            });

            onStart?.Invoke();
            // 同時播放所有的子動畫（這邊可以用 Counter 來計算何時真正 onComplete）
            foreach (ITweenWorker worker in _workers)
            {
                worker.Play(null, subAnimComplete);
            }
        }

        public void Stop()
        {
            foreach (ITweenWorker worker in _workers) worker.Stop();
        }

        public void PlayBackwards()
        {
            foreach (ITweenWorker worker in _workers) worker.PlayBackwards();
        }
    }
}
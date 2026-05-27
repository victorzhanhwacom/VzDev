using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace VzDev.DotweenUtils
{
    /// DOTweenBase 是一個 ScriptableObject 類別，作為所有 DOTween 動畫設定的基底類別。它定義了動畫的基本屬性，如持續時間、延遲、緩動方式，以及是否使用特定的起始值和結束值。具體的動畫類別（如 DOTweenMover）將繼承自這個基底類別，並添加更多特定於動畫類型的屬性和方法。這種設計使得我們可以在 Unity 編輯器中創建不同類型的動畫設定資產，並在場景中通過 DOTweenPlayer 來執行這些動畫。
    public abstract class DOTweenBaseData : ScriptableObject
    {
        public float duration = 0.3f, delay;
        public Ease ease = Ease.OutQuad;

        // 核心魔術：傳入要被斷畫的 GameObject，並在內部做好「紀錄起點」與「產生 Tween」的動作
        // 這裡回傳一個包裝好的「動畫操作器」
        public abstract ITweenWorker CreateWorker(GameObject target);
    }

    // 用來讓 Player 方便操作的介面，隱藏不同動畫的實作細節
    public interface ITweenWorker
    {
        void Play(UnityEvent onComplete);
        void Stop();
        void PlayBackwards();
    }
}
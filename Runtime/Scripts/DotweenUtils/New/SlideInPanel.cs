using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using NaughtyAttributes;

public class SlideInPanel : MonoBehaviour
{
    [Header("Layout 內的兩個物件")]
    public LayoutElement item1Layout; 
    public LayoutElement item2Layout;

    [Header("從右滑入的第三個物件")]
    public RectTransform item3;

    [Header("設定")]
    public float item3Width = 300f;       // Item3 的寬度
    public float totalWidth = 1200f;      // HorizontalLayout 的總寬度
    public float duration = 0.5f;
    public Ease easeType = Ease.OutCubic;

    private float fullWidth;             // Item1、2 各自的初始寬度

    void Start()
    {
        fullWidth = totalWidth / 2f;

        // 初始化：Item1、2 各佔一半
        item1Layout.preferredWidth = fullWidth;
        item2Layout.preferredWidth = fullWidth;

        // Item3 初始在畫面右側外
        item3.anchoredPosition = new Vector2(item3Width + 50f, 0f);
    }

    [Button]
    public void SlideIn()
    {
        float remainWidth = (totalWidth - item3Width) / 2f;

        // Item3 從右側滑入
        item3.DOAnchorPos(new Vector2(0f, 0f), duration)
             .SetEase(easeType);

        // Item1、Item2 同步縮減寬度，平均分配剩餘空間
        DOTween.To(
            () => item1Layout.preferredWidth,
            x => item1Layout.preferredWidth = x,
            remainWidth,
            duration
        ).SetEase(easeType);

        DOTween.To(
            () => item2Layout.preferredWidth,
            x => item2Layout.preferredWidth = x,
            remainWidth,
            duration
        ).SetEase(easeType);
    }

    [Button]
    public void SlideOut()
    {
        // 反向動畫
        item3.DOAnchorPos(new Vector2(item3Width + 50f, 0f), duration)
             .SetEase(easeType);

        DOTween.To(
            () => item1Layout.preferredWidth,
            x => item1Layout.preferredWidth = x,
            fullWidth,
            duration
        ).SetEase(easeType);

        DOTween.To(
            () => item2Layout.preferredWidth,
            x => item2Layout.preferredWidth = x,
            fullWidth,
            duration
        ).SetEase(easeType);
    }
}
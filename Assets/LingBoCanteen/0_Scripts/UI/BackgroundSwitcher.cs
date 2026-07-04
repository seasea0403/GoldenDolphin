using UnityEngine;

using DG.Tweening; // 引入命名空间

public class BackgroundSwitcher : MonoBehaviour

{

    // 将三个背景按顺序排列在 Inspector 中

    public RectTransform[] backgrounds;

    public float switchDuration = 0.5f; // 切换动画时长

    private int currentIndex = 0;

    void Start()

    {

        // 初始化：确保除了第一个背景外，其他都在屏幕上方（Y = Screen Height）

        for (int i = 1; i < backgrounds.Length; i++)

        {

            Vector3 pos = backgrounds[i].anchoredPosition;

            pos.y = Screen.height;

            backgrounds[i].anchoredPosition = pos;

        }

    }

    public void SwitchToNext()

    {

        // 1. 将当前背景移出屏幕下方

        RectTransform currentBg = backgrounds[currentIndex];

        currentBg.DOAnchorPosY(-Screen.height, switchDuration).SetEase(Ease.InOutQuad);

        // 2. 计算下一个索引

        currentIndex = (currentIndex + 1) % backgrounds.Length;

        RectTransform nextBg = backgrounds[currentIndex];

        // 3. 将下一个背景从屏幕上方移入

        nextBg.DOAnchorPosY(0, switchDuration).SetEase(Ease.InOutQuad);

    }

}
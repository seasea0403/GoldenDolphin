using DG.Tweening;
using UnityEngine;

public class StampDrop : MonoBehaviour
{
    public Vector2 targetPos = Vector2.zero;

    public Vector2 startOffset = new Vector2(200, -200);

    public float duration = 0.2f;

    public void Play()
    {
        var rt = GetComponent<RectTransform>();

        rt.anchoredPosition = targetPos + startOffset;

        rt.DOAnchorPos(targetPos, duration).SetEase(Ease.InQuad);
    }
}
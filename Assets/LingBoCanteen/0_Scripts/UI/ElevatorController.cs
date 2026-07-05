using UnityEngine;
using DG.Tweening;

public class ElevatorController : MonoBehaviour
{
    public RectTransform container; 

    public float floorHeight = 450f;

    public float moveTime = 0.8f;
    public float shakeIntensity = 10f;

    private int currentFloor = 1; 
    private bool isMoving = false;

    void Start()
    {
        if (container == null)
        {
            return;
        }

        container.anchoredPosition = Vector2.zero;
    }

    public void GoDown()
    {
        if (isMoving || currentFloor >= 2)
        {
            return;
        }
        currentFloor++;
        MoveToFloor(currentFloor);
    }

    public void GoUp()
    {
        if (isMoving || currentFloor <= 0)
        {
            return;
        }
        currentFloor--;
        MoveToFloor(currentFloor);
    }

    void MoveToFloor(int floor)
    {
        if (container == null) return;

        isMoving = true;
        float targetY = 0;

        if (floor == 2) targetY = floorHeight;
        else if (floor == 0) targetY = -floorHeight;

        container.DOAnchorPosY(targetY, moveTime)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                container.DOShakeAnchorPos(0.3f, new Vector2(0, shakeIntensity), 20, 90)
                         .OnComplete(() => isMoving = false);
            });
    }
}
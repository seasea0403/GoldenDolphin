using UnityEngine;
using DG.Tweening;

public class ElevatorController : MonoBehaviour
{
    public RectTransform container;

    public float floorHeight = 450f;

    public float moveTime = 0.8f;
    public float shakeIntensity = 12f;

    public GameObject humanButtons;

    //当前楼层索引（0=天堂, 1=人间, 2=地狱）
    public static int CurrentFloorIndex { get; private set; } = 1;

    //当前楼层名称（"天堂"/"人间"/"地狱"）
    public static string CurrentFloorName { get; private set; } = "人间";

    // 事件：到达新楼层时自动广播（参数是楼层名称）
    public static event System.Action<string> OnFloorChanged;


    private int currentFloor = 1;        // 0=天堂, 1=人间, 2=地狱
    private bool isMoving = false;
    private string currentFloorName = "人间";

    void Start()
    {
        if (container == null)
        {
            return;
        }

        container.anchoredPosition = Vector2.zero;
        UpdateButtonsVisibility();

        CurrentFloorIndex = currentFloor;
        CurrentFloorName = currentFloorName;
    }

    public void GoDown()
    {
        if (isMoving || currentFloor >= 2) return;

        currentFloor++;
        UpdateFloorName();

        HideHumanButtons();
        MoveToFloor(currentFloor);
    }

    public void GoUp()
    {
        if (isMoving || currentFloor <= 0) return;

        currentFloor--;
        UpdateFloorName();

        HideHumanButtons();
        MoveToFloor(currentFloor);
    }

    void UpdateFloorName()
    {
        if (currentFloor == 0) currentFloorName = "天堂";
        else if (currentFloor == 1) currentFloorName = "人间";
        else if (currentFloor == 2) currentFloorName = "地狱";

        CurrentFloorIndex = currentFloor;
        CurrentFloorName = currentFloorName;
    }

    void HideHumanButtons()
    {
        if (humanButtons != null)
            humanButtons.SetActive(false);
    }

    void UpdateButtonsVisibility()
    {
        if (humanButtons != null)
            humanButtons.SetActive(currentFloor == 1);
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
                .OnComplete(() =>
                {
                    isMoving = false;
                    Debug.Log("Arrival：" + currentFloorName);

                    UpdateButtonsVisibility();

                    //楼层到达后广播事件
                    OnFloorChanged?.Invoke(currentFloorName);
                });
            });
    }
}
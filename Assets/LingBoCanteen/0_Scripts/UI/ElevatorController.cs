using UnityEngine;
using DG.Tweening;

public class ElevatorController : MonoBehaviour
{
    public RectTransform container;

    public float floorHeight = 450f;

    public float moveTime = 0.8f;        
    public float shakeIntensity = 12f;  

    public GameObject humanButtons;      

    private int currentFloor = 1;        // 0=天堂, 1=人间, 2=地狱
    private bool isMoving = false;       
    private string currentFloorName = "人间"; 

    void Start()
    {
        // 安全检查
        if (container == null)
        {
            return;
        }

        container.anchoredPosition = Vector2.zero;

        UpdateButtonsVisibility();

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

                    // 如果回到了人间，重新显示人间的按钮组
                    UpdateButtonsVisibility();
                });
            });
    }
}
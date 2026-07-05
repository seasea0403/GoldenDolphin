using UnityEngine;
using DG.Tweening;

public class ElevatorController : MonoBehaviour
{
    public Transform elevatorContainer; // 把 ElevatorContainer 拖进来
    public float moveTime = 0.8f;

    private float floorHeight = 10.8f; // 默认高度，Start里会自动获取准确的
    private int currentFloor = 1;      // 0=地狱, 1=人间, 2=天堂
    private bool isMoving = false;

    void Start()
    {
        // 自动获取子物体（人间）的世界高度，确保适配不同分辨率的图片
        SpriteRenderer sr = elevatorContainer.GetChild(0).GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            floorHeight = sr.bounds.size.y;
        }

        // 初始化：停在人间
        elevatorContainer.position = new Vector3(0, 0, 0);
    }

    // 上升按钮调用
    public void GoUp()
    {
        if (isMoving || currentFloor >= 2) return;
        currentFloor++;
        MoveElevator();
    }

    // 下降按钮调用
    public void GoDown()
    {
        if (isMoving || currentFloor <= 0) return;
        currentFloor--;
        MoveElevator();
    }

    void MoveElevator()
    {
        isMoving = true;
        // 目标Y：人间是0，每上一层，容器要往下走（负值）
        float targetY = -currentFloor * floorHeight;

        // 1. 平滑移动电梯舱（世界坐标位移）
        elevatorContainer.DOMoveY(targetY, moveTime)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                // 2. 到达后：电梯刹车的晃动顿挫感（上下微震）
                // 这里震的是 ElevatorContainer，模拟电梯落地
                elevatorContainer.DOShakePosition(0.3f, new Vector3(0, 0.15f, 0), 20, 90, false, true)
                    .OnComplete(() => isMoving = false);
            });
    }
}
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityGameFramework.Runtime;
using LingBoCanteen;

public class ElevatorController : MonoBehaviour
{
    [Header("电梯UI容器")]
    public RectTransform container;

    [Header("背景过渡层（用于模拟电梯上下滑动进入背景的效果）")]
    public RectTransform backgroundTransition;

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
        // 电梯楼层与 DataNode 里存档的 Region 保持一致（比如读档、或场景重进时）
        SyncFloorFromDataNode();

        if (container != null)
        {
            container.anchoredPosition = GetAnchoredPositionForFloor(currentFloor);
        }

        // 背景过渡层初始位置设为0（与容器重合）
        if (backgroundTransition != null)
        {
            backgroundTransition.localPosition = Vector3.zero;
        }

        UpdateButtonsVisibility();
    }

    /// <summary>
    /// 按当前 DataNode 里的 Area.CurrentType 同步电梯楼层索引/名称（不触发移动动画，也不改写任何数据）。
    /// </summary>
    private void SyncFloorFromDataNode()
    {
        GameRegion region = GameRegion.Mortal;
        if (LingBoCanteen.GameEntry.DataNode != null && LingBoCanteen.GameEntry.DataNode.GetNode("Area.CurrentType") != null)
        {
            region = (GameRegion)LingBoCanteen.GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value;
        }

        currentFloor = region switch
        {
            GameRegion.Heaven => 0,
            GameRegion.Hell => 2,
            _ => 1,
        };

        UpdateFloorName();
    }

    private Vector2 GetAnchoredPositionForFloor(int floor)
    {
        if (floor == 2) return new Vector2(0f, floorHeight);
        if (floor == 0) return new Vector2(0f, -floorHeight);
        return Vector2.zero;
    }

    /// <summary>
    /// 电梯是否允许被使用：必须处于人间，且尚未使用过电梯（一次性单向传送）。
    /// </summary>
    private bool CanUseElevator()
    {
        if (LingBoCanteen.GameEntry.DataNode == null)
        {
            return true;
        }

        bool usedOnce = LingBoCanteen.GameEntry.DataNode.GetNode("Area.ElevatorUsedOnce") != null
            && LingBoCanteen.GameEntry.DataNode.GetData<VarBoolean>("Area.ElevatorUsedOnce").Value;

        GameRegion region = LingBoCanteen.GameEntry.DataNode.GetNode("Area.CurrentType") != null
            ? (GameRegion)LingBoCanteen.GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value
            : GameRegion.Mortal;

        return !usedOnce && region == GameRegion.Mortal;
    }

    public void GoDown()
    {
        if (isMoving || currentFloor != 1 || !CanUseElevator()) return;

        currentFloor++;
        UpdateFloorName();
        ApplyRegionChange(GameRegion.Hell, Constant.GameConstant.HELL_INIT_SAN);

        HideHumanButtons();
        MoveToFloor(currentFloor);
    }

    public void GoUp()
    {
        if (isMoving || currentFloor != 1 || !CanUseElevator()) return;

        currentFloor--;
        UpdateFloorName();
        ApplyRegionChange(GameRegion.Heaven, Constant.GameConstant.HEAVEN_INIT_SAN);

        HideHumanButtons();
        MoveToFloor(currentFloor);
    }

    /// <summary>
    /// 写入 DataNode：区域切换为目标区域、San强制刷新为目标值、标记电梯已使用一次。
    /// 同时刷新场景的 Region 装饰（遮罩颜色/背景图），HUD 会在下一帧的 Update 中自动刷新数值。
    /// </summary>
    private void ApplyRegionChange(GameRegion targetRegion, int forcedSan)
    {
        if (LingBoCanteen.GameEntry.DataNode == null)
        {
            return;
        }

        int previousSan = LingBoCanteen.GameEntry.DataNode.GetData<VarInt32>("Player.San").Value;
        
        LingBoCanteen.GameEntry.DataNode.SetData("Area.CurrentType", (VarInt32)(int)targetRegion);
        LingBoCanteen.GameEntry.DataNode.SetData("Player.San", (VarInt32)forcedSan);
        LingBoCanteen.GameEntry.DataNode.SetData("Area.ElevatorUsedOnce", (VarBoolean)true);

        // ★【新增】检查当前日期，如果 <= 15，则标记HasMovedBeforeDay15
        int currentDay = LingBoCanteen.GameEntry.DataNode.GetNode("DayCurrent.Value") != null
            ? LingBoCanteen.GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value").Value
            : 1;
        if (currentDay <= 15)
        {
            LingBoCanteen.GameEntry.DataNode.SetData("Area.HasMovedBeforeDay15", (VarBoolean)true);
            Debug.Log($"[Elevator] 在第{currentDay}天使用电梯，HasMovedBeforeDay15已标记为true");
        }

        // 诊断日志：记录电梯使用时的SAN变化
        Debug.Log($"[Elevator] 区域切换到: {targetRegion} | 原SAN: {previousSan} | 新SAN: {forcedSan} | 电梯已标记为使用过");

        // 播放区域切换音效
        SoundManager.Instance.PlayAreaSwitchSound();

        // 触发BGM切换（根据新的Region自动播放对应的BGM）
        SoundManager.Instance?.PlayMusicForCurrentGameState();

        if (AreaSwitchManager.Instance != null)
        {
            AreaSwitchManager.Instance.ApplyRegionDecorations();
        }
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
            humanButtons.SetActive(currentFloor == 1 && CanUseElevator());
    }

    void MoveToFloor(int floor)
    {
        if (container == null) return;

        isMoving = true;
        Vector3 targetPos = Vector3.zero;
        if (floor == 2) targetPos = new Vector3(0, floorHeight, 0);
        else if (floor == 0) targetPos = new Vector3(0, -floorHeight, 0);

        Sequence seq = DOTween.Sequence();

        // 1. 平滑移动到目标楼层
        seq.Append(container.DOLocalMove(targetPos, moveTime).SetEase(Ease.InOutQuad));

        // 2. 背景同步
        if (backgroundTransition != null)
        {
            backgroundTransition.localPosition = -targetPos;
            seq.Insert(0, backgroundTransition.DOLocalMove(Vector3.zero, moveTime).SetEase(Ease.InOutQuad));
        }

        // 3. 电梯效果
        seq.Append(container.DOLocalMoveY(targetPos.y - shakeIntensity * 1.2f, 0.04f).SetEase(Ease.OutQuad));
        seq.Append(container.DOLocalMoveY(targetPos.y + shakeIntensity * 0.5f, 0.05f).SetEase(Ease.InOutQuad));
        seq.Append(container.DOLocalMoveY(targetPos.y, 0.08f).SetEase(Ease.InOutQuad));

        seq.OnComplete(() =>
        {
            isMoving = false;
            Debug.Log("Arrival：" + currentFloorName);
            UpdateButtonsVisibility();
            OnFloorChanged?.Invoke(currentFloorName);
        });
    }
}
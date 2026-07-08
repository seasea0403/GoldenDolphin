using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityGameFramework.Runtime;
using LingBoCanteen;
using System.Collections;

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
    
    // 事件：自动跳转完成时广播
    public static event System.Action OnTransitionComplete;


    private int currentFloor = 1;        // 0=天堂, 1=人间, 2=地狱
    private bool isMoving = false;
    private string currentFloorName = "人间";

    void Start()
    {
        // ★【修复】延迟检查container，避免Start()执行时还未初始化
        // container应该通过Inspector绑定，如果为null说明Inspector配置有问题
        if (container == null)
        {
            Debug.LogWarning("[ElevatorController] Start时container为null，将在第一次使用时重新查找");
            // 尝试自动查找
            container = GetComponent<RectTransform>();
            if (container == null)
            {
                container = transform.Find("Container") as RectTransform;
            }
        }
        
        if (container != null)
        {
            Debug.Log($"[ElevatorController] Start: container已绑定，当前位置={container.anchoredPosition}");
        }

        // 电梯楼层与 DataNode 里存档的 Region 保持一致（比如读档、或场景重进时）
        SyncFloorFromDataNode();

        if (container != null)
        {
            Vector2 initialPos = GetAnchoredPositionForFloor(currentFloor);
            container.anchoredPosition = initialPos;
            Debug.Log($"[ElevatorController] Start: 根据楼层{currentFloor}设置container初始位置={initialPos}");
        }

        // 背景过渡层初始位置设为0（与容器重合）
        if (backgroundTransition != null)
        {
            backgroundTransition.localPosition = Vector3.zero;
        }

        UpdateButtonsVisibility();
    }

    void Update()
    {
        // ★【修复】移除对humanButtons.activeSelf的检查，确保始终监控SAN值
        // 即使按钮被隐藏，仍需要检测SAN值并进行自动切换
        // 只在电梯正在运动时停止检测，其他时间都要实时检测
        if (isMoving)
        {
            return;
        }

        // 获取当前SAN值
        if (LingBoCanteen.GameEntry.DataNode == null)
        {
            return;
        }

        VarInt32 sanVar = LingBoCanteen.GameEntry.DataNode.GetData<VarInt32>("Player.San");
        if (sanVar == null)
        {
            return;
        }

        int currentSan = sanVar.Value;
        GameRegion targetRegion = GameRegion.Mortal;
        int targetSan = 50;  // 人间的默认SAN值
        bool needsSwitch = false;

        // 根据当前区域和SAN值判断是否需要切换
        if (currentFloor == 0)  // 天堂
        {
            // 天堂：SAN >= 57 时留在天堂，否则切换回人间
            if (currentSan < 43)
            {
                Debug.Log($"[ElevatorController] 自动切换：SAN({currentSan})已低于43，需要从天堂切换回人间");
                targetRegion = GameRegion.Mortal;
                targetSan = 50;
                needsSwitch = true;
            }
        }
        else if (currentFloor == 2)  // 地狱
        {
            // 地狱：SAN <= 43 时留在地狱，否则切换回人间
            if (currentSan >= 57)
            {
                Debug.Log($"[ElevatorController] 自动切换：SAN({currentSan})已高于57，需要从地狱切换回人间");
                targetRegion = GameRegion.Mortal;
                targetSan = 50;
                needsSwitch = true;
            }
        }
        else  // 人间
        {
            // 人间：SAN >= 57 时切换天堂，SAN <= 43 时切换地狱
            if (currentSan >= 57)
            {
                Debug.Log($"[ElevatorController] 自动切换：SAN({currentSan})已高于等于57，需要从人间自动切换到天堂");
                targetRegion = GameRegion.Heaven;
                targetSan = Constant.GameConstant.HEAVEN_INIT_SAN;
                needsSwitch = true;
            }
            else if (currentSan <= 43)
            {
                Debug.Log($"[ElevatorController] 自动切换：SAN({currentSan})已低于等于43，需要从人间自动切换到地狱");
                targetRegion = GameRegion.Hell;
                targetSan = Constant.GameConstant.HELL_INIT_SAN;
                needsSwitch = true;
            }
        }

        // 如果需要切换，触发自动切换并更新SAN值
        if (needsSwitch)
        {
            StartCoroutine(DoAutoTransitionWithSanUpdate(targetRegion, targetSan, null));
        }
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

        Debug.Log($"[ElevatorController] SyncFloorFromDataNode: 从DataNode读取区域={region}，映射到楼层={currentFloor}");
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
        if (isMoving) return;

        // ★【修复】允许点击按钮直接切换到地狱，SAN会被强制重置为HELL_INIT_SAN
        // 不再检查SAN范围，只检查是否能使用电梯（一次性、人间才能用）
        if (currentFloor != 1 || !CanUseElevator()) return;

        Debug.Log($"[Elevator] GoDown: 允许切换到地狱");
        currentFloor++;
        UpdateFloorName();
        ApplyRegionChange(GameRegion.Hell, Constant.GameConstant.HELL_INIT_SAN);
        HideHumanButtons();
        MoveToFloor(currentFloor);
    }

    public void GoUp()
    {
        if (isMoving) return;

        // ★【修复】允许点击按钮直接切换到天堂，SAN会被强制重置为HEAVEN_INIT_SAN
        // 不再检查SAN范围，只检查是否能使用电梯（一次性、人间才能用）
        if (currentFloor != 1 || !CanUseElevator()) return;

        Debug.Log($"[Elevator] GoUp: 允许切换到天堂");
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
        // ★【修复】检查container引用，确保其存在且有效
        if (container == null)
        {
            Debug.LogError("[ElevatorController] MoveToFloor: container为null！无法播放电梯动画");
            isMoving = false;
            return;
        }

        isMoving = true;
        Vector3 targetPos = Vector3.zero;
        if (floor == 2) targetPos = new Vector3(0, floorHeight, 0);
        else if (floor == 0) targetPos = new Vector3(0, -floorHeight, 0);

        Debug.Log($"[ElevatorController] MoveToFloor({floor}): 开始动画，container当前位置={container.anchoredPosition}，目标位置={targetPos}");

        Sequence seq = DOTween.Sequence();

        // 1. 平滑移动到目标楼层
        seq.Append(container.DOLocalMove(targetPos, moveTime).SetEase(Ease.InOutQuad));

        // 2. 背景同步
        if (backgroundTransition != null)
        {
            backgroundTransition.localPosition = -targetPos;
            seq.Insert(0, backgroundTransition.DOLocalMove(Vector3.zero, moveTime).SetEase(Ease.InOutQuad));
        }

        // 3. 电梯到达时的震动效果
        seq.Append(container.DOLocalMoveY(targetPos.y - shakeIntensity * 1.2f, 0.04f).SetEase(Ease.OutQuad));
        seq.Append(container.DOLocalMoveY(targetPos.y + shakeIntensity * 0.5f, 0.05f).SetEase(Ease.InOutQuad));
        seq.Append(container.DOLocalMoveY(targetPos.y, 0.08f).SetEase(Ease.InOutQuad));

        seq.OnComplete(() =>
        {
            isMoving = false;
            Debug.Log($"[ElevatorController] MoveToFloor({floor})动画完成，Arrival：{currentFloorName}");
            UpdateButtonsVisibility();
            OnFloorChanged?.Invoke(currentFloorName);
        });
    }

    /// <summary>
    /// 自动根据目标区域播放电梯跳转动画（支持多层跳转，如天堂→人间→地狱）。
    /// 由SettlePanel在区域需要改变时调用，跳转完成后触发OnTransitionComplete事件，并执行onComplete回调。
    /// 该协程运行在ElevatorController自身（始终常驻激活），不受SettlePanel隐藏面板时SetActive(false)的影响。
    /// </summary>
    public void AutoTransitionToRegion(LingBoCanteen.GameRegion targetRegion, System.Action onComplete = null)
    {
        StartCoroutine(DoAutoTransition(targetRegion, onComplete));
    }

    /// <summary>
    /// ★【新增】自动切换带SAN值更新（用于Update中的自动检测切换）。
    /// </summary>
    private IEnumerator DoAutoTransitionWithSanUpdate(LingBoCanteen.GameRegion targetRegion, int targetSan, System.Action onComplete)
    {
        // 先播放动画切换到目标区域
        yield return StartCoroutine(DoAutoTransition(targetRegion, null));
        
        // 动画完成后，更新SAN值
        if (LingBoCanteen.GameEntry.DataNode != null)
        {
            LingBoCanteen.GameEntry.DataNode.SetData("Player.San", (VarInt32)targetSan);
            Debug.Log($"[ElevatorController] DoAutoTransitionWithSanUpdate: 更新SAN = {targetSan}");
        }
        
        // 执行完成回调
        onComplete?.Invoke();
    }

    private IEnumerator DoAutoTransition(LingBoCanteen.GameRegion targetRegion, System.Action onComplete)
    {
        // ★【修复】检查container引用，确保AutoTransition能正确执行
        if (container == null)
        {
            Debug.LogError("[ElevatorController] DoAutoTransition: container为null，无法播放电梯动画！");
            onComplete?.Invoke();
            yield break;
        }

        int targetFloor = targetRegion switch
        {
            LingBoCanteen.GameRegion.Heaven => 0,
            LingBoCanteen.GameRegion.Hell => 2,
            _ => 1,
        };

        int startFloor = currentFloor;
        Debug.Log($"[ElevatorController] AutoTransition开始：从第{startFloor}层({currentFloorName}) → 第{targetFloor}层，container={container}");

        // 如果需要经过多个楼层，逐层跳转
        if (startFloor < targetFloor)
        {
            // 向下跳转：逐层经过
            for (int floor = startFloor + 1; floor <= targetFloor; floor++)
            {
                currentFloor = floor;
                UpdateFloorName();
                Debug.Log($"[ElevatorController] 正在跳转到第{floor}层");
                MoveToFloor(floor);
                yield return new WaitUntil(() => !isMoving); // 等待当前跳转完成
                yield return new WaitForSeconds(0.3f); // 每层间隔0.3秒
            }
        }
        else if (startFloor > targetFloor)
        {
            // 向上跳转：逐层经过
            for (int floor = startFloor - 1; floor >= targetFloor; floor--)
            {
                currentFloor = floor;
                UpdateFloorName();
                Debug.Log($"[ElevatorController] 正在跳转到第{floor}层");
                MoveToFloor(floor);
                yield return new WaitUntil(() => !isMoving); // 等待当前跳转完成
                yield return new WaitForSeconds(0.3f); // 每层间隔0.3秒
            }
        }

        // 跳转完成，更新DataNode
        LingBoCanteen.EveningDayFlow.ApplyRegionChange(targetRegion);
        Debug.Log($"[ElevatorController] DoAutoTransition已更新DataNode");

        // 刷新装饰（背景图等）
        if (LingBoCanteen.AreaSwitchManager.Instance != null)
        {
            LingBoCanteen.AreaSwitchManager.Instance.ApplyRegionDecorations();
        }

        // 播放区域切换音效
        SoundManager.Instance?.PlayAreaSwitchSound();

        // 触发BGM切换
        SoundManager.Instance?.PlayMusicForCurrentGameState();

        Debug.Log($"[ElevatorController] AutoTransition完成：已到达第{currentFloor}层({currentFloorName})");

        // 触发完成事件
        OnTransitionComplete?.Invoke();

        // 执行传入的完成回调（例如推进到下一天）
        onComplete?.Invoke();
    }
}
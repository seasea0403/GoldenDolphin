using System.Collections.Generic;
using GameFramework;
using GameFramework.DataTable;
using LingBoCanteen.Definition.Enum;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 点单区槽位管理器：固定 3 个槽位，负责顾客生成、离场后的 10s 冷却，
    /// 以及"多个顾客同时需要同一道菜时优先满足耐心值最低者"的全局上菜结算。
    /// 挂在 Main 场景中一个常驻节点上，Inspector 里按顺序拖入 3 个槽位点和 1 个出生点。
    /// </summary>
    public class CustomerSlotManager : MonoBehaviour
    {
        private enum SlotState
        {
            Empty,
            Cooldown,
            Occupied,
        }

        [Header("3 个站位点")]
        [SerializeField] private Transform[] m_SlotPoints;

        [Header("顾客出生点")]
        [SerializeField] private Transform m_SpawnPoint;

        [Header("当前所在区域")]
        [SerializeField] private GameRegion m_CurrentRegion = GameRegion.Mortal;

        [Header("3 个槽位的需求气泡")]
        [SerializeField] private CustomerBubbleView[] m_Bubbles;

        public static CustomerSlotManager Instance { get; private set; }

        private SlotState[] m_SlotStates;
        private float[] m_SlotCooldownTimer;
        private CustomerEntity[] m_SlotOccupants;
        private int m_NextEntityId = 1;

        private IDishUnlockService m_DishService;

        private int m_TotalCustomerToday = 0;
        private int m_CustomersSpawnedToday = 0;
        private int m_CustomersLeftToday = 0;
        private string[] m_PendingPortraitNames;
        private string m_LastGeneratedPortraitName = null;  // 记录上一次生成的顾客立绘名，用于去重

        // ★ 【新增】剧情系统相关
        private bool m_CanSpawnCustomers = false;  // 是否允许生成顾客

        private void Awake()
        {
            Instance = this;
            m_DishService = new DishUnlockService();

            int slotCount = m_SlotPoints.Length;
            m_SlotStates = new SlotState[slotCount];
            m_SlotCooldownTimer = new float[slotCount];
            m_SlotOccupants = new CustomerEntity[slotCount];
            m_PendingPortraitNames = new string[slotCount];
        }

        private void Start()
        {
            // 读取最新的所处区域与当天顾客总数
            int currentDay = GetCurrentDaySafely();
            if (GameEntry.DataNode.GetNode("Area.CurrentType") != null)
            {
                m_CurrentRegion = (GameRegion)GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value;
            }

            DRDay dayRow = GameEntry.DataTable.GetDataTable<DRDay>().GetDataRow(currentDay);
            m_TotalCustomerToday = dayRow != null ? dayRow.CustomerCount : 5;

            // 同步到 DataNode 中以供 HUD 实时读取和显示
            GameEntry.DataNode.SetData("Business.TodayTotalGuestCount", (VarInt32)m_TotalCustomerToday);
            GameEntry.DataNode.SetData("Business.TodayServeCustomerCount", (VarInt32)0);

            m_CustomersSpawnedToday = 0;
            m_CustomersLeftToday = 0;

            // ★ 【新增】先禁止客人生成，等待剧情系统允许
            m_CanSpawnCustomers = false;

            // 如果PlotTriggerManager存在且正在播放剧情，订阅其完成事件
            if (PlotTriggerManager.Instance != null)
            {
                if (PlotTriggerManager.Instance.IsPlayingPlot())
                {
                    PlotTriggerManager.Instance.OnPlotDialogueComplete += EnableCustomerSpawning;
                }
                else
                {
                    // 没有剧情，直接允许客人生成
                    m_CanSpawnCustomers = true;
                }
            }
            else
            {
                // 没有PlotTriggerManager，直接允许客人生成
                m_CanSpawnCustomers = true;
            }

            // 初始顾客数量在 [2, 3]，但不能超过本日顾客总数
            int initialCount = UnityEngine.Random.Range(2, 4); // 返回 2 或 3
            initialCount = Mathf.Min(initialCount, m_TotalCustomerToday);

            for (int i = 0; i < m_SlotStates.Length; i++)
            {
                if (i < initialCount)
                {
                    // 初始顾客延迟出现的间隔在 [0, 10]
                    m_SlotStates[i] = SlotState.Cooldown;
                    m_SlotCooldownTimer[i] = UnityEngine.Random.Range(0f, 10f);
                    // 预先选择立绘，避免与上一个重复
                    m_PendingPortraitNames[i] = ChooseGuestAssetForSlot(i);
                    m_LastGeneratedPortraitName = m_PendingPortraitNames[i];
                    m_CustomersSpawnedToday++;
                }
                else
                {
                    m_SlotStates[i] = SlotState.Empty;
                    m_PendingPortraitNames[i] = null;
                }
            }

            // 确保至少一个顾客立刻出现（timer=0）以满足 "一进入场景至少一个顾客出现" 的体验
            if (initialCount > 0)
            {
                int firstIndex = 0;
                m_SlotCooldownTimer[firstIndex] = 0f;
            }
        }

        /// <summary>
        /// 剧情播放完成后的回调，允许客人生成
        /// </summary>
        private void EnableCustomerSpawning()
        {
            m_CanSpawnCustomers = true;
            Log.Info("✅ 剧情完成，客人开始生成");

            // 取消订阅事件
            if (PlotTriggerManager.Instance != null)
            {
                PlotTriggerManager.Instance.OnPlotDialogueComplete -= EnableCustomerSpawning;
            }
        }

        private void Update()
        {
            // ★ 【新增】如果还未允许生成客人（剧情未完成），则不执行任何逻辑
            if (!m_CanSpawnCustomers)
            {
                return;
            }

            for (int i = 0; i < m_SlotStates.Length; i++)
            {
                if (m_SlotStates[i] == SlotState.Cooldown)
                {
                    m_SlotCooldownTimer[i] -= Time.deltaTime;
                    if (m_SlotCooldownTimer[i] <= 0f)
                    {
                        // 倒计时结束，立刻生成顾客并设为 Occupied
                        SpawnCustomer(i);
                    }
                }
                else if (m_SlotStates[i] == SlotState.Empty)
                {
                    // 空槽位：如果当天还有未生成的顾客数量，就预约生成，进入间隔 CD (5s ~ 10s)
                    if (m_CustomersSpawnedToday < m_TotalCustomerToday)
                    {
                        m_SlotStates[i] = SlotState.Cooldown;
                        m_SlotCooldownTimer[i] = UnityEngine.Random.Range(5f, 10f);
                        // 预先选择立绘，避免与上一个重复
                        m_PendingPortraitNames[i] = ChooseGuestAssetForSlot(i);
                        m_LastGeneratedPortraitName = m_PendingPortraitNames[i];
                        m_CustomersSpawnedToday++;
                    }
                }
            }
        }

        private void SpawnCustomer(int slotIndex)
        {
            // 先占用槽位，避免同一帧重复生成；若 ShowEntity 因资源缺失失败，槽位会卡住，
            // 后续接入真实 Customer 预制体后即可正常触发 OnShow。
            m_SlotStates[slotIndex] = SlotState.Occupied;

            int currentDay = GetCurrentDaySafely();
            List<int> unlockedDishIds = m_DishService.GetUnlockedDishIds(currentDay);

            int dishCount = Random.value < Constant.GameConstant.DUAL_ORDER_CHANCE ? 2 : 1;
            List<int> requiredDishes = WeightedDishSelector.PickDishes(unlockedDishIds, m_DishService.GetDishWeight, dishCount);

            List<Sprite> icons = new List<Sprite>(requiredDishes.Count);
            foreach (int dishId in requiredDishes)
            {
                icons.Add(m_DishService.GetDishIcon(dishId));
            }

            CustomerBuff buff = CustomerBuffUtility.PickBuff(m_CurrentRegion);
            float patience = Constant.GameConstant.DEFAULT_GUEST_WAIT_TIME + CustomerBuffUtility.GetPatienceModifier(buff);

            // 取已预选立绘（由 Start/Update 在进入 Cooldown 阶段时提前选好），若为空则即时选取
            string selectedGuestAssetName = m_PendingPortraitNames != null && !string.IsNullOrEmpty(m_PendingPortraitNames[slotIndex]) ? m_PendingPortraitNames[slotIndex] : ChooseGuestAssetForSlot(slotIndex);
            // 清除预选记录
            if (m_PendingPortraitNames != null)
            {
                m_PendingPortraitNames[slotIndex] = null;
            }

            // ★ 【新增】第一个客人使用剧情人物的立绘
            if (m_CustomersSpawnedToday == 1 && PlotTriggerManager.Instance != null)
            {
                string plotCharacterId = PlotTriggerManager.Instance.GetTodayPlotCharacterId();
                if (!string.IsNullOrEmpty(plotCharacterId))
                {
                    // 从DRGuest表查找对应的立绘资源名
                    selectedGuestAssetName = GetGuestAssetNameByCharacterId(plotCharacterId);
                    Log.Info($"✅ 第一个客人使用剧情人物 {plotCharacterId} 的立绘: {selectedGuestAssetName}");
                }
            }

            // 兜底默认立绘
            if (string.IsNullOrEmpty(selectedGuestAssetName))
            {
                if (m_CurrentRegion == GameRegion.Heaven)
                {
                    selectedGuestAssetName = "AngelCust_01";
                }
                else if (m_CurrentRegion == GameRegion.Hell)
                {
                    selectedGuestAssetName = "SinCust_01";
                }
                else
                {
                    selectedGuestAssetName = "HumanCust_01";
                }
            }

            CustomerSpawnData spawnData = new CustomerSpawnData
            {
                SlotIndex = slotIndex,
                SpawnPosition = m_SpawnPoint.position,
                SlotPosition = m_SlotPoints[slotIndex].position,
                RequiredDishIds = requiredDishes,
                RequiredDishIcons = icons,
                Buff = buff,
                PatienceSeconds = patience,
                Region = m_CurrentRegion,
                AssetName = selectedGuestAssetName,
            };

            // 更新上一次生成的立绘名记录，用于下一次生成时进行去重
            m_LastGeneratedPortraitName = selectedGuestAssetName;

            int entityId = m_NextEntityId++;
            GameEntry.Entity.ShowEntity<CustomerEntity>(entityId, AssetUtility.GetEntityAsset("Customer"), "Customer", spawnData);

            // 播放顾客进店音效
            SoundManager.Instance.PlayCustomerEnterSound();
        }

        /// <summary>
        /// 根据剧情人物的立绘资源名直接使用
        /// </summary>
        private string GetGuestAssetNameByCharacterId(string characterId)
        {
            // characterId已经是立绘资源名，直接返回
            return characterId;
        }

        /// <summary>
        /// 安全读取 DataNode 里的 "DayCurrent.Value"。
        /// 如果该节点还不存在（例如没有从 Launch 流程正常进入，而是直接在 Game 场景按 Play 测试），
        /// 不会抛异常导致整个槽位卡死，而是回退到第 1 天并打印一条警告方便排查。
        /// </summary>
        private int GetCurrentDaySafely()
        {
            const string path = "DayCurrent.Value";
            if (GameEntry.DataNode.GetNode(path) == null)
            {
                Log.Warning("DataNode '{0}' 不存在，可能没有经过 ProcedureLaunch 完整初始化（比如直接在 Game 场景按 Play）。已回退为第 1 天。", path);
                return 1;
            }

            return GameEntry.DataNode.GetData<VarInt32>(path).Value;
        }

        /// <summary>
        /// 为指定槽位选择一个合适的顾客立绘资源名，尽量避免与左右相邻槽位（已生成或已预选）的重复。
        /// </summary>
        private string ChooseGuestAssetForSlot(int slotIndex)
        {
            string selected = null;
            IDataTable<DRGuest> guestTable = GameEntry.DataTable.GetDataTable<DRGuest>();
            if (guestTable == null)
            {
                // 与 GetCurrentDaySafely() 同理：Guest 配置表需要经过 ProcedureLaunch 加载，
                // 如果直接在 Main 场景按 Play（跳过 Launcher 启动流程），该表不会被加载，会导致立绘永远回退成区域默认的第一个立绘。
                Log.Warning("DataTable 'DRGuest' 尚未加载（可能没有经过 ProcedureLaunch 完整初始化，比如直接在 Game 场景按 Play）。立绘将回退为区域默认立绘。");
            }
            if (guestTable != null)
            {
                DRGuest[] allGuests = guestTable.GetAllDataRows();
                int targetDbRegion = 0;
                if (m_CurrentRegion == GameRegion.Heaven)
                {
                    targetDbRegion = 1;
                }
                else if (m_CurrentRegion == GameRegion.Hell)
                {
                    targetDbRegion = 2;
                }
                else
                {
                    targetDbRegion = 0;
                }

                List<DRGuest> eligibleGuests = new List<DRGuest>();
                foreach (DRGuest guestRow in allGuests)
                {
                    if (guestRow.GuestType != 1 && guestRow.Region == targetDbRegion)
                    {
                        eligibleGuests.Add(guestRow);
                    }
                }

                if (eligibleGuests.Count > 0)
                {
                    // 简化为：只避开上一次生成的立绘名（不检测左右邻居），实现相邻两个顾客立绘不一致
                    List<DRGuest> filtered = new List<DRGuest>();
                    foreach (var g in eligibleGuests)
                    {
                        // 如果这个立绘与上一次生成的不同，就纳入可选池
                        if (string.IsNullOrEmpty(m_LastGeneratedPortraitName) || g.AssetName != m_LastGeneratedPortraitName)
                        {
                            filtered.Add(g);
                        }
                    }

                    // 如果过滤后没有结果（全部都跟上一个相同），直接从全部中随机选（兜底）
                    List<DRGuest> pool = filtered.Count > 0 ? filtered : eligibleGuests;
                    DRGuest randomGuest = pool[UnityEngine.Random.Range(0, pool.Count)];
                    selected = randomGuest.AssetName;
                }
            }

            if (string.IsNullOrEmpty(selected))
            {
                if (m_CurrentRegion == GameRegion.Heaven)
                {
                    selected = "AngelCust_01";
                }
                else if (m_CurrentRegion == GameRegion.Hell)
                {
                    selected = "SinCust_01";
                }
                else
                {
                    selected = "HumanCust_01";
                }
            }

            Debug.Log($"[CustomerSlotManager] ChooseGuestAssetForSlot(slot={slotIndex}) => {selected} (lastGenerated={m_LastGeneratedPortraitName})");

            return selected;
        }

        /// <summary>
        /// 顾客实体在 OnShow 时调用，登记自己占用的槽位，并绑定对应槽位的需求气泡。
        /// </summary>
        public void RegisterOccupant(int slotIndex, CustomerEntity occupant)
        {
            m_SlotOccupants[slotIndex] = occupant;

            if (m_Bubbles != null && slotIndex < m_Bubbles.Length && m_Bubbles[slotIndex] != null)
            {
                m_Bubbles[slotIndex].Bind(occupant);
            }
        }

        /// <summary>
        /// 顾客离场动画结束后调用：解绑气泡、清空槽位占用并推进当日顾客离开计数。
        /// </summary>
        public void OnCustomerLeft(int slotIndex, bool success)
        {
            m_SlotOccupants[slotIndex] = null;
            m_SlotStates[slotIndex] = SlotState.Empty; // 改为空，Update 中会视剩余需生成总数重置为 Cooldown

            m_CustomersLeftToday++;

            // 播放顾客离开音效
            if (success)
            {
                SoundManager.Instance.PlayCustomerLeaveSound();
            }
            else
            {
                SoundManager.Instance.PlayCustomerAngrySound();
            }

            if (success)
            {
                int served = 0;
                if (GameEntry.DataNode.GetNode("Business.TodayServeCustomerCount") != null)
                {
                    served = GameEntry.DataNode.GetData<VarInt32>("Business.TodayServeCustomerCount").Value;
                }
                GameEntry.DataNode.SetData("Business.TodayServeCustomerCount", (VarInt32)(served + 1));
            }

            if (m_Bubbles != null && slotIndex < m_Bubbles.Length && m_Bubbles[slotIndex] != null)
            {
                m_Bubbles[slotIndex].Unbind();
            }

            if (m_CustomersLeftToday >= m_TotalCustomerToday)
            {
                Log.Info("本日所有顾客 ({0}位) 已全部处理完毕，标记本日营业结算。", m_TotalCustomerToday);
                GameEntry.DataNode.SetData("DayCurrent.IsDaySettled", (VarBoolean)true);
                GameEntry.DataNode.SetData("DayCurrent.Phase", (VarInt32)(int)TimeSection.Evening);

                // 触发BGM切换（从Day切换到Evening，应播放傍晚超市采购BGM）
                SoundManager.Instance?.PlayMusicForCurrentGameState();

                // 白天营业结束：关闭点单区/备菜区/烹调区的世界物体与 UI，切换显示傍晚(打烊结算)区域
                if (AreaSwitchManager.Instance != null)
                {
                    AreaSwitchManager.Instance.SwitchToEvening();
                }
            }
            
        }

        /// <summary>
        /// 上菜：把做好的菜品交给场上需要它的顾客，成功后按 Dish 表 EarnMoney 与
        /// <see cref="Constant.GameConstant.ORDER_SUCCESS_BASE_SAN"/> 结算金币与 San 值。
        /// 如果多个顾客同时需要该菜品，优先满足剩余耐心值最低（最快超时）的那一位。
        /// </summary>
        /// <returns>是否有顾客成功收下这道菜。</returns>
        public bool ServeDish(int dishId)
        {
            CustomerEntity target = null;
            for (int i = 0; i < m_SlotOccupants.Length; i++)
            {
                CustomerEntity occupant = m_SlotOccupants[i];
                if (occupant == null || !occupant.NeedsDish(dishId))
                {
                    continue;
                }

                if (target == null || occupant.PatienceRemaining < target.PatienceRemaining)
                {
                    target = occupant;
                }
            }

            if (target == null || !target.TryFulfillDish(dishId))
            {
                return false;
            }

            SettleServeReward(dishId);
            return true;
        }

        /// <summary>
        /// 上菜成功后的金币/San 结算：金币加 Dish 表的 EarnMoney，San 加固定的成功值。
        /// 同时累加当日金币/San变化量（供傍晚"结束今日"结算面板展示）。
        /// </summary>
        private void SettleServeReward(int dishId)
        {
            DRDish dishRow = GameEntry.DataTable.GetDataTable<DRDish>().GetDataRow(dishId);
            int earnMoney = dishRow != null ? dishRow.EarnMoney : 0;

            int gold = GameEntry.DataNode.GetData<VarInt32>("Player.Gold");
            GameEntry.DataNode.SetData("Player.Gold", (VarInt32)(gold + earnMoney));

            // 播放金币获得音效
            SoundManager.Instance.PlayGoldGetSound();
            int sanDelta = Constant.GameConstant.ORDER_SUCCESS_BASE_SAN;
            int san = GameEntry.DataNode.GetData<VarInt32>("Player.San");
            int newSan = san + sanDelta;
            GameEntry.DataNode.SetData("Player.San", (VarInt32)newSan);

            // 诊断日志：记录SAN变化
            Debug.Log($"[SAN Update] 完成订单: {dishId} | 原SAN: {san} | 变化值: {sanDelta} | 新SAN: {newSan}");

            // 播放SAN变化音效
            if (sanDelta > 0)
            {
                SoundManager.Instance.PlaySanUpSound();
            }
            else if (sanDelta < 0)
            {
                SoundManager.Instance.PlaySanDownSound();
            }

            AccumulateTodayDelta(earnMoney, sanDelta);
        }
        

        /// <summary>
        /// 累加当日金币/San变化量到 "Business.TodayEarnGold" / "Business.TodaySanDelta"，
        /// 供傍晚"结束今日"结算面板（<see cref="SettlePanel"/>）读取展示，新的一天开始时会被重置为 0。
        /// </summary>
        private void AccumulateTodayDelta(int goldDelta, int sanDelta)
        {
            int todayGold = GameEntry.DataNode.GetNode("Business.TodayEarnGold") != null
                ? GameEntry.DataNode.GetData<VarInt32>("Business.TodayEarnGold").Value
                : 0;
            GameEntry.DataNode.SetData("Business.TodayEarnGold", (VarInt32)(todayGold + goldDelta));

            int todaySan = GameEntry.DataNode.GetNode("Business.TodaySanDelta") != null
                ? GameEntry.DataNode.GetData<VarInt32>("Business.TodaySanDelta").Value
                : 0;
            GameEntry.DataNode.SetData("Business.TodaySanDelta", (VarInt32)(todaySan + sanDelta));
        }

        /// <summary>
        /// 重新初始化当天顾客（用于进入下一天时重置）。
        /// </summary>
        public void ReinitializeDaily()
        {
            // 清除所有现有顾客
            for (int i = 0; i < m_SlotOccupants.Length; i++)
            {
                if (m_SlotOccupants[i] != null)
                {
                    // 如果需要，可以在这里销毁顾客实体，目前假设由 AreaSwitchManager 处理
                    m_SlotOccupants[i] = null;
                }
                m_SlotStates[i] = SlotState.Empty;
                m_SlotCooldownTimer[i] = 0f;
                m_PendingPortraitNames[i] = null;
            }

            // 重新执行 Start() 中的初始化逻辑
            int currentDay = GetCurrentDaySafely();
            if (GameEntry.DataNode.GetNode("Area.CurrentType") != null)
            {
                m_CurrentRegion = (GameRegion)GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value;
            }

            DRDay dayRow = GameEntry.DataTable.GetDataTable<DRDay>().GetDataRow(currentDay);
            m_TotalCustomerToday = dayRow != null ? dayRow.CustomerCount : 5;

            // 同步到 DataNode 中以供 HUD 实时读取和显示
            GameEntry.DataNode.SetData("Business.TodayTotalGuestCount", (VarInt32)m_TotalCustomerToday);
            GameEntry.DataNode.SetData("Business.TodayServeCustomerCount", (VarInt32)0);

            m_CustomersSpawnedToday = 0;
            m_CustomersLeftToday = 0;

            // 初始顾客数量在 [2, 3]，但不能超过本日顾客总数
            int initialCount = UnityEngine.Random.Range(2, 4); // 返回 2 或 3
            initialCount = Mathf.Min(initialCount, m_TotalCustomerToday);

            for (int i = 0; i < m_SlotStates.Length; i++)
            {
                if (i < initialCount)
                {
                    // 初始顾客延迟出现的间隔在 [0, 10]
                    m_SlotStates[i] = SlotState.Cooldown;
                    m_SlotCooldownTimer[i] = UnityEngine.Random.Range(0f, 10f);
                    // 预先选择立绘，避免与上一个重复
                    m_PendingPortraitNames[i] = ChooseGuestAssetForSlot(i);
                    m_LastGeneratedPortraitName = m_PendingPortraitNames[i];
                    m_CustomersSpawnedToday++;
                }
                else
                {
                    m_SlotStates[i] = SlotState.Empty;
                    m_PendingPortraitNames[i] = null;
                }
            }

            // 确保至少一个顾客立刻出现（timer=0）以满足 "一进入场景至少一个顾客出现" 的体验
            if (initialCount > 0)
            {
                int firstIndex = 0;
                m_SlotCooldownTimer[firstIndex] = 0f;
            }
        }
    }
}

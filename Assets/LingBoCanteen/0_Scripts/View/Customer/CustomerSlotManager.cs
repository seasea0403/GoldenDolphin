using System.Collections.Generic;
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

        private void Awake()
        {
            Instance = this;
            m_DishService = new DishUnlockService();

            int slotCount = m_SlotPoints.Length;
            m_SlotStates = new SlotState[slotCount];
            m_SlotCooldownTimer = new float[slotCount];
            m_SlotOccupants = new CustomerEntity[slotCount];
        }

        private void Update()
        {
            for (int i = 0; i < m_SlotStates.Length; i++)
            {
                if (m_SlotStates[i] == SlotState.Cooldown)
                {
                    m_SlotCooldownTimer[i] -= Time.deltaTime;
                    if (m_SlotCooldownTimer[i] <= 0f)
                    {
                        m_SlotStates[i] = SlotState.Empty;
                    }
                }

                if (m_SlotStates[i] == SlotState.Empty)
                {
                    SpawnCustomer(i);
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
            };

            int entityId = m_NextEntityId++;
            GameEntry.Entity.ShowEntity<CustomerEntity>(entityId, AssetUtility.GetEntityAsset("Customer"), "Customer", spawnData);
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
        /// 顾客离场动画结束后调用：解绑气泡、清空槽位占用并进入 10s 冷却。
        /// </summary>
        public void OnCustomerLeft(int slotIndex, bool success)
        {
            m_SlotOccupants[slotIndex] = null;
            m_SlotStates[slotIndex] = SlotState.Cooldown;
            m_SlotCooldownTimer[slotIndex] = Constant.GameConstant.NEXT_CUSTOMER_INTERVAL;

            if (m_Bubbles != null && slotIndex < m_Bubbles.Length && m_Bubbles[slotIndex] != null)
            {
                m_Bubbles[slotIndex].Unbind();
            }
        }

        /// <summary>
        /// 上菜：把做好的菜品交给场上需要它的顾客。
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

            return target != null && target.TryFulfillDish(dishId);
        }
    }
}

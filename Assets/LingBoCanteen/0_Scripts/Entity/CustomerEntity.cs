using System.Collections.Generic;
using LingBoCanteen.Definition.Enum;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 生成一个顾客所需的全部数据，由 <see cref="CustomerSlotManager"/> 计算后作为 userData 传入实体。
    /// </summary>
    public class CustomerSpawnData
    {
        public int SlotIndex;
        public Vector3 SpawnPosition;
        public Vector3 SlotPosition;
        public List<int> RequiredDishIds;
        public List<Sprite> RequiredDishIcons;
        public CustomerBuff Buff;
        public float PatienceSeconds;
        public GameRegion Region;
        public string AssetName;
    }

    /// <summary>
    /// 顾客实体：只负责入场/离场移动与订单数据（世界物体本身），不再持有任何 UI 组件。
    /// 顾客头顶的需求气泡（Buff/菜品图标/耐心 Slider）由 <see cref="CustomerBubbleView"/> 负责显示，
    /// 通过 <see cref="CustomerSlotManager"/> 在顾客生成/离场时 Bind/Unbind，
    /// 每帧读取本类暴露的只读属性来刷新，CustomerEntity 无需反向持有 UI 引用。
    /// 上菜结算的"谁优先被满足"逻辑在 <see cref="CustomerSlotManager"/> 里做全局判断，
    /// 这里只负责响应 <see cref="TryFulfillDish"/> 的结果。
    /// </summary>
    public class CustomerEntity : EntityLogic
    {
        private enum CustomerState
        {
            Entering,
            Waiting,
            Leaving,
        }

        [Header("移动设置")]
        [SerializeField] private float m_MoveDuration = 0.6f;
        [SerializeField] private float m_LeaveDistance = 8f;

        private CustomerSpawnData m_Data;
        private List<int> m_RemainingDishIds;
        private float m_PatienceRemaining;
        private bool m_IsResolved;
        private bool m_LastSuccess;

        private CustomerState m_State;
        private float m_MoveElapsed;
        private Vector3 m_LeaveFrom;
        private Vector3 m_LeaveTo;
        private float m_WaitElapsed;
        private bool m_SlothRolled;

        /// <summary>
        /// 当前剩余耐心时间（秒），供 CustomerSlotManager/CustomerBubbleView 使用。
        /// </summary>
        public float PatienceRemaining => m_PatienceRemaining;

        /// <summary>
        /// 本次订单的耐心时间上限（秒），Slider 的 maxValue 用这个。
        /// </summary>
        public float PatienceMax => m_Data != null ? m_Data.PatienceSeconds : 0f;

        /// <summary>
        /// 抽到的 Buff，气泡文字/背景色展示用。
        /// </summary>
        public CustomerBuff Buff => m_Data != null ? m_Data.Buff : CustomerBuff.None;

        /// <summary>
        /// 剩余尚未满足的菜品 Id 列表（只读）。
        /// </summary>
        public IReadOnlyList<int> RemainingDishIds => m_RemainingDishIds;

        /// <summary>
        /// 剩余尚未满足的菜品图标，与 RemainingDishIds 一一对应。
        /// </summary>
        public IReadOnlyList<Sprite> RemainingDishIcons => m_Data != null ? m_Data.RequiredDishIcons : null;

        /// <summary>
        /// 该顾客使用的立绘资源名（供邻位去重判断使用）。
        /// </summary>
        public string PortraitAssetName { get; private set; }

        /// <summary>
        /// 该顾客是否仍需要指定菜品。
        /// </summary>
        public bool NeedsDish(int dishId)
        {
            return !m_IsResolved && m_RemainingDishIds.Contains(dishId);
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_Data = userData as CustomerSpawnData;
            if (m_Data == null)
            {
                Log.Error("CustomerEntity 需要 CustomerSpawnData 作为 userData。");
                return;
            }

            m_RemainingDishIds = new List<int>(m_Data.RequiredDishIds);
            m_PatienceRemaining = m_Data.PatienceSeconds;
            m_IsResolved = false;
            m_LastSuccess = false;
            m_WaitElapsed = 0f;
            m_SlothRolled = false;

            m_State = CustomerState.Entering;
            m_MoveElapsed = 0f;
            CachedTransform.position = m_Data.SpawnPosition;

            // 强制设置顾客实体的缩放，确保在所有平台和实体预制件设置下保持一致
            CachedTransform.localScale = Vector3.one * 0.4f;

            PortraitAssetName = m_Data.AssetName;
            ApplyCustomerPortrait(m_Data.AssetName);

            CustomerSlotManager.Instance.RegisterOccupant(m_Data.SlotIndex, this);

            // 顾客可能恰好在切换区域的瞬间生成（Prefab 默认 Renderer/Collider 均为启用状态），
            // 主动向 AreaSwitchManager 查询当前应有的显隐状态并立即同步，避免短暂显示在错误区域，
            // 或者玩家切回订单区后依然看不到该顾客。
            if (AreaSwitchManager.Instance != null)
            {
                AreaSwitchManager.Instance.SyncCustomerVisibility(gameObject);
            }
        }

        private void ApplyCustomerPortrait(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return;
            }

            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Log.Warning("CustomerEntity '{0}' has no SpriteRenderer to apply portrait '{1}'.", Name, assetName);
                return;
            }

            Sprite portrait = CustomerPortraitUtility.GetPortrait(assetName, sprite =>
            {
                if (sprite != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprite;
                }
            });

            if (portrait != null)
            {
                spriteRenderer.sprite = portrait;
            }
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (m_Data == null)
            {
                return;
            }

            switch (m_State)
            {
                case CustomerState.Entering:
                    UpdateMove(m_Data.SpawnPosition, m_Data.SlotPosition, CustomerState.Waiting);
                    break;

                case CustomerState.Waiting:
                    // ★ 【新增】对话进行中时暂停耐心计时
                    if (CustomerSlotManager.Instance != null && !CustomerSlotManager.Instance.IsDialogueInProgress())
                    {
                        m_PatienceRemaining -= elapseSeconds;
                        m_WaitElapsed += elapseSeconds;
                    }

                    // 懒惰：等待达到指定秒数后，只判定一次是否提前离开（不影响耐心计时本身）
                    if (!m_SlothRolled && m_Data.Buff == CustomerBuff.Sloth
                        && m_WaitElapsed >= Constant.GameConstant.BUFF_SLOTH_WAIT_SECONDS)
                    {
                        m_SlothRolled = true;
                        if (Random.value < Constant.GameConstant.BUFF_SLOTH_LEAVE_CHANCE)
                        {
                            BeginLeave(false);
                            break;
                        }
                    }

                    if (m_PatienceRemaining <= 0f)
                    {
                        m_PatienceRemaining = 0f;
                        BeginLeave(false);
                    }
                    break;

                case CustomerState.Leaving:
                    UpdateMove(m_LeaveFrom, m_LeaveTo, CustomerState.Leaving);
                    break;
            }
        }

        /// <summary>
        /// 尝试用做好的菜品满足这位顾客的一项需求。
        /// 命中后如果需求已全部满足，则触发完成离场；否则只是把对应的订单图标去掉。
        /// </summary>
        public bool TryFulfillDish(int dishId)
        {
            if (!NeedsDish(dishId))
            {
                return false;
            }

            int index = m_RemainingDishIds.IndexOf(dishId);
            m_RemainingDishIds.RemoveAt(index);
            if (m_Data.RequiredDishIcons != null && index < m_Data.RequiredDishIcons.Count)
            {
                m_Data.RequiredDishIcons.RemoveAt(index);
            }

            if (m_RemainingDishIds.Count == 0)
            {
                BeginLeave(true);
            }

            return true;
        }

        private void UpdateMove(Vector3 from, Vector3 to, CustomerState nextStateWhenArrived)
        {
            m_MoveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(m_MoveElapsed / m_MoveDuration);
            CachedTransform.position = Vector3.Lerp(from, to, t);

            if (t < 1f)
            {
                return;
            }

            m_MoveElapsed = 0f;

            if (m_State == CustomerState.Leaving)
            {
                // 离场移动结束：通知槽位管理器进入 10s 冷却（并解绑气泡），再回收实体
                int slotIndex = m_Data.SlotIndex;
                bool success = m_LastSuccess;
                m_Data = null; // 防止后续 OnUpdate 再次访问
                CustomerSlotManager.Instance.OnCustomerLeft(slotIndex, success);
                GameEntry.Entity.HideEntity(Entity);
                return;
            }

            m_State = nextStateWhenArrived;
        }

        private void BeginLeave(bool success)
        {
            if (m_IsResolved)
            {
                return;
            }

            m_IsResolved = true;
            m_LastSuccess = success;
            m_LeaveFrom = CachedTransform.position;
            m_LeaveTo = m_LeaveFrom + Vector3.left * m_LeaveDistance;
            m_MoveElapsed = 0f;
            m_State = CustomerState.Leaving;
        }
    }
}

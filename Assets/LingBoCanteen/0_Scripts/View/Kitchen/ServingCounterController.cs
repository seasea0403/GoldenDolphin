using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 上菜区（桌布区域）：点击后收集所有已关火未糊的锅具成品菜并摆盘展示，
    /// 供玩家确认后点击"上菜铃"（<see cref="ServiceBellController"/>）执行真正的上菜结算。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ServingCounterController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] m_Slots;

        private int[] m_SlotDishIds;
        private bool[] m_SlotOccupied;

        private void Awake()
        {
            int count = m_Slots != null ? m_Slots.Length : 0;
            m_SlotDishIds = new int[count];
            m_SlotOccupied = new bool[count];

            for (int i = 0; i < count; i++)
            {
                if (m_Slots[i] != null)
                {
                    m_Slots[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnMouseDown()
        {
            KitchenManager.Instance?.CollectReadyDishes(this);
        }

        public bool HasEmptySlot()
        {
            for (int i = 0; i < m_SlotOccupied.Length; i++)
            {
                if (!m_SlotOccupied[i])
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryPlaceDish(int dishId, Sprite sprite)
        {
            for (int i = 0; i < m_SlotOccupied.Length; i++)
            {
                if (m_SlotOccupied[i])
                {
                    continue;
                }

                m_SlotOccupied[i] = true;
                m_SlotDishIds[i] = dishId;
                if (m_Slots[i] != null)
                {
                    m_Slots[i].sprite = sprite;
                    m_Slots[i].gameObject.SetActive(true);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 供 <see cref="ServiceBellController"/> 调用：对每个已摆盘的菜品尝试按现有订单匹配逻辑上菜结算，
        /// 结算成功（<see cref="CustomerSlotManager.ServeDish"/> 内部已经处理金币/San）的清空槽位，
        /// 没有顾客需要的菜品继续留在桌上，等待下一次点铃或补单。
        /// </summary>
        public void TryServeAll()
        {
            if (CustomerSlotManager.Instance == null)
            {
                return;
            }

            for (int i = 0; i < m_SlotOccupied.Length; i++)
            {
                if (!m_SlotOccupied[i])
                {
                    continue;
                }

                if (CustomerSlotManager.Instance.ServeDish(m_SlotDishIds[i]))
                {
                    ClearSlot(i);
                }
            }
        }

        private void ClearSlot(int index)
        {
            m_SlotOccupied[index] = false;
            m_SlotDishIds[index] = 0;
            if (m_Slots[index] != null)
            {
                m_Slots[index].sprite = null;
                m_Slots[index].gameObject.SetActive(false);
            }
        }
    }
}

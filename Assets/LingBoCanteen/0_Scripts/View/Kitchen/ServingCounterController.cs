using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 上菜区（桌布区域）：点击后收集所有已关火未糊的锅具成品菜并摆盘展示，
    /// 供玩家确认后点击"上菜铃"（<see cref="ServiceBellController"/>）执行真正的上菜结算。
    /// 当plate上有菜品时，可以拖动plate到垃圾桶清空所有菜品。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ServingCounterController : KitchenStationBase
    {
        [SerializeField] private SpriteRenderer[] m_Slots;
        [Tooltip("拖拽时使用的预览图片（可选，若为空则使用第一个菜品图片）")]
        [SerializeField] private Sprite m_DragPreviewSprite;

        private int[] m_SlotDishIds;
        private bool[] m_SlotOccupied;
        private Sprite[] m_SlotSprites;
        private bool m_IsDragging;

        protected override void Awake()
        {
            base.Awake();

            int count = m_Slots != null ? m_Slots.Length : 0;
            m_SlotDishIds = new int[count];
            m_SlotOccupied = new bool[count];
            m_SlotSprites = new Sprite[count];

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
            // 如果plate上有菜品，优先允许拖拽；否则触发收集菜品
            if (HasAnyDish())
            {
                BeginDragPlate();
            }
            else
            {
                KitchenManager.Instance?.CollectReadyDishes(this);
            }
        }

        /// <summary>
        /// 检查plate上是否有任何菜品
        /// </summary>
        private bool HasAnyDish()
        {
            for (int i = 0; i < m_SlotOccupied.Length; i++)
            {
                if (m_SlotOccupied[i])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 开始拖拽plate到垃圾桶
        /// </summary>
        private void BeginDragPlate()
        {
            if (KitchenDragController.Instance == null || m_IsDragging)
            {
                return;
            }

            // 选择拖拽预览图：优先使用配置的预览图，否则使用第一个有内容的菜品图
            Sprite previewSprite = m_DragPreviewSprite;
            if (previewSprite == null)
            {
                for (int i = 0; i < m_SlotSprites.Length; i++)
                {
                    if (m_SlotOccupied[i] && m_SlotSprites[i] != null)
                    {
                        previewSprite = m_SlotSprites[i];
                        break;
                    }
                }
            }

            if (previewSprite == null)
            {
                return;
            }

            m_IsDragging = true;
            
            // 拖拽期间隐藏菜品，避免视觉混乱
            for (int i = 0; i < m_Slots.Length; i++)
            {
                if (m_Slots[i] != null)
                {
                    m_Slots[i].gameObject.SetActive(false);
                }
            }

            KitchenDragPayload payload = new KitchenDragPayload(KitchenDragItemKind.Plate, 0, previewSprite)
            {
                OnAccepted = ClearAllDishes,
                OnReturnToOrigin = () =>
                {
                    m_IsDragging = false;
                    // 恢复显示所有菜品
                    for (int i = 0; i < m_SlotOccupied.Length; i++)
                    {
                        if (m_SlotOccupied[i] && m_Slots[i] != null)
                        {
                            m_Slots[i].gameObject.SetActive(true);
                        }
                    }
                },
            };

            KitchenDragController.Instance.BeginDrag(payload, previewSprite, transform.position);
        }

        /// <summary>
        /// 清空plate上的所有菜品
        /// </summary>
        private void ClearAllDishes()
        {
            m_IsDragging = false;
            for (int i = 0; i < m_SlotOccupied.Length; i++)
            {
                ClearSlot(i);
            }
        }

        protected override bool CanAccept(KitchenDragPayload payload)
        {
            // ServingCounter作为垃圾桶的接收端点，这里返回false
            // 它本身是拖拽的源头，不是接收端
            return false;
        }

        protected override void Accept(KitchenDragPayload payload)
        {
            // ServingCounter 不接收任何拖拽，此方法不应被调用
            // CanAccept 已经返回 false，所以这里永远不会执行
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
                m_SlotSprites[i] = sprite;
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
            m_SlotSprites[index] = null;
            if (m_Slots[index] != null)
            {
                m_Slots[index].sprite = null;
                m_Slots[index].gameObject.SetActive(false);
            }
        }
    }
}

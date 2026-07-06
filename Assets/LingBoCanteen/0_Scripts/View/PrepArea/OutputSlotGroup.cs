using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 碗槽 / 杯槽的容器：共 4 格，按"从前到后、先空先填"的顺序自动收纳。
    /// </summary>
    public class OutputSlotGroup : MonoBehaviour
    {
        [Tooltip("按填充优先级从前到后排列，共 4 格")]
        [SerializeField] private OutputSlotView[] m_Slots;

        /// <summary>
        /// 尝试把产出物放进第一个空槽，成功返回 true；全满返回 false（调用方自行滞留重试）。
        /// </summary>
        public bool TryAddItem(int outputId, string outputName, Sprite sprite)
        {
            if (m_Slots == null)
            {
                return false;
            }

            for (int i = 0; i < m_Slots.Length; i++)
            {
                OutputSlotView slot = m_Slots[i];
                if (slot != null && slot.IsEmpty)
                {
                    slot.SetItem(outputId, outputName, sprite);
                    return true;
                }
            }

            return false;
        }

        public bool HasEmptySlot()
        {
            if (m_Slots == null)
            {
                return false;
            }

            for (int i = 0; i < m_Slots.Length; i++)
            {
                if (m_Slots[i] != null && m_Slots[i].IsEmpty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

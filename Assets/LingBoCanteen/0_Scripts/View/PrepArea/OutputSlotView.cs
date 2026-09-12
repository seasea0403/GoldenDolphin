using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 单个收纳槽（碗/杯）。存放"加工产出物"，此时食材 Id 已变为 OutputId（+3000），
    /// 名称显示为 OutputName，悬停只显示名称不显示数量。
    /// </summary>
    public class OutputSlotView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer m_IconRenderer;

        public bool IsEmpty { get; private set; } = true;
        public int OutputId { get; private set; }
        public string OutputName { get; private set; }

        private void Awake()
        {
            // 场景里摆放时图标物体可能是激活状态（方便策划摆位置），这里统一在启动时
            // 按"空槽"状态清一次，避免忘记手动关闭图标物体导致空槽看起来像已经有产出物。
            Clear();
        }

        public void SetItem(int outputId, string outputName, Sprite sprite)
        {
            OutputId = outputId;
            OutputName = outputName;
            IsEmpty = false;

            if (m_IconRenderer != null)
            {
                m_IconRenderer.sprite = sprite;
                m_IconRenderer.gameObject.SetActive(true);
            }
        }

        public void Clear()
        {
            IsEmpty = true;
            OutputId = 0;
            OutputName = null;

            if (m_IconRenderer != null)
            {
                m_IconRenderer.sprite = null;
                m_IconRenderer.gameObject.SetActive(false);
            }
        }

        private void OnMouseEnter()
        {
            if (!IsEmpty && IngredientTooltipView.Instance != null)
            {
                IngredientTooltipView.Instance.ShowNameOnly(transform.position, OutputName);
            }
        }

        private void OnMouseExit()
        {
            IngredientTooltipView.Instance?.Hide();
        }

        /// <summary>
        /// 供烹调区拖拽逻辑调用：点击非空槽位即可拖拽该产出物投放到锅具里。
        /// 场景中若没有摆放 <see cref="KitchenDragController"/>（例如纯备菜区调试场景）则什么都不做。
        /// </summary>
        private void OnMouseDown()
        {
            if (UIFormSceneInputBlocker.IsSceneInputBlocked) return;

            if (IsEmpty || KitchenDragController.Instance == null)
            {
                return;
            }

            Sprite icon = m_IconRenderer != null ? m_IconRenderer.sprite : null;
            if (!TryTakeItem(out int outputId, out string outputName))
            {
                return;
            }

            KitchenDragPayload payload = new KitchenDragPayload(KitchenDragItemKind.Ingredient, outputId, icon)
            {
                OnReturnToOrigin = () => SetItem(outputId, outputName, icon),
            };

            KitchenDragController.Instance.BeginDrag(payload, icon, transform.position);
        }

        /// <summary>
        /// 供烹调区拖拽逻辑调用：取走该槽产出物（清空槽位并返回其 Id/名称）。
        /// 烹调区的接收逻辑不在本次备菜区范围内，这里只负责清空槽位。
        /// </summary>
        public bool TryTakeItem(out int outputId, out string outputName)
        {
            outputId = OutputId;
            outputName = OutputName;

            if (IsEmpty)
            {
                return false;
            }

            Clear();
            return true;
        }
    }
}

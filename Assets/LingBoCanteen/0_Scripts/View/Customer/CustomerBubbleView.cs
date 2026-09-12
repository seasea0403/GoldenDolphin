using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LingBoCanteen
{
    /// <summary>
    /// 顾客需求气泡：Screen Space UI，跟随对应顾客的世界坐标显示 Buff / 菜品图标 / 耐心倒计时。
    ///
    /// 说明1（气泡如何与世界物体配合）：
    /// 顾客固定只有 3 个槽位，所以气泡不用 Instantiate/Destroy 现造——直接在场景里预先摆好 3 个
    /// CustomerBubbleView（挂在一个 Screen Space - Camera/Overlay 的 Canvas 下），由
    /// CustomerSlotManager 在顾客 OnShow / 离场时调用 Bind(customer) / Unbind()。
    /// 绑定后每帧在 LateUpdate 里把顾客世界坐标转换成屏幕坐标来跟随，
    /// 而不是把气泡做成顾客预制体下的 World Space 子物体——这样可以继续用现有的 UI 摄像机/排序，
    /// 不需要额外处理 World Space Canvas 的缩放和排序层问题。
    ///
    /// 说明2（1 个菜和 2 个菜不是简单显隐，而是位置+缩放变化）：
    /// 固定放 3 个 Image 槽位（m_DishIconSlots），1 道菜时只启用 m_SingleDishSlotIndex 对应的槽位
    /// 并应用居中、放大的位置/缩放；2 道菜时启用 m_DualDishSlotIndices 对应的两个槽位，
    /// 应用左右分布、缩小的位置/缩放。缩放统一通过 RectTransform.localScale 实现（不改 sizeDelta），
    /// 配合 Image.preserveAspect = true，图标不会被拉伸变形。
    /// </summary>
    public class CustomerBubbleView : MonoBehaviour
    {
        [Header("跟随设置")]
        [SerializeField] private Camera m_WorldCamera;
        [SerializeField] private RectTransform m_SelfRect;
        [SerializeField] private Vector3 m_WorldOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Buff 展示")]
        [SerializeField] private Image m_BuffBackground;
        [SerializeField] private TextMeshProUGUI m_BuffText;
        [SerializeField] private Color m_VirtueColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private Color m_SinColor = new Color(0.55f, 0.1f, 0.1f);

        [Header("耐心倒计时")]
        [SerializeField] private Slider m_PatienceSlider;

        [Header("菜品图标（固定 3 个槽位，按需求数量 1/2 切换布局）")]
        [SerializeField] private Image[] m_DishIconSlots = new Image[3];

        [Header("单菜品布局：居中放大显示")]
        [SerializeField] private int m_SingleDishSlotIndex = 1;
        [SerializeField] private Vector2 m_SingleDishAnchoredPosition = Vector2.zero;
        [SerializeField] private Vector3 m_SingleDishScale = Vector3.one;

        [Header("双菜品布局：左右两个缩小显示")]
        [SerializeField] private int[] m_DualDishSlotIndices = { 0, 2 };
        [SerializeField] private Vector2[] m_DualDishAnchoredPositions = { new Vector2(-50f, 0f), new Vector2(50f, 0f) };
        [SerializeField] private Vector3 m_DualDishScale = new Vector3(0.75f, 0.75f, 1f);

        private CustomerEntity m_BoundCustomer;
        private int m_LastDishCount = -1;

        /// <summary>
        /// 当前绑定顾客抽到的 Buff，供 <see cref="CustomerBuffHoverTrigger"/> 悬浮提示查询。
        /// </summary>
        public LingBoCanteen.Definition.Enum.CustomerBuff CurrentBuff =>
            m_BoundCustomer != null ? m_BoundCustomer.Buff : LingBoCanteen.Definition.Enum.CustomerBuff.None;

        private void Awake()
        {
            if (m_WorldCamera == null)
            {
                m_WorldCamera = Camera.main;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 绑定到指定顾客，开始跟随并展示其订单信息。
        /// </summary>
        public void Bind(CustomerEntity customer)
        {
            m_BoundCustomer = customer;
            m_LastDishCount = -1;
            gameObject.SetActive(true);

            RefreshBuffDisplay();
            RefreshDishIcons();
            FollowTarget();
            RefreshSlider();
        }

        /// <summary>
        /// 解绑并隐藏，供下一位顾客复用。
        /// </summary>
        public void Unbind()
        {
            m_BoundCustomer = null;
            gameObject.SetActive(false);
            CustomerBuffTooltipView.Instance?.Hide();
        }

        private void LateUpdate()
        {
            if (m_BoundCustomer == null)
            {
                return;
            }

            FollowTarget();
            RefreshSlider();

            int currentDishCount = m_BoundCustomer.RemainingDishIds.Count;
            if (currentDishCount != m_LastDishCount)
            {
                RefreshDishIcons();
            }
        }

        private void FollowTarget()
        {
            Vector3 worldPosition = m_BoundCustomer.CachedTransform.position + m_WorldOffset;
            Vector3 screenPosition = m_WorldCamera.WorldToScreenPoint(worldPosition);
            m_SelfRect.position = screenPosition;
        }

        private void RefreshBuffDisplay()
        {
            // 如果没有 Buff（例如人间），隐藏 Buff 展示
            if (m_BoundCustomer.Buff == LingBoCanteen.Definition.Enum.CustomerBuff.None)
            {
                if (m_BuffBackground != null) m_BuffBackground.gameObject.SetActive(false);
                if (m_BuffText != null) m_BuffText.gameObject.SetActive(false);
                return;
            }

            if (m_BuffBackground != null) m_BuffBackground.gameObject.SetActive(true);
            if (m_BuffText != null) m_BuffText.gameObject.SetActive(true);

            bool isVirtue = CustomerBuffUtility.IsVirtue(m_BoundCustomer.Buff);
            m_BuffBackground.color = isVirtue ? m_VirtueColor : m_SinColor;
            m_BuffText.text = CustomerBuffUtility.GetDescription(m_BoundCustomer.Buff);
        }

        private void RefreshSlider()
        {
            m_PatienceSlider.maxValue = m_BoundCustomer.PatienceMax;
            m_PatienceSlider.value = m_BoundCustomer.PatienceRemaining;
        }

        /// <summary>
        /// 按剩余菜品数量（1 或 2）切换图标槽位的启用状态、位置与缩放。
        /// </summary>
        private void RefreshDishIcons()
        {
            var remainingIds = m_BoundCustomer.RemainingDishIds;
            var remainingIcons = m_BoundCustomer.RemainingDishIcons;
            int count = remainingIds.Count;
            m_LastDishCount = count;

            for (int i = 0; i < m_DishIconSlots.Length; i++)
            {
                m_DishIconSlots[i].gameObject.SetActive(false);
            }

            if (count <= 0)
            {
                return;
            }

            if (count == 1)
            {
                ApplyDishSlot(m_SingleDishSlotIndex, 0, remainingIcons, m_SingleDishAnchoredPosition, m_SingleDishScale);
            }
            else
            {
                for (int i = 0; i < m_DualDishSlotIndices.Length && i < count; i++)
                {
                    ApplyDishSlot(m_DualDishSlotIndices[i], i, remainingIcons, m_DualDishAnchoredPositions[i], m_DualDishScale);
                }
            }
        }

        private void ApplyDishSlot(int slotIndex, int dishIndex, System.Collections.Generic.IReadOnlyList<Sprite> icons, Vector2 anchoredPosition, Vector3 scale)
        {
            if (slotIndex < 0 || slotIndex >= m_DishIconSlots.Length)
            {
                return;
            }

            Image slot = m_DishIconSlots[slotIndex];
            slot.gameObject.SetActive(true);

            // preserveAspect = true：无论槽位 RectTransform 多大，Sprite 都按原始宽高比缩放显示，
            // 不会因为槽位宽高比和贴图不一致而被拉伸变形；1/2 道菜的大小差异用 localScale 控制。
            slot.preserveAspect = true;
            slot.rectTransform.anchoredPosition = anchoredPosition;
            slot.rectTransform.localScale = scale;

            if (icons != null && dishIndex < icons.Count)
            {
                slot.sprite = icons[dishIndex];
            }
        }
    }
}

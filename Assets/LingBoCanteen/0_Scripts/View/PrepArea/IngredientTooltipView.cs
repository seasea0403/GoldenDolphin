using TMPro;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 备菜区通用悬浮提示：鼠标悬停在可交互食材/收纳槽产出物上时显示名称（+ 剩余数量）。
    /// 场景内放置一个实例，挂到 Screen Space 或 World Space Canvas 下，默认隐藏。
    /// </summary>
    public class IngredientTooltipView : MonoBehaviour
    {
        public static IngredientTooltipView Instance { get; private set; }

        [SerializeField] private RectTransform m_Root;
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private TMP_Text m_CountText;
        [SerializeField] private Vector2 m_ScreenOffset = new Vector2(16f, 16f);
        [SerializeField] private Camera m_WorldCamera;

        private void Awake()
        {
            Instance = this;
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 显示名称 + 数量（isUnlimited 为 true 时数量显示"无限"）。
        /// </summary>
        public void Show(Vector3 worldPosition, string name, int count, bool isUnlimited)
        {
            if (m_Root == null)
            {
                return;
            }

            m_Root.gameObject.SetActive(true);
            if (m_NameText != null)
            {
                m_NameText.text = name;
            }

            if (m_CountText != null)
            {
                m_CountText.text = isUnlimited ? "无限" : count.ToString();
            }

            FollowWorldPosition(worldPosition);
        }

        /// <summary>
        /// 只显示名称，不显示数量（收纳槽内已产出的物品用这个）。
        /// </summary>
        public void ShowNameOnly(Vector3 worldPosition, string name)
        {
            if (m_Root == null)
            {
                return;
            }

            m_Root.gameObject.SetActive(true);
            if (m_NameText != null)
            {
                m_NameText.text = name;
            }

            if (m_CountText != null)
            {
                m_CountText.text = string.Empty;
            }

            FollowWorldPosition(worldPosition);
        }

        public void Hide()
        {
            if (m_Root != null)
            {
                m_Root.gameObject.SetActive(false);
            }
        }

        private void FollowWorldPosition(Vector3 worldPosition)
        {
            if (m_Root == null)
            {
                return;
            }

            // 直接跟随鼠标位置 + offset
            Vector3 mouseScreenPos = Input.mousePosition;
            m_Root.position = mouseScreenPos + (Vector3)m_ScreenOffset;
        }
    }
}

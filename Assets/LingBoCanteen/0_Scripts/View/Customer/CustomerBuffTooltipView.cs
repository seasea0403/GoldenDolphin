using TMPro;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 顾客 Buff 悬浮提示：鼠标悬停在气泡的 Buff 图标上时显示该 Buff 的名称与具体效果说明。
    /// 场景内放置一个实例（挂到 Screen Space Canvas 下，与 <see cref="IngredientTooltipView"/> 同级），默认隐藏。
    /// </summary>
    public class CustomerBuffTooltipView : MonoBehaviour
    {
        public static CustomerBuffTooltipView Instance { get; private set; }

        [SerializeField] private RectTransform m_Root;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_DescriptionText;
        [SerializeField] private Vector2 m_ScreenOffset = new Vector2(16f, 16f);

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
        /// 显示 Buff 名称 + 效果说明，位置跟随当前鼠标位置。
        /// </summary>
        public void Show(string title, string description)
        {
            if (m_Root == null)
            {
                return;
            }

            m_Root.gameObject.SetActive(true);
            if (m_TitleText != null)
            {
                m_TitleText.text = title;
            }

            if (m_DescriptionText != null)
            {
                m_DescriptionText.text = description;
            }

            m_Root.position = Input.mousePosition + (Vector3)m_ScreenOffset;
        }

        public void Hide()
        {
            if (m_Root != null)
            {
                m_Root.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (m_Root != null && m_Root.gameObject.activeSelf)
            {
                m_Root.position = Input.mousePosition + (Vector3)m_ScreenOffset;
            }
        }
    }
}

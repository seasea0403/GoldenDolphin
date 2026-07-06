using UnityEngine;
using UnityEngine.UI;

namespace LingBoCanteen
{
    /// <summary>
    /// 傍晚界面整体按钮入口：
    /// - 结束今日按钮 -&gt; 打开结算面板 <see cref="SettlePanel"/>；
    /// - 超市图标按钮 -&gt; 显示超市面板。
    /// 电梯上/下按钮的显隐与点击逻辑由 <see cref="ElevatorController"/> 自行管理，不在此类处理。
    /// </summary>
    public class EveningUIController : MonoBehaviour
    {
        [Header("结束今日按钮")]
        [SerializeField] private Button m_EndDayButton;

        [Header("结算面板")]
        [SerializeField] private SettlePanel m_SettlePanel;

        [Header("超市面板")]
        [SerializeField] private GameObject m_MarketPanel;

        [Header("超市图标按钮")]
        [SerializeField] private Button m_SupermarketButton;

        private void Awake()
        {
            if (m_EndDayButton != null)
            {
                m_EndDayButton.onClick.AddListener(OnEndDayClicked);
            }

            if (m_SupermarketButton != null)
            {
                m_SupermarketButton.onClick.AddListener(OnSupermarketClicked);
            }
        }

        private void OnEndDayClicked()
        {
            if (m_SettlePanel != null)
            {
                m_SettlePanel.Open();
            }
        }

        private void OnSupermarketClicked()
        {
            if (m_MarketPanel != null)
            {
                m_MarketPanel.SetActive(true);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 超市购买二级确认窗口：显示食材图标/名称/单价/当前剩余个数，
    /// 用步进器调节购买数量（上限 = 当前金币可负担的最大份数），
    /// 点击确认后才真正执行购买并弹出"购买成功"浮窗；右上角关闭按钮关闭自身。
    /// </summary>
    public class MarketPurchasePanel : MonoBehaviour
    {
        [SerializeField] private GameObject m_Root;
        [SerializeField] private Image m_IconImage;
        [SerializeField] private TextMeshProUGUI m_NameText;
        [SerializeField] private TextMeshProUGUI m_PriceText;
        [SerializeField] private TextMeshProUGUI m_StockText;
        [SerializeField] private TextMeshProUGUI m_CountText;
        [SerializeField] private Button m_MinusButton;
        [SerializeField] private Button m_PlusButton;
        [SerializeField] private Button m_ConfirmButton;
        [SerializeField] private Button m_CloseButton;

        private DRIngredient m_Row;
        private int m_Count = 1;
        private System.Action<int, int> m_OnConfirm;

        private void Awake()
        {
            if (m_MinusButton != null)
            {
                m_MinusButton.onClick.AddListener(() => ChangeCount(-1));
                UIButtonSoundHelper.BindButtonSound(m_MinusButton);
            }

            if (m_PlusButton != null)
            {
                m_PlusButton.onClick.AddListener(() => ChangeCount(1));
                UIButtonSoundHelper.BindButtonSound(m_PlusButton);
            }

            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.onClick.AddListener(OnConfirmClicked);
                UIButtonSoundHelper.BindButtonSound(m_ConfirmButton);
            }

            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.AddListener(Close);
                UIButtonSoundHelper.BindButtonSound(m_CloseButton);
            }

            m_Root?.SetActive(false);
        }

        /// <summary>
        /// 打开窗口。onConfirm 回调参数为 (ingredientId, 购买数量)。
        /// </summary>
        public void Open(DRIngredient row, System.Action<int, int> onConfirm)
        {
            if (row == null)
            {
                return;
            }

            m_Row = row;
            m_OnConfirm = onConfirm;
            m_Count = 1;

            if (m_Root != null)
            {
                m_Root.SetActive(true);
            }

            RefreshUI();
        }

        public void Close()
        {
            m_Row = null;
            m_OnConfirm = null;
            m_Root?.SetActive(false);
        }

        /// <summary>
        /// 当前金币可负担的最大购买份数（至少为 1，便于展示）。
        /// </summary>
        private int GetMaxCount()
        {
            int gold = GetCurrentGold();
            if (m_Row == null || m_Row.ConsumeMoney <= 0)
            {
                return 1;
            }

            return Mathf.Max(1, gold / m_Row.ConsumeMoney);
        }

        private void ChangeCount(int delta)
        {
            m_Count = Mathf.Clamp(m_Count + delta, 1, GetMaxCount());
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (m_Row == null)
            {
                return;
            }

            if (m_IconImage != null)
            {
                Sprite sprite = IngredientUtility.GetSprite(m_Row, m_Row.InitialAssetName);
                if (sprite != null)
                {
                    m_IconImage.sprite = sprite;
                }
            }

            if (m_NameText != null)
            {
                m_NameText.text = m_Row.Name;
            }

            if (m_PriceText != null)
            {
                m_PriceText.text = "单价: " + m_Row.ConsumeMoney;
            }

            if (m_StockText != null)
            {
                m_StockText.text = "剩余: " + IngredientUtility.GetStock(m_Row.Id);
            }

            if (m_CountText != null)
            {
                m_CountText.text = m_Count.ToString();
            }

            if (m_ConfirmButton != null)
            {
                // 金币不足以支付当前数量时禁用确认键
                m_ConfirmButton.interactable = GetCurrentGold() >= m_Row.ConsumeMoney * m_Count;
            }

            // 步进器按钮状态：每次都显式设置 interactable（顺带修复预制体里 plus 键被勾选成不可交互导致完全没响应的问题）
            int maxCount = GetMaxCount();
            if (m_PlusButton != null)
            {
                m_PlusButton.interactable = m_Count < maxCount;
            }

            if (m_MinusButton != null)
            {
                m_MinusButton.interactable = m_Count > 1;
            }
        }

        private void OnConfirmClicked()
        {
            if (m_Row == null)
            {
                return;
            }

            int ingredientId = m_Row.Id;
            int count = m_Count;
            m_OnConfirm?.Invoke(ingredientId, count);
            Close();
        }

        private int GetCurrentGold()
        {
            VarInt32 goldVar = GameEntry.DataNode.GetData<VarInt32>("Player.Gold");
            return goldVar != null ? goldVar.Value : 0;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using TMPro;

namespace LingBoCanteen
{
    /// <summary>
    /// 超市食材单个槽位，显示食材信息（图片、价格、库存）、管理购买按钮状态、显示 Tooltip、处理点击购买。
    /// 挂载到预制体的每个 slot 实体上。
    /// </summary>
    public class MarketIngredientSlot : MonoBehaviour
    {
        [SerializeField] private Image m_IconImage;
        [SerializeField] private TextMeshProUGUI m_PriceText;
        [SerializeField] private TextMeshProUGUI m_StockText;
        [SerializeField] private Image m_MaskImage;
        [SerializeField] private Button m_PurchaseButton;
        [SerializeField] private Color m_GrayedOutColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

        private int m_IngredientId = -1;
        private int m_CurrentDay = 1;
        private DRIngredient m_Row;
        private bool m_IsUnlocked = false;
        private CanvasGroup m_CanvasGroup;

        public delegate void PurchaseClickedDelegate(int ingredientId);
        public event PurchaseClickedDelegate OnPurchaseClicked;

        private void Awake()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (m_PurchaseButton != null)
            {
                m_PurchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
                // 绑定音效
                UIButtonSoundHelper.BindButtonSound(m_PurchaseButton);
            }
        }

        private void OnMouseEnter()
        {
            if (m_IngredientId <= 0 || m_Row == null)
            {
                return;
            }

            int stock = IngredientUtility.GetStock(m_IngredientId);
            if (IngredientTooltipView.Instance != null)
            {
                IngredientTooltipView.Instance.Show(transform.position, m_Row.Name, stock, false);
            }
        }

        private void OnMouseExit()
        {
            if (IngredientTooltipView.Instance != null)
            {
                IngredientTooltipView.Instance.Hide();
            }
        }

        /// <summary>
        /// 设置该槽位要显示的食材（含当前天数用于判断解锁状态）。
        /// </summary>
        public void SetIngredientId(int ingredientId, int currentDay)
        {
            m_IngredientId = ingredientId;
            m_CurrentDay = currentDay;
            m_Row = GameEntry.DataTable.GetDataTable<DRIngredient>().GetDataRow(ingredientId);

            if (m_Row == null)
            {
                Log.Error("MarketIngredientSlot 找不到 Id 为 {0} 的 DRIngredient 配置。", ingredientId);
                SetEmpty();
                return;
            }

            // 检查是否解锁
            m_IsUnlocked = IngredientUtility.IsUnlocked(ingredientId, currentDay);

            // 显示食材图标（同时设置到购买按钮上）
            Sprite ingredientSprite = IngredientUtility.GetSprite(m_Row, m_Row.InitialAssetName);
            if (m_IconImage != null && ingredientSprite != null)
            {
                m_IconImage.sprite = ingredientSprite;
            }

            if (m_PurchaseButton != null)
            {
                Image buttonImage = m_PurchaseButton.GetComponent<Image>();
                if (buttonImage != null && ingredientSprite != null)
                {
                    buttonImage.sprite = ingredientSprite;
                }
            }

            // 显示价格
            if (m_PriceText != null)
            {
                m_PriceText.text = m_Row.ConsumeMoney.ToString();
            }

            // 显示库存
            if (m_StockText != null)
            {
                int stock = IngredientUtility.GetStock(ingredientId);
                m_StockText.text = "x" + stock;
            }

            RefreshButtonState();
        }

        /// <summary>
        /// 清空该槽位（用于最后一页的空槽位）。
        /// </summary>
        public void SetEmpty()
        {
            m_IngredientId = -1;
            m_Row = null;
            m_IsUnlocked = false;

            if (m_IconImage != null)
            {
                m_IconImage.sprite = null;
            }

            if (m_PurchaseButton != null)
            {
                Image buttonImage = m_PurchaseButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = null;
                }
            }

            if (m_PriceText != null)
            {
                m_PriceText.text = "";
            }

            if (m_StockText != null)
            {
                m_StockText.text = "";
            }

            SetMaskVisible(false);
            SetButtonInteractable(false);
        }

        /// <summary>
        /// 刷新购买按钮的状态：
        /// - 未解锁：显示 Mask、禁用按钮、恢复正常色。
        /// - 已解锁 + 金币足够：隐藏 Mask、启用按钮、正常色。
        /// - 已解锁 + 金币不足：隐藏 Mask、禁用按钮、置灰。
        /// </summary>
        public void RefreshButtonState()
        {
            if (m_IngredientId <= 0 || m_Row == null)
            {
                return;
            }

            SetMaskVisible(!m_IsUnlocked);

            if (!m_IsUnlocked)
            {
                // 未解锁：禁用按钮
                SetButtonInteractable(false);
                SetButtonColor(Color.white);
                return;
            }

            // 已解锁：检查金币
            VarInt32 goldVar = GameEntry.DataNode.GetData<VarInt32>("Player.Gold");
            int currentGold = goldVar != null ? goldVar.Value : 0;
            bool canAfford = currentGold >= m_Row.ConsumeMoney;

            SetButtonInteractable(canAfford);
            SetButtonColor(canAfford ? Color.white : m_GrayedOutColor);
        }

        private void SetMaskVisible(bool visible)
        {
            if (m_MaskImage != null)
            {
                m_MaskImage.gameObject.SetActive(visible);
            }
        }

        private void SetButtonInteractable(bool interactable)
        {
            if (m_PurchaseButton != null)
            {
                m_PurchaseButton.interactable = interactable;
            }
        }

        private void SetButtonColor(Color color)
        {
            if (m_PurchaseButton == null)
            {
                return;
            }

            Image buttonImage = m_PurchaseButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = color;
            }
        }

        private void OnPurchaseButtonClicked()
        {
            if (m_IngredientId <= 0)
            {
                return;
            }

            OnPurchaseClicked?.Invoke(m_IngredientId);
        }
    }
}

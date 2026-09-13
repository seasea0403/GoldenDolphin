using System.Collections.Generic;
using LingBoCanteen.Definition.Enum;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using TMPro;
namespace LingBoCanteen
{
    /// <summary>
    /// 超市面板主逻辑，负责初始化食材槽位、管理分页、刷新 UI 状态（库存、金币显示）。
    /// 只需要在 Inspector 里配置 8 个固定的槽位锚点位置（<see cref="m_SlotPositions"/>，2*4 排布）
    /// 和 1 个槽位预制体（<see cref="m_SlotPrefab"/>），脚本会在每个锚点下各实例化 1 个槽位实例
    /// （全程只有 8 个实例，翻页时复用，重新填充对应页的数据），不需要预先摆好每一页的槽位。
    /// </summary>
    public class MarketPanel : MonoBehaviour
    {
        [SerializeField] private Transform[] m_SlotPositions;
        [SerializeField] private MarketIngredientSlot m_SlotPrefab;
        [SerializeField] private TextMeshProUGUI m_GoldDisplay;
        [SerializeField] private MarketPurchasePanel m_PurchasePanel;
        [SerializeField] private Button m_PrevPageButton;
        [SerializeField] private Button m_NextPageButton;
        public GameObject uiRoot;

        private MarketIngredientSlot[] m_SlotInstances;
        private List<int> m_UnlockedIngredientIds = new List<int>();
        private int m_CurrentPage = 0;
        private int m_TotalPages = 1;
        private int m_CurrentDay = 1;

        public int CurrentPage => m_CurrentPage;
        public int TotalPages => m_TotalPages;

        private void Awake()
        {
            if (m_PurchasePanel == null)
            {
                m_PurchasePanel = GetComponentInChildren<MarketPurchasePanel>(true);
            }

            // 翻页箭头按钮：代码里绑定点击事件（如果已在 Inspector 里用 onClick 接过会重复触发，
            // 请把 Inspector 里的手动接线删掉，统一由这里管理），翻页后由 RefreshUI 更新可用状态
            if (m_PrevPageButton != null)
            {
                m_PrevPageButton.onClick.AddListener(PrevPage);
                UIButtonSoundHelper.BindButtonSound(m_PrevPageButton);
            }

            if (m_NextPageButton != null)
            {
                m_NextPageButton.onClick.AddListener(NextPage);
                UIButtonSoundHelper.BindButtonSound(m_NextPageButton);
            }

            InitializeSlots();
        }

        private void OnEnable()
        {
            // 每次打开超市都重置：二级窗口收起、重新加载当前天数的解锁食材
            m_PurchasePanel?.Close();
            int currentDay = GetEffectiveShoppingDay();
            Log.Info($"[MarketPanel.OnEnable] 重新加载食材 - GetEffectiveShoppingDay() = {currentDay}");
            
            // m_SlotInstances可能还未初始化，先确保初始化
            if (m_SlotInstances == null || m_SlotInstances.Length == 0)
            {
                InitializeSlots();
            }
            
            // 强制重新加载当天的解锁食材
            m_CurrentDay = currentDay;
            m_UnlockedIngredientIds = new List<int>(IngredientUtility.GetUnlockedShelfIds(m_CurrentDay));
            // 傍晚超市预览的是"明天解锁"的食材，备菜区场景里还没有它们，
            // EnsureUnlockDefaultStock 不会被 Ingredient.Refresh 触发，这里统一补发首次解锁的默认库存（已发放过的不会重复发）
            foreach (int id in m_UnlockedIngredientIds)
            {
                IngredientUtility.EnsureUnlockDefaultStock(id);
            }

            m_TotalPages = Mathf.Max(1, Mathf.CeilToInt((float)m_UnlockedIngredientIds.Count / (m_SlotInstances?.Length ?? 8)));
            Log.Info($"[MarketPanel] 重新加载天数{m_CurrentDay}的解锁物品，总页数={m_TotalPages}，已解锁食材数={m_UnlockedIngredientIds.Count}，内容=[{string.Join(",", m_UnlockedIngredientIds)}]");
            
            SetCurrentPage(0);
            RefreshUI();
        }

        /// <summary>
        /// 在每个锚点位置下实例化 1 个槽位（共 <see cref="m_SlotPositions"/>.Length 个，通常为 8），
        /// 并根据当前天数计算总页数，最后填充第 0 页的数据。
        /// </summary>
        private void InitializeSlots()
        {
            if (m_SlotPositions == null || m_SlotPositions.Length == 0 || m_SlotPrefab == null)
            {
                Log.Error("MarketPanel 未在 Inspector 中配置 SlotPositions 或 SlotPrefab。");
                return;
            }

            m_SlotInstances = new MarketIngredientSlot[m_SlotPositions.Length];
            for (int i = 0; i < m_SlotPositions.Length; i++)
            {
                if (m_SlotPositions[i] == null)
                {
                    continue;
                }

                MarketIngredientSlot slot = Instantiate(m_SlotPrefab, m_SlotPositions[i]);
                slot.transform.localPosition = Vector3.zero;
                slot.transform.localScale = Vector3.one;
                slot.OnItemClicked += OnSlotItemClicked;
                m_SlotInstances[i] = slot;
            }

            m_CurrentDay = GetCurrentDaySafely();
            m_UnlockedIngredientIds = new List<int>(IngredientUtility.GetUnlockedShelfIds(m_CurrentDay));
            m_TotalPages = Mathf.Max(1, Mathf.CeilToInt((float)m_UnlockedIngredientIds.Count / m_SlotInstances.Length));

            SetCurrentPage(0);
        }

        /// <summary>
        /// 设置当前显示的页数（从 0 开始），用该页对应的食材数据重新填充 8 个槽位实例。
        /// </summary>
        public void SetCurrentPage(int pageIndex)
        {
            if (m_SlotInstances == null || pageIndex < 0 || pageIndex >= m_TotalPages)
            {
                return;
            }

            m_CurrentPage = pageIndex;

            int startIndex = pageIndex * m_SlotInstances.Length;
            for (int i = 0; i < m_SlotInstances.Length; i++)
            {
                MarketIngredientSlot slot = m_SlotInstances[i];
                if (slot == null)
                {
                    continue;
                }

                int ingredientIndex = startIndex + i;
                if (ingredientIndex < m_UnlockedIngredientIds.Count)
                {
                    slot.SetIngredientId(m_UnlockedIngredientIds[ingredientIndex], m_CurrentDay);
                }
                else
                {
                    slot.SetEmpty();
                }
            }

            RefreshUI();
        }

        /// <summary>
        /// 翻到上一页。
        /// </summary>
        public void PrevPage()
        {
            SetCurrentPage(m_CurrentPage - 1);
        }

        /// <summary>
        /// 翻到下一页。
        /// </summary>
        public void NextPage()
        {
            SetCurrentPage(m_CurrentPage + 1);
        }

        /// <summary>
        /// 刷新 UI 状态：更新金币显示、刷新当前 8 个槽位的购买按钮状态。
        /// </summary>
        public void RefreshUI()
        {
            UpdateGoldDisplay();

            if (m_PrevPageButton != null)
            {
                m_PrevPageButton.interactable = m_CurrentPage > 0;
            }

            if (m_NextPageButton != null)
            {
                m_NextPageButton.interactable = m_CurrentPage < m_TotalPages - 1;
            }

            if (m_SlotInstances == null)
            {
                return;
            }

            foreach (MarketIngredientSlot slot in m_SlotInstances)
            {
                if (slot != null)
                {
                    slot.RefreshButtonState();
                    slot.RefreshStockDisplay();
                }
            }
        }

        private void UpdateGoldDisplay()
        {
            if (m_GoldDisplay == null)
            {
                return;
            }

            VarInt32 goldVar = GameEntry.DataNode.GetData<VarInt32>("Player.Gold");
            int gold = goldVar != null ? goldVar.Value : 0;
            m_GoldDisplay.text = "金币: " + gold;
        }

        /// <summary>
        /// 槽位物品被点击：打开购买二级窗口，确认后走 <see cref="OnPurchaseConfirmed"/>。
        /// </summary>
        private void OnSlotItemClicked(int ingredientId)
        {
            DRIngredient row = GameEntry.DataTable.GetDataTable<DRIngredient>().GetDataRow(ingredientId);
            if (row == null)
            {
                return;
            }

            if (m_PurchasePanel == null)
            {
                Log.Error("MarketPanel 未配置 MarketPurchasePanel（二级购买窗口）。");
                return;
            }

            m_PurchasePanel.Open(row, OnPurchaseConfirmed);
        }

        /// <summary>
        /// 二级窗口确认购买：按选定数量扣金币、加库存、弹成功浮窗并刷新界面。
        /// </summary>
        private void OnPurchaseConfirmed(int ingredientId, int count)
        {
            DRIngredient row = GameEntry.DataTable.GetDataTable<DRIngredient>().GetDataRow(ingredientId);
            if (row == null || count <= 0)
            {
                return;
            }

            VarInt32 goldVar = GameEntry.DataNode.GetData<VarInt32>("Player.Gold");
            int currentGold = goldVar != null ? goldVar.Value : 0;
            int totalCost = row.ConsumeMoney * count;

            // 检查金币是否足够
            if (currentGold < totalCost)
            {
                Log.Warning("金币不足，无法购买食材 {0} x {1}（需要 {2}，当前 {3}）。", ingredientId, count, totalCost, currentGold);
                return;
            }

            // 扣金币
            int newGold = currentGold - totalCost;
            GameEntry.DataNode.SetData("Player.Gold", (VarInt32)newGold);

            // 增加库存
            IngredientUtility.AddStock(ingredientId, count);

            Log.Info("购买食材成功：{0} x {1}（花费 {2} 金币，剩余 {3}）。", row.Name, count, totalCost, newGold);

            GameToastView.Instance?.Show(ToastType.PurchaseSuccess, "购买成功");

            // 刷新 UI
            RefreshUI();
        }

        private int GetCurrentDaySafely()
        {
            VarInt32 dayVar = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value");
            if (dayVar != null)
            {
                int day = dayVar.Value;
                Log.Info($"[MarketPanel] GetCurrentDaySafely(): DayCurrent.Value = {day}");
                return day;
            }

            Log.Warning("DayCurrent.Value 尚未初始化，默认使用 Day 1。");
            return 1;
        }

        /// <summary>
        /// 计算超市货架解锁应使用的"有效天数"：
        /// - 若当前处于傍晚阶段（DayCurrent.Phase == Evening，即结算前的采购窗口），
        ///   玩家实际是在为明天做准备，因此使用 currentDay + 1；
        /// - 若处于白天阶段，直接使用当前天数。
        /// </summary>
        private int GetEffectiveShoppingDay()
        {
            int currentDay = GetCurrentDaySafely();
            int phaseValue = -1;

            if (GameEntry.DataNode != null && GameEntry.DataNode.GetNode("DayCurrent.Phase") != null)
            {
                phaseValue = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Phase").Value;
                Log.Info($"[MarketPanel] GetEffectiveShoppingDay(): DayCurrent.Phase = {phaseValue} (Day={phaseValue == (int)TimeSection.Day}, Evening={phaseValue == (int)TimeSection.Evening})");
                
                if (phaseValue == (int)TimeSection.Evening)
                {
                    int effectiveDay = currentDay + 1;
                    Log.Info($"[MarketPanel] GetEffectiveShoppingDay(): 傍晚阶段，返回 {effectiveDay}");
                    return effectiveDay;
                }
            }

            Log.Info($"[MarketPanel] GetEffectiveShoppingDay(): 白天阶段，返回 {currentDay}");
            return currentDay;
        }
        public void close()
        {
            if (uiRoot != null)
            {
                uiRoot.SetActive(false);
            }
        }
    }
}
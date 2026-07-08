using System.Collections.Generic;
using UnityEngine;
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
            InitializeSlots();
        }

        private void OnEnable()
        {
            // ★【修复】每次打开超市都重新加载当前天数的解锁食材，解决从第一天到第四天不更新的问题
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
                slot.OnPurchaseClicked += OnSlotPurchaseClicked;
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

            if (m_SlotInstances == null)
            {
                return;
            }

            foreach (MarketIngredientSlot slot in m_SlotInstances)
            {
                if (slot != null)
                {
                    slot.RefreshButtonState();
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

        private void OnSlotPurchaseClicked(int ingredientId)
        {
            DRIngredient row = GameEntry.DataTable.GetDataTable<DRIngredient>().GetDataRow(ingredientId);
            if (row == null)
            {
                return;
            }

            VarInt32 goldVar = GameEntry.DataNode.GetData<VarInt32>("Player.Gold");
            int currentGold = goldVar != null ? goldVar.Value : 0;

            // 检查金币是否足够
            if (currentGold < row.ConsumeMoney)
            {
                Log.Warning("金币不足，无法购买食材 {0}（需要 {1}，当前 {2}）。", ingredientId, row.ConsumeMoney, currentGold);
                return;
            }

            // 扣金币
            int newGold = currentGold - row.ConsumeMoney;
            GameEntry.DataNode.SetData("Player.Gold", (VarInt32)newGold);

            // 增加库存
            IngredientUtility.AddStock(ingredientId, 1);

            Log.Info("购买食材成功：{0}（花费 {1} 金币，剩余 {2}）。", row.Name, row.ConsumeMoney, newGold);

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
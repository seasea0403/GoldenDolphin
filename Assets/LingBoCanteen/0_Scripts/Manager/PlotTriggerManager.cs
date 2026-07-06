using GameFramework;
using GameFramework.DataTable;
using UnityGameFramework.Runtime;
using UnityEngine;
using System.Collections.Generic;

namespace LingBoCanteen
{
    /// <summary>
    /// 剧情触发管理器：负责检查当天是否有剧情、加载对话资源、触发对话UI
    /// 单例模式，由ProcedureGame启动时初始化
    /// 从Day表读取PlotId，然后通过PlotIdMapping找到对话资源
    /// </summary>
    public class PlotTriggerManager : MonoBehaviour
    {
        private struct PlotInfo
        {
            public string dialogueAssetName;
            public string portraitAssetName;

            public PlotInfo(string dialogue, string portrait)
            {
                dialogueAssetName = dialogue;
                portraitAssetName = portrait;
            }
        }

        // PlotId映射表：PlotId -> (对话资源名, 剧情人物立绘资源名)
        private static readonly Dictionary<int, PlotInfo> PlotIdMapping = new Dictionary<int, PlotInfo>()
        {
            // 现在只使用 DRGuest 的 Id 来解析剧情顾客立绘（字符串形式），对应 DataTables/Guest.txt 中的条目：
            // 2001 -> PlotCustomer_Celebrity
            // 2002 -> PlotCustomer_Deserter
            // 2003 -> PlotCustomer_Genius
            { 1, new PlotInfo("Day1", "2003") },
            { 2, new PlotInfo("Day4", "2001") },
            { 3, new PlotInfo("Day5", "2003") },
            { 4, new PlotInfo("Day9", "2002") },
            { 5, new PlotInfo("Day12", "2001") },
            { 6, new PlotInfo("Day15", "2002") },
            { 7, new PlotInfo("Day16", "") }, // 无专属NPC，首日顾客不替换立绘
            { 8, new PlotInfo("Day17", "2001") },
            { 9, new PlotInfo("Day18", "2002") },
            { 10, new PlotInfo("Day19", "2003") },
        };

        public static PlotTriggerManager Instance { get; private set; }

        // 当天剧情信息
        private int m_TodayPlotId = 0;
        private string m_TodayPlotCharacterId = "";  // 剧情人物的立绘资源名
        private DialogueAsset m_TodayDialogueAsset;
        private bool m_IsPlayingPlot = false;
        private int m_CurrentDayNumber = -1;

        // 回调：当对话完成时触发
        public event System.Action OnPlotDialogueComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 检查并播放当天的剧情（如果有的话）
        /// </summary>
        public void CheckAndPlayPlotForDay(int dayNumber)
        {
            m_CurrentDayNumber = dayNumber;

            // 从Day表获取PlotId
            int plotId = GetPlotIdFromDay(dayNumber);
            if (plotId == 0)
            {
                Log.Info($"Day {dayNumber} 没有剧情（PlotId=0），直接进入正常流程");
                m_IsPlayingPlot = false;
                m_TodayPlotId = 0;
                m_TodayPlotCharacterId = "";
                OnPlotDialogueComplete?.Invoke();
                return;
            }

            m_TodayPlotId = plotId;

            // 不再检查剧情是否已完成，始终播放对应剧情

            // 从映射表查找对话资源名和人物ID
            if (!PlotIdMapping.TryGetValue(plotId, out var plotInfo))
            {
                Log.Error($"PlotId {plotId} not found in PlotIdMapping!");
                m_IsPlayingPlot = false;
                OnPlotDialogueComplete?.Invoke();
                return;
            }

            string dialogueAssetName = plotInfo.dialogueAssetName;
            string portraitSpecifier = plotInfo.portraitAssetName;

            // ★【新增】第17天时，根据HasMovedBeforeDay15标志选择不同的对话
            if (dayNumber == 17)
            {
                bool hasMovedBeforeDay15 = GameEntry.DataNode.GetNode("Area.HasMovedBeforeDay15") != null
                    && GameEntry.DataNode.GetData<VarBoolean>("Area.HasMovedBeforeDay15").Value;
                
                if (!hasMovedBeforeDay15)
                {
                    // 隐藏路线：加载Day16_2
                    dialogueAssetName = "Day16_2";
                    Log.Info($"[Day17 Plot] 进入隐藏路线，加载对话: {dialogueAssetName}");
                }
                else
                {
                    // 正常路线：加载Day16_1
                    dialogueAssetName = "Day16_1";
                    Log.Info($"[Day17 Plot] 正常路线，加载对话: {dialogueAssetName}");
                }
            }

            // 仅支持通过 DRGuest 的数字 Id 来解析立绘资源名。
            // 非数字的 portraitSpecifier 将被忽略以避免使用硬编码资源名。
            m_TodayPlotCharacterId = string.Empty;
            if (!string.IsNullOrEmpty(portraitSpecifier))
            {
                if (int.TryParse(portraitSpecifier, out int guestId) && guestId > 0)
                {
                    // 从 DRGuest 表中查找对应 Id 的 AssetName
                    IDataTable<DRGuest> guestTable = GameEntry.DataTable.GetDataTable<DRGuest>();
                    if (guestTable != null)
                    {
                        DRGuest guestRow = guestTable.GetDataRow(guestId);
                        if (guestRow != null && !string.IsNullOrEmpty(guestRow.AssetName))
                        {
                            m_TodayPlotCharacterId = guestRow.AssetName;
                            Log.Info($"PlotTriggerManager: resolved portrait from DRGuest id {guestId} -> {m_TodayPlotCharacterId}");
                        }
                        else
                        {
                            Log.Warning($"PlotTriggerManager: DRGuest id {guestId} not found or has empty AssetName. portraitSpecifier ignored.");
                        }
                    }
                    else
                    {
                        Log.Warning("PlotTriggerManager: DRGuest data table not loaded. Cannot resolve guest id to AssetName.");
                    }
                }
                else
                {
                    Log.Info($"PlotTriggerManager: portrait specifier '{portraitSpecifier}' is not a numeric DRGuest id; ignoring per ID-only policy.");
                }
            }

            // 加载对话资源
            m_TodayDialogueAsset = Resources.Load<DialogueAsset>($"DialogueAssets/{dialogueAssetName}");
            if (m_TodayDialogueAsset == null)
            {
                Log.Error($"Failed to load DialogueAsset: DialogueAssets/{dialogueAssetName}");
                m_IsPlayingPlot = false;
                OnPlotDialogueComplete?.Invoke();
                return;
            }

            // ★ 【新增】检查对话是否为空（没有有效的对话行）
            if (m_TodayDialogueAsset.lines == null || m_TodayDialogueAsset.lines.Count == 0)
            {
                Log.Warning($"DialogueAsset '{dialogueAssetName}' is empty (no dialogue lines). Skip dialogue and proceed normally.");
                m_IsPlayingPlot = false;
                OnPlotDialogueComplete?.Invoke();
                return;
            }

            m_IsPlayingPlot = true;

            // 直接获取场景中的DialogueFormLogic并播放对话
            DialogueFormLogic dialogueForm = FindObjectOfType<DialogueFormLogic>();
            if (dialogueForm == null)
            {
                Log.Error("DialogueFormLogic not found in scene!");
                m_IsPlayingPlot = false;
                OnPlotDialogueComplete?.Invoke();
                return;
            }

            // 直接播放对话
            dialogueForm.PlayDialogue(m_TodayDialogueAsset, () =>
            {
                OnDialoguePlayComplete();
            });
        }

        /// <summary>
        /// 从Day表获取PlotId
        /// </summary>
        private int GetPlotIdFromDay(int dayNumber)
        {
            IDataTable<DRDay> dayTable = GameEntry.DataTable.GetDataTable<DRDay>();
            if (dayTable == null)
            {
                Log.Error("DRDay table not found!");
                return 0;
            }

            DRDay dayRow = dayTable.GetDataRow(dayNumber);
            if (dayRow == null)
            {
                Log.Warning($"DRDay row not found for day {dayNumber}");
                return 0;
            }

            return dayRow.PlotId;
        }

        /// <summary>
        /// 对话播放完成
        /// </summary>
        private void OnDialoguePlayComplete()
        {
            // 不再标记剧情为已完成，允许同一剧情多次触发（如需其他行为，可在此扩展）
            m_IsPlayingPlot = false;

            // 触发回调：进入正常游戏流程
            OnPlotDialogueComplete?.Invoke();
        }

        /// <summary>
        /// 获取当天的剧情人物ID（用于客人立绘绑定）
        /// </summary>
        public string GetTodayPlotCharacterId()
        {
            return m_TodayPlotCharacterId;
        }

        /// <summary>
        /// 外部调用：使用 DRGuest 的 Id 设置当天剧情立绘（解析为 AssetName）。
        /// 传入 guestId <= 0 会被忽略。
        /// </summary>
        public void SetTodayPlotGuestById(int guestId)
        {
            if (guestId <= 0)
            {
                Log.Warning($"SetTodayPlotGuestById called with invalid id: {guestId}");
                return;
            }

            IDataTable<DRGuest> guestTable = GameEntry.DataTable.GetDataTable<DRGuest>();
            if (guestTable == null)
            {
                Log.Warning("SetTodayPlotGuestById: DRGuest table not loaded; cannot resolve id to AssetName.");
                return;
            }

            DRGuest guestRow = guestTable.GetDataRow(guestId);
            if (guestRow == null)
            {
                Log.Warning($"SetTodayPlotGuestById: DRGuest id {guestId} not found.");
                return;
            }

            if (string.IsNullOrEmpty(guestRow.AssetName))
            {
                Log.Warning($"SetTodayPlotGuestById: DRGuest id {guestId} has empty AssetName.");
                return;
            }

            m_TodayPlotCharacterId = guestRow.AssetName;
            Log.Info($"SetTodayPlotGuestById: Set today's plot character to DRGuest id {guestId} -> {m_TodayPlotCharacterId}");
        }

        /// <summary>
        /// 是否正在播放剧情
        /// </summary>
        public bool IsPlayingPlot()
        {
            return m_IsPlayingPlot;
        }

        // 已移除剧情完成检测与标记逻辑，以便同一剧情可多次触发（如果需要持久化控制，请在外部实现）
    }
}

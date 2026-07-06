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
            { 1, new PlotInfo("Day1", "PlotCustomer_Genius") },
            { 2, new PlotInfo("Day4", "PlotCustomer_celebrity") },
            { 3, new PlotInfo("Day5", "PlotCustomer_Genius") },
            // 在这里继续添加其他剧情映射
            // 格式：{ PlotId, new PlotInfo(DialogueAssetName, PortraitAssetName) }
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

            // 检查是否已经完成过该剧情
            string plotKey = $"Plot_{plotId}";
            if (HasFinishedPlot(plotKey))
            {
                Log.Info($"Day {dayNumber} 的剧情（PlotId={plotId}）已完成过，不再显示");
                m_IsPlayingPlot = false;
                OnPlotDialogueComplete?.Invoke();
                return;
            }

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

            // 支持两种写法：
            // 1) 直接写立绘资源名（例如 "PlotCustomer_Genius"）——保持原有行为；
            // 2) 写成数字字符串（例如 "5"），表示 DRGuest 表中的 Id，那么从 DRGuest 表取对应的 AssetName。
            m_TodayPlotCharacterId = portraitSpecifier; // 默认值
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
                            Log.Warning($"PlotTriggerManager: DRGuest id {guestId} not found or has empty AssetName. Using specifier '{portraitSpecifier}' as-is.");
                        }
                    }
                    else
                    {
                        Log.Warning("PlotTriggerManager: DRGuest data table not loaded. Cannot resolve guest id to AssetName.");
                    }
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
            if (m_TodayPlotId > 0)
            {
                // 标记剧情为已完成
                string plotKey = $"Plot_{m_TodayPlotId}";
                MarkPlotAsFinished(plotKey);
            }

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

        /// <summary>
        /// 检查剧情是否已完成
        /// </summary>
        private bool HasFinishedPlot(string plotKey)
        {
            // 先检查节点是否存在，避免类型转换异常
            if (GameEntry.DataNode.GetNode("Story.FinishedPlotIdList") == null)
            {
                return false;
            }

            try
            {
                var finishedList = GameEntry.DataNode.GetData<VarString>("Story.FinishedPlotIdList");
                if (finishedList == null || string.IsNullOrEmpty(finishedList.Value))
                    return false;

                var plotIds = finishedList.Value.Split(',');
                foreach (var id in plotIds)
                {
                    if (id.Trim() == plotKey)
                        return true;
                }
            }
            catch (System.Exception e)
            {
                Log.Warning($"Error reading finished plots: {e.Message}");
            }
            return false;
        }

        /// <summary>
        /// 标记剧情为已完成
        /// </summary>
        private void MarkPlotAsFinished(string plotKey)
        {
            string currentValue = "";

            try
            {
                // 先检查节点是否存在
                if (GameEntry.DataNode.GetNode("Story.FinishedPlotIdList") != null)
                {
                    var finishedList = GameEntry.DataNode.GetData<VarString>("Story.FinishedPlotIdList");
                    currentValue = finishedList?.Value ?? "";
                }
            }
            catch (System.Exception e)
            {
                Log.Warning($"Error reading finished plots: {e.Message}");
                currentValue = "";
            }

            if (string.IsNullOrEmpty(currentValue))
            {
                GameEntry.DataNode.SetData("Story.FinishedPlotIdList", (VarString)plotKey);
            }
            else
            {
                string newValue = currentValue + "," + plotKey;
                GameEntry.DataNode.SetData("Story.FinishedPlotIdList", (VarString)newValue);
            }

            Log.Info($"✅ 剧情 {plotKey} 标记为已完成");
        }
    }
}

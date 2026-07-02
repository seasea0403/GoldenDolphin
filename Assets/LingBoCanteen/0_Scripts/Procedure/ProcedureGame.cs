using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 游戏主流程。
    /// 职责：加载游戏主场景，驱动"市场→营业→结算"日内阶段循环（20天），
    ///       检测怀疑剧情条件，处理游戏结束。
    /// </summary>
    public class ProcedureGame : ProcedureBase
    {
        // ─────────── 日内阶段枚举 ───────────
        public enum DayPhase
        {
            Market      = 0,    // 购买食材
            Business    = 1,    // 营业主玩法
            Story       = 2,    // 剧情检测（可选，时长极短）
            Settlement  = 3,    // 当日结算
        }

        private bool m_SceneLoaded = false;

        // 当前日阶段，通过事件驱动推进
        private DayPhase m_CurrentPhase = DayPhase.Market;

        // 结算完成标志，由 UIDaySettlement 关闭时触发事件置位
        private bool m_SettlementDone = false;

        // 游戏结束标志
        private bool m_GameOver = false;
        private bool m_IsHiddenEnding = false;

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            m_SceneLoaded     = false;
            m_SettlementDone  = false;
            m_GameOver        = false;
            m_CurrentPhase    = DayPhase.Market;

            // ── 订阅场景事件 ──
            GameEntry.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnSceneLoaded);
            GameEntry.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnSceneFailed);

            // ── 订阅阶段推进事件（由各 Manager / UI 触发） ──
            GameEntry.Event.Subscribe(EventId.PhaseMarketEnd,      OnMarketEnd);
            GameEntry.Event.Subscribe(EventId.PhaseBusinessEnd,    OnBusinessEnd);
            GameEntry.Event.Subscribe(EventId.PhaseSettlementEnd,  OnSettlementEnd);

            // ── 订阅游戏结束事件 ──
            GameEntry.Event.Subscribe(EventId.GameOver, OnGameOver);

            // 加载主游戏场景
            GameEntry.Scene.LoadScene(AssetUtility.GetSceneAsset("Main"), this);
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!m_SceneLoaded) return;

            if (m_GameOver)
            {
                // 打开对应结局界面后不再驱动日循环
                return;
            }

            // 结算阶段完成 → 推进到下一天或触发结局
            if (m_CurrentPhase == DayPhase.Settlement && m_SettlementDone)
            {
                m_SettlementDone = false;
                AdvanceToNextDay();
            }
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnSceneLoaded);
            GameEntry.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnSceneFailed);
            GameEntry.Event.Unsubscribe(EventId.PhaseMarketEnd,     OnMarketEnd);
            GameEntry.Event.Unsubscribe(EventId.PhaseBusinessEnd,   OnBusinessEnd);
            GameEntry.Event.Unsubscribe(EventId.PhaseSettlementEnd, OnSettlementEnd);
            GameEntry.Event.Unsubscribe(EventId.GameOver, OnGameOver);

            GameEntry.UI.CloseAllLoadedUIForms();
            base.OnLeave(procedureOwner, isShutdown);
        }

        // ─────────── 场景加载回调 ───────────

        private void OnSceneLoaded(object sender, GameEventArgs e)
        {
            LoadSceneSuccessEventArgs args = (LoadSceneSuccessEventArgs)e;
            if (args.UserData != this) return;

            m_SceneLoaded = true;
            Log.Info("Game scene loaded.");

            // 打开常驻 HUD
            GameEntry.UI.OpenUIForm(AssetUtility.GetUIFormAsset(UIFormId.UIHud), "UI");

            // 启动第一天
            StartDay();
        }

        private void OnSceneFailed(object sender, GameEventArgs e)
        {
            LoadSceneFailureEventArgs args = (LoadSceneFailureEventArgs)e;
            if (args.UserData != this) return;

            Log.Fatal("Load game scene failure: {0}", args.ErrorMessage);
        }

        // ─────────── 日循环驱动 ───────────

        /// <summary>
        /// 开始当天：通知 DayManager 解锁食材/配方，进入市场阶段。
        /// </summary>
        private void StartDay()
        {
            // 重置当日营业数据节点
            GameEntry.DataNode.SetData("Business.TodayRevenue",            (VarInt32)0);
            GameEntry.DataNode.SetData("Business.TodayServeCustomerCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.WaitingCustomerNum",      (VarInt32)0);
            GameEntry.DataNode.SetData("Business.HasUnservedOrder",        (VarBoolean)false);
            GameEntry.DataNode.SetData("DayCurrent.IsDaySettled",         (VarBoolean)false);

            SetPhase(DayPhase.Market);

            // 触发 DayStart 事件，由 DayManager 处理食材解锁、剧情 NPC 安排等
            GameEntry.Event.Fire(this, DayStartEventArgs.Create(
                GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value").Value));

            // 打开市场购买界面
            GameEntry.UI.OpenUIForm(AssetUtility.GetUIFormAsset(UIFormId.UIMarket), "UI");
        }

        /// <summary>
        /// 每天结算完成后推进：天数+1，检测结局条件，或进入下一天。
        /// </summary>
        private void AdvanceToNextDay()
        {
            int currentDay = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value").Value;
            bool isHiddenPlot   = GameEntry.DataNode.GetData<VarBoolean>("Story.Suspect.TriggerHiddenPlot").Value;
            bool plotInterrupt  = GameEntry.DataNode.GetData<VarBoolean>("Story.Suspect.PlotInterrupt").Value;
            bool allStoryFinish = GameEntry.DataNode.GetData<VarBoolean>("Story.IsAllStoryFinish").Value;

            // Day20 完成 → 判断结局
            if (currentDay >= GameConst.TotalDays)
            {
                bool hiddenEnding = isHiddenPlot && !plotInterrupt && allStoryFinish;
                TriggerGameOver(hiddenEnding);
                return;
            }

            // 正常推进
            GameEntry.DataNode.SetData("DayCurrent.Value", (VarInt32)(currentDay + 1));
            StartDay();
        }

        private void TriggerGameOver(bool isHiddenEnding)
        {
            m_GameOver       = true;
            m_IsHiddenEnding = isHiddenEnding;
            GameEntry.Event.Fire(this, GameOverEventArgs.Create(isHiddenEnding));
            GameEntry.UI.OpenUIForm(AssetUtility.GetUIFormAsset(UIFormId.UIGameOver), "UI");
        }

        private void SetPhase(DayPhase phase)
        {
            m_CurrentPhase = phase;
            GameEntry.DataNode.SetData("DayCurrent.Phase", (VarInt32)(int)phase);
        }

        // ─────────── 阶段推进事件回调 ───────────

        // 玩家关闭市场界面 → 进入营业阶段
        private void OnMarketEnd(object sender, GameEventArgs e)
        {
            if (m_CurrentPhase != DayPhase.Market) return;

            SetPhase(DayPhase.Business);

            // 通知 CustomerSpawner 等系统开始营业
            GameEntry.Event.Fire(this, BusinessStartEventArgs.Create(
                GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value").Value));

            // 打开订单板 UI
            GameEntry.UI.OpenUIForm(AssetUtility.GetUIFormAsset(UIFormId.UIOrderBoard), "UI");
        }

        // 所有顾客离开（CustomerManager 触发）→ 进入剧情检测/结算
        private void OnBusinessEnd(object sender, GameEventArgs e)
        {
            if (m_CurrentPhase != DayPhase.Business) return;

            // 关闭订单板
            GameEntry.UI.CloseUIForm(UIFormId.UIOrderBoard);

            // 检查是否有待触发的剧情
            int day        = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value").Value;
            bool hasMoved  = GameEntry.DataNode.GetData<VarBoolean>("Area.HasMovedBeforeDay15").Value;

            // Day16-20 且前15天未移动 → 进剧情阶段，由 StoryManager 处理
            bool storyTriggered = (day >= GameConst.SuspectStartDay) && !hasMoved;

            if (storyTriggered)
            {
                SetPhase(DayPhase.Story);
                GameEntry.Event.Fire(this, StoryPhaseStartEventArgs.Create(day));
                // StoryManager 处理完剧情后会 Fire PhaseSettlementEnd
            }
            else
            {
                // 跳过剧情直接结算
                BeginSettlement();
            }
        }

        // 结算界面关闭 → OnUpdate 中驱动下一天
        private void OnSettlementEnd(object sender, GameEventArgs e)
        {
            if (m_CurrentPhase != DayPhase.Settlement) return;

            GameEntry.DataNode.SetData("DayCurrent.IsDaySettled", (VarBoolean)true);
            m_SettlementDone = true;
        }

        private void OnGameOver(object sender, GameEventArgs e)
        {
            m_GameOver = true;
        }

        // ─────────── 公开方法（供 StoryManager 调用）───────────

        /// <summary>
        /// 剧情阶段完成，由 StoryManager 调用后进入结算。
        /// </summary>
        public void OnStoryPhaseDone()
        {
            if (m_CurrentPhase != DayPhase.Story) return;
            BeginSettlement();
        }

        private void BeginSettlement()
        {
            SetPhase(DayPhase.Settlement);
            GameEntry.UI.OpenUIForm(AssetUtility.GetUIFormAsset(UIFormId.UIDaySettlement), "UI");
        }
    }
}

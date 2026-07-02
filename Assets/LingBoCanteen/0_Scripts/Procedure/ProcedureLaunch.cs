using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 启动流程。
    /// 职责：初始化 DataNode 默认值，加载 DefaultConfig，完成后跳转 Preload。
    /// </summary>
    public class ProcedureLaunch : ProcedureBase
    {
        private bool m_ConfigLoaded = false;

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            // 订阅 Config 加载事件
            GameEntry.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnConfigLoaded);
            GameEntry.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnConfigFailed);

            // 初始化所有 DataNode 默认值
            InitDataNodes();

            // 加载默认配置表
            GameEntry.Config.ReadData(AssetUtility.GetConfigAsset("DefaultConfig"));
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_ConfigLoaded)
            {
                ChangeState<ProcedurePreload>(procedureOwner);
            }
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnConfigLoaded);
            GameEntry.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnConfigFailed);
            base.OnLeave(procedureOwner, isShutdown);
        }

        private void InitDataNodes()
        {
            // 玩家永久属性
            GameEntry.DataNode.SetData("Player.San", (VarInt32)GameConst.InitSan);
            GameEntry.DataNode.SetData("Player.Gold", (VarInt32)GameConst.InitGold);
            GameEntry.DataNode.SetData("Player.HaveRecruitHelper", (VarBoolean)false);

            // 当前天数
            GameEntry.DataNode.SetData("DayCurrent.Value", (VarInt32)1);
            GameEntry.DataNode.SetData("DayCurrent.Phase", (VarInt32)0); // 0=市场 1=营业 2=结算
            GameEntry.DataNode.SetData("DayCurrent.IsDaySettled", (VarBoolean)false);

            // 区域/电梯
            GameEntry.DataNode.SetData("Area.CurrentType", (VarInt32)0); // 0=人间
            GameEntry.DataNode.SetData("Area.HasMovedBeforeDay15", (VarBoolean)false);
            GameEntry.DataNode.SetData("Area.ElevatorUsedOnce", (VarBoolean)false);

            // 当日营业（每天重置）
            GameEntry.DataNode.SetData("Business.TodayRevenue", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.TodayServeCustomerCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.WaitingCustomerNum", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.HasUnservedOrder", (VarBoolean)false);

            // 库存
            GameEntry.DataNode.SetData("Storage.SeasoningLeftCount", (VarInt32)99);

            // 剧情 NPC 对话次数
            GameEntry.DataNode.SetData("Story.NPC.GeniusTalkCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.NPC.VIPTalkCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.NPC.DeserterTalkCount", (VarInt32)0);

            // 怀疑/剧情状态
            GameEntry.DataNode.SetData("Story.Suspect.CardNum", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.Suspect.TriggerHiddenPlot", (VarBoolean)false);
            GameEntry.DataNode.SetData("Story.Suspect.PlotInterrupt", (VarBoolean)false);
            GameEntry.DataNode.SetData("Story.PlotStage", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.IsAllStoryFinish", (VarBoolean)false);
        }

        private void OnConfigLoaded(object sender, GameEventArgs e)
        {
            LoadConfigSuccessEventArgs args = (LoadConfigSuccessEventArgs)e;
            Log.Info("Config loaded: {0}", args.ConfigAssetName);
            m_ConfigLoaded = true;
        }

        private void OnConfigFailed(object sender, GameEventArgs e)
        {
            LoadConfigFailureEventArgs args = (LoadConfigFailureEventArgs)e;
            Log.Fatal("Load config failure, asset name '{0}', error message '{1}'.",
                args.ConfigAssetName, args.ErrorMessage);
        }
    }
}

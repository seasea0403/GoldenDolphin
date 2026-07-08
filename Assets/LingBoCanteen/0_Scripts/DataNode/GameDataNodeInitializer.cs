using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 游戏数据节点初始化器
    /// 新档启动时初始化所有根节点的初始值，完全对齐UGF DataNode组件规范
    /// </summary>
    public class GameDataNodeInitializer : MonoBehaviour
    {
        private void Start()
        {
            // Initialization moved to ProcedureLaunch to guarantee GameEntry is ready.
        }

        /// <summary>
        /// 初始化所有需要的 DataNode 数据，供 Procedure 在合适时机调用。
        /// </summary>
        public static void Initialize()
        {
            // 1. 玩家永久属性（存档项）
            GameEntry.DataNode.SetData("Player.San", (VarInt32)50);
            GameEntry.DataNode.SetData("Player.Gold", (VarInt32)Constant.GameConstant.INITIAL_GOLD);
            GameEntry.DataNode.SetData("Player.HasRecruitHelper", (VarBoolean)false);

            // 2. 当前天数与时段状态
            GameEntry.DataNode.SetData("DayCurrent.Value", (VarInt32)1);
            GameEntry.DataNode.SetData("DayCurrent.IsDaySettled", (VarBoolean)false);
            GameEntry.DataNode.SetData("DayCurrent.Phase", (VarInt32)(int)TimeSection.Day);
            // 注：Phase 对应 TimeSection 枚举：0=白天 1=傍晚

            // 兼容处理：确保 DayCurrent.Phase 为合法值（迁移旧存档中可能存在的 2->Evening）
            var _phaseNode = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Phase");
            if (_phaseNode != null)
            {
                int _raw = _phaseNode.Value;
                int _san = _raw == (int)TimeSection.Day ? (int)TimeSection.Day : (int)TimeSection.Evening;
                if (_san != _raw)
                {
                    GameEntry.DataNode.SetData("DayCurrent.Phase", (VarInt32)_san);
                    Log.Info($"[Init] 迁移旧 Phase 值 {_raw} -> {_san}");
                }
            }

            // 3. 区域与电梯状态（存档项）
            GameEntry.DataNode.SetData("Area.CurrentType", (VarInt32)(int)GameRegion.Mortal);
            // 注：CurrentType 对应 GameRegion 枚举：1=人间 2=天堂 3=地狱 4=人间?
            GameEntry.DataNode.SetData("Area.HasMovedBeforeDay15", (VarBoolean)false);
            GameEntry.DataNode.SetData("Area.ElevatorUsedOnce", (VarBoolean)false);
            GameEntry.DataNode.SetData("Area.IsSuspicionMode", (VarBoolean)false);

            // 4. 当日营业临时数据（每日凌晨重置，不存档）
            GameEntry.DataNode.SetData("Business.TodayServeCustomerCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.TodayFailedCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.TodayTotalGuestCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.WaitingCustomerNum", (VarInt32)2);
            GameEntry.DataNode.SetData("Business.HasUnservedOrder", (VarBoolean)false);
            GameEntry.DataNode.SetData("Business.TodayEarnGold", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.TodaySanDelta", (VarInt32)0);

            // 5. 库存数据（存档项）
            // 5.1 食材库存 <食材ID, 持有数量>
            Dictionary<int, int> ingredientStock = new Dictionary<int, int>();
            VarObject ingredientStockVar = ReferencePool.Acquire<VarObject>();
            ingredientStockVar.Value = ingredientStock;
            GameEntry.DataNode.SetData("Storage.IngredientStockDict", ingredientStockVar);

            // 5.2 调料库存 <调料ID, 持有数量>
            Dictionary<int, int> seasoningStock = new Dictionary<int, int>();
            VarObject seasoningStockVar = ReferencePool.Acquire<VarObject>();
            seasoningStockVar.Value = seasoningStock;
            GameEntry.DataNode.SetData("Storage.SeasoningStockDict", seasoningStockVar);

            // 6. 剧情与NPC进度（存档项）
            // 6.1 NPC好感对话次数
            GameEntry.DataNode.SetData("Story.NPC.GeniusTalkCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.NPC.VIPTalkCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.NPC.DeserterTalkCount", (VarInt32)0);

            // 6.2 怀疑线进度
            GameEntry.DataNode.SetData("Story.Suspect.CardNum", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.Suspect.TriggerHiddenPlot", (VarBoolean)false);
            GameEntry.DataNode.SetData("Story.Suspect.PlotInterrupt", (VarBoolean)false);

            // 6.3 全局剧情进度
            GameEntry.DataNode.SetData("Story.PlotStage", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.IsAllStoryFinish", (VarBoolean)false);
            
            // 已完成剧情ID列表
            List<int> finishedPlotIds = new List<int>();
            VarObject finishedPlotVar = ReferencePool.Acquire<VarObject>();
            finishedPlotVar.Value = finishedPlotIds;
            GameEntry.DataNode.SetData("Story.FinishedPlotIdList", finishedPlotVar);

            // 7. 游戏设置（存档项）
            GameEntry.DataNode.SetData("Settings.BGMVolume", (VarSingle)1f);
            GameEntry.DataNode.SetData("Settings.SFXVolume", (VarSingle)1f);
            GameEntry.DataNode.SetData("Settings.IsFullScreen", (VarBoolean)true);
        }
    }
}
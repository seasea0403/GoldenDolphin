using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 傍晚阶段结束后推进到下一天的公共流程：递增天数、重置当日营业临时数据、
    /// 切回白天时段与订单区域、刷新货架食材解锁状态。
    /// 由 <see cref="SettlePanel"/> 在"确定"按钮的两条分支（触及San边界/未触及）里共同调用。
    /// </summary>
    public static class EveningDayFlow
    {
        public static void AdvanceToNextDay()
        {
            if (GameEntry.DataNode == null)
            {
                return;
            }

            int currentDay = GameEntry.DataNode.GetNode("DayCurrent.Value") != null
                ? GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value").Value
                : 1;
            int nextDay = currentDay + 1;

            // 检查是否达到第20天（游戏结束）
            if (currentDay >= Constant.GameConstant.MAX_DAY)
            {
                // 触发游戏结局
                TriggerGameEnding();
                return;
            }

            GameEntry.DataNode.SetData("DayCurrent.Value", (VarInt32)nextDay);
            GameEntry.DataNode.SetData("DayCurrent.IsDaySettled", (VarBoolean)false);
            GameEntry.DataNode.SetData("DayCurrent.Phase", (VarInt32)(int)TimeSection.Day);

            // ★【新增】检查SAN值是否超出Mortal区间，自动切换region
            CheckAndUpdateRegionBySAN();

            // ★【新增】进入第16天时，检查HasMovedBeforeDay15标志
            if (nextDay == 16)
            {
                CheckDay15Transition();
            }

            // 重置当日营业临时数据（不涉及存档项：Player/Area/Storage/Story/Settings）
            GameEntry.DataNode.SetData("Business.TodayServeCustomerCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.TodayFailedCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.TodayTotalGuestCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.HasUnservedOrder", (VarBoolean)false);
            GameEntry.DataNode.SetData("Business.TodayEarnGold", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.TodaySanDelta", (VarInt32)0);

            // 重新初始化顾客（清理旧顾客，加载新一天的顾客）
            if (CustomerSlotManager.Instance != null)
            {
                CustomerSlotManager.Instance.ReinitializeDaily();
            }

            // 重置烹调区所有锅具的状态
            if (KitchenManager.Instance != null)
            {
                KitchenManager.Instance.ResetAllPots();
            }

            // 重置备菜区所有工具的状态
            PrepDragController.ResetAllStations();

            // 新的一天可能解锁新食材，刷新货架/冰箱锁定与库存展示
            if (PrepAreaManager.Instance != null)
            {
                PrepAreaManager.Instance.RefreshAllShelfIngredients();
            }

            // 在切回白天前，确保剧情系统为新的一天准备并播放（如果有的话）
            if (PlotTriggerManager.Instance == null)
            {
                GameObject plotManagerObj = new GameObject("PlotTriggerManager");
                plotManagerObj.transform.SetParent(null);
                plotManagerObj.AddComponent<PlotTriggerManager>();
            }

            PlotTriggerManager.Instance?.CheckAndPlayPlotForDay(nextDay);

            // 切回白天订单区域（会自动关闭傍晚区域的 UI 与世界物体）
            if (AreaSwitchManager.Instance != null)
            {
                AreaSwitchManager.Instance.SwitchToArea(AreaSwitchManager.AreaType.Order);
            }

            // ★【新增】天数改变时，强制重新播放背景音乐
            // 确保即使是同一首音乐，也会重新播放（例如从傍晚回到白天时的音乐更新）
            SoundManager.Instance?.ForceReplayMusicForCurrentGameState();
        }

        /// <summary>
        /// 检查SAN值是否超出Mortal区间，自动切换region。
        /// 当SAN >= SAN_MORTAL_MAX 时切换到Heaven，当SAN <= SAN_MORTAL_MIN 时切换到Hell。
        /// 仅切换区域，不重置SAN值。
        /// </summary>
        private static void CheckAndUpdateRegionBySAN()
        {
            if (GameEntry.DataNode == null)
            {
                return;
            }

            // 只对Mortal区域进行检查（Heaven和Hell有各自的SAN边界管理逻辑）
            var regionData = GameEntry.DataNode.GetNode("Area.CurrentType");
            if (regionData == null)
            {
                return;
            }

            int currentRegion = GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value;
            if (currentRegion != (int)GameRegion.Mortal)
            {
                return; // 只在Mortal区域进行自动切换
            }

            // 读取当前SAN值
            int currentSan = GameEntry.DataNode.GetNode("Player.San") != null
                ? GameEntry.DataNode.GetData<VarInt32>("Player.San").Value
                : Constant.GameConstant.INITIAL_SAN;

            // 判断是否需要切换region
            GameRegion newRegion = GameRegion.Mortal;

            if (currentSan >= Constant.GameConstant.SAN_MORTAL_MAX)
            {
                // SAN超过上界，自动升天
                newRegion = GameRegion.Heaven;
                Debug.Log($"[SAN AutoSwitch] SAN={currentSan} >= {Constant.GameConstant.SAN_MORTAL_MAX}, 自动切换到Heaven");
            }
            else if (currentSan <= Constant.GameConstant.SAN_MORTAL_MIN)
            {
                // SAN低于下界，自动下地狱
                newRegion = GameRegion.Hell;
                Debug.Log($"[SAN AutoSwitch] SAN={currentSan} <= {Constant.GameConstant.SAN_MORTAL_MIN}, 自动切换到Hell");
            }

            // 应用region变化（不重置SAN值）
            if (newRegion != GameRegion.Mortal)
            {
                GameEntry.DataNode.SetData("Area.CurrentType", (VarInt32)(int)newRegion);
                GameEntry.DataNode.SetData("Area.ElevatorUsedOnce", (VarBoolean)true); // 标记曾改变过Region
            }
        }

        /// <summary>
        /// 触发游戏结局（第20天结束）。
        /// 仅在数据中标记游戏已结束，由场景中的 GameEndingManager 自己检测并处理
        /// </summary>
        private static void TriggerGameEnding()
        {
            if (GameEntry.DataNode == null)
            {
                return;
            }

            // 标记游戏已结束
            GameEntry.DataNode.SetData("Game.IsEnding", (VarBoolean)true);
            
            // 记录是否曾在第15天之前改变过Region，用于GameEndingManager判断是TrueEnding还是NormalEnding
            bool hasMovedBeforeDay15 = GameEntry.DataNode.GetNode("Area.HasMovedBeforeDay15") != null
                && GameEntry.DataNode.GetData<VarBoolean>("Area.HasMovedBeforeDay15").Value;
            GameEntry.DataNode.SetData("Game.HasChangedRegion", (VarBoolean)hasMovedBeforeDay15);

            Debug.Log($"[EveningDayFlow] 游戏结束触发，第15天前曾改变Region: {hasMovedBeforeDay15}");
        }

        /// <summary>
        /// 进入第16天时的检查：如果HasMovedBeforeDay15为false，则激活怀疑线。
        /// 对话内容将在第17天由PlotTriggerManager根据路线选择Day16_1或Day16_2显示。
        /// </summary>
        private static void CheckDay15Transition()
        {
            if (GameEntry.DataNode == null)
            {
                return;
            }

            bool hasMovedBeforeDay15 = GameEntry.DataNode.GetNode("Area.HasMovedBeforeDay15") != null
                && GameEntry.DataNode.GetData<VarBoolean>("Area.HasMovedBeforeDay15").Value;

            if (!hasMovedBeforeDay15)
            {
                Debug.Log("[Day15 Transition] 检测到玩家第15天前未改变过区域，进入隐藏路线...");
                
                // 显示怀疑线（settlement面板的Suspect Root）
                if (GameEntry.DataNode.GetNode("Story.Suspect.PlotInterrupt") != null)
                {
                    GameEntry.DataNode.SetData("Story.Suspect.PlotInterrupt", (VarBoolean)true);
                    Debug.Log("[Day15 Transition] 怀疑线已激活，HUD区域将显示'人间？'");
                }
            }
            else
            {
                Debug.Log("[Day15 Transition] 检测到玩家曾在第15天前改变过区域，正常进行");
            }
        }
    }
}

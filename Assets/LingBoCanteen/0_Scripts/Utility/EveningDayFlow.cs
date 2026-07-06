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
            
            // 记录是否曾改变过Region，用于GameEndingManager判断结局类型
            bool hasChangedRegion = GameEntry.DataNode.GetNode("Area.ElevatorUsedOnce") != null
                && GameEntry.DataNode.GetData<VarBoolean>("Area.ElevatorUsedOnce").Value;
            GameEntry.DataNode.SetData("Game.HasChangedRegion", (VarBoolean)hasChangedRegion);

            Debug.Log($"[EveningDayFlow] 游戏结束触发，已改变Region: {hasChangedRegion}");
        }
    }
}

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
        /// 触发游戏结局（第20天结束）。
        /// </summary>
        private static void TriggerGameEnding()
        {
            if (GameEntry.DataNode == null)
            {
                return;
            }

            // 检查是否曾改变过Region（使用过电梯）
            bool hasChangedRegion = GameEntry.DataNode.GetNode("Area.ElevatorUsedOnce") != null
                && GameEntry.DataNode.GetData<VarBoolean>("Area.ElevatorUsedOnce").Value;

            if (GameEndingManager.Instance != null)
            {
                if (hasChangedRegion)
                {
                    // 曾改变过Region，触发普通结局（Day20_1 + NormalEnding）
                    GameEndingManager.Instance.TriggerNormalEnding();
                }
                else
                {
                    // 全程未改变Region，触发真实结局（Day20_2 + TrueEnding）
                    GameEndingManager.Instance.TriggerTrueEnding();
                }
            }
            else
            {
                Debug.LogError("[EveningDayFlow] GameEndingManager instance not found!");
            }
        }
    }
}

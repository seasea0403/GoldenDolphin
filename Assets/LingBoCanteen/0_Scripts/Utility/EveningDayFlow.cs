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

            // ★【修改】分离计算与执行，不在这里直接切换
            // 由SettlePanel根据目标区域决定是否播放电梯动画

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
            // ★ 【修改】确保剧情系统先于顾客初始化准备好，这样 CustomerSlotManager 在
            // ReinitializeDaily() 里读取 PlotTriggerManager.GetTodayPlotCharacterId() 时能拿到当天最新值
            if (PlotTriggerManager.Instance == null)
            {
                GameObject plotManagerObj = new GameObject("PlotTriggerManager");
                plotManagerObj.transform.SetParent(null);
                plotManagerObj.AddComponent<PlotTriggerManager>();
            }

            PlotTriggerManager.Instance?.CheckAndPlayPlotForDay(nextDay);

            // 重新初始化顾客（清理旧顾客，加载新一天的顾客）
            // ★ 【修改】确保顾客初始化在区域改变、剧情检查之后进行，避免与剧情立绘冲突
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
        /// 全区域SAN自动切换逻辑：人间、天堂、地狱均可互相自动跳转
        /// 返回目标区域，不修改DataNode（由调用者决定是否执行切换）
        /// </summary>
        private static GameRegion CalculateTargetRegionBySAN()
        {
            if (GameEntry.DataNode == null)
            {
                return GameRegion.Mortal;
            }

            // 读取当前所在区域
            int currentRegion = GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value;
            GameRegion curRegionType = (GameRegion)currentRegion;

            // 读取当前SAN值，兜底初始值
            int currentSan = GameEntry.DataNode.GetNode("Player.San") != null
                ? GameEntry.DataNode.GetData<VarInt32>("Player.San").Value
                : Constant.GameConstant.INITIAL_SAN;

            GameRegion targetRegion = curRegionType;

            // ========== 分区域判定规则 ==========
            switch (curRegionType)
            {
                case GameRegion.Mortal:
                    // 人间：SAN超上限去天堂，低于下限去地狱
                    if (currentSan >= Constant.GameConstant.SAN_MORTAL_MAX)
                    {
                        targetRegion = GameRegion.Heaven;
                        Debug.Log($"[SAN AutoSwitch][Mortal→Heaven] SAN={currentSan} ≥ {Constant.GameConstant.SAN_MORTAL_MAX}");
                    }
                    else if (currentSan <= Constant.GameConstant.SAN_MORTAL_MIN)
                    {
                        targetRegion = GameRegion.Hell;
                        Debug.Log($"[SAN AutoSwitch][Mortal→Hell] SAN={currentSan} ≤ {Constant.GameConstant.SAN_MORTAL_MIN}");
                    }
                    break;

                case GameRegion.Heaven:
                    // 天堂：SAN回落至正常人间区间，自动回归人间
                    if (currentSan > Constant.GameConstant.SAN_MORTAL_MIN && currentSan < Constant.GameConstant.SAN_MORTAL_MAX)
                    {
                        targetRegion = GameRegion.Mortal;
                        Debug.Log($"[SAN AutoSwitch][Heaven→Mortal] SAN回落至正常区间 {currentSan}");
                    }
                    break;

                case GameRegion.Hell:
                    // 地狱：SAN回升至正常人间区间，自动回归人间
                    if (currentSan > Constant.GameConstant.SAN_MORTAL_MIN && currentSan < Constant.GameConstant.SAN_MORTAL_MAX)
                    {
                        targetRegion = GameRegion.Mortal;
                        Debug.Log($"[SAN AutoSwitch][Hell→Mortal] SAN回升至正常区间 {currentSan}");
                    }
                    break;
            }

            return targetRegion;
        }

        /// <summary>
        /// 公开方法供SettlePanel查询是否需要区域切换。
        /// 返回目标区域，如果不需要切换则返回当前区域。
        /// </summary>
        public static GameRegion GetTargetRegionForNextDay()
        {
            return CalculateTargetRegionBySAN();
        }

        /// <summary>
        /// 执行区域切换的DataNode更新（由ElevatorController在动画完成后调用）。
        /// </summary>
        public static void ApplyRegionChange(GameRegion targetRegion)
        {
            if (GameEntry.DataNode == null)
            {
                return;
            }

            int currentRegion = GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value;
            GameRegion curRegionType = (GameRegion)currentRegion;

            if (targetRegion != curRegionType)
            {
                GameEntry.DataNode.SetData("Area.CurrentType", (VarInt32)(int)targetRegion);
                GameEntry.DataNode.SetData("Area.ElevatorUsedOnce", (VarBoolean)true);
                
                // 同步更新全局标记：15天前是否切换过区域（结局分支用）
                int currentDay = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value").Value;
                if (currentDay < 15)
                {
                    GameEntry.DataNode.SetData("Area.HasMovedBeforeDay15", (VarBoolean)true);
                    Debug.Log($"[EveningDayFlow] 当前天数{currentDay}<15，标记 HasMovedBeforeDay15 = true");
                }
                
                Debug.Log($"[EveningDayFlow] 区域已切换到: {targetRegion}");
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

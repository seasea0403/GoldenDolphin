using System.Collections;
using LingBoCanteen.Definition.Enum;
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
        /// <summary>
        /// 对外仍是同步调用入口（兼容 SettlePanel 现有调用方式），内部转发到协程，
        /// 全程用 <see cref="TransitionCutsceneView"/> 过场动画盖住"重置数据/剧情判定/重建顾客"
        /// 这几步的中间过程，避免玩家看到"先出现顾客、又因为剧情弹窗被打断重来"的闪烁。
        /// </summary>
        public static void AdvanceToNextDay()
        {
            CoroutineExecutor.Instance?.ExecuteCoroutine(AdvanceToNextDayRoutine());
        }

        private static IEnumerator AdvanceToNextDayRoutine()
        {
            if (GameEntry.DataNode == null)
            {
                yield break;
            }

            int currentDay = GameEntry.DataNode.GetNode("DayCurrent.Value") != null
                ? GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value").Value
                : 1;
            int nextDay = currentDay + 1;

            // 检查是否达到第20天（游戏结束）
            if (currentDay >= Constant.GameConstant.MAX_DAY)
            {
                // 触发游戏结局（结局自身有独立的黑屏/淡入流程，这里不需要过场动画）
                TriggerGameEnding();
                yield break;
            }

            // 过场动画先盖住整个屏幕，下面所有"重置/剧情判定/重建顾客"步骤都在它背后完成，
            // 玩家只会在动画淡出的一瞬间看到"已经准备好的"最终画面（有剧情就先看到对话框，
            // 没有剧情则直接看到点单区），不会再看到顾客先冒出来又被剧情打断的中间态。
            if (TransitionCutsceneView.Instance != null)
            {
                yield return TransitionCutsceneView.Instance.ShowAsync();
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

            // 该调用如果当天存在剧情会同步打开对话框 UI；由于此刻仍在过场动画背后，
            // 对话框弹出的这一瞬间玩家看不见，等过场淡出时会和场景一起"整体呈现"。
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

            // ★【新增】天数改变时，强制重新播放背景音乐（三界切换后的场景 BGM 在过场淡出前就绪）
            // 确保即使是同一首音乐，也会重新播放（例如从傍晚回到白天时的音乐更新）
            SoundManager.Instance?.ForceReplayMusicForCurrentGameState();

            // 过场动画淡出：此时场景（含可能的剧情对话框）已经完全准备好
            if (TransitionCutsceneView.Instance != null)
            {
                yield return TransitionCutsceneView.Instance.HideAsync();
            }

            // 若当天有新菜品解锁，弹“解锁新配方”提示（放在过场淡出之后，避免和过场自身的文字提示重叠）
            ShowDishUnlockToastIfAny(nextDay);
        }

        /// <summary>
        /// 若 <paramref name="day"/> 对应的 DRDay.UnlockDishIds 非空，弹出“解锁新配方”浮窗。
        /// 供本类(每日推进) 与 <see cref="CustomerSlotManager"/>(首日进入) 共用。
        /// </summary>
        public static void ShowDishUnlockToastIfAny(int day)
        {
            if (GameToastView.Instance == null)
            {
                return;
            }

            DRDay dayRow = GameEntry.DataTable.GetDataTable<DRDay>()?.GetDataRow(day);
            if (dayRow != null && dayRow.UnlockDishIds != null && dayRow.UnlockDishIds.Length > 0)
            {
                GameToastView.Instance.Show(ToastType.DishUnlocked, "解锁新配方");
            }
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
        /// 注意：这里是"每日结算后按SAN值自动判定的区域切换"，不等同于玩家手动使用电梯按钮，
        /// 因此不会写 "Area.ElevatorUsedOnce"（该字段语义是"手动电梯是否已使用"，只应由
        /// ElevatorController.GoUp/GoDown 在玩家真正点击按钮时设置），否则会导致自动切换回人间后
        /// 傍晚电梯按钮永久无法再次显示。
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

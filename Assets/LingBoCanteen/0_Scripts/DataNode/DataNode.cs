using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 定义数据节点
    /// </summary>
    public class DataNode : MonoBehaviour
    {
        private void Start()
        {
            //设置节点上的数据
            // 1. 玩家SAN、金币
            GameEntry.DataNode.SetData<int>(Player.San, 50);
            GameEntry.DataNode.SetData<int>(Player.Gold, 30);
            GameEntry.DataNode.SetData<bool>(Player.HaveRecruitHelper, false);

            // 2. 当前天数
            GameEntry.DataNode.SetData<int>(DayCurrent.Value, 1);
            GameEntry.DataNode.SetData<bool>(DayCurrent.IsDaySettled, false);

            // 3. 区域电梯状态
            GameEntry.DataNode.SetData<int>(Area.CurrentType, 0); // 0=人间
            GameEntry.DataNode.SetData<bool>(Area.HasMovedBeforeDay15, false);
            GameEntry.DataNode.SetData<bool>(Area.ElevatorUsedOnce, false);

            // 4. 当日营业数据
            GameEntry.DataNode.SetData<int>(Business.TodayServeCustomerCount, 0);
            GameEntry.DataNode.SetData<int>(Business.WaitingCustomerNum, 2);
            GameEntry.DataNode.SetData<bool>(Business.HasUnservedOrder, false);

            // 5. 库存
            Dictionary<int, int> stockDict = new Dictionary<int, int>();
            GameEntry.DataNode.SetData<Dictionary<int, int>>(Storage.IngredientStockDict, stockDict);
            GameEntry.DataNode.SetData<int>(Storage.SeasoningLeftCount, 99);

            // 6. 剧情NPC好感
            GameEntry.DataNode.SetData<int>(Story.NPC.GeniusTalkCount, 0);
            GameEntry.DataNode.SetData<int>(Story.NPC.VIPTalkCount, 0);
            GameEntry.DataNode.SetData<int>(Story.NPC.DeserterTalkCount, 0);

            // 7. 怀疑卡、剧情标记
            GameEntry.DataNode.SetData<int>(Story.Suspect.CardNum, 0);
            GameEntry.DataNode.SetData<bool>(Story.Suspect.TriggerHiddenPlot, false);
            GameEntry.DataNode.SetData<bool>(Story.Suspect.PlotInterrupt, false);
            GameEntry.DataNode.SetData<int>(Story.PlotStage, 0);
            GameEntry.DataNode.SetData<bool>(Story.IsAllStoryFinish, false);
        }
    }
}
// Root
// ├── Player        // 玩家永久属性（存档）
// ├── DayCurrent    // 当前天数
// ├── Area          // 区域/电梯状态
// ├── Business      // 当日营业临时数据（每日清空）
// ├── Storage       // 食材调料库存
// └── Story         // NPC好感、怀疑卡、剧情进度
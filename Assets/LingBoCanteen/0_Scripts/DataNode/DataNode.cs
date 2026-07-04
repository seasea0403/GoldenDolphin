using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
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
            //设置节点上的数据（字符串路径 + VarX 变量类型）
            // 1. 玩家SAN、金币
            GameEntry.DataNode.SetData("Player.San", (VarInt32)50);
            GameEntry.DataNode.SetData("Player.Gold", (VarInt32)30);
            GameEntry.DataNode.SetData("Player.HaveRecruitHelper", (VarBoolean)false);

            // 2. 当前天数
            GameEntry.DataNode.SetData("DayCurrent.Value", (VarInt32)1);
            GameEntry.DataNode.SetData("DayCurrent.IsDaySettled", (VarBoolean)false);
            GameEntry.DataNode.SetData("DayCurrent.Phase", (VarInt32)0); //0=营业 1=购物 2=结算

            // 3. 区域电梯状态
            GameEntry.DataNode.SetData("Area.CurrentType", (VarInt32)0); // 0=人间
            GameEntry.DataNode.SetData("Area.HasMovedBeforeDay15", (VarBoolean)false);
            GameEntry.DataNode.SetData("Area.ElevatorUsedOnce", (VarBoolean)false);

            // 4. 当日营业数据
            GameEntry.DataNode.SetData("Business.TodayServeCustomerCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Business.WaitingCustomerNum", (VarInt32)2);
            GameEntry.DataNode.SetData("Business.HasUnservedOrder", (VarBoolean)false);

            // 5. 库存
            Dictionary<int, int> stockDict = new Dictionary<int, int>();
            VarObject stockVar = ReferencePool.Acquire<VarObject>();
            stockVar.Value = stockDict;
            GameEntry.DataNode.SetData("Storage.IngredientStockDict", stockVar);
            GameEntry.DataNode.SetData("Storage.SeasoningLeftCount", (VarInt32)99);

            // 6. 剧情NPC好感
            GameEntry.DataNode.SetData("Story.NPC.GeniusTalkCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.NPC.VIPTalkCount", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.NPC.DeserterTalkCount", (VarInt32)0);

            // 7. 怀疑卡、剧情标记
            GameEntry.DataNode.SetData("Story.Suspect.CardNum", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.Suspect.TriggerHiddenPlot", (VarBoolean)false);
            GameEntry.DataNode.SetData("Story.Suspect.PlotInterrupt", (VarBoolean)false);
            GameEntry.DataNode.SetData("Story.PlotStage", (VarInt32)0);
            GameEntry.DataNode.SetData("Story.IsAllStoryFinish", (VarBoolean)false);
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
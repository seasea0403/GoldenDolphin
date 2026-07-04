Root
├── Player          玩家永久属性（存档）
│   ├── San                     San值
│   ├── Gold                    金币
│   └── HasRecruitHelper        是否已招募帮厨
│
├── DayCurrent      当前天状态
│   ├── Value                   当前天数
│   ├── IsDaySettled            当日是否已结算
│   └── Phase                   当前时段（对应TimeSection枚举）
│
├── Area            区域与电梯状态（存档）
│   ├── CurrentType             当前区域（对应GameRegion枚举）
│   ├── HasMovedBeforeDay15     15天前是否移动过区域
│   ├── ElevatorUsedOnce        手动电梯是否已使用
│   └── IsSuspicionMode         是否进入怀疑模式
│
├── Business        当日营业临时数据（每日重置，不存档）
│   ├── TodayServeCustomerCount 当日已服务顾客数
│   ├── TodayFailedCount        当日失败订单数
│   ├── TodayTotalGuestCount    当日顾客总数
│   ├── WaitingCustomerNum      当前等待区顾客数
│   ├── HasUnservedOrder        是否存在未完成订单
│   └── TodayEarnGold           当日收入金币
│
├── Storage         库存数据（存档）
│   ├── IngredientStockDict     食材库存 <int食材ID, int数量>
│   └── SeasoningStockDict      调料库存 <int调料ID, int数量>
│
├── Story           剧情进度（存档）
│   ├── NPC
│   │   ├── GeniusTalkCount     天才对话次数
│   │   ├── VIPTalkCount        大人物对话次数
│   │   └── DeserterTalkCount   逃兵对话次数
│   ├── Suspect
│   │   ├── CardNum             怀疑卡数量
│   │   ├── TriggerHiddenPlot   是否触发隐藏剧情
│   │   └── PlotInterrupt       怀疑剧情是否中断
│   ├── PlotStage               全局剧情阶段
│   ├── IsAllStoryFinish        全部剧情是否完成
│   └── FinishedPlotIdList      已完成剧情ID列表
│
└── Settings        游戏设置（存档）
    ├── BGMVolume               背景音乐音量
    ├── SFXVolume               音效音量
    └── IsFullScreen            是否全屏
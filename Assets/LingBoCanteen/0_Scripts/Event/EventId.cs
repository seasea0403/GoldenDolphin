namespace LingBoCanteen
{
    /// <summary>
    /// 游戏自定义事件 ID 常量。
    /// 按模块分段，每段 100 个槽位。
    /// </summary>
    public static class EventId
    {
        // ── 菜单 (10000) ──────────────────────────────────────
        /// <summary>主菜单"开始游戏"按钮点击</summary>
        public const int MenuStartGame         = 10001;

        // ── 日循环阶段 (10010) ────────────────────────────────
        /// <summary>市场阶段结束（玩家关闭购买界面）</summary>
        public const int PhaseMarketEnd        = 10011;
        /// <summary>营业阶段结束（所有顾客已离开）</summary>
        public const int PhaseBusinessEnd      = 10012;
        /// <summary>结算阶段结束（结算 UI 关闭）</summary>
        public const int PhaseSettlementEnd    = 10013;

        // ── Day (10100) ───────────────────────────────────────
        /// <summary>新的一天开始</summary>
        public const int DayStart              = 10101;
        /// <summary>营业开始（顾客开始生成）</summary>
        public const int BusinessStart         = 10102;
        /// <summary>剧情阶段开始</summary>
        public const int StoryPhaseStart       = 10103;
        /// <summary>游戏结束</summary>
        public const int GameOver              = 10104;

        // ── Customer (10200) ─────────────────────────────────
        /// <summary>顾客进入</summary>
        public const int CustomerEnter         = 10201;
        /// <summary>顾客下单</summary>
        public const int CustomerOrder         = 10202;
        /// <summary>顾客离开（含满意/愤怒）</summary>
        public const int CustomerLeave         = 10203;
        /// <summary>顾客等待超时</summary>
        public const int CustomerTimeout       = 10204;

        // ── Kitchen (10300) ──────────────────────────────────
        /// <summary>开始烹饪</summary>
        public const int CookingStart          = 10301;
        /// <summary>菜品完成（缓冲期内）</summary>
        public const int CookingDone           = 10302;
        /// <summary>菜品烧糊（超过缓冲期）</summary>
        public const int CookingBurned         = 10303;
        /// <summary>菜品已上桌</summary>
        public const int DishServed            = 10304;

        // ── Order (10400) ────────────────────────────────────
        /// <summary>订单创建</summary>
        public const int OrderCreated          = 10401;
        /// <summary>订单完成（成功）</summary>
        public const int OrderCompleted        = 10402;
        /// <summary>订单失败</summary>
        public const int OrderFailed           = 10403;

        // ── SAN / Player (10500) ─────────────────────────────
        /// <summary>SAN 值变化</summary>
        public const int SanChanged            = 10501;
        /// <summary>金币变化</summary>
        public const int GoldChanged           = 10502;

        // ── Story (10600) ────────────────────────────────────
        /// <summary>剧情对话开始</summary>
        public const int StoryDialogStart      = 10601;
        /// <summary>剧情对话结束</summary>
        public const int StoryDialogEnd        = 10602;
        /// <summary>获得怀疑卡</summary>
        public const int SuspectCardGot        = 10603;
        /// <summary>剧情阶段编号变化</summary>
        public const int PlotStageChanged      = 10604;

        // ── Market (10700) ───────────────────────────────────
        /// <summary>购买食材</summary>
        public const int IngredientBought      = 10701;
        /// <summary>食材解锁</summary>
        public const int IngredientUnlocked    = 10702;

        // ── Player Action (10800) ────────────────────────────
        /// <summary>玩家移动（用于怀疑剧情中断检测）</summary>
        public const int PlayerMoved           = 10801;
    }
}

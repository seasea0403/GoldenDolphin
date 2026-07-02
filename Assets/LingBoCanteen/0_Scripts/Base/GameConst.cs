namespace LingBoCanteen
{
    /// <summary>
    /// 游戏全局常量（对应 DefaultConfig.txt 中的配置项）。
    /// 运行时请通过 GameEntry.Config 读取；这里的值仅作编辑期回退默认。
    /// </summary>
    public static class GameConst
    {
        // 玩家初始值
        public const int InitSan  = 50;
        public const int InitGold = 30;

        // 顾客
        public const float DefaultWaitTime = 45f;   // 秒

        // 厨房
        public const float CookTime   = 6f;         // 秒
        public const float BufferTime = 5f;         // 超熟缓冲秒数

        // SAN 值变化
        public const int SanSuccessBase = 2;        // 完成订单基础 +san
        public const int SanFailPenalty = 3;        // 失败固定 -san
        public const int SanBonusMin    = -1;       // 随机修正最小值
        public const int SanBonusMax    = 2;        // 随机修正最大值

        // 关卡
        public const int TotalDays        = 20;     // 游戏总天数
        public const int SuspectStartDay  = 16;     // 怀疑剧情开始天
    }
}

// 游戏核心状态枚举
namespace LingBoCanteen
{
    /// <summary>
    /// 游戏区域（三界）
    /// </summary>
    public enum GameRegion
    {
        Unknown = 0,
        Mortal = 1,    // 人间
        Heaven = 2,    // 天堂
        Hell = 3,      // 地狱
        MortalSus = 4, // 人间?（怀疑模式）
    }

    /// <summary>
    /// 每日时段
    /// </summary>
    public enum TimeSection : byte
    {
        Day = 0,    // 白天（营业）
        Evening = 1,// 傍晚（采购）
    }

    /// <summary>
    /// 游戏结局类型
    /// </summary>
    public enum EndingType
    {
        None = 0,
        Death_Heaven = 1,          // San值满值死亡
        Death_Hell = 2,            // San值为0死亡
        Normal = 3,         // 普通结局（一无所知）
        Truth = 4,          // 真相结局（44≤san≤57）
        Hidden = 5,         // 隐藏结局（怀疑线）
    }
}

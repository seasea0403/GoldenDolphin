// 食材与烹饪枚举
namespace LingBoCanteen
{
    /// <summary>
    /// 食材处理方式
    /// </summary>
    public enum IngredientProcessType
    {
        None = 0,   // 无需处理
        Cut = 1,    // 可切
        Squeeze = 2,// 可榨汁
        Both = 3,   // 可切可榨汁
    }

    /// <summary>
    /// 烹饪器具类型
    /// </summary>
    public enum CookingToolType
    {
        Wok = 5001,         // 炒锅
        Steamer = 5002,     // 蒸锅
        PressurePot = 5003, // 高压锅
        SoupPot = 5004,     // 汤锅
        Oven = 5005,        // 烤箱
    }

    /// <summary>
    /// 烹饪状态
    /// </summary>
    public enum CookingState
    {
        Idle = 0,       // 空闲
        Cooking = 1,    // 烹饪中
        Finished = 2,   // 已成熟
        Burnt = 3,      // 已糊
    }

    /// <summary>
    /// 调料类型
    /// </summary>
    public enum SeasoningType
    {
        Oil = 4001,         // 油
        Sauce = 4002,       // 调味酱汁
        Cream = 4003,       // 奶油
        Salt = 4004,        // 火山盐
        Sugar = 4005,       // 糖
        BlackPepper = 4006, // 黑胡椒
    }
}

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

    /// <summary>
    /// 调料类型
    /// </summary>
    public enum PotType
    {
        Wok = 5001,        // 炒锅
        Steamer = 5002,    // 蒸锅
        PressurePot = 5003,// 高压锅
        SoupPot = 5004,    // 汤锅
        Oven = 5005,       // 烤箱
    }

    /// <summary>
    /// 食材存放区域（决定库存/解锁/交互模式规则），按 Id 区间划分：
    /// Shelf 1001-1016，Fridge 1017-1021，Drawer 1022-1023。
    /// </summary>
    public enum IngredientAreaType
    {
        Shelf = 0,  // 货架常规食材：有库存、可采购、按天解锁
        Fridge = 1, // 冰箱肉类食材：无库存限制、默认解锁
        Drawer = 2, // 抽屉特殊食材（鸡蛋/面粉）：点击进入操作界面，非拖拽加工
    }

    /// <summary>
    /// 加工工位当前状态。
    /// </summary>
    public enum ProcessStationState
    {
        Empty = 0,            // 空闲，可放入新食材
        Processing = 1,       // 加工动画播放中
        WaitingForOutput = 2, // 加工已完成，但产出因收纳槽已满而滞留在工位上
    }
}

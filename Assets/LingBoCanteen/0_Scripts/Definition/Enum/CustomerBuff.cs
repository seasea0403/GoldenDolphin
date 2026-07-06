namespace LingBoCanteen.Definition.Enum
{
    /// <summary>
    /// 顾客状态 buff 枚举（美德 / 原罪）
    /// 与数据表或 FSM 中使用的整型值对应。
    /// </summary>
    public enum CustomerBuff
    {
        // 美德
        None = 0,
        Temperate = 1,      // 温和：耐心值消耗时间增加 +10s
        Diligent = 2,       // 勤奋：作为顾客时加快 10% 烹饪速度
        Thoughtful = 3,     // 慎虑：完成顾客订单时多获得 10% 报酬
        Frugal = 4,         // 节制：制作餐品时少消耗一份原材料

        // 原罪
        Wrath = 100,        // 暴怒：耐心值消耗时间减少 10s
        Sloth = 101,        // 懒惰：等待 10s 后有 50% 概率离开，减少 san 值
        Greed = 102,        // 贪婪：完成顾客订单时少获得 10% 报酬
        Gluttony = 103      // 暴食：制作餐品时多消耗一份原材料
    }
}

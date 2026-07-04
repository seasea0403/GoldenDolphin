// 顾客与buff枚举
namespace LingBoCanteen.Definition.Enum
{
    /// <summary>
    /// 顾客类型
    /// </summary>
    public enum GuestType
    {
        Normal = 0, // 普通顾客
        Story = 1,  // 剧情NPC
    }

    /// <summary>
    /// 顾客订单状态
    /// </summary>
    public enum OrderState
    {
        Waiting = 0,    // 等待中
        Cooking = 1,    // 制作中
        Success = 2,    // 完成
        Failed = 3,     // 失败（超时/做错）
    }

    /// <summary>
    /// Buff类型（美德/原罪）
    /// </summary>
    public enum BuffType
    {
        Virtue = 1, // 美德（天堂）
        Sin = 2,    // 原罪（地狱）
    }

    /// <summary>
    /// Buff具体效果ID
    /// </summary>
    public enum BuffEffectId
    {
        Gentle = 1,     // 温和：耐心+10s
        Diligent = 2,   // 勤奋：烹饪速度+10%
        Generous = 3,   // 慷慨：金币+10%
        Temperate = 4,  // 节制：少消耗1份材料
        Wrath = 11,     // 暴怒：耐心-10s
        Sloth = 12,     // 懒惰：10s后50%概率离开
        Greed = 13,     // 贪婪：金币-10%
        Gluttony = 14,  // 暴食：多消耗1份材料
    }
}

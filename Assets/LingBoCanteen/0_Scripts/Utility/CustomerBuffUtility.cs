using LingBoCanteen.Definition.Enum;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 顾客 Buff 抽取与效果解析工具。
    /// </summary>
    public static class CustomerBuffUtility
    {
        // 美德（天堂）
        private static readonly CustomerBuff[] VirtueBuffs =
        {
            CustomerBuff.Temperate,
            CustomerBuff.Diligent,
            CustomerBuff.Thoughtful,
            CustomerBuff.Frugal,
        };

        // 原罪（地狱）
        private static readonly CustomerBuff[] SinBuffs =
        {
            CustomerBuff.Wrath,
            CustomerBuff.Sloth,
            CustomerBuff.Greed,
            CustomerBuff.Gluttony,
        };

        /// <summary>
        /// 根据顾客所在区域随机抽取一个 Buff。
        /// Mortal（人间/怀疑模式）：美德+原罪共 8 个全池随机；
        /// Heaven（天堂）：仅美德 4 选 1；Hell（地狱）：仅原罪 4 选 1。
        /// </summary>
        public static CustomerBuff PickBuff(GameRegion region)
        {
            switch (region)
            {
                case GameRegion.Heaven:
                    return VirtueBuffs[Random.Range(0, VirtueBuffs.Length)];

                case GameRegion.Hell:
                    return SinBuffs[Random.Range(0, SinBuffs.Length)];

                case GameRegion.Mortal:
                case GameRegion.MortalSus:
                default:
                    int roll = Random.Range(0, VirtueBuffs.Length + SinBuffs.Length);
                    return roll < VirtueBuffs.Length
                        ? VirtueBuffs[roll]
                        : SinBuffs[roll - VirtueBuffs.Length];
            }
        }

        /// <summary>
        /// 是否为美德类 Buff（枚举值 &lt; 100 为美德，&gt;= 100 为原罪）。
        /// </summary>
        public static bool IsVirtue(CustomerBuff buff)
        {
            return (int)buff < 100;
        }

        /// <summary>
        /// 计算该 Buff 对默认耐心时间（<see cref="Constant.GameConstant.DEFAULT_GUEST_WAIT_TIME"/>）的修正值。
        /// 目前只有 Temperate/Wrath 直接影响耐心时间，其余 Buff（烹饪速度、报酬、耗材等）在对应系统里处理。
        /// </summary>
        public static float GetPatienceModifier(CustomerBuff buff)
        {
            switch (buff)
            {
                case CustomerBuff.Temperate:
                    return Constant.GameConstant.BUFF_PATIENCE_DELTA;
                case CustomerBuff.Wrath:
                    return -Constant.GameConstant.BUFF_PATIENCE_DELTA;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 烹饪时长的修正倍率：如果本次要做的菜有勤奋(Diligent) Buff 顾客在等待，烹饪速度加快
        /// <see cref="Constant.GameConstant.BUFF_COOK_SPEED_RATIO"/>（即耗时按比例缩短），否则倍率为 1（不变）。
        /// </summary>
        public static float GetCookDurationMultiplier(bool hasDiligentWaiter)
        {
            return hasDiligentWaiter ? 1f - Constant.GameConstant.BUFF_COOK_SPEED_RATIO : 1f;
        }

        /// <summary>
        /// 完成订单报酬的修正倍率：慎虑(Thoughtful)多得、贪婪(Greed)少得，均为
        /// <see cref="Constant.GameConstant.BUFF_REWARD_RATIO"/>，其余 Buff 不影响报酬。
        /// </summary>
        public static float GetRewardMultiplier(CustomerBuff buff)
        {
            switch (buff)
            {
                case CustomerBuff.Thoughtful:
                    return 1f + Constant.GameConstant.BUFF_REWARD_RATIO;
                case CustomerBuff.Greed:
                    return 1f - Constant.GameConstant.BUFF_REWARD_RATIO;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// 获取 Buff 的具体效果说明文案，供悬浮提示（Tooltip）展示。
        /// </summary>
        public static string GetEffectDescription(CustomerBuff buff)
        {
            switch (buff)
            {
                case CustomerBuff.Temperate: return "耐心消耗时间增加10秒";
                case CustomerBuff.Diligent: return "作为顾客时烹饪速度加快10%";
                case CustomerBuff.Thoughtful: return "完成订单时多获得10%报酬";
                case CustomerBuff.Frugal: return "制作餐品时少消耗一份原材料";
                case CustomerBuff.Wrath: return "耐心消耗时间减少10秒";
                case CustomerBuff.Sloth: return "等待10秒后有50%概率提前离开";
                case CustomerBuff.Greed: return "完成订单时少获得10%报酬";
                case CustomerBuff.Gluttony: return "制作餐品时多消耗一份原材料";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 获取 Buff 的展示文案，用于顾客气泡上的文字。
        /// </summary>
        public static string GetDescription(CustomerBuff buff)
        {
            switch (buff)
            {
                case CustomerBuff.Temperate: return "温和";
                case CustomerBuff.Diligent: return "勤奋";
                case CustomerBuff.Thoughtful: return "慎虑";
                case CustomerBuff.Frugal: return "节制";
                case CustomerBuff.Wrath: return "暴怒";
                case CustomerBuff.Sloth: return "懒惰";
                case CustomerBuff.Greed: return "贪婪";
                case CustomerBuff.Gluttony: return "暴食";
                default: return string.Empty;
            }
        }
    }
}

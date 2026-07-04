
namespace LingBoCanteen
{
    public static partial class Constant
    {
        public static class GameConstant
        {
            // 初始数值
            public const int INITIAL_SAN = 50;
            public const int INITIAL_GOLD = 30;
            public const int MAX_DAY = 20;
            
            // 时间与数值规则
            public const float DEFAULT_GUEST_WAIT_TIME = 45f;    // 顾客默认等待时间（秒）
            public const float DEFAULT_COOK_TIME = 6f;           // 默认烹饪时长（秒）
            public const float COOK_BUFFER_TIME = 5f;            // 成熟后缓冲时间（秒）
            public const float CUT_ANIM_TIME = 5f;               // 切菜动画时长
            public const float SQUEEZE_ANIM_TIME = 5f;           // 榨汁动画时长
            
            // San值区间
            public const int SAN_MORTAL_MIN = 43;
            public const int SAN_MORTAL_MAX = 57;
            
            // 订单San值规则
            public const int ORDER_SUCCESS_BASE_SAN = 2;
            public const int ORDER_FAIL_SAN = -3;
            
            // 区域跳转初始San
            public const int HEAVEN_INIT_SAN = 70;
            public const int HELL_INIT_SAN = 30;
            
            // 顾客配置
            public const int MAX_WAITING_GUEST = 3;  // 等待区最大顾客数
        }
    }
}

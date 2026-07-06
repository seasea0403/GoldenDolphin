
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
            public const float CUT_ANIM_TIME = 1f;               // 切菜动画时长
            public const float SQUEEZE_ANIM_TIME = 1.6f;           // 榨汁动画时长
            
            // San值区间
            public const int SAN_MORTAL_MIN = 43;
            public const int SAN_MORTAL_MAX = 57;
            
            // 订单San值规则
            public const int ORDER_SUCCESS_BASE_SAN = 2;
            public const int ORDER_FAIL_SAN = -3;
            
            // 区域跳转初始San
            public const int HEAVEN_INIT_SAN = 70;
            public const int HELL_INIT_SAN = 30;

            // San值边界（触碰边界视为自动被电梯送往天堂/地狱）
            public const int SAN_BOUNDARY_MIN = 0;
            public const int SAN_BOUNDARY_MAX = 100;
            
            // 顾客配置
            public const int MAX_WAITING_GUEST = 3;      // 等待区最大顾客数
            public const float NEXT_CUSTOMER_INTERVAL = 10f; // 一个顾客离场后，同一槽位下一位顾客的进入间隔（秒）
            public const float BUFF_PATIENCE_DELTA = 10f;    // 温和/暴怒等 Buff 对耐心时间的修正幅度（秒）
            public const float DUAL_ORDER_CHANCE = 0.1f;     // 顾客点 2 道菜的概率(10%)，其余为 1 道菜(90%)

            // 备菜区配置
            public const float CUTTING_MID_TIME = 0.6f;      // 放入菜板后，切到中间态的时间点（秒）
            public const float CUTTING_FINAL_TIME = 1.1f;    // 放入菜板后，切到最终态的时间点（秒）
            public const int BOWL_SLOT_COUNT = 4;            // 碗槽数量
            public const int GLASS_SLOT_COUNT = 4;           // 杯槽数量
            public const int DEFAULT_DRAWER_CLICK_COUNT = 5; // 抽屉食材（鸡蛋/面粉）默认所需点击次数
            public const int DEFAULT_UNLOCK_STOCK = 10;      // 货架食材首次解锁时自动赋予的初始库存

            // 烹调区配置
            public const float COOK_FINISH_HANDLE_TIME = 2f; // 烹饪完成缓冲期内，进度条 handle 保持"完成态"特殊样式的时长（秒）
        }
    }
}

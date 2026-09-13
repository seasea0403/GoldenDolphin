namespace LingBoCanteen.Definition.Enum
{
    /// <summary>
    /// 全局浮窗提示（<see cref="LingBoCanteen.GameToastView"/>）的类型，用于选择对应的背景图。
    /// </summary>
    public enum ToastType
    {
        CustomerAllServed,  // 当日顾客全部处理完毕
        PurchaseSuccess,    // 超市购买成功
        ServeFailed,        // 上菜失败（没有顾客需要该菜品）
        DishUnlocked,       // 当天解锁了新菜品配方
    }
}

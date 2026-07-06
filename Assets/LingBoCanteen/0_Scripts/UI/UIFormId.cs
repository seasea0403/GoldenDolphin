namespace LingBoCanteen
{
    /// <summary>
    /// 界面编号。
    /// </summary>
    public enum UIFormId : int
    {
        Undefined = 0,

        /// <summary>
        /// 剧情对话弹窗
        /// </summary>
        DialogueForm = 1,

        /// <summary>
        /// 主菜单界面
        /// </summary>
        MenuForm = 100,

        /// <summary>
        /// 设置界面
        /// </summary>
        SettingForm = 101,

        /// <summary>
        /// HUD常驻界面
        /// </summary>
        HUDForm = 200,

        /// <summary>
        /// 选锅界面
        /// </summary>
        PotForm = 201,

        /// <summary>
        /// 每日结算界面
        /// </summary>
        SettleForm = 300,

        /// <summary>
        /// 食材超市采购界面
        /// </summary>
        MarketForm = 400,

        /// <summary>
        /// 游戏结局界面
        /// </summary>
        EndingForm = 500,
    }
}
//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;
using TMPro;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    public class HUDForm : UGuiForm
    {
        [Header("天数文本 第X天")]
        [SerializeField] private TMP_Text m_DayText = null;
        [Header("当前区域文本 人间/天堂/地狱")]
        [SerializeField] private TMP_Text m_RegionText = null;
        [Header("金币文本")]
        [SerializeField] private TMP_Text m_GoldText = null;
        [Header("SAN值文本")]
        [SerializeField] private TMP_Text m_SanText = null;
        [Header("当日顾客数量文本")]
        [SerializeField] private TMP_Text m_CustomerCountText = null;

        // 区域文字映射
        private string GetRegionDisplayText(GameRegion region)
        {
            return region switch
            {
                GameRegion.Mortal => "人间",
                GameRegion.Heaven => "天堂",
                GameRegion.Hell => "地狱",
                GameRegion.MortalSus => "人间？",
                _ => "未知区域"
            };
        }

        /// <summary>
        /// 刷新所有HUD显示内容
        /// </summary>
        public void RefreshAllHUD()
        {
            // HUDForm 目前以静态物体形式常驻在场景中（并未经由 GameEntry.UI.OpenUIForm 打开），
            // 因此这里对每个 DataNode 路径做存在性检查，避免在 DataNode 尚未初始化完成前抛出异常。
            if (GameEntry.DataNode.GetNode("DayCurrent.Value") == null ||
                GameEntry.DataNode.GetNode("Area.CurrentType") == null ||
                GameEntry.DataNode.GetNode("Player.Gold") == null ||
                GameEntry.DataNode.GetNode("Player.San") == null)
            {
                return;
            }

            // 读取DataNode数据
            int currentDay = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value");
            int regionInt = GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType");
            int gold = GameEntry.DataNode.GetData<VarInt32>("Player.Gold");
            int san = GameEntry.DataNode.GetData<VarInt32>("Player.San");

            GameRegion currentRegion = (GameRegion)regionInt;

            // 赋值文本
            m_DayText.text = $"第{currentDay}天";
            m_RegionText.text = GetRegionDisplayText(currentRegion);
            m_GoldText.text = $"{gold}";
            m_SanText.text = $"{san}";

            if (m_CustomerCountText != null)
            {
                int totalGuestCount = 0;
                if (GameEntry.DataNode.GetNode("Business.TodayTotalGuestCount") != null)
                {
                    totalGuestCount = GameEntry.DataNode.GetData<VarInt32>("Business.TodayTotalGuestCount").Value;
                }

                int servedCount = 0;
                if (GameEntry.DataNode.GetNode("Business.TodayServeCustomerCount") != null)
                {
                    servedCount = GameEntry.DataNode.GetData<VarInt32>("Business.TodayServeCustomerCount").Value;
                }

                m_CustomerCountText.text = $"顾客：{servedCount}/{totalGuestCount}";
            }
        }

        // 注意：当前 HUDForm 是作为常驻静态物体直接摆放在 Main 场景中的（并未经由
        // GameEntry.UI.OpenUIForm(UIFormId.HUDForm, ...) 动态打开），因此 GameFramework
        // 用于承载 UIFormLogic 生命周期回调的 UIForm 包装组件并不存在于该物体上，
        // 上面的 OnOpen/OnUpdate 永远不会被框架调用到，导致 HUD 数值一直停留在初始值。
        // 这里额外声明真正的 Unity 引擎生命周期方法，确保无论是否经过 UGF 打开流程，
        // HUD 都能在物体存在于场景中时正常刷新。
        private void Awake()
        {
            RefreshAllHUD();
        }

        private void Update()
        {
            RefreshAllHUD();
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnOpen(object userData)
#else
        protected internal override void OnOpen(object userData)
#endif
        {
            base.OnOpen(userData);
            // 打开HUD时立刻刷新一次
            RefreshAllHUD();
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
#else
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
#endif
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            // 每帧实时刷新，保证数值变化立刻同步显示
            RefreshAllHUD();
        }
    }
}
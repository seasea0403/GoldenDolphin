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
            // 读取DataNode数据
            int currentDay = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value");
            int regionInt = GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType");
            int gold = GameEntry.DataNode.GetData<VarInt32>("Player.Gold");
            int san = GameEntry.DataNode.GetData<VarInt32>("Player.San");

            GameRegion currentRegion = (GameRegion)regionInt;

            // 赋值文本
            m_DayText.text = $"第{currentDay}天";
            m_RegionText.text = GetRegionDisplayText(currentRegion);
            m_GoldText.text = $"金币：{gold}";
            m_SanText.text = $"SAN：{san}";
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
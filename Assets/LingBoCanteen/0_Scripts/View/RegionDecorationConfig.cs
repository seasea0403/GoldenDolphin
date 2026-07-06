using System;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 单个区域对应的场景渲染与遮罩绑定配置。
    /// 可以通过 Inspector 灵活指定不同区域在不同的辖区 (Region) 下需要切换的图片以及渲染遮罩颜色。
    /// </summary>
    [Serializable]
    public struct RegionDecorationConfig
    {
        [Header("绑定的区域（人间/天堂/地狱）")]
        public GameRegion Region;

        [Header("遮罩遮罩(Mask)的颜色")]
        public Color MaskColor;

        [Header("Order区相应的专属背景图精灵")]
        public Sprite OrderBackgroundSprite;
    }
}

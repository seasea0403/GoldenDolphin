using System;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 烹调区拖拽负载种类：决定了哪些工位愿意接收这次拖拽。
    /// </summary>
    public enum KitchenDragItemKind
    {
        /// <summary>碗/杯槽产出物（切制/榨汁的半成品，或已完成的中间菜品），只能投入锅具。</summary>
        Ingredient = 0,

        /// <summary>调料，只能投入锅具，命中后额外播放撒料动画。</summary>
        Seasoning = 1,

        /// <summary>整锅（非烹饪阶段），可以拖到垃圾桶重置。仅在Idle状态下允许拖拽。</summary>
        Pot = 2,

        /// <summary>糊锅后的锅内食物，只能拖去垃圾桶。</summary>
        Food = 3,
    }

    /// <summary>
    /// 烹调区拖拽负载：涵盖碗/杯槽产出物、调料投放到锅具、整锅拖到垃圾桶、以及糊锅食物拖到垃圾桶等场景
    /// （锅本身只在非烹饪阶段允许拖拽）。复用同一个类是因为这些操作都只需要"一个 Id + 一张图标 + 落地/弹回回调"，
    /// 用 <see cref="Kind"/> 区分各工位是否愿意接收，避免误判。
    /// </summary>
    public class KitchenDragPayload
    {
        public readonly KitchenDragItemKind Kind;

        /// <summary>
        /// 依 <see cref="Kind"/> 不同含义不同：Ingredient 为 OutputId 或半成品菜品 DishId；
        /// Seasoning 为 SeasoningType 对应的 Id；Pot 为 PotType 对应的 Id；Food 为 DishId。
        /// </summary>
        public readonly int ItemId;

        /// <summary>
        /// 拖拽跟随鼠标显示的图标，同时也用于成功落入锅具后气泡区的缩略图标。
        /// </summary>
        public readonly Sprite Icon;

        /// <summary>
        /// 命中有效工位并被接受后调用。
        /// </summary>
        public Action OnAccepted;

        /// <summary>
        /// 未命中任何工位（或被拒绝）时调用，负责恢复来源的显示。
        /// </summary>
        public Action OnReturnToOrigin;

        public KitchenDragPayload(KitchenDragItemKind kind, int itemId, Sprite icon)
        {
            Kind = kind;
            ItemId = itemId;
            Icon = icon;
        }
    }
}

using System;

namespace LingBoCanteen
{
    /// <summary>
    /// 一次拾取拖拽携带的数据。IsPreCut = true 表示这是菜板上"可切可榨汁"食材切制完成后的
    /// 中间产物被再次拖拽（此时只允许被榨汁机接收），false 表示直接从货架/冰箱拾取的生食材。
    /// </summary>
    public class IngredientDragPayload
    {
        public readonly int IngredientId;
        public readonly DRIngredient Row;
        public readonly bool IsPreCut;

        /// <summary>被某个工位成功接收时回调（例如扣减货架库存 / 清空菜板暂存）。</summary>
        public Action OnAccepted;

        /// <summary>未被任何工位接收，弹回原位时回调。</summary>
        public Action OnReturnToOrigin;

        public IngredientDragPayload(DRIngredient row, bool isPreCut)
        {
            Row = row;
            IngredientId = row.Id;
            IsPreCut = isPreCut;
        }
    }
}

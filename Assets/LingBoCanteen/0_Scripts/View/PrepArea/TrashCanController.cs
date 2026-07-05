using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 垃圾桶：来者不拒，接受任意一次拖拽丢弃——
    /// 无论是货架/冰箱食材本体（直接从 <see cref="Ingredient"/> 拖拽而来，IsPreCut=false），
    /// 还是菜板上停留等待二次加工（可切可榨汁）/因碗槽已满而滞留（仅可切）的半成品
    /// （<see cref="CuttingBoardController.TryPickPreCutItem"/> 再次点击拖拽而来，IsPreCut=true）。
    /// 丢弃后播放一次 Open 动画；扣减货架库存、恢复菜板到进入场景时的初始状态，
    /// 这些都已经由拖拽发起方设置在 <see cref="IngredientDragPayload.OnAccepted"/> 回调里，
    /// 本类不需要关心来源，也不维护占用状态，可以被反复接收。
    /// </summary>
    public class TrashCanController : ProcessStationBase
    {
        [SerializeField] private Animator m_TrashAnimator;
        [SerializeField] private string m_PlayAnimTrigger = "Open";

        protected override void Awake()
        {
            base.Awake();

            // 同其它工位：避免 Animator 默认状态一进场就自动播放，只在真正 Accept 时才启用。
            if (m_TrashAnimator != null)
            {
                m_TrashAnimator.enabled = false;
            }
        }

        protected override bool CanAccept(IngredientDragPayload payload)
        {
            return payload != null;
        }

        protected override void Accept(IngredientDragPayload payload)
        {
            if (m_TrashAnimator != null)
            {
                m_TrashAnimator.enabled = true;
                m_TrashAnimator.SetTrigger(m_PlayAnimTrigger);
            }
        }
    }
}

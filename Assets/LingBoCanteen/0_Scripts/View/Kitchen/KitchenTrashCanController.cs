using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 烹调区垃圾桶：接收糊锅食物的 <see cref="KitchenDragPayload"/>（锅本身不可拖拽），播放开盖动画。
    /// 不维护占用状态，可反复使用；实际的重置副作用完全由负载自带的 OnAccepted 回调
    /// （<see cref="PotController"/> 设置）完成，写法与备菜区 TrashCanController 一致。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KitchenTrashCanController : KitchenStationBase
    {
        [SerializeField] private Animator m_TrashAnimator;
        [SerializeField] private string m_PlayAnimTrigger = "Open";

        protected override void Awake()
        {
            base.Awake();
            if (m_TrashAnimator != null)
            {
                m_TrashAnimator.enabled = false;
            }
        }

        protected override bool CanAccept(KitchenDragPayload payload)
        {
            return payload != null && (payload.Kind == KitchenDragItemKind.Food || payload.Kind == KitchenDragItemKind.Ingredient);
        }

        protected override void Accept(KitchenDragPayload payload)
        {
            if (m_TrashAnimator != null)
            {
                m_TrashAnimator.enabled = true;
                m_TrashAnimator.SetTrigger(m_PlayAnimTrigger);
            }
        }
    }
}

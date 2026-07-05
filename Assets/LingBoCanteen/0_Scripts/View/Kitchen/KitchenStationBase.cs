using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 烹调区可接收拖拽的工位公共基类：负责向 <see cref="KitchenDragController"/> 注册/反注册与命中判定，
    /// 具体准入规则由子类实现。写法与备菜区 <see cref="ProcessStationBase"/> 完全对齐。
    /// 要求挂载对象上有一个 Collider2D 作为"碰撞区域"。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class KitchenStationBase : MonoBehaviour
    {
        protected Collider2D DropZone { get; private set; }

        protected virtual void Awake()
        {
            DropZone = GetComponent<Collider2D>();
        }

        protected virtual void OnEnable()
        {
            KitchenDragController.RegisterStation(this);
        }

        protected virtual void OnDisable()
        {
            KitchenDragController.UnregisterStation(this);
        }

        public bool ContainsPoint(Vector3 worldPosition)
        {
            return DropZone != null && DropZone.OverlapPoint(worldPosition);
        }

        /// <summary>
        /// 判断是否可以接收这次拖拽；若可以则立即执行 Accept 并返回 true，否则返回 false（调用方负责弹回原位）。
        /// </summary>
        public bool TryAccept(KitchenDragPayload payload)
        {
            if (!CanAccept(payload))
            {
                return false;
            }

            Accept(payload);
            return true;
        }

        protected abstract bool CanAccept(KitchenDragPayload payload);

        protected abstract void Accept(KitchenDragPayload payload);
    }
}

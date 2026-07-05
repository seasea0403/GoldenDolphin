using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 加工工位（菜板/榨汁机）公共基类：负责向 <see cref="PrepDragController"/> 注册/反注册、
    /// 碰撞区域命中判定，具体的准入规则与加工时序由子类实现。
    /// 要求挂载对象上有一个 Collider2D 作为"碰撞区域"。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class ProcessStationBase : MonoBehaviour
    {
        protected Collider2D DropZone { get; private set; }
        protected ProcessStationState State { get; set; } = ProcessStationState.Empty;

        protected virtual void Awake()
        {
            DropZone = GetComponent<Collider2D>();
        }

        protected virtual void OnEnable()
        {
            PrepDragController.RegisterStation(this);
        }

        protected virtual void OnDisable()
        {
            PrepDragController.UnregisterStation(this);
        }

        public bool ContainsPoint(Vector3 worldPosition)
        {
            return DropZone != null && DropZone.OverlapPoint(worldPosition);
        }

        /// <summary>
        /// 判断是否可以接收这次拖拽；若可以则立即执行 Accept 并返回 true，否则返回 false（调用方负责弹回原位）。
        /// </summary>
        public bool TryAccept(IngredientDragPayload payload)
        {
            if (!CanAccept(payload))
            {
                return false;
            }

            Accept(payload);
            return true;
        }

        protected abstract bool CanAccept(IngredientDragPayload payload);

        protected abstract void Accept(IngredientDragPayload payload);
    }
}

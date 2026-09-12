using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 烹调区调料罐：场景内放 6 个实例，对应 SeasoningType 枚举（Id 4001-4006）。
    /// 无库存限制，点击即可拖拽投放到任意处于 Idle 状态的锅具里。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SeasoningItem : MonoBehaviour
    {
        [SerializeField] private SeasoningType m_SeasoningType;
        [SerializeField] private SpriteRenderer m_IconRenderer;

        private void OnMouseDown()
        {
            if (UIFormSceneInputBlocker.IsSceneInputBlocked) return;

            if (KitchenDragController.Instance == null || m_IconRenderer == null)
            {
                return;
            }

            Sprite icon = m_IconRenderer.sprite;
            KitchenDragPayload payload = new KitchenDragPayload(KitchenDragItemKind.Seasoning, (int)m_SeasoningType, icon);
            KitchenDragController.Instance.BeginDrag(payload, icon, transform.position);
        }
    }
}

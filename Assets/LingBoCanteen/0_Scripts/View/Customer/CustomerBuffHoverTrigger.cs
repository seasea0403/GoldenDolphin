using UnityEngine;
using UnityEngine.EventSystems;

namespace LingBoCanteen
{
    /// <summary>
    /// 挂在 <see cref="CustomerBubbleView"/> 的 Buff 图标/背景 Image 上（该 Image 需勾选 Raycast Target）。
    /// 鼠标悬浮时通过 <see cref="CustomerBuffTooltipView"/> 显示当前顾客抽到的 Buff 名称与效果说明。
    /// </summary>
    public class CustomerBuffHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private CustomerBubbleView m_BubbleView;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (m_BubbleView == null || CustomerBuffTooltipView.Instance == null)
            {
                return;
            }

            Definition.Enum.CustomerBuff buff = m_BubbleView.CurrentBuff;
            if (buff == Definition.Enum.CustomerBuff.None)
            {
                return;
            }

            CustomerBuffTooltipView.Instance.Show(CustomerBuffUtility.GetDescription(buff), CustomerBuffUtility.GetEffectDescription(buff));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CustomerBuffTooltipView.Instance?.Hide();
        }
    }
}

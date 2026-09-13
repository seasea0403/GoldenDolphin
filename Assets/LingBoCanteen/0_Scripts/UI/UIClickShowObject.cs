using UnityEngine;
using UnityEngine.EventSystems;

namespace LingBoCanteen
{
    /// <summary>
    /// 挂在任意带 Raycast Target 的 UI 上（Image、Text 等），点击后控制目标物体的显隐。
    /// 用法一：直接把本组件挂在按钮物体上，拖入 m_Target
    /// 用法二：不要本组件，改在 Button 的 onClick 里调用 ShowTarget()
    /// </summary>
    public class UIClickShowObject : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] public GameObject m_Target;      // 要显示的物体
        [SerializeField] public bool m_ToggleMode;        // true=点一下开/关切换，false=只负责显示
        [SerializeField] private bool m_HideSelfOnClick;   // 点击后隐藏自己所在的界面

        public void OnPointerClick(PointerEventData eventData)
        {
            ShowTarget();
        }

        /// <summary>也可以从 Button 的 onClick 里调用这个方法。</summary>
        public void ShowTarget()
        {
            if (m_Target == null) return;

            if (m_ToggleMode)
                m_Target.SetActive(!m_Target.activeSelf);
            else
                m_Target.SetActive(true);

            if (m_HideSelfOnClick)
                gameObject.SetActive(false);
        }
    }
}
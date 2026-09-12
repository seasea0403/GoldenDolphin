using UnityEngine;
using UnityEngine.EventSystems;

namespace LingBoCanteen
{
    /// <summary>
    /// 场景点击屏蔽判断：指针位于任意可被射线命中的 UI 元素上时，视为屏蔽场景点击。
    /// 纯查询、无状态，不依赖任何组件挂载，因此不存在"忘记注销导致永久屏蔽"的问题。
    /// 注意：仅识别带 GraphicRaycaster 的 Canvas 中的 UI，且点击位置必须有可命中的 UI 元素
    /// （透明图片只要勾选 Raycast Target 即可命中），点在完全没有 UI 元素的空白处不会屏蔽。
    /// </summary>
    public class UIFormSceneInputBlocker : MonoBehaviour
    {
        /// <summary>
        /// 当前指针是否悬停在 UI 上（悬停期间场景点击视为被 UI 消费）。
        /// </summary>
        public static bool IsSceneInputBlocked
        {
            get
            {
                EventSystem eventSystem = EventSystem.current;
                return eventSystem != null && eventSystem.IsPointerOverGameObject();
            }
        }
    }
}

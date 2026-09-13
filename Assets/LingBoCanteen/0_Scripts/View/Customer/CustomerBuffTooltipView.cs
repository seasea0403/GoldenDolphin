using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingBoCanteen
{
    /// <summary>
    /// 顾客 Buff 悬浮提示：鼠标悬停在气泡的 Buff 图标上时显示该 Buff 的名称与具体效果说明。
    /// 场景内放置一个实例（挂到 Screen Space Canvas 下，与 <see cref="IngredientTooltipView"/> 同级），默认隐藏。
    /// m_ScreenOffset 按"画布本地设计单位"解释，与 anchoredPosition 同一套单位，不受分辨率/CanvasScaler影响。
    /// </summary>
    public class CustomerBuffTooltipView : MonoBehaviour
    {
        public static CustomerBuffTooltipView Instance { get; private set; }

        [SerializeField] private RectTransform m_Root;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_DescriptionText;
        [SerializeField] private Vector2 m_ScreenOffset = new Vector2(16f, 16f);

        private Canvas m_Canvas;
        private Transform m_HoverSource;

        private void Awake()
        {
            Instance = this;
            m_Canvas = m_Root != null ? m_Root.GetComponentInParent<Canvas>() : null;
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 显示 Buff 名称 + 效果说明，位置跟随当前鼠标位置。
        /// hoverSource 传触发方（Buff 图标）自身 transform，用于判断"挡住的是否是别的面板"；有遮挡时不显示。
        /// </summary>
        public void Show(string title, string description, Transform hoverSource = null)
        {
            m_HoverSource = hoverSource;
            if (m_Root == null || IsBlockedByHigherCanvas())
            {
                return;
            }

            m_Root.gameObject.SetActive(true);
            if (m_TitleText != null)
            {
                m_TitleText.text = title;
            }

            if (m_DescriptionText != null)
            {
                m_DescriptionText.text = description;
            }

            FollowMouse();
        }

        public void Hide()
        {
            if (m_Root != null)
            {
                m_Root.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (m_Root != null && m_Root.gameObject.activeSelf)
            {
                if (IsBlockedByHigherCanvas())
                {
                    Hide();
                    return;
                }

                FollowMouse();
            }
        }

        private void FollowMouse()
        {
            RectTransform parentRect = m_Root.parent as RectTransform;
            if (parentRect == null)
            {
                m_Root.position = Input.mousePosition;
                return;
            }

            Camera uiCamera = m_Canvas != null && m_Canvas.renderMode == RenderMode.ScreenSpaceCamera
                ? m_Canvas.worldCamera
                : null;

            // 先把鼠标屏幕坐标转换到父容器的本地空间（已经把 CanvasScaler 缩放换算掉了），
            // 再叠加"画布本地设计单位"的偏移，任何分辨率下相对光标的视觉距离都一致。
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, uiCamera, out Vector2 localPoint))
            {
                m_Root.localPosition = localPoint + m_ScreenOffset;
            }
        }

        /// <summary>
        /// 若指针位置的最上层 UI 命中不是 Buff 图标自身（或其子物体），说明有别的面板挡在上面
        /// （比如食谱面板和本提示共用同一个 Canvas，无法用 Canvas.sortingOrder 区分谁在上面，
        /// 只有实际做一次射线检测、看最上层命中的是谁，才是可靠的判断方式）。
        /// </summary>
        private bool IsBlockedByHigherCanvas()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            var pointerData = new PointerEventData(eventSystem) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);
            if (results.Count == 0)
            {
                return false;
            }

            Transform topHit = results[0].gameObject.transform;
            if (m_HoverSource != null && (topHit == m_HoverSource || topHit.IsChildOf(m_HoverSource)))
            {
                return false;
            }

            return true;
        }
    }
}

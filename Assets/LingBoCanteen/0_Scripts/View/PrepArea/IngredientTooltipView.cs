using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingBoCanteen
{
    /// <summary>
    /// 备菜区通用悬浮提示：鼠标悬停在可交互食材/收纳槽产出物上时显示名称（+ 剩余数量）。
    /// 场景内放置一个实例，挂到 Screen Space 或 World Space Canvas 下，默认隐藏。
    /// m_ScreenOffset 按"画布本地设计单位"解释（与 RectTransform 的 anchoredPosition 同一套单位），
    /// 而不是原始屏幕像素：先把鼠标屏幕坐标转换到父容器的本地空间，再加上这个偏移，
    /// 这样无论分辨率/窗口大小、CanvasScaler 缩放为多少，提示相对光标的视觉距离都保持一致。
    /// 触发源全部是世界物体（碰撞体 OnMouseEnter），所以只要指针命中了任意 UI 元素
    /// （比如食谱等模态面板），就说明有东西挡在上面，提示会被压下不显示。
    /// </summary>
    public class IngredientTooltipView : MonoBehaviour
    {
        public static IngredientTooltipView Instance { get; private set; }

        [SerializeField] private RectTransform m_Root;
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private TMP_Text m_CountText;
        [SerializeField] private Vector2 m_ScreenOffset = new Vector2(16f, 16f);
        [SerializeField] private Camera m_WorldCamera;

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
        /// 显示名称 + 数量（isUnlimited 为 true 时数量显示"无限"）。
        /// hoverSource 传触发方自身 transform，用于判断"挡住的是否是别的面板"；有遮挡时不显示。
        /// </summary>
        public void Show(Vector3 worldPosition, string name, int count, bool isUnlimited, Transform hoverSource = null)
        {
            m_HoverSource = hoverSource;
            if (m_Root == null || IsBlockedByHigherCanvas())
            {
                return;
            }

            m_Root.gameObject.SetActive(true);
            if (m_NameText != null)
            {
                m_NameText.text = name;
            }

            if (m_CountText != null)
            {
                m_CountText.text = isUnlimited ? "无限" : count.ToString();
            }

            FollowWorldPosition(worldPosition);
        }

        /// <summary>
        /// 只显示名称，不显示数量（收纳槽内已产出的物品用这个）。
        /// hoverSource 传触发方自身 transform，用于判断"挡住的是否是别的面板"；有遮挡时不显示。
        /// </summary>
        public void ShowNameOnly(Vector3 worldPosition, string name, Transform hoverSource = null)
        {
            m_HoverSource = hoverSource;
            if (m_Root == null || IsBlockedByHigherCanvas())
            {
                return;
            }

            m_Root.gameObject.SetActive(true);
            if (m_NameText != null)
            {
                m_NameText.text = name;
            }

            if (m_CountText != null)
            {
                m_CountText.text = string.Empty;
            }

            FollowWorldPosition(worldPosition);
        }

        public void Hide()
        {
            if (m_Root != null)
            {
                m_Root.gameObject.SetActive(false);
            }
        }

        private void FollowWorldPosition(Vector3 worldPosition)
        {
            if (m_Root == null)
            {
                return;
            }

            RectTransform parentRect = m_Root.parent as RectTransform;
            if (parentRect == null)
            {
                m_Root.position = Input.mousePosition;
                return;
            }

            Camera uiCamera = m_Canvas != null && m_Canvas.renderMode == RenderMode.ScreenSpaceCamera
                ? m_Canvas.worldCamera
                : null;

            // 先把鼠标屏幕坐标转换到父容器的本地空间（这一步已经把 CanvasScaler 的缩放换算掉了），
            // 再叠加"画布本地设计单位"的偏移，偏移量在任何分辨率下相对光标/其他 UI 的视觉距离都一致。
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, uiCamera, out Vector2 localPoint))
            {
                m_Root.localPosition = localPoint + m_ScreenOffset;
            }
        }

        /// <summary>
        /// 显示期间若弹出了遮挡的 UI（如食谱页面），立即压下提示。
        /// </summary>
        private void LateUpdate()
        {
            if (m_Root != null && m_Root.gameObject.activeSelf && IsBlockedByHigherCanvas())
            {
                Hide();
            }
        }

        /// <summary>
        /// 若指针位置的最上层 UI 命中不是触发源自身（或其子物体），说明有别的面板挡在上面
        /// （比如食谱面板，它和本提示共用同一个 Canvas，无法用 Canvas.sortingOrder 区分谁在上面，
        /// 只有实际做一次射线检测、看最上层命中的是谁，才是可靠的判断方式）。
        /// 触发源大多是纯世界物体（无 UI Graphic），此时任意 UI 命中都会被判定为遮挡。
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

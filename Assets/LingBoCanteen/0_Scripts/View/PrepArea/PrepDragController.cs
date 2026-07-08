using System.Collections.Generic;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 备菜区拾取拖拽的全局控制器：负责跟随鼠标显示拾取物、松开时判定命中的加工工位，
    /// 命中则交给工位处理，否则通知来源"弹回原位"。
    /// 工位（菜板/榨汁机）通过 <see cref="ProcessStationBase"/> 基类在 OnEnable/OnDisable 中自动注册。
    /// </summary>
    public class PrepDragController : MonoBehaviour
    {
        public static PrepDragController Instance { get; private set; }

        [SerializeField] private SpriteRenderer m_GhostRenderer;
        [SerializeField] private Camera m_WorldCamera;

        private static readonly List<ProcessStationBase> s_Stations = new List<ProcessStationBase>();

        private IngredientDragPayload m_CurrentPayload;
        private bool m_IsDragging;

        public bool IsDragging => m_IsDragging;

        private void Awake()
        {
            Instance = this;
            if (m_GhostRenderer != null)
            {
                m_GhostRenderer.gameObject.SetActive(false);
                // 如果 Ghost 有 LockColliderWhileScaleSprite，设置其缩放倍数为 0.7
                LockColliderWhileScaleSprite ghostScaler = m_GhostRenderer.GetComponent<LockColliderWhileScaleSprite>();
                if (ghostScaler != null)
                {
                    ghostScaler.ScaleMultiplier = 0.7f;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void RegisterStation(ProcessStationBase station)
        {
            if (!s_Stations.Contains(station))
            {
                s_Stations.Add(station);
            }
        }

        public static void UnregisterStation(ProcessStationBase station)
        {
            s_Stations.Remove(station);
        }

        /// <summary>
        /// 开始一次拖拽，ghostSprite 优先使用食材配置的移动态资源（MoveAssetName），
        /// 未配置则由调用方传入初始 Sprite 兜底。
        /// </summary>
        public void BeginDrag(IngredientDragPayload payload, Sprite ghostSprite, Vector3 startWorldPosition)
        {
            if (m_IsDragging)
            {
                return;
            }

            m_CurrentPayload = payload;
            m_IsDragging = true;

            if (m_GhostRenderer != null)
            {
                m_GhostRenderer.sprite = ghostSprite;
                m_GhostRenderer.gameObject.SetActive(true);
                m_GhostRenderer.transform.position = startWorldPosition;
            }

            // 隐藏食材tooltip
            IngredientTooltipView.Instance?.Hide();
        }

        /// <summary>
        /// 强制取消当前的拖拽，并安全地将负载弹回到原位
        /// </summary>
        public void CancelDrag()
        {
            if (!m_IsDragging)
            {
                return;
            }

            m_IsDragging = false;
            if (m_GhostRenderer != null)
            {
                m_GhostRenderer.gameObject.SetActive(false);
            }

            IngredientDragPayload payload = m_CurrentPayload;
            m_CurrentPayload = null;

            payload?.OnReturnToOrigin?.Invoke();
        }

        private void Update()
        {
            if (!m_IsDragging)
            {
                return;
            }

            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            if (m_GhostRenderer != null)
            {
                m_GhostRenderer.transform.position = mouseWorldPosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndDrag(mouseWorldPosition);
            }
        }

        private void EndDrag(Vector3 mouseWorldPosition)
        {
            m_IsDragging = false;
            if (m_GhostRenderer != null)
            {
                m_GhostRenderer.gameObject.SetActive(false);
            }

            IngredientDragPayload payload = m_CurrentPayload;
            m_CurrentPayload = null;

            ProcessStationBase hitStation = FindStationAt(mouseWorldPosition);
            if (hitStation != null && hitStation.TryAccept(payload))
            {
                payload.OnAccepted?.Invoke();
                return;
            }

            payload.OnReturnToOrigin?.Invoke();
        }

        private ProcessStationBase FindStationAt(Vector3 worldPosition)
        {
            for (int i = 0; i < s_Stations.Count; i++)
            {
                ProcessStationBase station = s_Stations[i];
                if (station != null && station.ContainsPoint(worldPosition))
                {
                    return station;
                }
            }

            return null;
        }

        private Vector3 GetMouseWorldPosition()
        {
            Camera cam = m_WorldCamera != null ? m_WorldCamera : Camera.main;
            if (cam == null)
            {
                return Input.mousePosition;
            }

            Vector3 screenPoint = Input.mousePosition;
            screenPoint.z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(screenPoint);
        }

        /// <summary>
        /// 进入下一天时调用：重置所有备菜工具的状态为空。
        /// </summary>
        public static void ResetAllStations()
        {
            foreach (ProcessStationBase station in s_Stations)
            {
                if (station != null)
                {
                    station.ResetForNewDay();
                }
            }
        }
    }
}

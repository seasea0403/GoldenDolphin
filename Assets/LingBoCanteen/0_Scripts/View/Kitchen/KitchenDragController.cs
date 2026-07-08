using System.Collections.Generic;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 烹调区拖拽的全局控制器：跟随鼠标显示拾取物、松开时判定命中的工位（锅具/垃圾桶），
    /// 命中则交给工位处理，否则通知来源"弹回原位"。写法与备菜区 <see cref="PrepDragController"/> 完全对齐，
    /// 两套系统分开是因为负载类型（<see cref="KitchenDragPayload"/> / IngredientDragPayload）不同。
    /// </summary>
    public class KitchenDragController : MonoBehaviour
    {
        public static KitchenDragController Instance { get; private set; }

        [SerializeField] private SpriteRenderer m_GhostRenderer;
        [SerializeField] private Camera m_WorldCamera;

        private static readonly List<KitchenStationBase> s_Stations = new List<KitchenStationBase>();

        private KitchenDragPayload m_CurrentPayload;
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

        public static void RegisterStation(KitchenStationBase station)
        {
            if (!s_Stations.Contains(station))
            {
                s_Stations.Add(station);
            }
        }

        public static void UnregisterStation(KitchenStationBase station)
        {
            s_Stations.Remove(station);
        }

        public void BeginDrag(KitchenDragPayload payload, Sprite ghostSprite, Vector3 startWorldPosition)
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

            KitchenDragPayload payload = m_CurrentPayload;
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

            KitchenDragPayload payload = m_CurrentPayload;
            m_CurrentPayload = null;

            KitchenStationBase hitStation = FindStationAt(mouseWorldPosition);
            if (hitStation != null && hitStation.TryAccept(payload))
            {
                payload.OnAccepted?.Invoke();
                return;
            }

            payload.OnReturnToOrigin?.Invoke();
        }

        private KitchenStationBase FindStationAt(Vector3 worldPosition)
        {
            for (int i = 0; i < s_Stations.Count; i++)
            {
                KitchenStationBase station = s_Stations[i];
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
    }
}

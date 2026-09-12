using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 上菜铃：点击后对上菜区当前摆盘的所有菜品执行一次上菜结算。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ServiceBellController : MonoBehaviour
    {
        [SerializeField] private ServingCounterController m_ServingCounter;

        private void OnEnable()
        {
            Collider2D col = GetComponent<Collider2D>();
            Debug.Log($"[ServiceBellController] OnEnable - Collider2D.enabled={col.enabled}, GameObject.activeSelf={gameObject.activeSelf}");
        }

        private void OnMouseDown()
        {
            if (UIFormSceneInputBlocker.IsSceneInputBlocked) return;

            Collider2D col = GetComponent<Collider2D>();
            Debug.Log($"[ServiceBellController] OnMouseDown - Collider2D.enabled={col.enabled}, ServingCounter={m_ServingCounter != null}");
            m_ServingCounter?.TryServeAll();
        }
    }
}

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

        private void OnMouseDown()
        {
            m_ServingCounter?.TryServeAll();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 烹调区全局管理器：登记场景内所有 <see cref="PotController"/>（2 个炉灶 + 1 个烤箱），
    /// 供"点击上菜区"一次性收集所有已关火未糊的锅具成品菜。
    /// </summary>
    public class KitchenManager : MonoBehaviour
    {
        public static KitchenManager Instance { get; private set; }

        private readonly List<PotController> m_Pots = new List<PotController>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterPot(PotController pot)
        {
            if (!m_Pots.Contains(pot))
            {
                m_Pots.Add(pot);
            }
        }

        public void UnregisterPot(PotController pot)
        {
            m_Pots.Remove(pot);
        }

        /// <summary>
        /// 点击"上菜区"（桌布区域）时调用：把所有已关火未糊的锅具成品菜依次装盘到 counter，
        /// 装盘成功的锅具会自动重置为空位；counter 满了就停止收集，留在锅里等下次再装盘。
        /// </summary>
        public void CollectReadyDishes(ServingCounterController counter)
        {
            if (counter == null)
            {
                return;
            }

            foreach (PotController pot in m_Pots)
            {
                if (pot == null || !pot.IsReadyToServe)
                {
                    continue;
                }

                if (!counter.HasEmptySlot())
                {
                    break;
                }

                if (pot.TryPlate(out int dishId, out Sprite sprite))
                {
                    counter.TryPlaceDish(dishId, sprite);
                }
            }
        }
    }
}

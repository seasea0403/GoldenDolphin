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
        private static KitchenManager s_Instance;

        /// <summary>
        /// 惰性查找：PotController.OnEnable 可能先于 KitchenManager.Awake 执行，
        /// 若只在 Awake 里赋值会导致锅具注册被静默跳过（登记锅具数=0，桌布永远收不到菜）。
        /// </summary>
        public static KitchenManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<KitchenManager>();
                }

                return s_Instance;
            }
            private set
            {
                s_Instance = value;
            }
        }

        private readonly List<PotController> m_Pots = new List<PotController>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
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

            Debug.Log($"[KitchenManager] CollectReadyDishes: 登记锅具数={m_Pots.Count}");
            foreach (PotController pot in m_Pots)
            {
                if (pot == null || !pot.IsReadyToServe)
                {
                    Debug.Log($"[KitchenManager] 跳过锅具 {(pot != null ? pot.name : "null")}：未处于已关火(ReadyToServe)状态");
                    continue;
                }

                if (!counter.HasEmptySlot())
                {
                    Debug.Log("[KitchenManager] 上菜区无空槽位，停止收集");
                    break;
                }

                if (pot.TryPlate(out int dishId, out Sprite sprite))
                {
                    bool placed = counter.TryPlaceDish(dishId, sprite);
                    Debug.Log($"[KitchenManager] 装盘: dishId={dishId}, sprite={(sprite != null ? sprite.name : "null")}, placed={placed}");
                }
            }
        }

        /// <summary>
        /// 进入下一天时调用：重置所有锅具的状态为空。
        /// </summary>
        public void ResetAllPots()
        {
            foreach (PotController pot in m_Pots)
            {
                if (pot != null)
                {
                    pot.ResetForNewDay();
                }
            }
        }
    }
}

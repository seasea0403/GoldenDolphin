using System;
using System.Collections.Generic;
using GameFramework.DataTable;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 菜品解锁状态与图标查询接口。
    /// </summary>
    public interface IDishUnlockService
    {
        /// <summary>
        /// 获取截至当前天数已解锁的所有菜品 Id。
        /// </summary>
        List<int> GetUnlockedDishIds(int currentDay);

        /// <summary>
        /// 获取菜品的抽取权重。
        /// </summary>
        int GetDishWeight(int dishId);

        /// <summary>
        /// 获取菜品图标。
        /// </summary>
        Sprite GetDishIcon(int dishId);
    }

    /// <summary>
    /// 正式实现：解锁天数来自 Day 表的 UnlockDishIds（逐日累加），权重来自 Dish 表的 Weight。
    /// 使用前需要保证 ProcedureLaunch 里已经预加载了 "Day" 和 "Dish" 两张数据表。
    /// </summary>
    public class DishUnlockService : IDishUnlockService
    {
        public List<int> GetUnlockedDishIds(int currentDay)
        {
            HashSet<int> unlockedIds = new HashSet<int>();

            IDataTable<DRDay> dayTable = GameEntry.DataTable.GetDataTable<DRDay>();
            DRDay[] unlockedDayRows = dayTable.GetDataRows(row => row.Id <= currentDay);
            foreach (DRDay dayRow in unlockedDayRows)
            {
                AppendIdsObject(dayRow.UnlockDishIds, unlockedIds);
            }

            return new List<int>(unlockedIds);
        }

        public int GetDishWeight(int dishId)
        {
            DRDish dishRow = GameEntry.DataTable.GetDataTable<DRDish>().GetDataRow(dishId);
            return dishRow != null ? dishRow.Weight : 1;
        }

        public Sprite GetDishIcon(int dishId)
        {
            // TODO: 按 DRDish.AssetName 通过 GameEntry.Resource 加载/缓存对应 Sprite。
            return null;
        }

        private static void AppendIdsObject(object idsOrString, HashSet<int> result)
        {
            if (idsOrString == null)
            {
                return;
            }

            // 支持多种由 DR 生成器可能输出的类型：int[] 或 string
            if (idsOrString is int[] ids)
            {
                foreach (int id in ids)
                {
                    result.Add(id);
                }
                return;
            }

            if (idsOrString is string idListString)
            {
                if (string.IsNullOrEmpty(idListString))
                {
                    return;
                }

                string[] parts = idListString.Split(',');
                foreach (string part in parts)
                {
                    string trimmed = part.Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    if (int.TryParse(trimmed, out int id))
                    {
                        result.Add(id);
                    }
                }
                return;
            }

            // 其它可尝试的类型（List<int> 等）
            if (idsOrString is System.Collections.IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item is int ii)
                    {
                        result.Add(ii);
                    }
                    else if (item is string s && int.TryParse(s, out int sid))
                    {
                        result.Add(sid);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 按权重、不放回地随机抽取若干个不重复的菜品。
    /// </summary>
    public static class WeightedDishSelector
    {
        public static List<int> PickDishes(List<int> unlockedDishIds, Func<int, int> weightGetter, int count)
        {
            List<int> pool = new List<int>(unlockedDishIds);
            List<int> result = new List<int>();
            count = Mathf.Min(count, pool.Count);

            for (int n = 0; n < count; n++)
            {
                int totalWeight = 0;
                for (int i = 0; i < pool.Count; i++)
                {
                    totalWeight += Mathf.Max(1, weightGetter(pool[i]));
                }

                int roll = UnityEngine.Random.Range(0, totalWeight);
                int accumulated = 0;
                int pickIndex = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    accumulated += Mathf.Max(1, weightGetter(pool[i]));
                    if (roll < accumulated)
                    {
                        pickIndex = i;
                        break;
                    }
                }

                result.Add(pool[pickIndex]);
                pool.RemoveAt(pickIndex);
            }

            return result;
        }
    }
}

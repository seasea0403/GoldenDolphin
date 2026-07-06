using System;
using System.Collections.Generic;
using GameFramework.DataTable;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 烹调区配方匹配工具：基于 Dish 表做"候选菜品逐步收窄 + 完全匹配判定"。
    /// 完全通用，不区分食材/调料/半成品菜品 Id —— 番茄肉酱面(2013)靠先做出番茄肉酱(2014)
    /// 再把 2014 当成一件新食材投入的嵌套配方规则，正是依赖这里的匹配逻辑对 Id 来源不做任何区分才能生效。
    /// </summary>
    public static class DishRecipeUtility
    {
        /// <summary>
        /// 单个菜品配方的解析结果缓存。
        /// </summary>
        public class DishRecipe
        {
            public int DishId;
            public int PotType;
            public int[] IngIds;
            public int[] SeasoningIds;

            /// <summary>
            /// 是否为"中间配料"菜品：Weight&lt;=0（Dish 表里 Weight 留空/0）表示该菜永远不会被顾客点单抽中，
            /// 例如番茄肉酱(2014)——做完不取出、不上菜，而是作为新食材留在锅里继续参与下一步配方匹配。
            /// </summary>
            public bool IsIntermediate;

            public int TotalItemCount => IngIds.Length + SeasoningIds.Length;

            public bool ContainsItem(int itemId)
            {
                return Array.IndexOf(IngIds, itemId) >= 0 || Array.IndexOf(SeasoningIds, itemId) >= 0;
            }
        }

        private static Dictionary<int, DishRecipe> s_RecipeCache;

        private static Dictionary<int, DishRecipe> GetAllRecipes()
        {
            if (s_RecipeCache != null)
            {
                return s_RecipeCache;
            }

            s_RecipeCache = new Dictionary<int, DishRecipe>();

            IDataTable<DRDish> dishTable = GameEntry.DataTable.GetDataTable<DRDish>();
            if (dishTable == null)
            {
                Log.Warning("DishRecipeUtility：Dish 数据表尚未加载。");
                return s_RecipeCache;
            }

            DRDish[] rows = dishTable.GetAllDataRows();
            foreach (DRDish row in rows)
            {
                if (string.IsNullOrEmpty(row.Pot) || !int.TryParse(row.Pot, out int potType))
                {
                    continue;
                }

                s_RecipeCache[row.Id] = new DishRecipe
                {
                    DishId = row.Id,
                    PotType = potType,
                    IngIds = DataTableExtension.ParseInt32Array(row.IngList),
                    SeasoningIds = DataTableExtension.ParseInt32Array(row.SeasoningList),
                    IsIntermediate = row.Weight <= 0,
                };
            }

            return s_RecipeCache;
        }

        /// <summary>
        /// 获取指定菜品的配方，找不到返回 null。
        /// </summary>
        public static DishRecipe GetRecipe(int dishId)
        {
            GetAllRecipes().TryGetValue(dishId, out DishRecipe recipe);
            return recipe;
        }

        /// <summary>
        /// 投放第一件食材/调料/半成品菜品时调用：找出锅具类型匹配且包含该 Id 的所有候选菜品。
        /// </summary>
        public static List<int> GetCandidateDishIds(int potType, int firstItemId)
        {
            List<int> result = new List<int>();
            foreach (DishRecipe recipe in GetAllRecipes().Values)
            {
                if (recipe.PotType == potType && recipe.ContainsItem(firstItemId))
                {
                    result.Add(recipe.DishId);
                }
            }

            return result;
        }

        /// <summary>
        /// 在已有候选菜品的基础上，用新投放的一件食材/调料继续收窄候选集合。
        /// </summary>
        public static List<int> FilterCandidates(List<int> candidates, int newItemId)
        {
            List<int> result = new List<int>();
            Dictionary<int, DishRecipe> all = GetAllRecipes();
            foreach (int dishId in candidates)
            {
                if (all.TryGetValue(dishId, out DishRecipe recipe) && recipe.ContainsItem(newItemId))
                {
                    result.Add(dishId);
                }
            }

            return result;
        }

        /// <summary>
        /// 在候选菜品中查找与当前已投放食材/调料"完全匹配"（数量与内容都对上）的菜品，找不到返回 -1。
        /// 完全匹配是开火按钮可点击的前提条件。
        /// </summary>
        public static int FindExactMatch(List<int> candidates, List<int> placedItemIds)
        {
            Dictionary<int, DishRecipe> all = GetAllRecipes();
            foreach (int dishId in candidates)
            {
                if (!all.TryGetValue(dishId, out DishRecipe recipe))
                {
                    continue;
                }

                if (recipe.TotalItemCount != placedItemIds.Count)
                {
                    continue;
                }

                bool allMatch = true;
                foreach (int itemId in placedItemIds)
                {
                    if (!recipe.ContainsItem(itemId))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    return dishId;
                }
            }

            return -1;
        }
    }
}

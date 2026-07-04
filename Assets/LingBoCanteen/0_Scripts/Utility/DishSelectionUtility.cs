using System;
using System.Collections.Generic;
using GameFramework.DataTable;
using GameFramework.Resource;
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
        // 图标缓存使用静态字段：GameFramework 的 EditorResourceComponent 是把 LoadAsset 请求排队到下一帧 Update 里才真正加载，
        // 无法在调用当帧同步拿到结果，因此改为在 ProcedurePreload 阶段统一预加载，GetDishIcon 只做同步查表。
        private static readonly Dictionary<int, Sprite> s_DishIconCache = new Dictionary<int, Sprite>();

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
            if (s_DishIconCache.TryGetValue(dishId, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            // 兜底：理论上 ProcedurePreload 已经预加载好全部菜品图标，这里不应该 miss。
            DRDish dishRow = GameEntry.DataTable.GetDataTable<DRDish>().GetDataRow(dishId);
            if (dishRow == null || string.IsNullOrEmpty(dishRow.AssetName))
            {
                return null;
            }

            string assetPath = AssetUtility.GetDishIconAsset(dishRow.AssetName);

#if UNITY_EDITOR
            // GameFramework 的 EditorResourceComponent 要等到下一帧 Update 才处理 LoadAsset 请求，
            // 本帧同步拿不到结果；这里直接用 AssetDatabase 同步加载一次并写入缓存，保证编辑器下立即可用。
            Sprite editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (editorSprite == null)
            {
                Texture2D editorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (editorTexture != null)
                {
                    editorSprite = Sprite.Create(editorTexture, new Rect(0f, 0f, editorTexture.width, editorTexture.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (editorSprite != null)
            {
                s_DishIconCache[dishId] = editorSprite;
                return editorSprite;
            }

            Log.Warning("Dish icon for dish '{0}' was not found at asset path '{1}'.", dishId, assetPath);
            return null;
#else
            Log.Warning("Dish icon for dish '{0}' was not preloaded, loading asynchronously now.", dishId);
            LoadDishIconAsync(dishId, dishRow.AssetName, null);
            return null;
#endif
        }

        /// <summary>
        /// 预加载 Dish 表中所有菜品的图标，供 ProcedurePreload 在进入游戏前统一等待完成。
        /// </summary>
        public static void PreloadAllDishIcons(Action onAllLoaded)
        {
            DRDish[] allDishRows = GameEntry.DataTable.GetDataTable<DRDish>().GetAllDataRows();
            if (allDishRows.Length == 0)
            {
                onAllLoaded?.Invoke();
                return;
            }

            int remaining = allDishRows.Length;
            foreach (DRDish dishRow in allDishRows)
            {
                int dishId = dishRow.Id;
                if (s_DishIconCache.ContainsKey(dishId) || string.IsNullOrEmpty(dishRow.AssetName))
                {
                    if (--remaining == 0)
                    {
                        onAllLoaded?.Invoke();
                    }
                    continue;
                }

                LoadDishIconAsync(dishId, dishRow.AssetName, () =>
                {
                    if (--remaining == 0)
                    {
                        onAllLoaded?.Invoke();
                    }
                });
            }
        }

        private static void LoadDishIconAsync(int dishId, string assetName, Action onComplete)
        {
            string assetPath = AssetUtility.GetDishIconAsset(assetName);
            GameEntry.Resource.LoadAsset(assetPath, Constant.AssetPriority.DishIconAsset, new LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    Sprite sprite = asset as Sprite;
                    if (sprite == null && asset is Texture2D texture)
                    {
                        sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }

                    s_DishIconCache[dishId] = sprite;
                    onComplete?.Invoke();
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    Log.Error("Can not load dish icon for dish '{0}' from '{1}' with error message '{2}'.", dishId, loadedAssetName, errorMessage);
                    s_DishIconCache[dishId] = null;
                    onComplete?.Invoke();
                }));
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

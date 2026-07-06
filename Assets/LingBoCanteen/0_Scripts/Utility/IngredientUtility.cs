using System.Collections.Generic;
using GameFramework;
using GameFramework.DataTable;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 食材区域判定 / 解锁状态 / 库存读写的统一入口。
    /// 库存数据落在 DataNode 的 "Storage.IngredientStockDict"（Dictionary&lt;int,int&gt;，
    /// 由 <see cref="GameDataNodeInitializer"/> 初始化，属于存档项）。
    /// 解锁天数复用 DRDay.UnlockIngIds（逐日累加，做法与 DishUnlockService 一致）。
    /// 食材美术资源统一按 "{Id}_{EnName}" 文件夹命名约定自动加载（见 <see cref="GetSprite"/>），
    /// 不需要在 Inspector 里逐个手动拖 Sprite。
    /// </summary>
    public static class IngredientUtility
    {
        // 资源缓存 Key = "{Id}/{assetName}"，避免同一张图重复加载。
        private static readonly Dictionary<string, Sprite> s_SpriteCache = new Dictionary<string, Sprite>();
        /// <summary>
        /// 根据食材 Id 区间判定所属存放区域。
        /// </summary>
        public static IngredientAreaType GetAreaType(int ingredientId)
        {
            if (ingredientId >= 1001 && ingredientId <= 1016)
            {
                return IngredientAreaType.Shelf;
            }

            if (ingredientId >= 1017 && ingredientId <= 1021)
            {
                return IngredientAreaType.Fridge;
            }

            return IngredientAreaType.Drawer;
        }

        /// <summary>
        /// 是否无库存数量限制（冰箱食材 / 抽屉食材）。
        /// </summary>
        public static bool IsUnlimitedStock(int ingredientId)
        {
            return GetAreaType(ingredientId) != IngredientAreaType.Shelf;
        }

        /// <summary>
        /// 截至 currentDay，累计已解锁的货架食材 Id 集合（读 DRDay.UnlockIngIds）。
        /// 冰箱/抽屉食材默认解锁，不受此限制。
        /// </summary>
        public static HashSet<int> GetUnlockedShelfIds(int currentDay)
        {
            HashSet<int> unlockedIds = new HashSet<int>();

            IDataTable<DRDay> dayTable = GameEntry.DataTable.GetDataTable<DRDay>();
            if (dayTable == null)
            {
                return unlockedIds;
            }

            DRDay[] unlockedDayRows = dayTable.GetDataRows(row => row.Id <= currentDay);
            foreach (DRDay dayRow in unlockedDayRows)
            {
                if (dayRow.UnlockIngIds == null)
                {
                    continue;
                }

                foreach (int id in dayRow.UnlockIngIds)
                {
                    unlockedIds.Add(id);
                }
            }

            return unlockedIds;
        }

        /// <summary>
        /// 货架食材是否已解锁（冰箱/抽屉食材恒为 true）。
        /// </summary>
        public static bool IsUnlocked(int ingredientId, int currentDay)
        {
            if (GetAreaType(ingredientId) != IngredientAreaType.Shelf)
            {
                return true;
            }

            return GetUnlockedShelfIds(currentDay).Contains(ingredientId);
        }

        /// <summary>
        /// 获取当前持有库存（冰箱/抽屉食材无意义，恒返回 0，请先用 <see cref="IsUnlimitedStock"/> 判断）。
        /// </summary>
        public static int GetStock(int ingredientId)
        {
            Dictionary<int, int> stockDict = GetStockDict();
            return stockDict != null && stockDict.TryGetValue(ingredientId, out int count) ? count : 0;
        }

        /// <summary>
        /// 增加库存（超市采购成功后调用），delta 可为负数。
        /// </summary>
        public static void AddStock(int ingredientId, int delta)
        {
            Dictionary<int, int> stockDict = GetStockDict();
            if (stockDict == null)
            {
                return;
            }

            stockDict.TryGetValue(ingredientId, out int count);
            count = System.Math.Max(0, count + delta);
            stockDict[ingredientId] = count;
        }

        /// <summary>
        /// 尝试消耗 1 份库存（拾取货架食材放入加工工位时调用），库存不足时返回 false。
        /// 冰箱/抽屉食材无库存限制，恒返回 true。
        /// </summary>
        public static bool TryConsumeStock(int ingredientId)
        {
            if (IsUnlimitedStock(ingredientId))
            {
                return true;
            }

            Dictionary<int, int> stockDict = GetStockDict();
            if (stockDict == null || !stockDict.TryGetValue(ingredientId, out int count) || count <= 0)
            {
                return false;
            }

            stockDict[ingredientId] = count - 1;
            return true;
        }

        /// <summary>
        /// 货架食材首次解锁时自动赋予默认库存（Constant.GameConstant.DEFAULT_UNLOCK_STOCK）。
        /// 用“库存字典里是否已存在该 Id 的 key”判断“是否已经发放过”，已发放过（哪怕已消耗到 0）
        /// 不会重复赋值，因此可以每次 <see cref="Ingredient.Refresh"/> 时安全地重复调用。
        /// 冰箱/抽屉食材无库存概念，直接忽略。
        /// </summary>
        public static void EnsureUnlockDefaultStock(int ingredientId)
        {
            if (GetAreaType(ingredientId) != IngredientAreaType.Shelf)
            {
                return;
            }

            Dictionary<int, int> stockDict = GetStockDict();
            if (stockDict == null || stockDict.ContainsKey(ingredientId))
            {
                return;
            }

            stockDict[ingredientId] = Constant.GameConstant.DEFAULT_UNLOCK_STOCK;
        }

        private static Dictionary<int, int> GetStockDict()
        {
            VarObject stockVar = GameEntry.DataNode.GetData<VarObject>("Storage.IngredientStockDict");
            if (stockVar == null)
            {
                Log.Warning("Storage.IngredientStockDict 尚未初始化，请确认 GameDataNodeInitializer.Initialize 已执行。");
                return null;
            }

            return stockVar.Value as Dictionary<int, int>;
        }

        /// <summary>
        /// 食材所属美术资源子文件夹（对应 Assets/LingBoCanteen/4_Arts/Foods/Ing/ 下的分类目录）。
        /// </summary>
        private static string GetAreaSubFolder(IngredientAreaType areaType)
        {
            switch (areaType)
            {
                case IngredientAreaType.Fridge:
                    return "InFridge";
                case IngredientAreaType.Drawer:
                    return "Drawer";
                default:
                    return "Common";
            }
        }

        /// <summary>
        /// 按 "{Id}_{EnName}" 文件夹约定自动加载食材美术资源（初始态/移动态/切好态/刀口态/最终态等），
        /// 无需在 Inspector 里逐个配置。assetName 传对应 DRIngredient 的 XxxAssetName 字段值，
        /// 为空（例如不可切食材没有 MidAssetName）时直接返回 null。
        /// </summary>
        public static Sprite GetSprite(DRIngredient row, string assetName)
        {
            if (row == null || string.IsNullOrEmpty(assetName))
            {
                return null;
            }

            string cacheKey = row.Id + "/" + assetName;
            if (s_SpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            string assetPath = AssetUtility.GetIngredientAsset(GetAreaSubFolder(GetAreaType(row.Id)), row.Id, row.EnName, assetName);

#if UNITY_EDITOR
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (sprite == null)
            {
                Log.Warning("食材美术资源未找到：Id={0} EnName={1} AssetName={2}，期望路径={3}（请确认文件夹按 “编号_英文名” 命名，文件按对应 AssetName 命名）。", row.Id, row.EnName, assetName, assetPath);
            }

            s_SpriteCache[cacheKey] = sprite;
            return sprite;
#else
            // 运行时走异步加载：先返回 null，加载完成后写入缓存，调用方下一次取（例如下一帧刷新）即可命中。
            GameEntry.Resource.LoadAsset(assetPath, Constant.AssetPriority.IngredientIconAsset, new LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    Sprite loadedSprite = asset as Sprite;
                    if (loadedSprite == null && asset is Texture2D texture)
                    {
                        loadedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }

                    s_SpriteCache[cacheKey] = loadedSprite;
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    Log.Error("加载食材美术资源失败：{0}，错误：{1}", loadedAssetName, errorMessage);
                    s_SpriteCache[cacheKey] = null;
                }));
            return null;
#endif
        }
    }
}

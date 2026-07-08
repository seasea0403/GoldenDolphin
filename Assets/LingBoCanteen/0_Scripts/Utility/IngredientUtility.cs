using System;
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
                Log.Warning("[IngredientUtility] DRDay 表未加载");
                return unlockedIds;
            }

            DRDay[] unlockedDayRows = dayTable.GetDataRows(row => row.Id <= currentDay);
            Log.Info($"[IngredientUtility] GetUnlockedShelfIds(currentDay={currentDay}): 查询到 {unlockedDayRows.Length} 行数据");
            
            foreach (DRDay dayRow in unlockedDayRows)
            {
                if (dayRow.UnlockIngIds == null)
                {
                    Log.Info($"[IngredientUtility] Day {dayRow.Id}: UnlockIngIds = null");
                    continue;
                }

                Log.Info($"[IngredientUtility] Day {dayRow.Id}: UnlockIngIds 数量 = {dayRow.UnlockIngIds.Length}, 内容 = [{string.Join(",", dayRow.UnlockIngIds)}]");
                
                foreach (int id in dayRow.UnlockIngIds)
                {
                    unlockedIds.Add(id);
                }
            }

            Log.Info($"[IngredientUtility] GetUnlockedShelfIds(currentDay={currentDay}): 最终解锁食材 = {{{string.Join(",", unlockedIds)}}}");
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
        /// 用"库存字典里是否已存在该 Id 的 key"判断"是否已经发放过"，已发放过（哪怕已消耗到 0）
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
                Log.Warning("食材美术资源未找到：Id={0} EnName={1} AssetName={2}，期望路径={3}（请确认文件夹按 编号_英文名 命名，文件按对应 AssetName 命名）。", row.Id, row.EnName, assetName, assetPath);
            }

            s_SpriteCache[cacheKey] = sprite;
            return sprite;
#else
            // 运行时从 AssetBundle 加载食材美术资源。
            // UGF 的 LoadAsset 是异步回调 API（最少需要 assetName + LoadAssetCallbacks 两个参数，
            // 且返回值为 void），无法在此同步方法中直接获取 Sprite。
            // Sprite 需要在资源初始化阶段通过 LoadAssetCallbacks 预加载到 s_SpriteCache。
            // 若运行时缓存未命中，说明该资源未被预加载。
            Sprite sprite = null;
            s_SpriteCache.TryGetValue(cacheKey, out sprite);

            if (sprite == null)
            {
                Log.Error("食材美术资源未预加载：{0}（请确保在 Ingredient 初始化流程中已通过 LoadAsset 异步预加载 Sprite）。", assetPath);
            }

            s_SpriteCache[cacheKey] = sprite;
            return sprite;
#endif
        }

        /// <summary>
        /// 预加载 Ingredient 表中所有食材的美术资源（初始态/移动态/中间产物/最终态等），
        /// 供 ProcedureLaunch 在进入游戏前统一等待完成。
        /// </summary>
        public static void PreloadAllIngredientSprites(Action onAllLoaded)
        {
            IDataTable<DRIngredient> ingredientTable = GameEntry.DataTable.GetDataTable<DRIngredient>();
            if (ingredientTable == null)
            {
                onAllLoaded?.Invoke();
                return;
            }

            DRIngredient[] allRows = ingredientTable.GetAllDataRows();
            if (allRows.Length == 0)
            {
                onAllLoaded?.Invoke();
                return;
            }

            // 统计需要加载的总资源数（跳过空的AssetName）
            int totalToLoad = 0;
            int loadedCount = 0;

            // 第一遍：统计总数
            foreach (DRIngredient row in allRows)
            {
                if (!string.IsNullOrEmpty(row.InitialAssetName))
                    totalToLoad++;
                if (!string.IsNullOrEmpty(row.MoveAssetName))
                    totalToLoad++;
                if (!string.IsNullOrEmpty(row.MidAssetName))
                    totalToLoad++;
                if (!string.IsNullOrEmpty(row.CutKnobName))
                    totalToLoad++;
                if (!string.IsNullOrEmpty(row.FinalAssetName))
                    totalToLoad++;
            }

            // 如果没有资源需要加载，直接完成
            if (totalToLoad == 0)
            {
                onAllLoaded?.Invoke();
                return;
            }

            // 第二遍：真正加载
            foreach (DRIngredient row in allRows)
            {
                // 加载初始资源
                if (!string.IsNullOrEmpty(row.InitialAssetName))
                {
                    LoadIngredientSpriteAsync(row, row.InitialAssetName, () =>
                    {
                        if (++loadedCount == totalToLoad)
                        {
                            Log.Info("All ingredient sprites preloaded.");
                            onAllLoaded?.Invoke();
                        }
                    });
                }

                // 加载移动态资源
                if (!string.IsNullOrEmpty(row.MoveAssetName))
                {
                    LoadIngredientSpriteAsync(row, row.MoveAssetName, () =>
                    {
                        if (++loadedCount == totalToLoad)
                        {
                            Log.Info("All ingredient sprites preloaded.");
                            onAllLoaded?.Invoke();
                        }
                    });
                }

                // 加载中间产物资源
                if (!string.IsNullOrEmpty(row.MidAssetName))
                {
                    LoadIngredientSpriteAsync(row, row.MidAssetName, () =>
                    {
                        if (++loadedCount == totalToLoad)
                        {
                            Log.Info("All ingredient sprites preloaded.");
                            onAllLoaded?.Invoke();
                        }
                    });
                }

                // 加载二次处理产物资源
                if (!string.IsNullOrEmpty(row.CutKnobName))
                {
                    LoadIngredientSpriteAsync(row, row.CutKnobName, () =>
                    {
                        if (++loadedCount == totalToLoad)
                        {
                            Log.Info("All ingredient sprites preloaded.");
                            onAllLoaded?.Invoke();
                        }
                    });
                }

                // 加载最终产物资源
                if (!string.IsNullOrEmpty(row.FinalAssetName))
                {
                    LoadIngredientSpriteAsync(row, row.FinalAssetName, () =>
                    {
                        if (++loadedCount == totalToLoad)
                        {
                            Log.Info("All ingredient sprites preloaded.");
                            onAllLoaded?.Invoke();
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 异步加载单个食材Sprite到缓存。
        /// </summary>
        private static void LoadIngredientSpriteAsync(DRIngredient row, string assetName, Action onComplete)
        {
            string cacheKey = row.Id + "/" + assetName;
            if (s_SpriteCache.ContainsKey(cacheKey))
            {
                // 已在缓存中，直接回调
                onComplete?.Invoke();
                return;
            }

            string assetPath = AssetUtility.GetIngredientAsset(GetAreaSubFolder(GetAreaType(row.Id)), row.Id, row.EnName, assetName);

            GameEntry.Resource.LoadAsset(assetPath, Constant.AssetPriority.IngredientIconAsset, new LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    Sprite sprite = asset as Sprite;
                    if (sprite == null && asset is Texture2D texture)
                    {
                        sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }

                    s_SpriteCache[cacheKey] = sprite;
                    if (sprite == null)
                    {
                        Log.Warning("食材美术资源加载为空：Id={0} EnName={1} AssetName={2}，路径={3}", row.Id, row.EnName, assetName, assetPath);
                    }

                    onComplete?.Invoke();
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    Log.Error("食材美术资源加载失败：Id={0} EnName={1} AssetName={2}，路径={3}，错误={4}", row.Id, row.EnName, assetName, assetPath, errorMessage);
                    s_SpriteCache[cacheKey] = null;
                    onComplete?.Invoke();
                }));
        }
    }
}

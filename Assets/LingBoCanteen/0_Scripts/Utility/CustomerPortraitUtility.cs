using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 顾客立绘/头像加载与缓存工具。
    /// </summary>
    public static class CustomerPortraitUtility
    {
        private static readonly Dictionary<string, Sprite> s_PortraitCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 获取顾客立绘 Sprite。
        /// 编辑器下同步返回，非编辑器下（打包后）异步加载并回调。
        /// </summary>
        public static Sprite GetPortrait(string assetName, Action<Sprite> onComplete = null)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }

            if (s_PortraitCache.TryGetValue(assetName, out Sprite cachedSprite))
            {
                onComplete?.Invoke(cachedSprite);
                return cachedSprite;
            }

            string assetPath = AssetUtility.GetCustomerPortraitAsset(assetName);

#if UNITY_EDITOR
            // 编辑器下直接用 AssetDatabase 同步加载并缓存，保证测试顺畅
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (sprite != null)
            {
                s_PortraitCache[assetName] = sprite;
                onComplete?.Invoke(sprite);
                return sprite;
            }

            Log.Warning("无法在路径 '{0}' 找到顾客立绘资源。", assetPath);
            onComplete?.Invoke(null);
            return null;
#else
            // 非编辑器下，走异步加载
            GameEntry.Resource.LoadAsset(assetPath, Constant.AssetPriority.IngredientIconAsset, new GameFramework.Resource.LoadAssetCallbacks(
                (loadedAssetName, asset, duration, userData) =>
                {
                    Sprite loadedSprite = asset as Sprite;
                    if (loadedSprite == null && asset is Texture2D texture)
                    {
                        loadedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }

                    s_PortraitCache[assetName] = loadedSprite;
                    onComplete?.Invoke(loadedSprite);
                },
                (loadedAssetName, status, errorMessage, userData) =>
                {
                    Log.Error("异步加载顾客立绘 '{0}' 失败，错误信息: '{1}'", assetName, errorMessage);
                    s_PortraitCache[assetName] = null;
                    onComplete?.Invoke(null);
                }));

            return null;
#endif
        }
    }
}

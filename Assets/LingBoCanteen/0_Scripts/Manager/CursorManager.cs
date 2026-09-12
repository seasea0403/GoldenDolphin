using System;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 自定义鼠标光标管理器。
    /// 默认显示手指光标，按住鼠标左键时切换为抓取光标。
    /// </summary>
    public class CursorManager : MonoSingleton<CursorManager>
    {
        private const string NormalCursorName = "Mouse";
        private const string DraggingCursorName = "Mouse_Dragginng";

        // 热点（点击生效点）取指尖位置，按贴图尺寸比例计算
        private static readonly Vector2 NormalHotspotFactor = new Vector2(0.28f, 0.18f);
        private static readonly Vector2 DraggingHotspotFactor = new Vector2(0.30f, 0.22f);

        private Texture2D m_NormalCursor;
        private Texture2D m_DraggingCursor;
        private bool m_IsLoaded;
        private bool m_CurrentIsDragging;

        /// <summary>
        /// 由 ProcedureLaunch 在资源系统初始化完成后调用，开始加载光标贴图。
        /// </summary>
        public void PrepareCursors()
        {
            if (m_IsLoaded)
            {
                return;
            }

            LoadCursorTexture(NormalCursorName, texture =>
            {
                m_NormalCursor = ScaleTexture2X(texture);
                TryApplyDefaultCursor();
            });

            LoadCursorTexture(DraggingCursorName, texture =>
            {
                m_DraggingCursor = ScaleTexture2X(texture);
                TryApplyDefaultCursor();
            });
        }

        private void TryApplyDefaultCursor()
        {
            if (m_IsLoaded || m_NormalCursor == null || m_DraggingCursor == null)
            {
                return;
            }

            m_IsLoaded = true;
            ApplyCursor(false);
        }

        private void Update()
        {
            if (!m_IsLoaded)
            {
                return;
            }

            bool isDragging = Input.GetMouseButton(0);
            if (isDragging != m_CurrentIsDragging)
            {
                ApplyCursor(isDragging);
            }
        }

        private void ApplyCursor(bool isDragging)
        {
            m_CurrentIsDragging = isDragging;
            Texture2D texture = isDragging ? m_DraggingCursor : m_NormalCursor;
            Vector2 hotspotFactor = isDragging ? DraggingHotspotFactor : NormalHotspotFactor;
            Vector2 hotspot = new Vector2(texture.width * hotspotFactor.x, texture.height * hotspotFactor.y);
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }

        /// <summary>
        /// 将贴图放大 2 倍（最近邻插值），硬件光标按贴图像素尺寸显示。
        /// </summary>
        private static Texture2D ScaleTexture2X(Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            Texture2D scaled = new Texture2D(width * 2, height * 2, TextureFormat.RGBA32, false);
            Color32[] srcPixels = source.GetPixels32();
            Color32[] dstPixels = new Color32[srcPixels.Length * 4];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 pixel = srcPixels[y * width + x];
                    int dy = y * 2;
                    int dx = x * 2;
                    dstPixels[dy * width * 2 + dx] = pixel;
                    dstPixels[dy * width * 2 + dx + 1] = pixel;
                    dstPixels[(dy + 1) * width * 2 + dx] = pixel;
                    dstPixels[(dy + 1) * width * 2 + dx + 1] = pixel;
                }
            }

            scaled.SetPixels32(dstPixels);
            scaled.Apply();
            return scaled;
        }

        private void LoadCursorTexture(string assetName, Action<Texture2D> onLoaded)
        {
            string assetPath = AssetUtility.GetUICursorAsset(assetName);

#if UNITY_EDITOR
            // 编辑器下直接同步加载：EditorResourceMode 为 0 时走真 Bundle，新增资源在未重新构建前会加载失败。
            Texture2D editorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (editorTexture != null)
            {
                onLoaded(editorTexture);
                return;
            }
#endif
            GameEntry.Resource.LoadAsset(assetPath, Constant.AssetPriority.UIFormAsset, new LoadAssetCallbacks(
                (asset, asset1, duration, userData) => onLoaded(asset1 as Texture2D),
                (asset, status, errorMessage, userData) => Log.Warning("Can not load cursor texture '{0}' with error message '{1}'.", asset, errorMessage)));
        }
    }
}

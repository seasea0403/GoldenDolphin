//------------------------------------------------------------
// LingBoCanteen 自定义工具
// 用途：自动扫描运行时通过 AssetUtility 路径加载的资源文件夹，
//       为它们分配 AssetBundle 名称，再调用 UGF 自带的
//       ResourceSyncToolsController.SyncFromProject() 生成/刷新
//       Assets/GameFramework/Configs/ResourceCollection.xml。
//
// 使用方法：
//   Unity 菜单栏 -> LingBoCanteen -> Build Resource Collection (Package Mode)
//
// 注意：
//   1. 只需要收集"运行时通过路径字符串动态加载"的资源
//      （即 AssetUtility.cs 里各 GetXxxAsset 方法拼出来的路径对应的文件夹）。
//      其余被 Prefab/Scene 直接以字段引用的贴图、动画等资源，
//      Unity 在构建 AssetBundle 时会自动作为依赖打进去，不需要单独收集。
//   2. 通过 Resources.Load() 加载的资源（例如 DialogueAssets、
//      TextMeshPro 字体等）必须放在名为 "Resources" 的文件夹下，
//      这套资源完全不走 UGF 的 AssetBundle 系统，也不需要写进
//      ResourceCollection.xml。
//   3. 执行前请确认场景已保存；执行后请打开
//      Game Framework -> Resource Tools -> Resource Editor
//      核对一下资源列表，再用 Resource Builder 构建 AssetBundle。
//------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor.ResourceTools;

namespace LingBoCanteen.Editor
{
    internal static class ResourceCollectionBuilder
    {
        // 与 UGF ResourceCollection.cs 里的 ResourceNameRegex 保持一致：
        // 只允许英文字母、数字、点、下划线、中划线、斜杠，
        // 不允许空格、中文、$、~ 等字符。
        private static readonly Regex ResourceNameRegex = new Regex(@"^([A-Za-z0-9\._-]+/)*[A-Za-z0-9\._-]+$");

        // 需要整个排除的子目录（相对于 RootPath），
        // 例如 DataTables/Excel 只是配置表的源 Excel 文件，不是运行时资源。
        private static readonly string[] ExcludedFolders =
        {
            "DataTables/Excel",
        };

        // 仅收集这些后缀的文件，避免误收 .xlsx、~$等非资源文件
        private static readonly string[] AllowedExtensions =
        {
            ".txt", ".bytes", ".ttf", ".otf", ".unity",
            ".wav", ".mp3", ".ogg",
            ".prefab", ".png", ".jpg", ".jpeg",
        };
        // 与 AssetUtility.cs 中 GetXxxAsset 方法一一对应的资源根目录
        // （相对于 Assets/LingBoCanteen/）。
        private static readonly string[] CollectFolders =
        {
            "Configs",
            "DataTables",
            "Fonts",
            "Scenes",
            "Music",
            "Sounds",
            "Entities",
            "4_Arts/Foods/Dishes",
            "4_Arts/Customers",
            "4_Arts/Foods/Ing",
            "UI/UIForms",
            "UI/UIItems",
        };

        private const string RootPath = "Assets/LingBoCanteen";

        [MenuItem("LingBoCanteen/Build Resource Collection (Package Mode)")]
        private static void Build()
        {
            if (!EditorUtility.DisplayDialog(
                "生成 ResourceCollection.xml",
                "即将扫描以下文件夹并为其中的资源分配 AssetBundle 名称：\n\n" +
                string.Join("\n", CollectFolders.Select(f => $"{RootPath}/{f}")) +
                "\n\n此操作会修改这些资源的 AssetBundle 归属设置，建议先提交/备份工程。是否继续？",
                "继续", "取消"))
            {
                return;
            }

            int assignedCount = 0;
            int skippedCount = 0;
            List<string> invalidNames = new List<string>();
            List<string> failedNames = new List<string>();

            // 直接往内存里的 ResourceCollection 对象写入，不依赖
            // ResourceSyncToolsController.SyncFromProject() 里的
            // AssetDatabase.GetUsedAssetBundleNames()（该接口在批量刚分配完
            // AssetBundle 名称后可能还没刷新缓存，导致读到空列表、
            // 生成一个"看起来成功但内容是空的" ResourceCollection.xml）。
            ResourceCollection resourceCollection = new ResourceCollection();

            foreach (string relativeFolder in CollectFolders)
            {
                string fullFolder = Utility_CombinePath(RootPath, relativeFolder);
                if (!AssetDatabase.IsValidFolder(fullFolder))
                {
                    Debug.LogWarning($"[ResourceCollectionBuilder] 文件夹不存在，已跳过: {fullFolder}");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { fullFolder });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // 跳过文件夹本身
                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        continue;
                    }

                    // 跳过整个被排除的子目录（如 DataTables/Excel 源文件）
                    if (ExcludedFolders.Any(excluded => assetPath.StartsWith($"{RootPath}/{excluded}/", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        skippedCount++;
                        continue;
                    }

                    // 跳过脚本、meta 等非资源文件
                    if (assetPath.EndsWith(".cs") || assetPath.EndsWith(".meta"))
                    {
                        skippedCount++;
                        continue;
                    }

                    // 只收集白名单后缀的真正资源文件（过滤掉 .xlsx、Excel锁文件等）
                    string ext = Path.GetExtension(assetPath).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(ext))
                    {
                        skippedCount++;
                        continue;
                    }

                    AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                    if (importer == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    string bundleName = BuildBundleName(assetPath);

                    // 与 UGF 同样的合法性校验，避免因为空格/特殊字符/文件名本身带 $ ~ 等导致写入失败
                    if (!ResourceNameRegex.IsMatch(bundleName))
                    {
                        invalidNames.Add(assetPath);
                        skippedCount++;
                        continue;
                    }

                    if (importer.assetBundleName != bundleName)
                    {
                        importer.assetBundleName = bundleName;
                        importer.assetBundleVariant = string.Empty;
                        importer.SaveAndReimport();
                    }

                    // 单文件 AssetBundle，assetName 为 null
                    if (!resourceCollection.HasResource(bundleName, null) &&
                        !resourceCollection.AddResource(bundleName, null, null, LoadType.LoadFromFile, false))
                    {
                        failedNames.Add($"{assetPath} (AddResource 失败)");
                        skippedCount++;
                        continue;
                    }

                    string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (!resourceCollection.AssignAsset(assetGuid, bundleName, null))
                    {
                        failedNames.Add($"{assetPath} (AssignAsset 失败)");
                        skippedCount++;
                        continue;
                    }

                    assignedCount++;
                }
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();

            if (invalidNames.Count > 0)
            {
                Debug.LogWarning("[ResourceCollectionBuilder] 以下资源因文件名包含空格/中文/特殊字符而被跳过，请重命名后重新执行：\n" + string.Join("\n", invalidNames));
            }

            if (failedNames.Count > 0)
            {
                Debug.LogWarning("[ResourceCollectionBuilder] 以下资源写入 ResourceCollection 失败：\n" + string.Join("\n", failedNames));
            }

            bool success = resourceCollection.Save();

            if (success)
            {
                Debug.Log($"[ResourceCollectionBuilder] 完成：分配 {assignedCount} 个资源（Resource 数 {resourceCollection.ResourceCount}，Asset 数 {resourceCollection.AssetCount}），跳过 {skippedCount} 个非资源/非法文件。" +
                           "ResourceCollection.xml 已刷新，请在 Resource Editor 中核对后再执行 Resource Builder。");
                string invalidHint = invalidNames.Count > 0 ? $"\n\n有 {invalidNames.Count} 个文件因命名不合法被跳过，详见 Console。" : string.Empty;
                EditorUtility.DisplayDialog("完成", $"已处理 {assignedCount} 个资源（Resource {resourceCollection.ResourceCount} / Asset {resourceCollection.AssetCount}）。\nResourceCollection.xml 已生成/刷新。{invalidHint}", "好的");
            }
            else
            {
                Debug.LogError("[ResourceCollectionBuilder] 保存 ResourceCollection.xml 失败，请检查 Console 报错。");
                EditorUtility.DisplayDialog("失败", "保存 ResourceCollection.xml 失败，请查看 Console 日志。", "好的");
            }
        }

        /// <summary>
        /// 清除本工具分配过的所有 AssetBundle 名称（不影响手动在 Inspector 里设置的其它资源）。
        /// 如果想整体重来，可以先执行这个，再执行 Build。
        /// </summary>
        [MenuItem("LingBoCanteen/Remove All Asset Bundle Names In Project")]
        private static void RemoveAll()
        {
            if (!EditorUtility.DisplayDialog(
                "清除所有 AssetBundle 名称",
                "此操作会清除工程中所有资源的 AssetBundle 归属设置。是否继续？",
                "继续", "取消"))
            {
                return;
            }

            string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (string bundleName in bundleNames)
            {
                AssetDatabase.RemoveAssetBundleName(bundleName, true);
            }

            AssetDatabase.Refresh();
            Debug.Log("[ResourceCollectionBuilder] 已清除所有 AssetBundle 名称。");
        }

        /// <summary>
        /// 根据资源相对路径生成 AssetBundle 名称：
        /// Assets/LingBoCanteen/DataTables/Ingredient.txt -> lingbocanteen/datatables/ingredient
        /// </summary>
        private static string BuildBundleName(string assetPath)
        {
            string relative = assetPath.Substring(RootPath.Length).TrimStart('/');
            string withoutExt = Path.ChangeExtension(relative, null);
            return withoutExt.Replace('\\', '/').ToLowerInvariant();
        }

        private static string Utility_CombinePath(string root, string relative)
        {
            return $"{root}/{relative}".Replace('\\', '/');
        }
    }
}

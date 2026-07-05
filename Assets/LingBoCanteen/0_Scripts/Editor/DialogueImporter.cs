using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DialogueImporter
{
    const string ConfigFolder = "Assets/LingBoCanteen/Configs/ExcelConfigs";
    const string SoFolder = "Assets/Resources/DialogueAssets";
    const string CharPath = "Assets/LingBoCanteen/Res/Char/";

    [MenuItem("Tools/剧情/导入CSV生成SO")]
    public static void Import()
    {
        // 确保目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(SoFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "DialogueAssets");

        // ★ 同时支持 .csv 和 .tsv，策划爱用哪个用哪个
        var files = new List<string>();
        if (Directory.Exists(ConfigFolder))
        {
            files.AddRange(Directory.GetFiles(ConfigFolder, "*.csv"));
            files.AddRange(Directory.GetFiles(ConfigFolder, "*.tsv"));
        }

        if (files.Count == 0)
        {
            Debug.LogWarning("ExcelConfigs 文件夹下没有找到任何 .csv 或 .tsv 文件");
            return;
        }

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string soPath = $"{SoFolder}/{fileName}.asset";

            // 创建或加载已有的 SO
            DialogueAsset asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(soPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DialogueAsset>();
                AssetDatabase.CreateAsset(asset, soPath);
            }
            asset.lines.Clear();

            // ★ 用新的 DialogueReader 读取（自动 UTF-8 + 智能分隔）
            List<string[]> rows = DialogueReader.Read(filePath);

            foreach (var row in rows)
            {
                if (row.Length < 4) continue;

                DialogueLine line = new DialogueLine
                {
                    id = int.Parse(row[0]),
                    speakerName = row[1],
                    speakingContent = row[2].Replace("\\n", "\n"), // 支持 \n 换行
                    charImage = LoadSprite(CharPath + row[3] + ".png")
                };
                asset.lines.Add(line);
            }

            EditorUtility.SetDirty(asset);
            Debug.Log($"✅ {fileName}.asset 生成完毕，共 {asset.lines.Count} 句");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("导入完成");
    }

    static Sprite LoadSprite(string path)
    {
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null) Debug.LogWarning("找不到图片：" + path);
        return sp;
    }
}
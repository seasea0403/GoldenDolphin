using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class StoryCharExtractor
{
    [MenuItem("Tools/剧情/提取剧本用到的所有字符")]
    public static void Extract()
    {
        string configFolder = "Assets/LingBoCanteen/Configs/ExcelConfigs";
        if (!Directory.Exists(configFolder))
        {
            Debug.LogError("ExcelConfigs 文件夹不存在，请检查路径！");
            return;
        }

        HashSet<char> chars = new HashSet<char>();

        string[] files = Directory.GetFiles(configFolder, "*.*");
        foreach (string file in files)
        {
            if (!file.EndsWith(".csv") && !file.EndsWith(".tsv")) continue;

            string[] lines;
            try { lines = File.ReadAllLines(file, Encoding.UTF8); }
            catch { continue; }

            foreach (string line in lines)
            {
                foreach (char c in line)
                {
                    // 汉字 (CJK统一表意文字)
                    if (c >= 0x4E00 && c <= 0x9FFF) chars.Add(c);
                    // 中文常用标点
                    else if ("，。！？；：、「」（）、…—～·《》￥".Contains(c)) chars.Add(c);
                    // 数字和英文字母
                    else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) chars.Add(c);
                    // 半角符号
                    else if ("(),.!?;:'-+= ".Contains(c)) chars.Add(c);
                }
            }
        }

        List<char> sorted = new List<char>(chars);
        sorted.Sort();
        StringBuilder sb = new StringBuilder();
        foreach (char c in sorted) sb.Append(c);

        string outDir = "Assets/Res/Font";
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        string outPath = outDir + "/story_chars.txt";
        File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log($"✅ 提取完成！剧本共用 {chars.Count} 个不重复字符，已保存到 {outPath}");
    }
}
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 读取剧情配置文件（CSV / TSV 通用）
/// 强制 UTF-8 编码，自动识别分隔符，正确处理引号包裹的字段
/// </summary>
public static class DialogueReader
{
    public static List<string[]> Read(string filePath)
    {
        List<string[]> result = new List<string[]>();

        if (!File.Exists(filePath))
        {
            UnityEngine.Debug.LogError("文件不存在: " + filePath);
            return result;
        }

        // ★ 关键：强制用 UTF-8 读取，不管 Windows 默认是什么编码
        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

        for (int i = 1; i < lines.Length; i++) // 跳过表头
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 自动判断分隔符：有 Tab 就用 Tab，没有就用逗号
            char separator = lines[i].Contains('\t') ? '\t' : ',';

            result.Add(SplitLine(lines[i], separator));
        }

        return result;
    }

    /// <summary>
    /// 解析单行，支持引号包裹（Excel 导出 CSV 时，字段内含逗号会自动加引号）
    /// </summary>
    static string[] SplitLine(string line, char separator)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        StringBuilder field = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Excel 里两个双引号 "" 代表一个字面意义上的 "
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == separator && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString());
        return fields.ToArray();
    }
}
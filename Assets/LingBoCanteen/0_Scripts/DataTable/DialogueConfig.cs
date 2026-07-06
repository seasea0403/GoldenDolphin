using System.Collections.Generic;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 单个天数的剧情配置数据
    /// </summary>
    [System.Serializable]
    public class DialogueConfig
    {
        public int dayNumber;                          // 天数
        public string plotId;                          // 剧情ID（如 "Day1"）
        public string dialogueAssetName;               // 对话资源名称（对应CSV名）
        public string associatedCharacterId;           // 关联的剧情人物ID（用于客人立绘绑定）
    }

    /// <summary>
    /// 全局剧情配置资源
    /// </summary>
    [CreateAssetMenu(fileName = "PlotConfig", menuName = "LingBoCanteen/Plot Config")]
    public class PlotConfigAsset : ScriptableObject
    {
        [SerializeField] private List<DialogueConfig> m_DialogueConfigs = new List<DialogueConfig>();

        public DialogueConfig GetConfigByDay(int dayNumber)
        {
            foreach (var config in m_DialogueConfigs)
            {
                if (config.dayNumber == dayNumber)
                    return config;
            }
            return null;
        }

        public bool HasPlotForDay(int dayNumber)
        {
            return GetConfigByDay(dayNumber) != null;
        }
    }
}

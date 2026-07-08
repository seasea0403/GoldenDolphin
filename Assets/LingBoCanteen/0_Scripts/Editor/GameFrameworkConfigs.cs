using System.IO;
using GameFramework;
using UnityGameFramework.Editor.ResourceTools;
using UnityEngine;
 
public static class GameFrameworkConfigs {
    [ResourceEditorConfigPath] 
    public static string ResourceEditorConfig = 
        Utility.Path.GetRegularPath(Path.Combine(
            Application.dataPath, 
            "LingBoCanteen/Configs/ResourceEditor.xml"));
}
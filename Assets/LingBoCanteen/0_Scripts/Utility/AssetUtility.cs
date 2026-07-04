using GameFramework;

namespace LingBoCanteen
{
    public static class AssetUtility
    {
        public static string GetConfigAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Configs/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }

        public static string GetDataTableAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/DataTables/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }

        public static string GetFontAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Fonts/{0}.ttf", assetName);
        }

        public static string GetSceneAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Scenes/{0}.unity", assetName);
        }

        public static string GetMusicAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Music/{0}.mp3", assetName);
        }

        public static string GetSoundAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Sounds/{0}.wav", assetName);
        }

        public static string GetEntityAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Entities/{0}.prefab", assetName);
        }

        public static string GetUIFormAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/UI/UIForms/{0}.prefab", assetName);
        }

        public static string GetUISoundAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/UI/UISounds/{0}.wav", assetName);
        }
    }
}

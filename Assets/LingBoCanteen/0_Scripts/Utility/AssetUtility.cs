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
            return Utility.Text.Format("Assets/LingBoCanteen/Music/{0}.wav", assetName);
        }

        public static string GetSoundAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Sounds/{0}.wav", assetName);
        }

        public static string GetEntityAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Entities/{0}.prefab", assetName);
        }

        public static string GetDishIconAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/4_Arts/Foods/Dishes/{0}.png", assetName);
        }

        public static string GetCustomerPortraitAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/4_Arts/Customers/{0}.png", assetName);
        }

        /// <summary>
        /// 按“编号_英文名”文件夹命名约定拼接食材美术资源路径。
        /// subFolder：货架食材传 "Common"，冰箱食材传 "InFridge"，抽屉食材传 "Drawer"。
        /// </summary>
        public static string GetIngredientAsset(string subFolder, int ingredientId, string enName, string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/4_Arts/Foods/Ing/{0}/{1}_{2}/{3}.png", subFolder, ingredientId, enName, assetName);
        }

        public static string GetUIFormAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/UI/UIForms/{0}.prefab", assetName);
        }

        public static string GetUISoundAsset(string assetName)
        {
            return Utility.Text.Format("Assets/LingBoCanteen/Sounds/{0}.wav", assetName);
        }
    }
}

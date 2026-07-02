using System.Runtime.InteropServices;

namespace LingBoCanteen
{
    /// <summary>
    /// 资源路径工具类。
    /// 统一维护所有资源 Asset 路径拼接规则，避免路径字符串散落在各处。
    /// </summary>
    public static class AssetUtility
    {
        // ── 基础路径 ────────────────────────────────────────────
        private const string DataTableRoot = "Assets/LingBoCanteen/Datatables/";
        private const string ConfigRoot    = "Assets/LingBoCanteen/Configs/";
        private const string SceneRoot     = "Assets/LingBoCanteen/Scenes/";
        private const string UIFormRoot    = "Assets/LingBoCanteen/UI/UIForms/";
        private const string MusicRoot     = "Assets/LingBoCanteen/Music/";
        private const string SoundRoot     = "Assets/LingBoCanteen/Sounds/";
        private const string EntityRoot    = "Assets/LingBoCanteen/Entities/";
        private const string FontRoot      = "Assets/LingBoCanteen/Fonts/";

        // ── DataTable ───────────────────────────────────────────
        /// <summary>
        /// 获取数据表资源路径。
        /// <param name="tableName">表名，如 "Ingredient"</param>
        /// <param name="fromBytes">是否加载二进制（.bytes），false 则加载文本（.txt）</param>
        /// </summary>
        public static string GetDataTableAsset(string tableName, bool fromBytes = true)
        {
            return string.Format("{0}{1}{2}", DataTableRoot, tableName, fromBytes ? ".bytes" : ".txt");
        }

        // ── Config ──────────────────────────────────────────────
        public static string GetConfigAsset(string configName)
        {
            return string.Format("{0}{1}.txt", ConfigRoot, configName);
        }

        // ── Scene ───────────────────────────────────────────────
        public static string GetSceneAsset(string sceneName)
        {
            return string.Format("{0}{1}.unity", SceneRoot, sceneName);
        }

        // ── UIForm ──────────────────────────────────────────────
        public static string GetUIFormAsset(string uiFormName)
        {
            return string.Format("{0}{1}.prefab", UIFormRoot, uiFormName);
        }

        // ── Music ───────────────────────────────────────────────
        public static string GetMusicAsset(string musicName)
        {
            return string.Format("{0}{1}.mp3", MusicRoot, musicName);
        }

        // ── Sound ───────────────────────────────────────────────
        public static string GetSoundAsset(string soundName)
        {
            return string.Format("{0}{1}.wav", SoundRoot, soundName);
        }

        // ── Entity ──────────────────────────────────────────────
        public static string GetEntityAsset(string entityName)
        {
            return string.Format("{0}{1}.prefab", EntityRoot, entityName);
        }
    }
}

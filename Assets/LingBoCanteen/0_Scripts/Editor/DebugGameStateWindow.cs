using UnityEditor;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LingBoCanteen.EditorTools
{
    /// <summary>
    /// 游戏状态调试窗口：Play 模式下直接修改 Day / San / 金币，方便测试
    /// 天数解锁、区域传送、结局分支等依赖数值的内容。
    /// 仅修改 DataNode 数值，不触发"进入下一天"的完整流程（剧情检查/顾客重刷/BGM 切换等）。
    /// 菜单：Tools → 调试 → 游戏状态调试窗口。
    /// </summary>
    public class DebugGameStateWindow : EditorWindow
    {
        [MenuItem("Tools/调试/游戏状态调试窗口")]
        private static void Open()
        {
            GetWindow<DebugGameStateWindow>("游戏状态调试");
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 模式后可修改 Day / San / 金币。", MessageType.Info);
                return;
            }

            if (GameEntry.DataNode == null || GameEntry.DataNode.GetNode("Player.San") == null)
            {
                EditorGUILayout.HelpBox("游戏数据尚未初始化（等 Launch 流程完成、进入游戏后再试）。", MessageType.Warning);
                return;
            }

            int day = ReadInt("DayCurrent.Value", 1);
            int san = ReadInt("Player.San", 50);
            int gold = ReadInt("Player.Gold", 0);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("当前状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"天数: {day}    San: {san}    金币: {gold}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("当天营业", EditorStyles.boldLabel);
            if (GUILayout.Button("立即结束当天营业（进入下一天）"))
            {
                EndBusinessDay();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("天数（改后自动刷新食材解锁显示）", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-1")) SetDay(day - 1);
                if (GUILayout.Button("+1")) SetDay(day + 1);
                if (GUILayout.Button("+5")) SetDay(day + 5);
                if (GUILayout.Button("重置为 1")) SetDay(1);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("San 值（改后触发区域传送判定）", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10")) SetSan(san - 10);
                if (GUILayout.Button("+10")) SetSan(san + 10);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("0 (地狱)")) SetSan(0);
                if (GUILayout.Button("30 (下界)")) SetSan(30);
                if (GUILayout.Button("50 (初始)")) SetSan(50);
                if (GUILayout.Button("70 (上界)")) SetSan(70);
                if (GUILayout.Button("100 (天堂)")) SetSan(100);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("金币", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-50")) SetGold(gold - 50);
                if (GUILayout.Button("+50")) SetGold(gold + 50);
                if (GUILayout.Button("+500")) SetGold(gold + 500);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "说明：\n" +
                "· 仅修改 DataNode 数值，不触发日推进完整流程（剧情/顾客/BGM 不重置）。\n" +
                "· San 修改后如需立即换区，可在游戏内正常完成一次订单结算。\n" +
                "· 区域传送等由 EveningDayFlow 在结算时统一判定。",
                MessageType.None);
        }

        private static int ReadInt(string path, int defaultValue)
        {
            if (GameEntry.DataNode.GetNode(path) == null)
            {
                return defaultValue;
            }

            VarInt32 var = GameEntry.DataNode.GetData<VarInt32>(path);
            return var != null ? var.Value : defaultValue;
        }

        private static void WriteInt(string path, int value)
        {
            GameEntry.DataNode.SetData(path, (VarInt32)value);
        }

        private static void SetDay(int day)
        {
            day = Mathf.Clamp(day, 1, Constant.GameConstant.MAX_DAY);
            WriteInt("DayCurrent.Value", day);
            Debug.Log($"[调试] DayCurrent.Value = {day}");

            // 天数影响食材解锁显示，顺手刷新备菜区货架与超市
            if (PrepAreaManager.Instance != null)
            {
                PrepAreaManager.Instance.RefreshAllShelfIngredients();
            }
        }

        private static void SetSan(int san)
        {
            san = Mathf.Clamp(san, Constant.GameConstant.SAN_BOUNDARY_MIN, Constant.GameConstant.SAN_BOUNDARY_MAX);
            WriteInt("Player.San", san);
            Debug.Log($"[调试] Player.San = {san}");
        }

        private static void SetGold(int gold)
        {
            gold = Mathf.Max(0, gold);
            WriteInt("Player.Gold", gold);
            Debug.Log($"[调试] Player.Gold = {gold}");
        }

        /// <summary>
        /// 与 SettlePanel"确定"按钮相同的流程：先按 San 判定并应用区域切换，
        /// 再走 EveningDayFlow 的完整日推进（过场/剧情/顾客重刷/锅具重置/货架刷新）。
        /// 跳过的只是结算面板本身的展示。
        /// </summary>
        private static void EndBusinessDay()
        {
            Debug.Log("[调试] 立即结束当天营业");
            EveningDayFlow.ApplyRegionChange(EveningDayFlow.GetTargetRegionForNextDay());
            EveningDayFlow.AdvanceToNextDay();
        }
    }
}

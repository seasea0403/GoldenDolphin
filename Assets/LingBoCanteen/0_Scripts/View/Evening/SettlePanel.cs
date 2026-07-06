using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 傍晚"结束今日"结算面板：由 <see cref="EveningUIController"/> 的结束今日按钮触发 <see cref="Open"/>。
    /// 展示第X天、当日金币变化（"Business.TodayEarnGold"）、当日San值变化（"Business.TodaySanDelta"）。
    /// 点击确定按钮后：
    /// - 若当前 Player.San 触及边界（&lt;=SAN_BOUNDARY_MIN 或 &gt;=SAN_BOUNDARY_MAX），
    ///   自动执行与手动点击电梯上/下按钮完全相同的效果（区域切换 + San 强制刷新为 70/30 + 标记电梯已使用），
    ///   等待 <see cref="m_BoundaryDelaySeconds"/>（默认 0.5 秒）后再进入下一天；
    /// - 否则直接进入下一天。
    /// </summary>
    public class SettlePanel : MonoBehaviour
    {
        [Header("面板根节点（SetActive 控制显隐）")]
        [SerializeField] private GameObject m_Root;

        [Header("文本显示")]
        [SerializeField] private TMP_Text m_DayText;
        [SerializeField] private TMP_Text m_GoldChangeText;
        [SerializeField] private TMP_Text m_SanChangeText;

        [Header("确定按钮")]
        [SerializeField] private Button m_ConfirmButton;

        [Header("触及San边界后，延迟进入下一天的时长（秒）")]
        [SerializeField] private float m_BoundaryDelaySeconds = 0.5f;

        private void Awake()
        {
            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (m_Root != null)
            {
                m_Root.SetActive(false);
            }
        }

        /// <summary>
        /// 打开结算面板并刷新展示数据，供 <see cref="EveningUIController"/> 调用。
        /// </summary>
        public void Open()
        {
            RefreshDisplay();

            if (m_Root != null)
            {
                m_Root.SetActive(true);
            }

            // 播放傍晚结算BGM
            BGMManager.Instance.PlayBGM(30005, true); // bgm_night_settle
        }

        private void RefreshDisplay()
        {
            int day = ReadInt("DayCurrent.Value", 1);
            int goldDelta = ReadInt("Business.TodayEarnGold", 0);
            int sanDelta = ReadInt("Business.TodaySanDelta", 0);

            if (m_DayText != null)
            {
                m_DayText.text = $"第{day}天";
            }

            if (m_GoldChangeText != null)
            {
                m_GoldChangeText.text = FormatDelta(goldDelta);
            }

            if (m_SanChangeText != null)
            {
                m_SanChangeText.text = FormatDelta(sanDelta);
            }
        }

        private void OnConfirmClicked()
        {
            if (m_Root != null)
            {
                m_Root.SetActive(false);
            }

            int san = ReadInt("Player.San", Constant.GameConstant.INITIAL_SAN);

            if (san <= Constant.GameConstant.SAN_BOUNDARY_MIN)
            {
                // 地狱结局：直接触发游戏结束
                if (GameEndingManager.Instance != null)
                {
                    GameEndingManager.Instance.TriggerHellEnding();
                }
                else
                {
                    Debug.LogError("[SettlePanel] GameEndingManager instance not found!");
                    StartCoroutine(DelayThenNextDay());
                }
            }
            else if (san >= Constant.GameConstant.SAN_BOUNDARY_MAX)
            {
                // 天堂结局：直接触发游戏结束
                if (GameEndingManager.Instance != null)
                {
                    GameEndingManager.Instance.TriggerHeavenEnding();
                }
                else
                {
                    Debug.LogError("[SettlePanel] GameEndingManager instance not found!");
                    StartCoroutine(DelayThenNextDay());
                }
            }
            else
            {
                EveningDayFlow.AdvanceToNextDay();
            }
        }

        /// <summary>
        /// 与手动点击电梯上/下按钮（<see cref="ElevatorController"/>）完全相同的效果：
        /// 切换区域、强制刷新San、标记电梯已使用。
        /// </summary>
        private void ApplyElevatorEffect(GameRegion region, int forcedSan)
        {
            if (GameEntry.DataNode == null)
            {
                return;
            }

            GameEntry.DataNode.SetData("Area.CurrentType", (VarInt32)(int)region);
            GameEntry.DataNode.SetData("Player.San", (VarInt32)forcedSan);
            GameEntry.DataNode.SetData("Area.ElevatorUsedOnce", (VarBoolean)true);

            // 播放区域切换音效
            SoundManager.Instance.PlayAreaSwitchSound();

            // 触发BGM切换（根据新的Region自动播放对应的BGM）
            if (GameBGMManager.Instance != null)
            {
                GameBGMManager.Instance.OnRegionChanged();
            }

            if (AreaSwitchManager.Instance != null)
            {
                AreaSwitchManager.Instance.ApplyRegionDecorations();
            }
        }

        private IEnumerator DelayThenNextDay()
        {
            yield return new WaitForSeconds(m_BoundaryDelaySeconds);
            EveningDayFlow.AdvanceToNextDay();
        }

        private static int ReadInt(string path, int fallback)
        {
            if (GameEntry.DataNode == null || GameEntry.DataNode.GetNode(path) == null)
            {
                return fallback;
            }

            return GameEntry.DataNode.GetData<VarInt32>(path).Value;
        }

        private static string FormatDelta(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }
    }
}

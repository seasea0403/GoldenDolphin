using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework;
using GameFramework.Event;

namespace LingBoCanteen
{
    /// <summary>
    /// 游戏背景音乐管理器
    /// 根据当前区域（Region）和时间阶段（TimeSection）智能选择和切换背景音乐
    /// 仅在游戏场景中运作（不管理菜单BGM）
    /// </summary>
    public class GameBGMManager : MonoSingleton<GameBGMManager>
    {
        /// <summary>
        /// BGM 音乐 ID 配置
        /// </summary>
        private const int MUSIC_ID_MORTAL_DAY = 30001;      // 人间区域白天经营BGM
        private const int MUSIC_ID_HEAVEN_DAY = 30002;      // 天堂区域经营BGM
        private const int MUSIC_ID_HELL_DAY = 30003;        // 地狱区域经营BGM
        private const int MUSIC_ID_EVENING_SHOPPING = 30004; // 傍晚超市采购BGM

        private int m_LastMusicId = -1; // 记录上一次播放的音乐ID，避免重复切换
        private GameRegion m_LastRegion = GameRegion.Mortal;
        private TimeSection m_LastTimeSection = TimeSection.Day;
        private bool m_IsInitialized = false;

        public override void Init()
        {
            base.Init();
            
            if (!m_IsInitialized)
            {
                m_IsInitialized = true;
                
                // 仅在游戏场景中初始化BGM管理
                // 初始化时立即播放当前状态对应的BGM
                PlayMusicForCurrentState();
            }
        }

        private void OnDestroy()
        {
            if (m_IsInitialized)
            {
                m_IsInitialized = false;
            }
        }

        /// <summary>
        /// 根据当前Region实时切换BGM（由其他系统调用）
        /// </summary>
        public void OnRegionChanged()
        {
            PlayMusicForCurrentState();
        }

        /// <summary>
        /// 根据时间阶段变化切换BGM（由其他系统调用）
        /// </summary>
        public void OnTimePhaseChanged()
        {
            PlayMusicForCurrentState();
        }

        /// <summary>
        /// 根据当前的Region和TimePhase播放对应的BGM
        /// </summary>
        private void PlayMusicForCurrentState()
        {
            if (!m_IsInitialized)
            {
                return;
            }

            // 获取当前区域
            GameRegion currentRegion = GameRegion.Mortal;
            var regionData = GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType");
            if (regionData != null)
            {
                currentRegion = (GameRegion)regionData.Value;
            }

            // 获取当前时间阶段
            TimeSection currentTimeSection = TimeSection.Day;
            var phaseData = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Phase");
            if (phaseData != null)
            {
                currentTimeSection = (TimeSection)phaseData.Value;
            }

            // 获取对应的音乐ID
            int newMusicId = GetMusicIdForState(currentRegion, currentTimeSection);

            // 如果是相同的音乐ID，则不切换
            if (newMusicId == m_LastMusicId)
            {
                return;
            }

            m_LastMusicId = newMusicId;
            m_LastRegion = currentRegion;
            m_LastTimeSection = currentTimeSection;

            // 播放新的背景音乐，使用平滑切换效果（0.5秒渐变）
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.CrossfadeBackgroundMusic(newMusicId, 0.5f);
            }
        }

        /// <summary>
        /// 根据区域和时间阶段获取对应的音乐ID
        /// </summary>
        private int GetMusicIdForState(GameRegion region, TimeSection timeSection)
        {
            // 如果是傍晚阶段，播放傍晚超市采购BGM（无论在哪个区域）
            if (timeSection == TimeSection.Evening)
            {
                return MUSIC_ID_EVENING_SHOPPING;
            }

            // 白天阶段，根据区域选择
            switch (region)
            {
                case GameRegion.Mortal:
                    return MUSIC_ID_MORTAL_DAY;
                case GameRegion.Heaven:
                    return MUSIC_ID_HEAVEN_DAY;
                case GameRegion.Hell:
                    return MUSIC_ID_HELL_DAY;
                default:
                    return MUSIC_ID_MORTAL_DAY;
            }
        }
    }
}

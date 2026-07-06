using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.DataNode;
using System;

namespace LingBoCanteen
{
    /// <summary>
    /// 游戏背景音乐管理器
    /// 根据当前区域（Region）和时间阶段（TimeSection）智能选择和切换背景音乐
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

        public override void Init()
        {
            base.Init();

            // 订阅数据节点变化事件
            GameEntry.DataNode.AddDataNodeChangeListener(this, OnDataNodeChanged);

            // 初始化播放正确的背景音乐
            UpdateBackgroundMusic();
        }

        private void OnDestroy()
        {
            // 取消订阅
            GameEntry.DataNode.RemoveDataNodeChangeListener(this, OnDataNodeChanged);
        }

        /// <summary>
        /// 数据节点变化监听回调
        /// </summary>
        private void OnDataNodeChanged(DataNodeChangedEventArgs e)
        {
            // 监听区域变化或时间阶段变化
            if (e.Name.StartsWith("Area.CurrentType") || e.Name.StartsWith("DayCurrent.Phase"))
            {
                UpdateBackgroundMusic();
            }
        }

        /// <summary>
        /// 更新背景音乐：根据当前区域和时间阶段选择合适的音乐
        /// </summary>
        private void UpdateBackgroundMusic()
        {
            // 获取当前区域
            GameRegion currentRegion = GameRegion.Mortal;
            VarInt32 regionData = GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType");
            if (regionData != null)
            {
                currentRegion = (GameRegion)regionData.Value;
            }

            // 获取当前时间阶段
            TimeSection currentTimeSection = TimeSection.Day;
            VarInt32 phaseData = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Phase");
            if (phaseData != null)
            {
                currentTimeSection = (TimeSection)phaseData.Value;
            }

            // 如果没有变化，则不切换
            if (currentRegion == m_LastRegion && currentTimeSection == m_LastTimeSection)
            {
                return;
            }

            m_LastRegion = currentRegion;
            m_LastTimeSection = currentTimeSection;

            // 根据区域和时间阶段决定播放的音乐
            int newMusicId = GetMusicIdForState(currentRegion, currentTimeSection);

            // 如果是相同的音乐ID，则不切换
            if (newMusicId == m_LastMusicId)
            {
                return;
            }

            m_LastMusicId = newMusicId;

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

        /// <summary>
        /// 手动切换背景音乐（如需特殊处理时调用）
        /// </summary>
        public void PlayMusicForRegion(GameRegion region, TimeSection timeSection)
        {
            m_LastRegion = region;
            m_LastTimeSection = timeSection;
            UpdateBackgroundMusic();
        }
    }
}

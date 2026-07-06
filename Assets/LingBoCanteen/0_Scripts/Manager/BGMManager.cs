using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening;
using GameFramework.Sound;
using GameFramework.DataTable;

namespace LingBoCanteen
{
    /// <summary>
    /// 背景音乐管理器
    /// 负责BGM的播放、切换、淡入淡出等
    /// </summary>
    public class BGMManager : MonoSingleton<BGMManager>
    {
        private int m_CurrentBGMId = -1;
        private int m_PlayingHandle = -1;
        private Tween m_FadeTween;
        
        [SerializeField] private float m_FadeDuration = 1f;

        public override void Init()
        {
            base.Init();
        }

        /// <summary>
        /// 播放指定ID的BGM，支持淡入淡出
        /// </summary>
        public void PlayBGM(int musicId, bool fadeIn = true)
        {
            // 如果已经在播放同一个BGM，则跳过
            if (m_CurrentBGMId == musicId)
            {
                return;
            }

            m_CurrentBGMId = musicId;

            // 停止之前的BGM淡出效果
            if (m_FadeTween != null && m_FadeTween.IsActive())
            {
                m_FadeTween.Kill();
            }

            // 如果之前有正在播放的BGM，先淡出
            if (m_PlayingHandle >= 0)
            {
                if (fadeIn)
                {
                    m_FadeTween = DOVirtual.Float(1f, 0f, m_FadeDuration, value =>
                    {
                        if (GameEntry.Sound != null)
                        {
                            GameEntry.Sound.SetVolume("Music", value);
                        }
                    }).OnComplete(() =>
                    {
                        if (GameEntry.Sound != null && m_PlayingHandle >= 0)
                        {
                            GameEntry.Sound.StopSound(m_PlayingHandle);
                        }
                        PlayNewBGM(musicId, fadeIn);
                    });
                }
                else
                {
                    if (GameEntry.Sound != null && m_PlayingHandle >= 0)
                    {
                        GameEntry.Sound.StopSound(m_PlayingHandle);
                    }
                    PlayNewBGM(musicId, fadeIn);
                }
            }
            else
            {
                PlayNewBGM(musicId, fadeIn);
            }
        }

        /// <summary>
        /// 停止BGM
        /// </summary>
        public void StopBGM(bool fadeOut = true)
        {
            if (m_PlayingHandle < 0)
            {
                return;
            }

            if (fadeOut)
            {
                m_FadeTween = DOVirtual.Float(1f, 0f, m_FadeDuration, value =>
                {
                    if (GameEntry.Sound != null)
                    {
                        GameEntry.Sound.SetVolume("Music", value);
                    }
                }).OnComplete(() =>
                {
                    if (GameEntry.Sound != null && m_PlayingHandle >= 0)
                    {
                        GameEntry.Sound.StopSound(m_PlayingHandle);
                    }
                    m_PlayingHandle = -1;
                    m_CurrentBGMId = -1;
                });
            }
            else
            {
                if (GameEntry.Sound != null && m_PlayingHandle >= 0)
                {
                    GameEntry.Sound.StopSound(m_PlayingHandle);
                }
                m_PlayingHandle = -1;
                m_CurrentBGMId = -1;
            }
        }

        /// <summary>
        /// 获取当前播放的BGM ID
        /// </summary>
        public int GetCurrentBGMId()
        {
            return m_CurrentBGMId;
        }

        private void PlayNewBGM(int musicId, bool fadeIn)
        {
            // 从DataTable中获取音乐资源名
            IDataTable<DRMusic> musicTable = GameEntry.DataTable.GetDataTable<DRMusic>();
            if (musicTable == null)
            {
                Debug.LogError("DRMusic DataTable 未加载");
                return;
            }

            DRMusic drMusic = musicTable.GetDataRow(musicId);
            if (drMusic == null)
            {
                Debug.LogError($"找不到ID为 {musicId} 的BGM配置");
                return;
            }

            // 播放BGM（循环）
            PlaySoundParams soundParams = new PlaySoundParams
            {
                Loop = true
            };
            string assetName = AssetUtility.GetMusicAsset(drMusic.AssetName);
            m_PlayingHandle = GameEntry.Sound.PlaySound(
                assetName,
                "Music",
                Constant.AssetPriority.MusicAsset,
                soundParams
            );

            if (m_PlayingHandle < 0)
            {
                Debug.LogError($"播放BGM失败: {drMusic.AssetName}");
                return;
            }

            // 如果需要淡入，则从0音量开始
            if (fadeIn)
            {
                GameEntry.Sound.SetVolume("Music", 0f);
                m_FadeTween = DOVirtual.Float(0f, 1f, m_FadeDuration, value =>
                {
                    if (GameEntry.Sound != null)
                    {
                        GameEntry.Sound.SetVolume("Music", value);
                    }
                });
            }
        }
    }
}

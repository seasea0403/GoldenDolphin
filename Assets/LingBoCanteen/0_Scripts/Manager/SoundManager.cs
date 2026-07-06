using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.Sound;
using DG.Tweening;

namespace LingBoCanteen
{
    /// <summary>
    /// 游戏音效管理器
    /// 负责游戏内音效的播放以及背景音乐的切换与渐变
    /// </summary>
    public class SoundManager : MonoSingleton<SoundManager>
    {
        private int m_CurrentMusicId = -1; // 当前播放的背景音乐ID
        private int m_CurrentMusicHandle = -1; // 当前音乐的播放句柄
        private Tweener m_MusicVolumeTweener; // 背景音乐音量渐变的Tweener

        public override void Init()
        {
            base.Init();
        }

        /// <summary>
        /// 播放指定ID的游戏音效（Sound组）
        /// </summary>
        public void PlaySound(int soundId)
        {
            DRSound drSound = GameEntry.DataTable.GetDataTable<DRSound>().GetDataRow(soundId);
            if (drSound == null)
            {
                Debug.LogError($"找不到ID为 {soundId} 的音效配置");
                return;
            }

            PlaySoundParams soundParams = new PlaySoundParams
            {
                Loop = false
            };
            string assetName = AssetUtility.GetSoundAsset(drSound.AssetName);
            GameEntry.Sound.PlaySound(
                assetName,
                "Sound",
                Constant.AssetPriority.SoundAsset,
                soundParams
            );
        }

        /// <summary>
        /// 播放UI音效
        /// </summary>
        public void PlayUISound(int uiSoundId)
        {
            DRUISound drUISound = GameEntry.DataTable.GetDataTable<DRUISound>().GetDataRow(uiSoundId);
            if (drUISound == null)
            {
                Debug.LogError($"找不到ID为 {uiSoundId} 的UI音效配置");
                return;
            }

            PlaySoundParams soundParams = new PlaySoundParams
            {
                Loop = false
            };
            string assetName = AssetUtility.GetUISoundAsset(drUISound.AssetName);
            GameEntry.Sound.PlaySound(
                assetName,
                "UISound",
                Constant.AssetPriority.UISoundAsset,
                soundParams
            );
        }

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        public void PlayButtonClickSound()
        {
            PlayUISound(10000); // UISound ID: 10000 = 按钮点击音效
        }

        /// <summary>
        /// 播放按钮悬浮音效
        /// </summary>
        public void PlayButtonHoverSound()
        {
            PlayUISound(10001); // UISound ID: 10001 = 按钮悬浮音效
        }

        /// <summary>
        /// 播放顾客进店音效
        /// </summary>
        public void PlayCustomerEnterSound()
        {
            PlaySound(20006); // Sound ID: 20006 = 顾客进店门铃音效
        }

        /// <summary>
        /// 播放顾客离开音效
        /// </summary>
        public void PlayCustomerLeaveSound()
        {
            PlaySound(20007); // Sound ID: 20007 = 顾客满意离开结算音效
        }

        /// <summary>
        /// 播放顾客生气音效
        /// </summary>
        public void PlayCustomerAngrySound()
        {
            PlaySound(20008); // Sound ID: 20008 = 顾客生气超时离场音效
        }

        /// <summary>
        /// 播放食材选中音效
        /// </summary>
        public void PlaySelectSound()
        {
            PlaySound(20000); // Sound ID: 20000 = 选中/拾取食材音效
        }

        /// <summary>
        /// 播放切菜音效
        /// </summary>
        public void PlayCutKnifeSound()
        {
            PlaySound(20001); // Sound ID: 20001 = 菜板切菜打击音效
        }

        /// <summary>
        /// 播放榨汁机循环音
        /// </summary>
        public void PlayJuicerLoopSound()
        {
            PlaySound(20002); // Sound ID: 20002 = 榨汁机持续工作循环音
        }

        /// <summary>
        /// 播放灶台开火音效
        /// </summary>
        public void PlayStoveOnSound()
        {
            PlaySound(20003); // Sound ID: 20003 = 灶台开火音效
        }

        /// <summary>
        /// 播放烹饪完成音效
        /// </summary>
        public void PlayCookCompleteSound()
        {
            PlaySound(20004); // Sound ID: 20004 = 烹饪完成提示音
        }

        /// <summary>
        /// 播放菜品烧糊音效
        /// </summary>
        public void PlayFoodBurnSound()
        {
            PlaySound(20005); // Sound ID: 20005 = 菜品烧糊失败音效
        }

        /// <summary>
        /// 播放食材放置音效
        /// </summary>
        public void PlayIngredientPlaceSound()
        {
            PlaySound(20009); // Sound ID: 20009 = 拖拽食材放置工作台音效
        }

        /// <summary>
        /// 播放获得金币音效
        /// </summary>
        public void PlayGoldGetSound()
        {
            PlaySound(20010); // Sound ID: 20010 = 完成订单获取金币音效
        }

        /// <summary>
        /// 播放SAN上升音效
        /// </summary>
        public void PlaySanUpSound()
        {
            PlaySound(20011); // Sound ID: 20011 = SAN数值上升音效
        }

        /// <summary>
        /// 播放SAN下降音效
        /// </summary>
        public void PlaySanDownSound()
        {
            PlaySound(20012); // Sound ID: 20012 = SAN数值下降音效
        }

        /// <summary>
        /// 播放区域切换音效
        /// </summary>
        public void PlayAreaSwitchSound()
        {
            PlaySound(20013); // Sound ID: 20013 = 人间/天堂/地狱区域切换过渡音
        }

        /// <summary>
        /// 播放超市购买成功音效
        /// </summary>
        public void PlayBuySuccessSound()
        {
            PlaySound(20014); // Sound ID: 20014 = 超市购买成功音效
        }

        /// <summary>
        /// 播放超市金币不足音效
        /// </summary>
        public void PlayBuyFailSound()
        {
            PlaySound(20015); // Sound ID: 20015 = 超市金币不足购买失败
        }

        /// <summary>
        /// 播放配方/食材解锁音效
        /// </summary>
        public void PlayUnlockSound()
        {
            PlaySound(20016); // Sound ID: 20016 = 配方/食材解锁提示音
        }

        /// <summary>
        /// 播放顾客耐心倒计时警告音
        /// </summary>
        public void PlayPatienceWarnSound()
        {
            PlaySound(20017); // Sound ID: 20017 = 顾客耐心倒计时警告音
        }

        #region 背景音乐管理（支持渐变效果）

        /// <summary>
        /// 播放背景音乐，支持音量渐入效果
        /// </summary>
        /// <param name="musicId">音乐ID</param>
        /// <param name="fadeDuration">渐入时长（秒），默认0.5秒</param>
        public void PlayBackgroundMusic(int musicId, float fadeDuration = 0.5f)
        {
            DRMusic drMusic = GameEntry.DataTable.GetDataTable<DRMusic>().GetDataRow(musicId);
            if (drMusic == null)
            {
                Debug.LogError($"找不到ID为 {musicId} 的音乐配置");
                return;
            }

            // 如果正在播放相同的音乐，则不重复播放
            if (m_CurrentMusicId == musicId)
            {
                return;
            }

            // 停止当前音乐（如果有）
            if (m_CurrentMusicHandle >= 0)
            {
                GameEntry.Sound.StopSound(m_CurrentMusicHandle);
            }

            // 中断之前的音量渐变
            if (m_MusicVolumeTweener != null && m_MusicVolumeTweener.IsActive())
            {
                m_MusicVolumeTweener.Kill();
            }

            m_CurrentMusicId = musicId;

            // 播放新的背景音乐
            PlaySoundParams musicParams = new PlaySoundParams
            {
                Loop = true,
                VolumeInSoundGroup = 0f // 先设置为0，然后渐入
            };

            string assetName = AssetUtility.GetMusicAsset(drMusic.AssetName);
            m_CurrentMusicHandle = GameEntry.Sound.PlaySound(
                assetName,
                "Music",
                Constant.AssetPriority.MusicAsset,
                musicParams
            );

            // 音量渐入
            if (fadeDuration > 0)
            {
                m_MusicVolumeTweener = DOTween.To(
                    () => GameEntry.Sound.GetSoundGroup("Music").Volume,
                    x => GameEntry.Sound.GetSoundGroup("Music").Volume = x,
                    1f,
                    fadeDuration
                );
            }
            else
            {
                GameEntry.Sound.GetSoundGroup("Music").Volume = 1f;
            }
        }

        /// <summary>
        /// 停止背景音乐，支持音量渐出效果
        /// </summary>
        /// <param name="fadeDuration">渐出时长（秒），默认0.5秒</param>
        public void StopBackgroundMusic(float fadeDuration = 0.5f)
        {
            if (m_CurrentMusicHandle < 0)
            {
                return;
            }

            // 中断之前的音量渐变
            if (m_MusicVolumeTweener != null && m_MusicVolumeTweener.IsActive())
            {
                m_MusicVolumeTweener.Kill();
            }

            if (fadeDuration > 0)
            {
                // 音量渐出后停止
                m_MusicVolumeTweener = DOTween.To(
                    () => GameEntry.Sound.GetSoundGroup("Music").Volume,
                    x => GameEntry.Sound.GetSoundGroup("Music").Volume = x,
                    0f,
                    fadeDuration
                ).OnComplete(() =>
                {
                    GameEntry.Sound.StopSound(m_CurrentMusicHandle);
                    m_CurrentMusicHandle = -1;
                    m_CurrentMusicId = -1;
                });
            }
            else
            {
                GameEntry.Sound.StopSound(m_CurrentMusicHandle);
                m_CurrentMusicHandle = -1;
                m_CurrentMusicId = -1;
            }
        }

        /// <summary>
        /// 平滑切换背景音乐（淡出当前，淡入新的）
        /// </summary>
        /// <param name="newMusicId">新音乐ID</param>
        /// <param name="fadeDuration">单程渐变时长（秒），默认0.5秒</param>
        public void CrossfadeBackgroundMusic(int newMusicId, float fadeDuration = 0.5f)
        {
            // 如果正在播放相同的音乐，则不重复切换
            if (m_CurrentMusicId == newMusicId)
            {
                return;
            }

            if (m_CurrentMusicHandle < 0)
            {
                // 当前没有播放任何音乐，直接播放新的
                PlayBackgroundMusic(newMusicId, fadeDuration);
                return;
            }

            // 中断之前的音量渐变
            if (m_MusicVolumeTweener != null && m_MusicVolumeTweener.IsActive())
            {
                m_MusicVolumeTweener.Kill();
            }

            // 记录旧音乐的句柄
            int oldMusicHandle = m_CurrentMusicHandle;

            // 渐出当前音乐
            m_MusicVolumeTweener = DOTween.To(
                () => GameEntry.Sound.GetSoundGroup("Music").Volume,
                x => GameEntry.Sound.GetSoundGroup("Music").Volume = x,
                0f,
                fadeDuration
            ).OnComplete(() =>
            {
                GameEntry.Sound.StopSound(oldMusicHandle);
                // 播放新的背景音乐（带渐入效果）
                PlayBackgroundMusic(newMusicId, fadeDuration);
            });
        }

        #endregion
    }
}

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

        /// <summary>
        /// 播放垃圾桶打开音效
        /// </summary>
        public void PlayTrashOpenSound()
        {
            PlaySound(20018); // Sound ID: 20018 = 垃圾桶打开的声音
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
                Debug.Log($"[SoundManager] 音乐 {musicId} 已在播放，跳过重复播放");
                return;
            }

            Debug.Log($"[SoundManager] 开始播放音乐 ID={musicId}，资源名={drMusic.AssetName}，淡入时长={fadeDuration}秒");

            // 中断之前的音量渐变
            if (m_MusicVolumeTweener != null && m_MusicVolumeTweener.IsActive())
            {
                m_MusicVolumeTweener.Kill();
            }

            // 停止当前音乐（如果有）
            if (m_CurrentMusicHandle >= 0)
            {
                Debug.Log($"[SoundManager] 停止当前音乐，Handle={m_CurrentMusicHandle}");
                GameEntry.Sound.StopSound(m_CurrentMusicHandle);
            }

            m_CurrentMusicId = musicId;

            // 播放新的背景音乐（使用默认音量）
            PlaySoundParams musicParams = new PlaySoundParams
            {
                Loop = true
            };

            string assetName = AssetUtility.GetMusicAsset(drMusic.AssetName);
            Debug.Log($"[SoundManager] 音乐完整资源路径={assetName}");
            
            m_CurrentMusicHandle = GameEntry.Sound.PlaySound(
                assetName,
                "Music",
                Constant.AssetPriority.MusicAsset,
                musicParams
            );

            Debug.Log($"[SoundManager] PlaySound 返回 Handle={m_CurrentMusicHandle}");

            // 音量渐入
            if (fadeDuration > 0)
            {
                // ★ 【修改】改用 SetVolume 而不是 GetSoundGroup，保持与 BGMManager 一致
                GameEntry.Sound.SetVolume("Music", 0f);
                m_MusicVolumeTweener = DOTween.To(
                    () => GameEntry.Sound.GetSoundGroup("Music").Volume,
                    x => GameEntry.Sound.SetVolume("Music", x),
                    1f,
                    fadeDuration
                );
                Debug.Log($"[SoundManager] 启动音量渐入到1.0，时长={fadeDuration}秒");
            }
            else
            {
                GameEntry.Sound.SetVolume("Music", 1f);
                Debug.Log($"[SoundManager] 直接设置音量=1.0");
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
                    x => GameEntry.Sound.SetVolume("Music", x),
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
            Debug.Log($"[CrossfadeBackgroundMusic] 切换到音乐ID={newMusicId}，当前音乐ID={m_CurrentMusicId}，当前Handle={m_CurrentMusicHandle}");

            // 如果正在播放相同的音乐，则不重复切换
            if (m_CurrentMusicId == newMusicId)
            {
                Debug.Log($"[CrossfadeBackgroundMusic] 正在播放相同的音乐ID={newMusicId}，跳过切换");
                return;
            }

            if (m_CurrentMusicHandle < 0)
            {
                // 当前没有播放任何音乐，直接播放新的
                Debug.Log($"[CrossfadeBackgroundMusic] 没有正在播放的音乐，直接播放新的ID={newMusicId}");
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

            Debug.Log($"[CrossfadeBackgroundMusic] 淡出旧音乐Handle={oldMusicHandle}，淡入新音乐ID={newMusicId}");

            // ★ 【修改】改用 SetVolume 方式来渐出
            m_MusicVolumeTweener = DOTween.To(
                () => GameEntry.Sound.GetSoundGroup("Music").Volume,
                x => GameEntry.Sound.SetVolume("Music", x),
                0f,
                fadeDuration
            ).OnComplete(() =>
            {
                Debug.Log($"[CrossfadeBackgroundMusic] 旧音乐淡出完成，停止Handle={oldMusicHandle}");
                GameEntry.Sound.StopSound(oldMusicHandle);
                // 播放新的背景音乐（带渐入效果）
                PlayBackgroundMusic(newMusicId, fadeDuration);
            });
        }

        /// <summary>
        /// 根据当前区域和时间阶段自动选择并播放对应的BGM
        /// 由AreaSwitchManager、ElevatorController等系统调用
        /// </summary>
        public void PlayMusicForCurrentGameState()
        {
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
            int musicId = GetMusicIdForGameState(currentRegion, currentTimeSection);

            // 诊断日志：记录BGM选择
            Debug.Log($"[BGM Selection] 区域: {currentRegion} | 时间: {currentTimeSection} | 选择音乐ID: {musicId}");

            // 使用平滑切换播放新的背景音乐
            CrossfadeBackgroundMusic(musicId, 0.5f);
        }

        /// <summary>
        /// 根据区域和时间阶段获取对应的音乐ID
        /// </summary>
        private int GetMusicIdForGameState(GameRegion region, TimeSection timeSection)
        {
            const int MUSIC_ID_MORTAL_DAY = 30001;      // 人间区域白天经营BGM
            const int MUSIC_ID_HEAVEN_DAY = 30002;      // 天堂区域经营BGM
            const int MUSIC_ID_HELL_DAY = 30003;        // 地狱区域经营BGM
            const int MUSIC_ID_EVENING_SHOPPING = 30004; // 傍晚超市采购BGM

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

        #endregion
    }
}


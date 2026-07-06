//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    public class SettingForm : MonoBehaviour
    {
        [Header("BGM音量滑块")]
        [SerializeField]
        private Slider m_BGMSlider = null;

        [Header("音效SFX音量滑块")]
        [SerializeField]
        private Slider m_SFXSlider = null;

        [Header("全屏开关Toggle")]
        [SerializeField]
        private Toggle m_FullScreenToggle = null;

        [Header("关闭设置界面按钮")]
        [SerializeField]
        private Button m_CloseBtn = null;

        /// <summary>
        /// BGM音量变化回调
        /// </summary>
        public void OnBGMVolumeChanged(float volume)
        {
            // 更新音效组件音量
            GameEntry.Sound.SetVolume("Music", volume);
            // 存入DataNode持久化
            GameEntry.DataNode.SetData("Settings.BGMVolume", (VarSingle)volume);
        }

        /// <summary>
        /// SFX音效音量变化回调
        /// </summary>
        public void OnSFXVolumeChanged(float volume)
        {
            GameEntry.Sound.SetVolume("Sound", volume);
            GameEntry.DataNode.SetData("Settings.SFXVolume", (VarSingle)volume);
        }

        /// <summary>
        /// 全屏切换回调
        /// </summary>
        public void OnFullScreenToggleChanged(bool isOn)
        {
            Screen.fullScreen = isOn;
            GameEntry.DataNode.SetData("Settings.IsFullScreen", (VarBoolean)isOn);
        }

        /// <summary>
        /// 关闭设置面板
        /// </summary>
        public void OnCloseButtonClick()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            // 每次显示时都重新初始化，确保所有事件都正确绑定
            // 读取DataNode保存的音量、全屏状态，同步到UI控件
            float bgmVol = GameEntry.DataNode.GetData<VarSingle>("Settings.BGMVolume");
            float sfxVol = GameEntry.DataNode.GetData<VarSingle>("Settings.SFXVolume");
            bool isFullScreen = GameEntry.DataNode.GetData<VarBoolean>("Settings.IsFullScreen");

            if (m_BGMSlider != null)
                m_BGMSlider.value = bgmVol;
            if (m_SFXSlider != null)
                m_SFXSlider.value = sfxVol;
            if (m_FullScreenToggle != null)
                m_FullScreenToggle.isOn = isFullScreen;

            // 绑定关闭按钮事件
            if (m_CloseBtn != null)
            {
                m_CloseBtn.onClick.RemoveAllListeners();
                m_CloseBtn.onClick.AddListener(OnCloseButtonClick);
                UIButtonSoundHelper.BindButtonSound(m_CloseBtn);
            }

            // 绑定滑块、Toggle监听
            if (m_BGMSlider != null)
            {
                m_BGMSlider.onValueChanged.RemoveAllListeners();
                m_BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            if (m_SFXSlider != null)
            {
                m_SFXSlider.onValueChanged.RemoveAllListeners();
                m_SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (m_FullScreenToggle != null)
            {
                m_FullScreenToggle.onValueChanged.RemoveAllListeners();
                m_FullScreenToggle.onValueChanged.AddListener(OnFullScreenToggleChanged);
            }
        }
    }
}
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
    public class SettingForm : UGuiForm
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
            Close();
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnOpen(object userData)
#else
        protected internal override void OnOpen(object userData)
#endif
        {
            base.OnOpen(userData);

            // 读取DataNode保存的音量、全屏状态，同步到UI控件
            float bgmVol = GameEntry.DataNode.GetData<VarSingle>("Settings.BGMVolume");
            float sfxVol = GameEntry.DataNode.GetData<VarSingle>("Settings.SFXVolume");
            bool isFullScreen = GameEntry.DataNode.GetData<VarBoolean>("Settings.IsFullScreen");

            m_BGMSlider.value = bgmVol;
            m_SFXSlider.value = sfxVol;
            m_FullScreenToggle.isOn = isFullScreen;

            // 绑定关闭按钮事件
            m_CloseBtn.onClick.RemoveAllListeners();
            m_CloseBtn.onClick.AddListener(OnCloseButtonClick);
            
            // 为关闭按钮绑定音效
            UIButtonSoundHelper.BindButtonSound(m_CloseBtn);

            // 绑定滑块、Toggle监听（可在Inspector绑定，这里做兜底）
            m_BGMSlider.onValueChanged.RemoveAllListeners();
            m_BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

            m_SFXSlider.onValueChanged.RemoveAllListeners();
            m_SFXSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            m_FullScreenToggle.onValueChanged.RemoveAllListeners();
            m_FullScreenToggle.onValueChanged.AddListener(OnFullScreenToggleChanged);
        }
    }
}
//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;
using UnityGameFramework.Runtime;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LingBoCanteen
{
    public class MenuForm : MonoBehaviour
    {
        [Header("按钮引用")]
        [SerializeField] private Button m_StartGameBtn;
        [SerializeField] private Button m_SettingBtn;
        [SerializeField] private Button m_ExitBtn;

        [Header("设置面板引用")]
        [SerializeField] private SettingForm m_SettingForm;

        [Header("开始按钮悬停关联动画")]
        [Tooltip("悬停开始按钮时播放动画的目标组件，不是按钮本身")]
        [SerializeField] private Animator m_StartHoverAnimator;
        [Tooltip("动画状态机里的动画名称")]
        [SerializeField] private string m_HoverAnimName = "StartHover";

        private ProcedureMenu m_CurrentMenuProcedure;

        private void Awake()
        {
            // 绑定按钮事件
            if (m_StartGameBtn != null)
                m_StartGameBtn.onClick.AddListener(OnStartGameClick);
            if (m_SettingBtn != null)
                m_SettingBtn.onClick.AddListener(OnSettingClick);
            if (m_ExitBtn != null)
                m_ExitBtn.onClick.AddListener(OnExitClick);

            // 为所有菜单按钮绑定音效
            UIButtonSoundHelper.BindButtonSound(m_StartGameBtn);
            UIButtonSoundHelper.BindButtonSound(m_SettingBtn);
            UIButtonSoundHelper.BindButtonSound(m_ExitBtn);

            // 获取当前菜单流程实例
            m_CurrentMenuProcedure = GameEntry.Procedure.CurrentProcedure as ProcedureMenu;
            
            // 重置悬停动画状态
            if (m_StartHoverAnimator != null)
            {
                m_StartHoverAnimator.Play("Idle", 0, 0f);
            }
        }

        /// <summary>
        /// 鼠标悬停进入开始按钮
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 只响应开始按钮的悬停
            if (eventData.pointerEnter == m_StartGameBtn.gameObject)
            {
                if (m_StartHoverAnimator != null && !string.IsNullOrEmpty(m_HoverAnimName))
                {
                    m_StartHoverAnimator.Play(m_HoverAnimName);
                }
            }
        }

        /// <summary>
        /// 鼠标悬停离开开始按钮
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerEnter == m_StartGameBtn.gameObject)
            {
                if (m_StartHoverAnimator != null)
                {
                    // 回到默认状态，如需倒放可改回退动画
                    m_StartHoverAnimator.Play("Idle");
                }
            }
        }

        /// <summary>
        /// 点击开始游戏
        /// </summary>
        private void OnStartGameClick()
        {
            if (m_CurrentMenuProcedure == null) return;
            
            // 通知流程层启动新游戏，UI不直接改流程状态
            m_CurrentMenuProcedure.StartGame();
        }

        /// <summary>
        /// 点击设置
        /// </summary>
        private void OnSettingClick()
        {
            if (m_SettingForm != null)
            {
                m_SettingForm.gameObject.SetActive(true);
                Debug.Log("[MenuForm] SettingForm opened.");
            }
            else
            {
                Debug.LogError("[MenuForm] SettingForm reference not assigned in Inspector!");
            }
        }

        /// <summary>
        /// 点击退出游戏
        /// </summary>
        private void OnExitClick()
        {
#if UNITY_EDITOR
            // 编辑器模式下停止播放
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 打包后退出应用
            Application.Quit();
#endif
        }

        /// <summary>
        /// UI关闭时清理
        /// </summary>
        private void OnDestroy()
        {
            m_CurrentMenuProcedure = null;
        }
    }
}

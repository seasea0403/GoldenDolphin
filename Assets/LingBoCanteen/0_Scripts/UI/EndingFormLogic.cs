using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 游戏结局面板逻辑：处理天堂、地狱、普通、真实四种结局的显示和交互。
    /// </summary>
    public class EndingFormLogic : UGuiForm
    {
        [SerializeField] private GameObject m_HeavenEnding;
        [SerializeField] private GameObject m_HellEnding;
        [SerializeField] private GameObject m_NormalEnding;
        [SerializeField] private GameObject m_TrueEnding;

        [SerializeField] private Button m_ReturnToMenuButton;

        private EndingType m_CurrentEndingType = EndingType.None;

        private void OnEnable()
        {
            if (m_ReturnToMenuButton != null)
            {
                m_ReturnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            }
        }

        private void OnDisable()
        {
            if (m_ReturnToMenuButton != null)
            {
                m_ReturnToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);
            }
        }

        /// <summary>
        /// 显示特定类型的结局。
        /// </summary>
        public void ShowEnding(EndingType endingType)
        {
            m_CurrentEndingType = endingType;

            // 隐藏所有结局面板
            if (m_HeavenEnding != null) m_HeavenEnding.SetActive(false);
            if (m_HellEnding != null) m_HellEnding.SetActive(false);
            if (m_NormalEnding != null) m_NormalEnding.SetActive(false);
            if (m_TrueEnding != null) m_TrueEnding.SetActive(false);

            // 显示对应的结局面板
            switch (endingType)
            {
                case EndingType.Death_Heaven:
                    if (m_HeavenEnding != null) m_HeavenEnding.SetActive(true);
                    break;
                case EndingType.Death_Hell:
                    if (m_HellEnding != null) m_HellEnding.SetActive(true);
                    break;
                case EndingType.Normal:
                    if (m_NormalEnding != null) m_NormalEnding.SetActive(true);
                    break;
                case EndingType.Truth:
                    if (m_TrueEnding != null) m_TrueEnding.SetActive(true);
                    break;
            }
        }

        private void OnReturnToMenuClicked()
        {
            // 返回主菜单（关闭EndingForm，加载Menu场景）
            GameEntry.UI.CloseUIForm(this);
            GameEntry.Scene.LoadScene("Menu", this);
        }

        /// <summary>
        /// 获取当前正在显示的结局类型。
        /// </summary>
        public EndingType CurrentEndingType => m_CurrentEndingType;
    }
}

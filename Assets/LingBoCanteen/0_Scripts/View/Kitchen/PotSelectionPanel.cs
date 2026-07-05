using System;
using UnityEngine;
using UnityEngine.UI;

namespace LingBoCanteen
{
    /// <summary>
    /// 点击空炉灶时弹出的选锅面板：4 个按钮对应 Pot 表 Id 5001-5004（炒锅/蒸锅/高压锅/汤锅）。
    /// 烤箱是场景内独立工位、恒定持有 Oven 这口锅，不需要这个面板。
    /// 场景里手动把 4 个按钮和它们各自的图标拖进 <see cref="m_Options"/>。
    /// </summary>
    public class PotSelectionPanel : MonoBehaviour
    {
        [Serializable]
        private class PotOption
        {
            public PotType Type;
            public Sprite Icon;
            public Button Button;
        }

        [SerializeField] private GameObject m_Root;
        [SerializeField] private PotOption[] m_Options;

        private PotController m_TargetPot;

        // 打开面板的点击和后续选锅点击如果落在同一次鼠标按下/抬起里，容易被误判成同一次点击生效两次；
        // 面板打开后必须先等鼠标左键松开一次，才认为“可以接受新的点击”。
        private bool m_ReadyForInput;

        private void Awake()
        {
            if (m_Options == null)
            {
                return;
            }

            foreach (PotOption option in m_Options)
            {
                if (option?.Button == null)
                {
                    continue;
                }

                PotOption captured = option;
                option.Button.onClick.AddListener(() => OnOptionClicked(captured));
            }

            m_Root?.SetActive(false);
        }

        private void Update()
        {
            if (!m_ReadyForInput && m_Root != null && m_Root.activeSelf && !Input.GetMouseButton(0))
            {
                m_ReadyForInput = true;
            }
        }

        /// <summary>
        /// 供 <see cref="PotController"/> 在空炉灶被点击时调用。
        /// </summary>
        public void Open(PotController targetPot)
        {
            m_TargetPot = targetPot;
            m_ReadyForInput = false;
            m_Root?.SetActive(true);
        }

        private void OnOptionClicked(PotOption option)
        {
            if (!m_ReadyForInput)
            {
                return;
            }

            m_Root?.SetActive(false);
            m_TargetPot?.SelectPot((int)option.Type, option.Icon);
            m_TargetPot = null;
        }
    }
}

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
                // 绑定音效
                UIButtonSoundHelper.BindButtonSound(option.Button);
            }

            m_Root?.SetActive(false);
        }


        /// <summary>
        /// 供 <see cref="PotController"/> 在空炉灶被点击时调用。
        /// </summary>
        public void Open(PotController targetPot)
        {
            m_TargetPot = targetPot;
            m_Root?.SetActive(true);
        }

        private void OnOptionClicked(PotOption option)
        {
            m_Root?.SetActive(false);
            m_TargetPot?.SelectPot((int)option.Type, option.Icon);
            m_TargetPot = null;
        }

        public void Close(){
            m_Root?.SetActive(false);            
        }
    }
}

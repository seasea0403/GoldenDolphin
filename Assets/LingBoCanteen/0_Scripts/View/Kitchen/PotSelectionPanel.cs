using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LingBoCanteen
{
    /// <summary>
    /// 点击空炉灶时弹出的选锅面板：4 个按钮对应 Pot 表 Id 5001-5004（炒锅/蒸锅/高压锅/汤锅）。
    /// 烤箱是场景内独立工位、恒定持有 Oven 这口锅，不需要这个面板。
    /// 场景里手动把 4 个按钮和它们各自的图标拖进 <see cref="m_Options"/>。
    /// 打开面板后的 0.5 秒内按钮不可交互，避免"点炉灶的那次松开鼠标"误选锅具。
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
        [Tooltip("打开面板后屏蔽按钮交互的时长（秒），防止点炉灶的那次松开鼠标误选锅具")]
        [SerializeField] private float m_InteractionLockDuration = 0.5f;

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

            if (m_Root == null)
            {
                return;
            }

            m_Root.SetActive(true);

            CanvasGroup canvasGroup = m_Root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = m_Root.AddComponent<CanvasGroup>();
            }

            StopAllCoroutines();
            StartCoroutine(UnlockInteractionAfterDelay(canvasGroup, m_InteractionLockDuration));
        }

        private IEnumerator UnlockInteractionAfterDelay(CanvasGroup canvasGroup, float delay)
        {
            canvasGroup.interactable = false;
            yield return new WaitForSeconds(delay);
            canvasGroup.interactable = true;
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

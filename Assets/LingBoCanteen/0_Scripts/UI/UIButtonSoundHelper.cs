using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LingBoCanteen
{
    /// <summary>
    /// UI按钮音效助手
    /// 自动为所有按钮添加音效反馈（点击+悬浮）
    /// 可以挂在任何常驻GameObject上（如Canvas），会在Start时扫描所有Button
    /// </summary>
    public class UIButtonSoundHelper : MonoBehaviour
    {
        private void Start()
        {
            BindAllButtons();
        }

        /// <summary>
        /// 绑定所有Button的音效
        /// </summary>
        private void BindAllButtons()
        {
            Button[] buttons = FindObjectsOfType<Button>();
            foreach (Button button in buttons)
            {
                BindButtonSound(button);
            }
        }

        /// <summary>
        /// 为单个Button绑定音效
        /// </summary>
        public static void BindButtonSound(Button button)
        {
            if (button == null || button.gameObject == null)
            {
                return;
            }

            // 检查是否已经绑定过（避免重复）
            if (button.gameObject.GetComponent<ButtonSoundComponent>() != null)
            {
                return;
            }

            // 添加专用的音效组件
            ButtonSoundComponent soundComponent = button.gameObject.AddComponent<ButtonSoundComponent>();
            
            // 绑定点击事件
            button.onClick.AddListener(soundComponent.PlayClickSound);
        }
    }

    /// <summary>
    /// 按钮音效组件，挂在每个需要音效的Button上
    /// </summary>
    public class ButtonSoundComponent : MonoBehaviour, IPointerEnterHandler
    {
        private Button m_Button;

        private void Awake()
        {
            m_Button = GetComponent<Button>();
        }

        public void PlayClickSound()
        {
            SoundManager.Instance?.PlayButtonClickSound();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 只在Button可交互时播放悬浮音效
            if (m_Button != null && m_Button.interactable)
            {
                SoundManager.Instance?.PlayButtonHoverSound();
            }
        }
    }
}

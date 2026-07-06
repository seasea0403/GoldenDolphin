using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 对话表单逻辑：管理对话面板的显示、推进、立绘显示/隐藏
    /// 配置在DialogueForm.prefab中
    /// </summary>
    public class DialogueFormLogic : MonoBehaviour
    {
        [Header("UI 组件")]
        [SerializeField] private TextMeshProUGUI m_SpeakerNameText;
        [SerializeField] private TextMeshProUGUI m_DialogueContentText;
        [SerializeField] private Image m_CharacterPortraitImage;
        [SerializeField] private CanvasGroup m_PortraitCanvasGroup;
        [SerializeField] private Button m_ContinueButton;

        private DialogueAsset m_CurrentDialogueAsset;
        private int m_CurrentLineIndex = 0;
        private System.Action m_OnDialogueComplete;
        private CanvasGroup m_ContentCanvasGroup;
        private bool m_IsWaitingForInput = false;
        private Sprite m_LastPortraitSprite;

        private void Awake()
        {
            if (m_PortraitCanvasGroup == null)
            {
                m_PortraitCanvasGroup = m_CharacterPortraitImage?.GetComponent<CanvasGroup>();
            }

            m_ContentCanvasGroup = GetComponent<CanvasGroup>();
            if (m_ContentCanvasGroup == null)
            {
                m_ContentCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (m_ContinueButton != null)
            {
                m_ContinueButton.onClick.AddListener(OnContinueButtonClicked);
            }
        }

        private void OnEnable()
        {
            // 支持 ESC / 空格推进对话
            if (m_ContinueButton == null)
            {
                // 如果没有按钮，监听键盘输入
            }
        }

        private void OnDestroy()
        {
            if (m_ContinueButton != null)
            {
                m_ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
            }
        }

        /// <summary>
        /// 开始播放对话
        /// </summary>
        public void PlayDialogue(DialogueAsset dialogueAsset, System.Action onComplete)
        {
            m_CurrentDialogueAsset = dialogueAsset;
            m_CurrentLineIndex = 0;
            m_OnDialogueComplete = onComplete;

            if (m_CurrentDialogueAsset == null || m_CurrentDialogueAsset.lines.Count == 0)
            {
                Log.Warning("DialogueAsset is null or empty!");
                m_OnDialogueComplete?.Invoke();
                return;
            }

            // 淡入内容
            StartCoroutine(FadeInContent());
        }

        private IEnumerator FadeInContent()
        {
            m_ContentCanvasGroup.alpha = 0f;
            m_ContentCanvasGroup.blocksRaycasts = false;
            float elapsed = 0f;
            float fadeDuration = 0.2f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                m_ContentCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            m_ContentCanvasGroup.alpha = 1f;
            m_ContentCanvasGroup.blocksRaycasts = true;
            // 显示第一行
            ShowNextLine();
        }

        /// <summary>
        /// 显示下一行对话
        /// </summary>
        private void ShowNextLine()
        {
            if (m_CurrentLineIndex >= m_CurrentDialogueAsset.lines.Count)
            {
                OnDialogueComplete();
                return;
            }

            DialogueLine currentLine = m_CurrentDialogueAsset.lines[m_CurrentLineIndex];

            m_SpeakerNameText.text = currentLine.speakerName;
            m_DialogueContentText.text = currentLine.speakingContent;

            // 更新立绘显示/隐藏
            UpdatePortrait(currentLine.charImage);

            m_CurrentLineIndex++;
            m_IsWaitingForInput = true;
        }

        /// <summary>
        /// 更新立绘显示
        /// 规则：
        /// - 如果charImage为null或说话人是"我"，隐藏立绘
        /// - 否则显示立绘
        /// </summary>
        private void UpdatePortrait(Sprite portraitSprite)
        {
            bool shouldShowPortrait = portraitSprite != null && m_SpeakerNameText.text != "我";

            if (shouldShowPortrait)
            {
                m_CharacterPortraitImage.sprite = portraitSprite;
                m_PortraitCanvasGroup.alpha = 1f;
                m_LastPortraitSprite = portraitSprite;
            }
            else
            {
                m_PortraitCanvasGroup.alpha = 0f;
                m_CharacterPortraitImage.sprite = null;
                m_LastPortraitSprite = null;
            }
        }



        /// <summary>
        /// 继续按钮点击回调
        /// </summary>
        private void OnContinueButtonClicked()
        {
            if (m_IsWaitingForInput)
            {
                ShowNextLine();
            }
        }

        /// <summary>
        /// 对话完成
        /// </summary>
        private void OnDialogueComplete()
        {
            m_IsWaitingForInput = false;
            StartCoroutine(FadeOutContent());
        }

        private IEnumerator FadeOutContent()
        {
            m_ContentCanvasGroup.alpha = 1f;
            m_ContentCanvasGroup.blocksRaycasts = false;
            float elapsed = 0f;
            float fadeDuration = 0.2f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                m_ContentCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                yield return null;
            }

            m_ContentCanvasGroup.alpha = 0f;

            // 触发完成回调
            m_OnDialogueComplete?.Invoke();
        }
    }
}

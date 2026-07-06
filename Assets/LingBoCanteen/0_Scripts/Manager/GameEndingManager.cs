using System.Collections;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 游戏结局管理器：负责处理各类游戏结局的流程，包括黑屏过渡、对话播放、面板显示等。
    /// </summary>
    public class GameEndingManager : MonoBehaviour
    {
        public static GameEndingManager Instance { get; private set; }

        [SerializeField] private CanvasGroup m_FadeScreen;  // 黑屏过渡层
        [SerializeField] private float m_FadeDuration = 0.5f;  // 过渡时间

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[GameEndingManager] Duplicate instance detected. Destroying...");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 触发天堂结局（SAN>=100）。
        /// </summary>
        public void TriggerHeavenEnding()
        {
            StartCoroutine(PlayEndingSequence(EndingType.Death_Heaven, null));
        }

        /// <summary>
        /// 触发地狱结局（SAN<=0）。
        /// </summary>
        public void TriggerHellEnding()
        {
            StartCoroutine(PlayEndingSequence(EndingType.Death_Hell, null));
        }

        /// <summary>
        /// 触发普通结局（第20天，曾改变过Region）。
        /// </summary>
        public void TriggerNormalEnding()
        {
            StartCoroutine(PlayEndingSequence(EndingType.Normal, "Day20_1"));
        }

        /// <summary>
        /// 触发真实结局（第20天，全程未改变Region）。
        /// </summary>
        public void TriggerTrueEnding()
        {
            StartCoroutine(PlayEndingSequence(EndingType.Truth, "Day20_2"));
        }

        /// <summary>
        /// 播放结局序列：黑屏过渡 → 对话播放（如有） → 打开EndingForm UI。
        /// </summary>
        private IEnumerator PlayEndingSequence(EndingType endingType, string dialogueAssetName)
        {
            // 1. 黑屏过渡（渐进）
            yield return StartCoroutine(FadeToBlack(true, m_FadeDuration));

            // 2. 播放对话（如有）
            if (!string.IsNullOrEmpty(dialogueAssetName))
            {
                if (TextManager.Instance != null)
                {
                    TextManager.Instance.PlayDialogueAsset(dialogueAssetName);
                    
                    // 等待对话播放完毕
                    yield return new WaitUntil(() => TextManager.Instance.IsFinishedPlaying());
                }
            }

            // 3. 打开EndingForm UI
            GameEntry.UI.OpenUIForm(UIFormId.EndingForm, null);

            // 4. 等待UI打开完毕
            yield return new WaitForSeconds(0.5f);

            // 5. 黑屏淡出
            yield return StartCoroutine(FadeToBlack(false, m_FadeDuration));

            // 6. 设置显示的结局类型
            if (GameEntry.UI.HasUIForm(UIFormId.EndingForm))
            {
                EndingFormLogic endingForm = GameEntry.UI.GetUIForm(UIFormId.EndingForm) as EndingFormLogic;
                if (endingForm != null)
                {
                    endingForm.ShowEnding(endingType);
                }
            }
        }

        /// <summary>
        /// 黑屏过渡效果。
        /// </summary>
        private IEnumerator FadeToBlack(bool fadeIn, float duration)
        {
            if (m_FadeScreen == null) yield break;

            float elapsed = 0f;
            float startAlpha = fadeIn ? 0f : 1f;
            float endAlpha = fadeIn ? 1f : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                m_FadeScreen.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }

            m_FadeScreen.alpha = endAlpha;
        }
    }
}

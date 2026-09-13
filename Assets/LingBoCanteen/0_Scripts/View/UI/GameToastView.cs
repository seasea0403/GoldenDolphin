using System;
using System.Collections;
using LingBoCanteen.Definition.Enum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingBoCanteen
{
    /// <summary>
    /// 全局浮窗提示：当日顾客全部处理完毕 / 超市购买成功 / 上菜失败时弹出。
    /// 渐显、渐隐各耗时 <see cref="Constant.GameConstant.TOAST_FADE_DURATION"/>（0.5秒），中间停留展示。
    /// 场景内放置一个单例实例（Screen Space Canvas 下），默认隐藏。
    /// </summary>
    public class GameToastView : MonoBehaviour
    {
        [Serializable]
        private struct ToastBackgroundEntry
        {
            public ToastType Type;
            public Sprite Background;
        }

        public static GameToastView Instance { get; private set; }

        [SerializeField] private GameObject m_Root;
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private Image m_BackgroundImage;
        [SerializeField] private TMP_Text m_MessageText;
        [SerializeField] private ToastBackgroundEntry[] m_Backgrounds;
        [Tooltip("渐显完成后到开始渐隐之间的停留展示时长（秒），渐显/渐隐各自的时长由 Constant.GameConstant.TOAST_FADE_DURATION 统一控制")]
        [SerializeField] private float m_DisplaySeconds = 1.5f;

        private Coroutine m_Routine;

        private void Awake()
        {
            Instance = this;
            if (m_Root != null)
            {
                m_Root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 显示一条浮窗提示。<paramref name="onFadeInComplete"/> 会在渐显动画播放完毕的那一刻回调，
        /// 供"当日顾客全部完成后才能跳转傍晚场景"这类需要等待渐显结束的调用方使用。
        /// </summary>
        public void Show(ToastType type, string message, Action onFadeInComplete = null)
        {
            if (m_Root == null || m_CanvasGroup == null)
            {
                onFadeInComplete?.Invoke();
                return;
            }

            if (m_Routine != null)
            {
                StopCoroutine(m_Routine);
            }

            ApplyBackground(type);
            if (m_MessageText != null)
            {
                m_MessageText.text = message;
            }

            m_Routine = StartCoroutine(ShowRoutine(onFadeInComplete));
        }

        private IEnumerator ShowRoutine(Action onFadeInComplete)
        {
            m_Root.SetActive(true);
            m_CanvasGroup.alpha = 0f;

            yield return m_CanvasGroup.FadeToAlpha(1f, Constant.GameConstant.TOAST_FADE_DURATION);
            onFadeInComplete?.Invoke();

            yield return new WaitForSeconds(m_DisplaySeconds);

            yield return m_CanvasGroup.FadeToAlpha(0f, Constant.GameConstant.TOAST_FADE_DURATION);
            m_Root.SetActive(false);
            m_Routine = null;
        }

        private void ApplyBackground(ToastType type)
        {
            if (m_BackgroundImage == null || m_Backgrounds == null)
            {
                return;
            }

            foreach (ToastBackgroundEntry entry in m_Backgrounds)
            {
                if (entry.Type == type)
                {
                    m_BackgroundImage.sprite = entry.Background;
                    return;
                }
            }
        }
    }
}

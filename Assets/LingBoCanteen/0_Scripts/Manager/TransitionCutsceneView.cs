using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 全局过场动画：包裹 <see cref="LoadingView"/>（切菜进度动画）+ 一段随时间轮播的提示文字。
    /// 用于 3 处："进入 Menu 之前"/"点击开始游戏进入 Main 场景"/"傍晚确认进入下一天"。
    /// 单例、跨场景常驻（<see cref="MonoSingleton{T}"/>），需要在常驻(Launch)场景里手动放一个
    /// 挂好 <see cref="LoadingView"/> 及美术资源引用的实例，代码无法自动创建这些美术引用。
    /// </summary>
    public class TransitionCutsceneView : MonoSingleton<TransitionCutsceneView>
    {
        [Serializable]
        public struct CaptionEntry
        {
            public string Text;
            public float Duration;
        }

        [Header("引用")]
        [SerializeField] private GameObject m_Root;
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private LoadingView m_LoadingView;
        [SerializeField] private TMP_Text m_CaptionText;

        [Header("提示文字（按顺序循环播放，每条可单独配置展示时长）")]
        [SerializeField] private CaptionEntry[] m_Captions =
        {
            new CaptionEntry { Text = "拿出厨具……", Duration = 1.2f },
            new CaptionEntry { Text = "准备切菜……", Duration = 1.2f },
        };

        [SerializeField] private float m_DefaultFadeDuration = 0.3f;
        [Tooltip("过场保底展示时长（秒）：加载很快时仍会至少停留这么久再隐藏，加载较慢时则直接按实际耗时，不会额外等待")]
        [SerializeField] private float m_MinDisplaySeconds = 2f;

        private Coroutine m_CaptionRoutine;
        private float m_ShownAtRealtime = -1f;
        private float m_CurrentProgress;

        public override void Init()
        {
            base.Init();
            if (m_Root != null)
            {
                m_Root.SetActive(false);
            }
        }

        /// <summary>
        /// 显示过场（淡入 + 复位切菜动画 + 开始轮播提示文字），协程返回时淡入已完成。
        /// </summary>
        public IEnumerator ShowAsync(float fadeDuration = -1f)
        {
            if (fadeDuration < 0f)
            {
                fadeDuration = m_DefaultFadeDuration;
            }

            if (m_Root != null)
            {
                m_Root.SetActive(true);
            }

            m_LoadingView?.ResetAnimation();
            m_CurrentProgress = 0f;

            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 0f;
                yield return m_CanvasGroup.FadeToAlpha(1f, fadeDuration);
            }

            if (m_CaptionRoutine != null)
            {
                StopCoroutine(m_CaptionRoutine);
            }
            m_CaptionRoutine = StartCoroutine(CaptionLoopRoutine());

            // 从完全淡入的这一刻开始计时，供 HideAsync 校验保底时长
            m_ShownAtRealtime = Time.unscaledTime;
        }

        /// <summary>
        /// 隐藏过场（若未达到 <see cref="m_MinDisplaySeconds"/> 保底时长，先把进度平滑推进到 100% 补足
        /// 剩余时间——这样即使调用方从未喂过真实进度（比如傍晚推进下一天只是同步重置数据，几乎不耗时），
        /// 切菜动作同步的 food 图片也会跟着平滑推进，而不是全程停在 0），再淡出 + 停止提示文字轮播，
        /// 协程返回时淡出已完成。
        /// </summary>
        public IEnumerator HideAsync(float fadeDuration = -1f)
        {
            if (fadeDuration < 0f)
            {
                fadeDuration = m_DefaultFadeDuration;
            }

            if (m_ShownAtRealtime >= 0f)
            {
                float remaining = m_MinDisplaySeconds - (Time.unscaledTime - m_ShownAtRealtime);
                if (remaining > 0f)
                {
                    yield return AnimateProgressToFullOverTime(remaining);
                }
            }

            SetProgress(1f);

            if (m_CanvasGroup != null)
            {
                yield return m_CanvasGroup.FadeToAlpha(0f, fadeDuration);
            }

            if (m_CaptionRoutine != null)
            {
                StopCoroutine(m_CaptionRoutine);
                m_CaptionRoutine = null;
            }

            if (m_Root != null)
            {
                m_Root.SetActive(false);
            }

            m_ShownAtRealtime = -1f;
        }

        /// <summary>把进度从当前值平滑推进到 100%，耗时 <paramref name="duration"/> 秒（不受 Time.timeScale 影响）。</summary>
        private IEnumerator AnimateProgressToFullOverTime(float duration)
        {
            float startProgress = m_CurrentProgress;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(Mathf.Lerp(startProgress, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetProgress(1f);
        }

        /// <summary>供加载中的调用方喂入真实进度（0~1）。</summary>
        public void SetProgress(float progress)
        {
            m_CurrentProgress = Mathf.Clamp01(progress);
            m_LoadingView?.SetProgress(m_CurrentProgress);
        }

        private IEnumerator CaptionLoopRoutine()
        {
            if (m_Captions == null || m_Captions.Length == 0)
            {
                yield break;
            }

            int index = 0;
            while (true)
            {
                CaptionEntry entry = m_Captions[index % m_Captions.Length];
                if (m_CaptionText != null)
                {
                    m_CaptionText.text = entry.Text;
                }

                yield return new WaitForSeconds(Mathf.Max(0.05f, entry.Duration));
                index++;
            }
        }
    }
}

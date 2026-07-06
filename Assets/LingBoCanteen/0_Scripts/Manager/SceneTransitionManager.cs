using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LingBoCanteen
{
    /// <summary>
    /// 场景切换过渡管理器：负责显示/隐藏黑屏过渡效果
    /// </summary>
    public class SceneTransitionManager : MonoSingleton<SceneTransitionManager>
    {
        [SerializeField] private CanvasGroup m_BlackScreenCanvasGroup;
        [SerializeField] private float m_FadeDuration = 0.5f;

        private bool m_IsTransitioning = false;

        public override void Init()
        {
            base.Init();
            
            // 如果没有配置CanvasGroup，自动创建
            if (m_BlackScreenCanvasGroup == null)
            {
                CreateBlackScreenUI();
            }
            
            // 初始状态为透明（隐藏）
            if (m_BlackScreenCanvasGroup != null)
            {
                m_BlackScreenCanvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// 执行场景过渡：先淡出到黑屏，等待指定时间，再淡入
        /// </summary>
        public IEnumerator TransitionScene(float holdDuration = 0f)
        {
            if (m_IsTransitioning)
            {
                yield break;
            }

            m_IsTransitioning = true;

            // 淡出到黑屏
            if (m_BlackScreenCanvasGroup != null)
            {
                yield return m_BlackScreenCanvasGroup.DOFade(1f, m_FadeDuration).WaitForCompletion();
            }

            // 保持黑屏时间
            if (holdDuration > 0)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            // 淡入（由调用方继续处理）
        }

        /// <summary>
        /// 淡入恢复场景显示
        /// </summary>
        public IEnumerator FadeInScene()
        {
            if (m_BlackScreenCanvasGroup != null)
            {
                yield return m_BlackScreenCanvasGroup.DOFade(0f, m_FadeDuration).WaitForCompletion();
            }

            m_IsTransitioning = false;
        }

        /// <summary>
        /// 立即隐藏黑屏（不动画）
        /// </summary>
        public void HideBlackScreenImmediate()
        {
            m_IsTransitioning = false;
            if (m_BlackScreenCanvasGroup != null)
            {
                m_BlackScreenCanvasGroup.alpha = 0f;
            }
        }

        private void CreateBlackScreenUI()
        {
            // 创建根Canvas
            GameObject canvasGO = new GameObject("BlackScreenCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // 最顶层

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            m_BlackScreenCanvasGroup = canvasGO.AddComponent<CanvasGroup>();

            // 创建黑色Image
            GameObject imageGO = new GameObject("BlackImage");
            imageGO.transform.SetParent(canvasGO.transform, false);

            Image image = imageGO.AddComponent<Image>();
            image.color = Color.black;

            RectTransform rectTransform = imageGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // 标记为DontDestroyOnLoad
            DontDestroyOnLoad(canvasGO);
        }
    }
}

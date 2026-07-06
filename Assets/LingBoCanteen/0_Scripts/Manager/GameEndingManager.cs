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
        [SerializeField] private EndingFormLogic m_EndingForm;  // 结局UI面板（放在场景中）
        [SerializeField] private CanvasGroup m_EndingFormCanvasGroup;  // EndingForm的CanvasGroup（用于淡入淡出）
        [SerializeField] private float m_FadeDuration = 0.5f;  // 过渡时间

        private bool m_HasTriggeredEnding = false;  // 防止重复触发结局

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

            // 如果没有在Inspector中拖入，尝试自动查找
            if (m_EndingForm == null)
            {
                m_EndingForm = FindObjectOfType<EndingFormLogic>();
                if (m_EndingForm != null)
                {
                    Debug.Log("[GameEndingManager] 自动找到EndingForm");
                }
            }

            // 自动获取EndingForm的CanvasGroup
            if (m_EndingForm != null && m_EndingFormCanvasGroup == null)
            {
                m_EndingFormCanvasGroup = m_EndingForm.GetComponent<CanvasGroup>();
                if (m_EndingFormCanvasGroup == null)
                {
                    m_EndingFormCanvasGroup = m_EndingForm.gameObject.AddComponent<CanvasGroup>();
                    Debug.Log("[GameEndingManager] 为EndingForm添加了CanvasGroup");
                }
            }

            // 初始时隐藏EndingForm
            if (m_EndingForm != null)
            {
                m_EndingForm.gameObject.SetActive(false);
            }

            // 初始时设置EndingForm的alpha为0
            if (m_EndingFormCanvasGroup != null)
            {
                m_EndingFormCanvasGroup.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            // 确保游戏时间恢复正常（以防万一）
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
                Debug.Log("[GameEndingManager] 游戏时间已恢复");
            }
        }

        private void Update()
        {
            // 检查游戏是否已结束
            if (m_HasTriggeredEnding || GameEntry.DataNode == null)
            {
                return;
            }

            try
            {
                // 先检查节点是否存在
                if (GameEntry.DataNode.GetNode("Game.IsEnding") == null)
                {
                    return;
                }

                VarBoolean isEnding = GameEntry.DataNode.GetData<VarBoolean>("Game.IsEnding");
                if (isEnding != null && isEnding.Value)
                {
                    m_HasTriggeredEnding = true;

                    // 判断结局类型：是否曾改变过Region
                    bool hasChangedRegion = false;
                    if (GameEntry.DataNode.GetNode("Game.HasChangedRegion") != null)
                    {
                        VarBoolean hasChangedRegionData = GameEntry.DataNode.GetData<VarBoolean>("Game.HasChangedRegion");
                        hasChangedRegion = hasChangedRegionData != null && hasChangedRegionData.Value;
                    }

                    if (hasChangedRegion)
                    {
                        Debug.Log("[GameEndingManager] 触发普通结局（第15天前曾改变过Region）");
                        TriggerNormalEnding();
                    }
                    else
                    {
                        Debug.Log("[GameEndingManager] 触发真实结局（第15天前未改变过Region，活到了第20天）");
                        TriggerTrueEnding();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameEndingManager] Update() 异常: {ex.Message}\n{ex.StackTrace}");
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
        /// 恢复游戏时间（用于退出结局界面时调用）。
        /// </summary>
        public void ResumeGameTime()
        {
            Time.timeScale = 1f;
            Debug.Log("[GameEndingManager] 游戏时间已恢复");
        }

        /// <summary>
        /// 播放结局序列：黑屏 + Panel同时淡入 → 对话播放（如有）→ 结局显示。
        /// </summary>
        private IEnumerator PlayEndingSequence(EndingType endingType, string dialogueAssetName)
        {
            // 1. 显示EndingForm（初始化）
            if (m_EndingForm != null)
            {
                m_EndingForm.gameObject.SetActive(true);
                Debug.Log("[GameEndingManager] EndingForm 已激活");
            }

            // 2. 黑屏 + Panel同时淡入
            yield return StartCoroutine(SimultaneousFade(true, m_FadeDuration));

            // 3. 暂停游戏
            Time.timeScale = 0f;
            Debug.Log("[GameEndingManager] 游戏已暂停");

            // 4. 播放对话（如有）
            if (!string.IsNullOrEmpty(dialogueAssetName))
            {
                if (TextManager.Instance != null)
                {
                    TextManager.Instance.PlayDialogueAsset(dialogueAssetName);
                    
                    // 等待对话播放完毕
                    yield return new WaitUntil(() => TextManager.Instance.IsFinishedPlaying());
                }
            }

            // 5. 设置显示的结局类型
            if (m_EndingForm != null)
            {
                m_EndingForm.ShowEnding(endingType);
                Debug.Log($"[GameEndingManager] 显示结局类型: {endingType}");
            }

            // 6. 黑屏保持全黑状态（游戏已暂停）
            Debug.Log("[GameEndingManager] 结局流程完成，黑屏+Panel保持显示，游戏暂停");
        }

        /// <summary>
        /// 同时对黑屏和Panel进行淡入。
        /// </summary>
        private IEnumerator SimultaneousFade(bool fadeIn, float duration)
        {
            float elapsed = 0f;
            float startAlpha = fadeIn ? 0f : 1f;
            float endAlpha = fadeIn ? 1f : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // 黑屏淡入
                if (m_FadeScreen != null)
                {
                    m_FadeScreen.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                }

                // Panel同时淡入
                if (m_EndingFormCanvasGroup != null)
                {
                    m_EndingFormCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                }

                yield return null;
            }

            // 确保最终值设置正确
            if (m_FadeScreen != null)
            {
                m_FadeScreen.alpha = endAlpha;
            }
            if (m_EndingFormCanvasGroup != null)
            {
                m_EndingFormCanvasGroup.alpha = endAlpha;
            }
        }
    }
}

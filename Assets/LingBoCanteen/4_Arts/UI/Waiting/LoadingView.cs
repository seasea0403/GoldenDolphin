using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LingBoCanteen
{
    /// <summary>
    /// 加载页面：人物在原地以固定频率持续切菜（时间驱动循环），
    /// 被切的食物跟随加载进度逐帧播放"盖子揭开、露出切段"的动画（进度驱动）。
    /// 把本脚本挂在 Loading 界面的根节点上，按 README 拖好引用即可。
    /// </summary>
    public class LoadingView : MonoBehaviour
    {
        [Header("场景引用")]
        [SerializeField] private Image m_Character;      // 人物 Image
        [SerializeField] private Image m_Food;           // 食物 Image
        [SerializeField] private Image m_BarFill;        // 进度条填充 Image（Type 设为 Filled / Horizontal）

        [Header("素材")]
        [SerializeField] private Sprite[] m_FoodStages;  // food_stages/food_00~17，索引=已切刀数
        [SerializeField] private Sprite m_FoodCutFinal;  // food_cut（100% 的最终图）
        [SerializeField] private Sprite m_CharUp;        // char_chop_up（举刀）
        [SerializeField] private Sprite m_CharDown;      // char_chop_down（落刀）

        [Header("切菜（时间驱动，与进度无关）")]
        [Tooltip("每一次切菜动作的间隔（秒），越小切得越快")]
        [SerializeField] private float m_ChopInterval = 0.6f;
        [Tooltip("每次切菜中落刀帧所占的比例（0~1）")]
        [SerializeField] private float m_ChopDownRatio = 0.4f;

        [Header("进度（切开的画面）")]
        [SerializeField] private int m_CutCount = 17;    // 总共切几刀（和 food_stages 数量对应）

        private float m_Progress;
        private float m_ChopTimer;   // 切菜循环计时器
        private RectTransform m_CharRect;
        private Vector2 m_InitCharPos;

        private void Awake()
        {
            m_CharRect = m_Character.rectTransform;
            m_InitCharPos = m_CharRect.anchoredPosition;  // 记录场景配置的初始位置
        }

        private void Update()
        {
            // ---- 切菜循环：固定频率原地切，与进度无关 ----
            m_ChopTimer += Time.deltaTime;
            float phase = (m_ChopTimer % m_ChopInterval) / m_ChopInterval;  // 0~1 一个切菜周期
            m_Character.sprite = phase < m_ChopDownRatio ? m_CharDown : m_CharUp;

            // ---- 食物帧跟随进度播放 ----
            float t = Mathf.Clamp01(m_Progress);
            int cutIndex = Mathf.Min(m_CutCount, Mathf.FloorToInt(t * m_CutCount));
            if (cutIndex >= m_CutCount)
                m_Food.sprite = m_FoodCutFinal;
            else if (m_FoodStages != null && cutIndex < m_FoodStages.Length)
                m_Food.sprite = m_FoodStages[cutIndex];

            // ---- 进度条 ----
            if (m_BarFill != null)
                m_BarFill.fillAmount = t;
        }

        /// <summary>由外部（真实加载逻辑）调用，progress 范围 0~1。</summary>
        public void SetProgress(float progress)
        {
            m_Progress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// 重置动画到初始态（进度清零，切菜计时归位），
        /// 同一个 LoadingView 实例被多次复用时，每次 Show 前调用一次。
        /// </summary>
        public void ResetAnimation()
        {
            m_Progress = 0f;
            m_ChopTimer = 0f;
            if (m_CharRect != null)
            {
                m_CharRect.anchoredPosition = m_InitCharPos;   // 复原到设置的位置
            }
            if (m_FoodStages != null && m_FoodStages.Length > 0)
            {
                m_Food.sprite = m_FoodStages[0];
            }
            if (m_Character != null)
            {
                m_Character.sprite = m_CharUp;
            }
            if (m_BarFill != null)
            {
                m_BarFill.fillAmount = 0f;
            }
        }
    }
}
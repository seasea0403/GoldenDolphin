using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 榨汁机：只接受"可切可榨汁"食材切制完成后再次拖拽过来的中间产物（IsPreCut = true）。
    /// 放入后播放榨汁动画（时长复用 Constant.GameConstant.SQUEEZE_ANIM_TIME），
    /// 动画播完后若杯槽有空位立即复位为空白并生成产出物；若杯槽已满则保持动画结束的最后一帧，
    /// 每帧重试直到杯槽腾出空位。
    /// </summary>
    public class JuicerController : ProcessStationBase
    {
        [SerializeField] private SpriteRenderer m_ItemRenderer;
        [SerializeField] private Animator m_JuicerAnimator;
        [SerializeField] private string m_PlayAnimTrigger = "Squeeze";

        private DRIngredient m_CurrentRow;
        private float m_Elapsed;

        private bool m_InitialItemActive;
        private Sprite m_InitialItemSprite;

        protected override void Awake()
        {
            base.Awake();

            // 道理同 CuttingBoardController：避免 Animator 默认状态一进场就自动播放。
            if (m_JuicerAnimator != null)
            {
                m_JuicerAnimator.enabled = false;
            }

            // 记录进入场景时台面物体本来的激活状态和图片，加工结束后要恢复成这个样子。
            if (m_ItemRenderer != null)
            {
                m_InitialItemActive = m_ItemRenderer.gameObject.activeSelf;
                m_InitialItemSprite = m_ItemRenderer.sprite;
            }
        }

        protected override bool CanAccept(IngredientDragPayload payload)
        {
            return State == ProcessStationState.Empty
                && payload.IsPreCut
                && payload.Row.ProcessType == (int)IngredientProcessType.Both;
        }

        protected override void Accept(IngredientDragPayload payload)
        {
            m_CurrentRow = payload.Row;
            m_Elapsed = 0f;
            State = ProcessStationState.Processing;

            if (m_ItemRenderer != null)
            {
                m_ItemRenderer.gameObject.SetActive(true);
                m_ItemRenderer.sprite = GetSprite(m_CurrentRow, m_CurrentRow.FinalAssetName);
            }

            if (m_JuicerAnimator != null)
            {
                m_JuicerAnimator.enabled = true;
                m_JuicerAnimator.SetTrigger(m_PlayAnimTrigger);
            }
        }

        private void Update()
        {
            if (State == ProcessStationState.Processing)
            {
                m_Elapsed += Time.deltaTime;
                if (m_Elapsed >= Constant.GameConstant.SQUEEZE_ANIM_TIME)
                {
                    State = ProcessStationState.WaitingForOutput;
                    TryFlushToGlass();
                }
            }
            else if (State == ProcessStationState.WaitingForOutput)
            {
                TryFlushToGlass();
            }
        }

        private void TryFlushToGlass()
        {
            OutputSlotGroup glassGroup = PrepAreaManager.Instance != null ? PrepAreaManager.Instance.GlassGroup : null;
            if (glassGroup == null)
            {
                return;
            }

            Sprite outputSprite = GetSprite(m_CurrentRow, m_CurrentRow.FinalAssetName);
            if (glassGroup.TryAddItem(m_CurrentRow.OutputId, m_CurrentRow.OutputName, outputSprite))
            {
                Reset();
            }
        }

        private void Reset()
        {
            m_CurrentRow = null;
            State = ProcessStationState.Empty;
            if (m_ItemRenderer != null)
            {
                // 恢复成进入场景时的样子，而不是强制隐藏物体/清空图片。
                m_ItemRenderer.sprite = m_InitialItemSprite;
                m_ItemRenderer.gameObject.SetActive(m_InitialItemActive);
            }

            if (m_JuicerAnimator != null)
            {
                m_JuicerAnimator.enabled = false;
            }
        }

        private static Sprite GetSprite(DRIngredient row, string assetName)
        {
            return IngredientUtility.GetSprite(row, assetName);
        }
    }
}

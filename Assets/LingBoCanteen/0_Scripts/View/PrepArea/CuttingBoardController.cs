using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 菜板：只接受 ProcessType 为 1（仅可切）或 3（可切可榨汁）的生食材。
    /// 放入后按 CUTTING_MID_TIME 切到 Mid 贴图，到 CUTTING_FINAL_TIME（动画播放完成）触发 OnCutDone：
    /// ProcessType=1（仅可切）尝试自动收纳进碗槽，台面全程保持 Mid 贴图（不会展示 FinalAssetName），
    /// 只有真正成功放入槽的那一刻才消失恢复初始态；槽满则持续展示 Mid，每帧重试；
    /// ProcessType=3（可切可榨汁）切到“刀口态”（CutKnobName）停留在菜板上，允许玩家再次点击
    /// 拖拽去榨汁机或垃圾桶（IsPreCut=true）。仅可切滞留（槽满）时同样允许再次点击拖去垃圾桶丢弃。
    /// </summary>
    public class CuttingBoardController : ProcessStationBase
    {
        [SerializeField] private SpriteRenderer m_ItemRenderer;
        [SerializeField] private Animator m_BoardAnimator;
        [SerializeField] private string m_PlayAnimTrigger = "Cut";

        private DRIngredient m_CurrentRow;
        private float m_Elapsed;
        private bool m_MidApplied;

        private bool m_InitialItemActive;
        private Sprite m_InitialItemSprite;

        protected override void Awake()
        {
            base.Awake();

            // Animator 组件会在 Awake/OnEnable 时自动播放其默认（Entry 指向的）状态，
            // 如果 Animator Controller 里默认状态就是“切”动画本身，会导致一进场就播放。
            // 这里先把组件禁用，只在真正 Accept 时才启用，空闲时再禁用。
            if (m_BoardAnimator != null)
            {
                m_BoardAnimator.enabled = false;
            }

            // 记录进入场景时台面物体本来的激活状态和图片，加工结束后要恢复成这个样子，
            // 而不是强制隐藏物体/清空图片。
            if (m_ItemRenderer != null)
            {
                m_InitialItemActive = m_ItemRenderer.gameObject.activeSelf;
                m_InitialItemSprite = m_ItemRenderer.sprite;
            }
        }

        protected override bool CanAccept(IngredientDragPayload payload)
        {
            if (State != ProcessStationState.Empty || payload.IsPreCut)
            {
                return false;
            }

            return payload.Row.ProcessType == (int)IngredientProcessType.Cut
                || payload.Row.ProcessType == (int)IngredientProcessType.Both;
        }

        protected override void Accept(IngredientDragPayload payload)
        {
            m_CurrentRow = payload.Row;
            m_Elapsed = 0f;
            m_MidApplied = false;
            State = ProcessStationState.Processing;

            if (m_ItemRenderer != null)
            {
                m_ItemRenderer.gameObject.SetActive(true);
                m_ItemRenderer.sprite = LoadSprite(m_CurrentRow, m_CurrentRow.InitialAssetName);
            }

            if (m_BoardAnimator != null)
            {
                m_BoardAnimator.enabled = true;
                m_BoardAnimator.SetTrigger(m_PlayAnimTrigger);
            }
        }

        private void Update()
        {
            if (State == ProcessStationState.Processing)
            {
                UpdateProcessing();
            }
            else if (State == ProcessStationState.WaitingForOutput)
            {
                TryFlushToBowl();
            }
        }

        private void UpdateProcessing()
        {
            m_Elapsed += Time.deltaTime;

            if (!m_MidApplied && m_Elapsed >= Constant.GameConstant.CUTTING_MID_TIME)
            {
                m_MidApplied = true;
                if (m_ItemRenderer != null)
                {
                    m_ItemRenderer.sprite = LoadSprite(m_CurrentRow, m_CurrentRow.MidAssetName);
                }
            }

            if (m_Elapsed >= Constant.GameConstant.CUTTING_FINAL_TIME)
            {
                OnCutDone();
            }
        }

        private void OnCutDone()
        {
            if (m_CurrentRow.ProcessType == (int)IngredientProcessType.Both)
            {
                // 可切可榨汁：切到“刀口态”，停留在菜板上等玩家再拖去榨汁机。
                if (m_ItemRenderer != null)
                {
                    m_ItemRenderer.sprite = LoadSprite(m_CurrentRow, m_CurrentRow.CutKnobName);
                }

                State = ProcessStationState.WaitingForOutput;
                return;
            }

            // 仅可切：台面继续保持 Mid 贴图（不展示 FinalAssetName），
            // 只有真正成功收纳进碗槽的那一刻才会消失、恢复台面初始状态；
            // 槽满则一直滞留展示 Mid，每帧重试，不会提前把 Final 图展示出来。
            State = ProcessStationState.WaitingForOutput;
            TryFlushToBowl();
        }

        private void TryFlushToBowl()
        {
            if (m_CurrentRow.ProcessType == (int)IngredientProcessType.Both)
            {
                // Both 类型不会自动进碗槽，需要玩家手动拖拽走，这里直接返回。
                return;
            }

            OutputSlotGroup bowlGroup = PrepAreaManager.Instance != null ? PrepAreaManager.Instance.BowlGroup : null;
            if (bowlGroup == null)
            {
                return;
            }

            // 进槽用的图标固定使用 FinalAssetName，跟台面此刻展示的 Mid 贴图无关，
            // 台面上从未真正显示过 Final 贴图（成功的瞬间直接清空台面）。
            Sprite finalSprite = LoadSprite(m_CurrentRow, m_CurrentRow.FinalAssetName);
            if (bowlGroup.TryAddItem(m_CurrentRow.OutputId, m_CurrentRow.OutputName, finalSprite))
            {
                ClearBoard();
            }
        }

        /// <summary>
        /// 供 <see cref="Ingredient"/>（菜板上等待二次加工的产出物，或因碗槽已满而滞留的
        /// 仅可切产出物）在被再次点击拾取时调用。可切可榨汁（Both）类型可以被拖去榨汁机或
        /// 垃圾桶；仅可切（Cut）滞留状态只能被拖去垃圾桶（榨汁机会拒绝，见
        /// <see cref="JuicerController.CanAccept"/>）。
        /// </summary>
        public bool TryPickPreCutItem(out IngredientDragPayload payload, out Sprite ghostSprite)
        {
            payload = null;
            ghostSprite = null;

            if (State != ProcessStationState.WaitingForOutput)
            {
                return false;
            }

            ghostSprite = m_ItemRenderer != null ? m_ItemRenderer.sprite : null;
            payload = new IngredientDragPayload(m_CurrentRow, isPreCut: true)
            {
                OnAccepted = ClearBoard,
            };
            return true;
        }

        private void OnMouseDown()
        {
            if (TryPickPreCutItem(out IngredientDragPayload payload, out Sprite ghostSprite))
            {
                PrepDragController.Instance.BeginDrag(payload, ghostSprite, transform.position);
            }
        }

        private void ClearBoard()
        {
            m_CurrentRow = null;
            State = ProcessStationState.Empty;
            if (m_ItemRenderer != null)
            {
                // 恢复成进入场景时的样子，而不是强制隐藏物体/清空图片。
                m_ItemRenderer.sprite = m_InitialItemSprite;
                m_ItemRenderer.gameObject.SetActive(m_InitialItemActive);
            }

            if (m_BoardAnimator != null)
            {
                m_BoardAnimator.enabled = false;
            }
        }

        private static Sprite LoadSprite(DRIngredient row, string assetName)
        {
            // 食材美术资源按 "{Id}_{EnName}" 文件夹约定自动加载，见 IngredientUtility.GetSprite。
            return IngredientUtility.GetSprite(row, assetName);
        }
    }
}

using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 备菜区食材统一交互逻辑，三类食材实体（货架常规食材 / 冰箱肉类食材 / 抽屉特殊食材）
    /// 均挂载本组件，通过 Id 区间（<see cref="IngredientUtility.GetAreaType"/>）区分行为分支：
    /// - Shelf/Fridge：悬停提示 + 点击拾取拖拽到菜板/榨汁机；
    /// - Drawer：点击直接打开对应的操作面板（打蛋/揉面），不走拖拽加工流程。
    /// 要求挂载对象上有一个 Collider2D 用于接收 OnMouse* 消息。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Ingredient : MonoBehaviour
    {
        [Header("基础配置")]
        [SerializeField] private int m_IngredientId;
        [SerializeField] private SpriteRenderer m_IconRenderer;

        [Header("货架专属（锁定/置灰）")]
        [Tooltip("未解锁时显示的锁头覆盖层，仅货架食材需要")]
        [SerializeField] private GameObject m_LockOverlay;
        [SerializeField] private Color m_OutOfStockColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color s_NormalColor = Color.white;

        [Header("抽屉专属（Drawer）")]
        [Tooltip("鸡蛋/面粉点击后弹出的操作面板，仅抽屉食材需要")]
        [SerializeField] private DrawerOperationPanel m_DrawerPanel;

        private DRIngredient m_Row;
        private IngredientAreaType m_AreaType;
        private bool m_IsInteractable;

        public int IngredientId => m_IngredientId;

        private void Awake()
        {
            m_AreaType = IngredientUtility.GetAreaType(m_IngredientId);
            m_Row = GameEntry.DataTable.GetDataTable<DRIngredient>().GetDataRow(m_IngredientId);
            if (m_Row == null)
            {
                Log.Error("Ingredient 找不到 Id 为 {0} 的 DRIngredient 配置。", m_IngredientId);
            }
        }

        private void OnEnable()
        {
            PrepAreaManager.Instance?.RegisterIngredient(this);
            Refresh();
        }

        private void OnDisable()
        {
            PrepAreaManager.Instance?.UnregisterIngredient(this);
        }

        /// <summary>
        /// 刷新解锁/库存显示状态。货架食材：未解锁显示锁头且不可交互；已解锁但库存为 0 置灰且不可交互。
        /// 冰箱/抽屉食材恒可交互。
        /// </summary>
        public void Refresh()
        {
            if (m_Row == null)
            {
                return;
            }

            if (m_AreaType == IngredientAreaType.Shelf)
            {
                int currentDay = GameEntry.DataNode.GetData<VarInt32>("DayCurrent.Value");
                bool locked = !IngredientUtility.IsUnlocked(m_IngredientId, currentDay);
                SetLockOverlay(locked);

                if (locked)
                {
                    m_IsInteractable = false;
                    return;
                }

                IngredientUtility.EnsureUnlockDefaultStock(m_IngredientId);

                int stock = IngredientUtility.GetStock(m_IngredientId);
                bool outOfStock = stock <= 0;
                if (m_IconRenderer != null)
                {
                    m_IconRenderer.color = outOfStock ? m_OutOfStockColor : s_NormalColor;
                }

                m_IsInteractable = !outOfStock;
            }
            else
            {
                // 冰箱食材 / 抽屉食材：无库存限制、默认解锁，恒可交互。
                SetLockOverlay(false);
                if (m_IconRenderer != null)
                {
                    m_IconRenderer.color = s_NormalColor;
                }

                m_IsInteractable = true;
            }
        }

        private void SetLockOverlay(bool locked)
        {
            if (m_LockOverlay != null)
            {
                m_LockOverlay.SetActive(locked);
            }
        }

        private void OnMouseEnter()
        {
            if (m_Row == null || IngredientTooltipView.Instance == null)
            {
                return;
            }

            bool isUnlimited = IngredientUtility.IsUnlimitedStock(m_IngredientId);
            int stock = isUnlimited ? 0 : IngredientUtility.GetStock(m_IngredientId);
            IngredientTooltipView.Instance.Show(transform.position, m_Row.Name, stock, isUnlimited);
        }

        private void OnMouseExit()
        {
            IngredientTooltipView.Instance?.Hide();
        }

        private void OnMouseDown()
        {
            Log.Info($"点击食材，ID:{m_IngredientId}，区域类型:{m_AreaType}，可交互:{m_IsInteractable}");
            
            if (!m_IsInteractable || m_Row == null)
            {
                return;
            }

            if (m_AreaType == IngredientAreaType.Drawer)
            {
                Log.Info("识别为抽屉食材，准备打开面板");
                if (m_DrawerPanel != null)
                {
                    Log.Info("m_DrawerPanel引用有效，执行Open");
                    m_DrawerPanel.Open(m_IngredientId);
                }
                else
                {
                    Log.Warning("抽屉食材 {0} 未配置 DrawerPanel，无法打开操作界面。", m_IngredientId);
                }
                return;
            }

            // 检查是否需要加工（可切或可榨汁）
            bool needsProcessing = m_Row.CanCut || m_Row.CanSqueeze;
            
            if (!needsProcessing)
            {
                // 不需要加工，直接把 output 添加到相应槽位（碗或杯）
                Log.Info($"食材 {m_IngredientId} 不需要加工，直接添加 output ({m_Row.OutputId})");
                if (PrepAreaManager.Instance != null)
                {
                    Sprite outputSprite = IngredientUtility.GetSprite(m_Row, m_Row.FinalAssetName ?? m_Row.InitialAssetName);
                    
                    // 判断应该添加到哪个槽位
                    OutputSlotGroup targetGroup = null;
                    if (m_IngredientId == 1010)  // 1010 是酸奶，添加到杯槽
                    {
                        targetGroup = PrepAreaManager.Instance.GlassGroup;
                        Log.Info($"食材 1010（酸奶）添加到 GlassGroup");
                    }
                    else
                    {
                        targetGroup = PrepAreaManager.Instance.BowlGroup;
                        Log.Info($"食材 {m_IngredientId} 添加到 BowlGroup");
                    }
                    
                    if (targetGroup != null)
                    {
                        bool success = targetGroup.TryAddItem(m_Row.OutputId, m_Row.OutputName, outputSprite);
                        if (success && m_AreaType == IngredientAreaType.Shelf)
                        {
                            IngredientUtility.TryConsumeStock(m_IngredientId);
                            Refresh();
                        }
                    }
                    else
                    {
                        Log.Warning("无法访问对应的槽位组");
                    }
                }
                else
                {
                    Log.Warning("无法访问 PrepAreaManager");
                }
                return;
            }

            BeginDrag();
        }

        private void BeginDrag()
        {
            if (PrepDragController.Instance == null)
            {
                Log.Warning("场景内缺少 PrepDragController，无法拾取拖拽食材。");
                return;
            }

            string moveAssetName = string.IsNullOrEmpty(m_Row.MoveAssetName) ? m_Row.InitialAssetName : m_Row.MoveAssetName;
            Sprite ghostSprite = IngredientUtility.GetSprite(m_Row, moveAssetName);
            if (ghostSprite == null && m_IconRenderer != null)
            {
                ghostSprite = m_IconRenderer.sprite;
            }

            IngredientAreaType areaType = m_AreaType;
            IngredientDragPayload payload = new IngredientDragPayload(m_Row, isPreCut: false)
            {
                OnAccepted = () =>
                {
                    if (areaType == IngredientAreaType.Shelf)
                    {
                        IngredientUtility.TryConsumeStock(m_IngredientId);
                        Refresh();
                    }
                },
            };

            PrepDragController.Instance.BeginDrag(payload, ghostSprite, transform.position);
        }
    }
}

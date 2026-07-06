using UnityEngine;
using UnityEngine.UI;
using GameFramework.Entity;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 白天阶段三区域（订单区、备菜区、烹调区）页面切换与显隐逻辑管理器。
    /// 核心设计原则：逻辑始终常驻运行，仅通过 CanvasGroup 与 Renderer/Collider 的启用/禁用控制视觉、交互的显隐，避免 logic 暂停。
    /// </summary>
    public class AreaSwitchManager : MonoBehaviour
    {
        public enum AreaType
        {
            /// <summary>
            /// 订单区
            /// </summary>
            Order = 0,

            /// <summary>
            /// 备菜区
            /// </summary>
            Prep = 1,

            /// <summary>
            /// 烹调区
            /// </summary>
            Cooking = 2
        }

        public static AreaSwitchManager Instance { get; private set; }

        [Header("UI Roots (对应各区域挂载 CanvasGroup 的 UI 根节点)")]
        [SerializeField] private CanvasGroup m_OrderUIRoot;
        [SerializeField] private CanvasGroup m_PrepUIRoot;
        [SerializeField] private CanvasGroup m_CookingUIRoot;

        [Header("World Object Groups (对应各区域在场景中的交互世界物体总根节点)")]
        [SerializeField] private GameObject m_OrderWorldGroup;
        [SerializeField] private GameObject m_PrepWorldGroup;
        [SerializeField] private GameObject m_CookingWorldGroup;

        [Header("Extra Area Objects (比如单独放置在根目录下的背景图、非层级内多余渲染物等)")]
        [SerializeField] private GameObject[] m_OrderExtraObjects;
        [SerializeField] private GameObject[] m_PrepExtraObjects;
        [SerializeField] private GameObject[] m_CookingExtraObjects;

        [Header("Evening (打烊/傍晚结算) 区域")]
        [Tooltip("傍晚阶段的 UI 根节点 (CanvasGroup)")]
        [SerializeField] private CanvasGroup m_EveningUIRoot;
        [Tooltip("傍晚阶段的世界物体总根节点")]
        [SerializeField] private GameObject m_EveningWorldGroup;
        [Tooltip("傍晚阶段独立配置的额外背景/物体")]
        [SerializeField] private GameObject[] m_EveningExtraObjects;

        [Header("Morning (早上启动) 区域")]
        [Tooltip("早上启动场景的根节点 (进入时自动激活)")]
        [SerializeField] private GameObject m_MorningRoot;

        [Header("Scene Toggle Buttons (可选：在 Inspector 中配置的跳转按钮组件列表)")]
        [SerializeField] private Button[] m_ToOrderButtons;
        [SerializeField] private Button[] m_ToPrepButtons;
        [SerializeField] private Button[] m_ToCookingButtons;

        [Header("Region 视觉效果关联配置")]
        [Tooltip("需要改变颜色的 Mask 遮罩组件 (可以挂在 UI Canvas 或摄像机前上渲染)")]
        [SerializeField] private Image m_MaskImage;
        [Tooltip("Order区域的背景图渲染器")]
        [SerializeField] private SpriteRenderer m_OrderBackgroundRenderer;
        [Tooltip("不同 Region 辖区的差异化渲染配置列表")]
        [SerializeField] private RegionDecorationConfig[] m_RegionConfigs;

        [Header("Properties")]
        [SerializeField] private AreaType m_DefaultArea = AreaType.Order;

        private AreaType m_CurrentArea;
        private bool m_IsEveningActive;

        public AreaType CurrentArea => m_CurrentArea;

        /// <summary>
        /// 当前是否处于傍晚阶段（白天三区域已全部关闭）。
        /// </summary>
        public bool IsEveningActive => m_IsEveningActive;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning($"[AreaSwitchManager] Duplicate instance detected on {gameObject.name}. Destroying...");
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 激活早上启动场景
            if (m_MorningRoot != null)
            {
                m_MorningRoot.SetActive(true);
            }

            // 读取当前区域并播放对应的BGM
            GameRegion currentRegion = GameRegion.Mortal;
            if (GameEntry.DataNode != null && GameEntry.DataNode.GetNode("Area.CurrentType") != null)
            {
                currentRegion = (GameRegion)GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value;
            }

            // 根据当前区域播放对应的BGM
            if (currentRegion == GameRegion.Heaven)
            {
                BGMManager.Instance.PlayBGM(30002, false); // bgm_heaven (无淡入)
            }
            else if (currentRegion == GameRegion.Hell)
            {
                BGMManager.Instance.PlayBGM(30003, false); // bgm_hell (无淡入)
            }
            else
            {
                BGMManager.Instance.PlayBGM(30001, false); // bgm_human_day (无淡入)
            }

            // 绑定按钮回调
            RegisterButtonCallbacks();

            // 首帧初始化：白天开始默认激活订单区，备菜区、烹调区自动进入逻辑运行、视觉静音状态
            SwitchToArea(m_DefaultArea);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // 傍晚阶段已激活时，白天三区域切换快捷键暂不生效，避免误触重新打开白天区域
            if (m_IsEveningActive)
            {
                return;
            }

            // 为方便调试与更优的用户交互体验，提供快捷键切换检测（Alpha 1-3 分别切订单、备菜、烹调）
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchToArea(AreaType.Order);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchToArea(AreaType.Prep);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SwitchToArea(AreaType.Cooking);
            }
        }

        /// <summary>
        /// 注册跳转按钮。
        /// </summary>
        private void RegisterButtonCallbacks()
        {
            if (m_ToOrderButtons != null)
            {
                foreach (var btn in m_ToOrderButtons)
                {
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SwitchToArea(AreaType.Order));
                        // 绑定音效
                        UIButtonSoundHelper.BindButtonSound(btn);
                    }
                }
            }

            if (m_ToPrepButtons != null)
            {
                foreach (var btn in m_ToPrepButtons)
                {
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SwitchToArea(AreaType.Prep));
                        // 绑定音效
                        UIButtonSoundHelper.BindButtonSound(btn);
                    }
                }
            }

            if (m_ToCookingButtons != null)
            {
                foreach (var btn in m_ToCookingButtons)
                {
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SwitchToArea(AreaType.Cooking));
                        // 绑定音效
                        UIButtonSoundHelper.BindButtonSound(btn);
                    }
                }
            }
        }

        /// <summary>
        /// 提供给 Unity UI Button 事件绑定的整型重载版本（Unity Event 在 Inspector 中不支持显示 Enum 参数下拉，但原生支持 int 参数）。
        /// </summary>
        /// <param name="areaIndex">0 = Order, 1 = Prep, 2 = Cooking</param>
        public void SwitchToAreaByInt(int areaIndex)
        {
            SwitchToArea((AreaType)areaIndex);
        }

        /// <summary>
        /// 核心切换方法：将指定区域设置为激活，其他区域置为非激活。全程绝不 Destroy、不 Rebuild，也不 SetActive(false)。
        /// </summary>
        /// <param name="targetArea">目标区域类型</param>
        public void SwitchToArea(AreaType targetArea)
        {
            m_CurrentArea = targetArea;
            m_IsEveningActive = false;
            Debug.Log($"[AreaSwitchManager] Switching area to {targetArea}");

            // 清理拖拽状态：如果玩家在当前区域拖动物品中途进行了（通过快捷键等）切换，安全取消该拖拽动作阻止残留的 ghost 留在屏幕上
            if (PrepDragController.Instance != null && PrepDragController.Instance.IsDragging)
            {
                PrepDragController.Instance.CancelDrag();
            }
            if (KitchenDragController.Instance != null && KitchenDragController.Instance.IsDragging)
            {
                KitchenDragController.Instance.CancelDrag();
            }

            // 根据当前最新的 Region 数据动态刷新对应的装饰(如 Mask 颜色、背景图精灵)
            ApplyRegionDecorations();

            // 1. 各 UI Roots (CanvasGroup) 的可见性与可交互性同步切换
            SetUIRootState(m_OrderUIRoot, targetArea == AreaType.Order);
            SetUIRootState(m_PrepUIRoot, targetArea == AreaType.Prep);
            SetUIRootState(m_CookingUIRoot, targetArea == AreaType.Cooking);

            // 2. 场景中各交互世界物体的渲染 (Renderer类) 及射线拾取 (Collider/Collider2D) 同步切换
            SetWorldGroupState(m_OrderWorldGroup, targetArea == AreaType.Order, m_OrderExtraObjects);
            SetWorldGroupState(m_PrepWorldGroup, targetArea == AreaType.Prep, m_PrepExtraObjects);
            SetWorldGroupState(m_CookingWorldGroup, targetArea == AreaType.Cooking, m_CookingExtraObjects);

            // 3. 傍晚区域始终随白天三区域切换而保持关闭状态
            SetUIRootState(m_EveningUIRoot, false);
            SetWorldGroupState(m_EveningWorldGroup, false, m_EveningExtraObjects);

            // 4. 动态生成的顾客实体不在任何 WorldGroup 层级下，需单独同步显隐状态：只有 Order 区域激活时才可见
            SetCustomerEntitiesVisibility(targetArea == AreaType.Order);
        }

        /// <summary>
        /// 一天营业结束后调用：关闭点单区/备菜区/烹调区的世界物体和 UI，打开傍晚(打烊结算)区域的世界物体和 UI。
        /// 全程沿用 CanvasGroup + Renderer/Collider 启停的显隐方案，不 Destroy、不 SetActive(false) 任何常驻节点。
        /// </summary>
        public void SwitchToEvening()
        {
            Debug.Log("[AreaSwitchManager] 白天营业结束，切换至傍晚(打烊结算)区域。");

            // 清理拖拽状态，避免残留 ghost
            if (PrepDragController.Instance != null && PrepDragController.Instance.IsDragging)
            {
                PrepDragController.Instance.CancelDrag();
            }
            if (KitchenDragController.Instance != null && KitchenDragController.Instance.IsDragging)
            {
                KitchenDragController.Instance.CancelDrag();
            }

            m_IsEveningActive = true;

            // 1. 关闭白天三区域的 UI 与世界物体
            SetUIRootState(m_OrderUIRoot, false);
            SetUIRootState(m_PrepUIRoot, false);
            SetUIRootState(m_CookingUIRoot, false);

            SetWorldGroupState(m_OrderWorldGroup, false, m_OrderExtraObjects);
            SetWorldGroupState(m_PrepWorldGroup, false, m_PrepExtraObjects);
            SetWorldGroupState(m_CookingWorldGroup, false, m_CookingExtraObjects);

            // 2. 打开傍晚区域的 UI 与世界物体
            SetUIRootState(m_EveningUIRoot, true);
            SetWorldGroupState(m_EveningWorldGroup, true, m_EveningExtraObjects);

            // 3. 傍晚阶段顾客一律不可见
            SetCustomerEntitiesVisibility(false);
        }

        /// <summary>
        /// 读取 DataNode 的最新 Region（游戏区域），并动态渲染遮罩色彩与 Order 背景图精灵。
        /// </summary>
        public void ApplyRegionDecorations()
        {
            GameRegion currentRegion = GameRegion.Mortal;
            if (GameEntry.DataNode != null && GameEntry.DataNode.GetNode("Area.CurrentType") != null)
            {
                currentRegion = (GameRegion)GameEntry.DataNode.GetData<VarInt32>("Area.CurrentType").Value;
            }

            if (m_RegionConfigs == null || m_RegionConfigs.Length == 0)
            {
                return;
            }

            RegionDecorationConfig? matchedConfig = null;
            foreach (var config in m_RegionConfigs)
            {
                if (config.Region == currentRegion)
                {
                    matchedConfig = config;
                    break;
                }
            }

            // 如果匹配到该所属分区配置，则进行改变
            if (matchedConfig.HasValue)
            {
                var cfg = matchedConfig.Value;

                if (m_MaskImage != null)
                {
                    m_MaskImage.color = cfg.MaskColor;
                }

                if (m_OrderBackgroundRenderer != null && cfg.OrderBackgroundSprite != null)
                {
                    m_OrderBackgroundRenderer.sprite = cfg.OrderBackgroundSprite;
                }
            }
        }

        /// <summary>
        /// 控制 CanvasGroup 状态，达成完美显隐与交互阻断，不产生任何 SetActive 侧边效应和逻辑中断
        /// </summary>
        private void SetUIRootState(CanvasGroup uiRoot, bool isActive)
        {
            if (uiRoot == null) return;

            uiRoot.alpha = isActive ? 1f : 0f;
            uiRoot.interactable = isActive;
            uiRoot.blocksRaycasts = isActive;
        }

        /// <summary>
        /// 在保持 GameObject 本身 Active 正常运行逻辑（计时器、事件、协程、Update 等）的情况下，
        /// 针对场景世界物体，只控制其 Renderer 组件 of values, UI visible, and Collider properties
        /// </summary>
        private void SetWorldGroupState(GameObject groupRoot, bool isActive, GameObject[] extraObjects)
        {
            if (groupRoot != null)
            {
                SetObjectHierarchyState(groupRoot, isActive);
            }

            // 处理独立配置的额外层（如脱离普通父子层级的背景图等）
            if (extraObjects != null)
            {
                foreach (var extra in extraObjects)
                {
                    if (extra != null)
                    {
                        SetObjectHierarchyState(extra, isActive);
                    }
                }
            }
        }

        /// <summary>
        /// 是否应当显示订单区顾客（供动态生成的顾客实体在 OnShow 时立刻查询自身应有的显隐状态，
        /// 避免因生成时机恰好落在两次 SwitchToArea 调用之间，导致短暂显示在错误区域，或者切回订单区后依然不可见）。
        /// </summary>
        public bool ShouldCustomerBeVisible()
        {
            return !m_IsEveningActive && m_CurrentArea == AreaType.Order;
        }

        /// <summary>
        /// 供顾客实体在 OnShow 时主动调用：把自身的 Renderer/Collider 状态同步为当前应有的显隐状态。
        /// 解决"顾客恰好在切换区域瞬间生成，导致显示在错误区域，或切回订单区后依然不可见"的时序问题。
        /// </summary>
        public void SyncCustomerVisibility(GameObject customerGo)
        {
            if (customerGo == null)
            {
                return;
            }

            SetObjectHierarchyState(customerGo, ShouldCustomerBeVisible());
        }

        /// <summary>
        /// 特殊防错处理：动态生成的顾客实体是不在 m_OrderWorldGroup 下的（游戏运行时他们存在于 GameFramework 的 Entity Group 容器下），
        /// 必须在切换区域时对动态顾客实体的显示组件及碰撞器也进行同步的显示置空/激活。全流程只调用一次，避免被其他区域调用覆盖。
        /// </summary>
        private void SetCustomerEntitiesVisibility(bool isVisible)
        {
            if (GameEntry.Entity == null)
            {
                return;
            }

            var customerGroup = GameEntry.Entity.GetEntityGroup("Customer");
            if (customerGroup == null)
            {
                return;
            }

            IEntity[] customerEntities = customerGroup.GetAllEntities();
            foreach (var entity in customerEntities)
            {
                if (entity != null && entity.Handle != null)
                {
                    GameObject go = entity.Handle as GameObject;
                    if (go != null)
                    {
                        SetObjectHierarchyState(go, isVisible);
                    }
                }
            }
        }

        /// <summary>
        /// 递归/树状管理指定 GameObject 的所有渲染、碰撞和 UI 组件状态。
        /// </summary>
        private void SetObjectHierarchyState(GameObject obj, bool isActive)
        {
            if (obj == null) return;

            // 特殊日志：用于调试 Cloth/Ring 碰撞体初始化问题（同时检查自身及子物体命名，避免顶层容器命名不含关键字导致漏判）
            bool logDetailed = obj.name.Contains("Cloth") || obj.name.Contains("Ring");

            // 1. 所有的渲染组件 (SpriteRenderer, MeshRenderer, TilemapRenderer 等)
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    r.enabled = isActive;
                    if (logDetailed || r.gameObject.name.Contains("Cloth") || r.gameObject.name.Contains("Ring"))
                    {
                        Debug.Log($"[AreaSwitchManager] Renderer: {r.gameObject.name}, enabled={isActive}");
                    }
                }
            }

            // 2. 所有的 UI 渲染组件 (Image, Text, RawImage, TMP_Text 等等，世界空间 UI 会用到这些)
            UnityEngine.UI.Graphic[] graphics = obj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var g in graphics)
            {
                if (g != null)
                {
                    g.enabled = isActive;
                }
            }

            // 3. 所有的 World Space Canvas 
            Canvas[] canvases = obj.GetComponentsInChildren<Canvas>(true);
            foreach (var c in canvases)
            {
                if (c != null)
                {
                    c.enabled = isActive;
                }
            }

            // 4. 所有 Collider2D 碰撞体
            Collider2D[] colliders2D = obj.GetComponentsInChildren<Collider2D>(true);
            foreach (var col in colliders2D)
            {
                if (col != null)
                {
                    col.enabled = isActive;
                    if (logDetailed || col.gameObject.name.Contains("Cloth") || col.gameObject.name.Contains("Ring"))
                    {
                        Debug.Log($"[AreaSwitchManager] Collider2D: {col.gameObject.name} ({col.GetType().Name}), enabled={isActive}");
                    }
                }
            }

            // 5. 所有 3D Collider 碰撞体
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = isActive;
                }
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 烹调区单个炉灶/烤箱工位：选锅、投料（碗/杯槽产出物 + 调料）与配方候选收窄、开火烹饪计时、
    /// 成熟缓冲/糊锅、糊锅食物拖去垃圾桶重置、装盘等全部状态在这里驱动。
    /// 场景中放 3 个实例：2 个普通炉灶（m_IsOven=false）+ 1 个烤箱（m_IsOven=true，恒定 PotType.Oven，永不进入 Empty）。
    ///
    /// 锅本身不可拖拽：Idle 状态下锅里还没放入任何食材/调料时，点击视为“重新选锅”（见 <see cref="OnMouseDown"/>）。
    /// 只有 Burnt 状态下的糊锅食物允许拖去垃圾桶。
    ///
    /// 碰撞体归属：<see cref="KitchenStationBase"/> 要求的 <see cref="Collider2D"/> 挂在本组件所在的同一个
    /// GameObject 上（就是接受 <see cref="OnMouseDown"/> 点击选锅的那个物体），后续投料的拖放命中检测
    /// （<see cref="KitchenStationBase.ContainsPoint"/>）用的也是这同一个碰撞体；m_PotRenderer/m_FoodRenderer/
    /// m_CoverRenderer 等只是纯展示用的子物体精灵，不需要各自再挂碰撞体。
    ///
    /// 嵌套配方（番茄肉酱 2014 -&gt; 番茄肉酱面 2013）无需任何专属代码：完成一道 <see cref="DishRecipeUtility.DishRecipe.IsIntermediate"/>
    /// 的菜时，不走 Finished/装盘流程，而是把该菜品自身的 DishId 当成"新投放的一件食材"重新走一遍投料逻辑，
    /// 后续配方是否把这个中间菜品 Id 列进 IngList，完全由 Dish 表数据决定。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PotController : KitchenStationBase
    {
        [System.Serializable]
        private struct SeasoningAnimEntry
        {
            public SeasoningType SeasoningType;
            public float Duration;
        }

        [Header("是否为烤箱：无需选锅界面，恒定持有 Oven 这口锅")]
        [SerializeField] private bool m_IsOven;

        [Header("选锅面板（仅普通炉灶需要配置）")]
        [SerializeField] private PotSelectionPanel m_SelectionPanel;

        [System.Serializable]
        private struct PotCoverEntry
        {
            public PotType Type;
            public Sprite CoverSprite;
        }

        [Header("显示")]
        [SerializeField] private SpriteRenderer m_PotRenderer;
        [SerializeField] private SpriteRenderer m_FoodRenderer;
        [SerializeField] private Sprite m_OvenDefaultSprite;
        [Tooltip("仅烤箱使用：开火烹饪期间炉体切换为该“已开启”外观，关火/糊锅后切回 m_OvenDefaultSprite")]
        [SerializeField] private Sprite m_OvenCookingSprite;
        [SerializeField] private Sprite m_BurntFoodSprite;

        [Header("锅盖：加水/加入第一件非调料食材后显示；开火前每次加入调料、开火、糊锅时都会去掉（不同锅型盖子不同）")]
        [SerializeField] private SpriteRenderer m_CoverRenderer;
        [SerializeField] private PotCoverEntry[] m_PotCovers;

        [Header("投料气泡区")]
        [SerializeField] private Image[] m_BubbleSlots;

        [Header("烹饪 UI（开火/关火复用同一个按钮）")]
        [SerializeField] private Button m_CookButton;
        [Tooltip("炉灶类（非烤箱）开火时按钮顺时针旋转的角度（默认 -90 表示顺时针 90 度），关火时转回初始角度；烤箱不旋转")]
        [SerializeField] private float m_CookButtonFiredAngle = -90f;
        [SerializeField] private Slider m_ProgressSlider;
        [SerializeField] private Image m_SliderHandleImage;
        [SerializeField] private Sprite m_HandleNormalSprite;
        [SerializeField] private Sprite m_HandleFinishedSprite;
        [Tooltip("关火倒计时（Finished）缓冲期的进度条填充区域：前 COOK_FINISH_HANDLE_TIME 秒外观不变，超过后变为警示色")]
        [SerializeField] private Image m_SliderFillImage;
        [SerializeField] private Color m_FillWarningColor = Color.red;
        private Color m_FillNormalColor = Color.white;

        [Header("拖拽配置")]
        [Tooltip("拖拽时使用的统一图片（可选，若为空则使用原锅图片）")]
        [SerializeField] private Sprite m_DragSprite;
        [Tooltip("移动态图片：当锅/烤箱有食材时可拖拽时的显示图片")]
        [SerializeField] private Sprite m_MovableSprite;

        [Header("动画（一）：倒水/撒料播放在“锅/食物”自身精灵的 Animator 上（Pour 与 Sprinkle 共用同一个 Animator）")]
        [Tooltip("Controller 需要的参数契约：int PotType、int SeasoningId、trigger Pour、trigger Sprinkle")]
        [SerializeField] private Animator m_PotAnimator;
        [SerializeField] private string m_PotTypeParam = "PotType";
        [SerializeField] private string m_SeasoningIdParam = "SeasoningId";
        [SerializeField] private string m_PourAnimTrigger = "Pour";
        [SerializeField] private string m_SprinkleAnimTrigger = "Sprinkle";
        [Tooltip("汤锅选中后倒水准备动画时长（秒），期间处于 Preparing 状态不可投料")]
        [SerializeField] private float m_PourAnimDuration = 1f;
        [Tooltip("不同调料的撒料动画时长不同（秒），气泡图标必须等对应动画播完才显示；未在列表中的调料类型使用下面的默认时长")]
        [SerializeField] private SeasoningAnimEntry[] m_SeasoningAnimDurations;
        [SerializeField] private float m_DefaultSeasoningAnimDuration = 1.1f;

        [Header("动画（二）：开火/烹饪循环播放在“炉灶”自身的 Animator 上，与上面（一）不是同一个组件")]
        [Tooltip("烤箱没有独立的“锅”对象，本身既是炉灶又是锅：可在 Inspector 里把这个字段和上面的 m_PotAnimator 指向同一个 Animator 组件")]
        [SerializeField] private Animator m_StoveAnimator;
        [SerializeField] private string m_IsCookingParam = "IsCooking";

        private PotStationState m_State = PotStationState.Empty;
        private int m_PotType;
        private Sprite m_OriginalPotSprite; // 保存选锅时的原始锅图片，用于恢复
        private readonly List<int> m_PlacedItemIds = new List<int>();
        private List<int> m_CandidateDishIds = new List<int>();
        private List<int> m_PendingCandidates;
        private int m_MatchedDishId = -1;
        private float m_CookElapsed;
        private float m_FinishedElapsed;
        private float m_PrepareElapsed;
        private Quaternion m_CookButtonInitialRotation;
        private float m_CookButtonInitialAlpha; // 保存按钮初始透明度，置灰时不改变透明度
        private Color m_CookButtonInitialColor; // 保存按钮初始颜色，用于恢复
        private static readonly DishUnlockService s_DishService = new DishUnlockService();

        public bool IsReadyToServe => m_State == PotStationState.ReadyToServe;

        protected override void Awake()
        {
            base.Awake();

            if (m_PotAnimator != null)
            {
                m_PotAnimator.enabled = false;
            }

            if (m_FoodRenderer != null)
            {
                m_FoodRenderer.gameObject.SetActive(false);
            }

            if (m_SliderFillImage != null)
            {
                m_FillNormalColor = m_SliderFillImage.color;
            }

            HideCover();
            ClearAllBubbleSlots();

            if (m_CookButton != null)
            {
                m_CookButton.onClick.AddListener(OnCookButtonClicked);
                m_CookButton.interactable = false;
                m_CookButtonInitialRotation = m_CookButton.transform.localRotation;
                
                // 保存按钮的初始颜色和透明度
                Image buttonImage = m_CookButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    m_CookButtonInitialColor = buttonImage.color;
                    m_CookButtonInitialAlpha = buttonImage.color.a;
                }
                else
                {
                    m_CookButtonInitialColor = Color.white;
                    m_CookButtonInitialAlpha = 1f;
                }
                
                // 绑定音效
                UIButtonSoundHelper.BindButtonSound(m_CookButton);
            }

            if (m_ProgressSlider != null)
            {
                m_ProgressSlider.value = 0f;
                m_ProgressSlider.gameObject.SetActive(false);
            }

            if (m_IsOven)
            {
                m_PotType = (int)PotType.Oven;
                m_State = PotStationState.Idle;
                if (m_PotRenderer != null)
                {
                    m_PotRenderer.sprite = m_OvenDefaultSprite != null ? m_OvenDefaultSprite : m_PotRenderer.sprite;
                    m_PotRenderer.gameObject.SetActive(true);
                }

                if (m_PotAnimator != null)
                {
                    m_PotAnimator.enabled = true;
                    m_PotAnimator.SetInteger(m_PotTypeParam, m_PotType);
                }
            }
            else
            {
                m_State = PotStationState.Empty;
                if (m_PotRenderer != null)
                {
                    m_PotRenderer.gameObject.SetActive(false);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            KitchenManager.Instance?.RegisterPot(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            KitchenManager.Instance?.UnregisterPot(this);
        }

        private void Update()
        {
            switch (m_State)
            {
                case PotStationState.Preparing:
                    UpdatePreparing();
                    break;

                case PotStationState.Cooking:
                    UpdateCooking();
                    break;

                case PotStationState.Finished:
                    UpdateFinished();
                    break;
            }
        }

        private void UpdatePreparing()
        {
            m_PrepareElapsed += Time.deltaTime;
            if (m_PrepareElapsed >= m_PourAnimDuration)
            {
                m_State = PotStationState.Idle;
            }
        }

        private bool m_ShouldOpenSelectionOnMouseUp;

        private void OnMouseDown()
        {
            m_ShouldOpenSelectionOnMouseUp = false;
            switch (m_State)
            {
                case PotStationState.Empty:
                    // 标记在鼠标释放时打开选锅面板
                    m_ShouldOpenSelectionOnMouseUp = true;
                    break;

                case PotStationState.Idle:
                    // 非烹饪状态：锅里有食材时可拖拽整锅到垃圾桶重置；无食材时点击为"重新选锅"
                    if (!m_IsOven && m_PlacedItemIds.Count > 0)
                    {
                        // 有食材，允许拖拽整锅到垃圾桶
                        BeginDragPot();
                    }
                    else if (!m_IsOven && m_PlacedItemIds.Count == 0)
                    {
                        // 无食材，重新选锅（标记在鼠标释放时打开）
                        ResetToEmpty();
                        m_ShouldOpenSelectionOnMouseUp = true;
                    }
                    break;

                case PotStationState.Burnt:
                    BeginDragFoodOnly();
                    break;
            }
        }

        private void OnMouseUp()
        {
            // 鼠标释放时才打开选锅面板
            if (m_ShouldOpenSelectionOnMouseUp)
            {
                m_ShouldOpenSelectionOnMouseUp = false;
                m_SelectionPanel?.Open(this);
            }
        }

        private void OnMouseExit()
        {
            // 离开烹调区时隐藏tooltip
            IngredientTooltipView.Instance?.Hide();
        }

        /// <summary>
        /// 供 <see cref="PotSelectionPanel"/> 在玩家选定锅具类型后调用。
        /// </summary>
        public void SelectPot(int potType, Sprite potSprite)
        {
            if (m_State != PotStationState.Empty)
            {
                return;
            }

            m_PotType = potType;
            m_OriginalPotSprite = potSprite; // 保存原始锅图片，用于后续恢复

            if (m_PotRenderer != null)
            {
                m_PotRenderer.sprite = potSprite;
                m_PotRenderer.gameObject.SetActive(true);
            }

            if (m_PotAnimator != null)
            {
                m_PotAnimator.enabled = true;
                m_PotAnimator.SetInteger(m_PotTypeParam, potType);
            }

            if (potType == (int)PotType.SoupPot)
            {
                // 汤锅：放置完成后自动播放倒水动画，动画播放完毕前不可投料（Preparing 状态下 CanAccept 天然拒绝）。
                m_PrepareElapsed = 0f;
                m_State = PotStationState.Preparing;
                m_PotAnimator?.SetTrigger(m_PourAnimTrigger);
            }
            else
            {
                m_State = PotStationState.Idle;
            }
        }

        protected override bool CanAccept(KitchenDragPayload payload)
        {
            if (m_State != PotStationState.Idle)
            {
                return false;
            }

            if (payload.Kind != KitchenDragItemKind.Ingredient && payload.Kind != KitchenDragItemKind.Seasoning)
            {
                return false;
            }

            if (m_PlacedItemIds.Contains(payload.ItemId))
            {
                // 每件食材/调料只能放入一次，不可重复添加。
                return false;
            }

            m_PendingCandidates = m_PlacedItemIds.Count == 0
                ? DishRecipeUtility.GetCandidateDishIds(m_PotType, payload.ItemId)
                : DishRecipeUtility.FilterCandidates(m_CandidateDishIds, payload.ItemId);

            // 过滤：只有当天已解锁的成品菜，或者是无需显式解锁的中间菜品（IsIntermediate == true），才能作为候选并被投入
            int currentDay = GetCurrentDaySafely();
            List<int> unlockedDishes = s_DishService.GetUnlockedDishIds(currentDay);
            m_PendingCandidates = m_PendingCandidates.FindAll(dishId =>
            {
                DishRecipeUtility.DishRecipe recipe = DishRecipeUtility.GetRecipe(dishId);
                if (recipe == null)
                {
                    return false;
                }
                return recipe.IsIntermediate || unlockedDishes.Contains(dishId);
            });

            return m_PendingCandidates.Count > 0;
        }

        protected override void Accept(KitchenDragPayload payload)
        {
            // 占位不跳位：这一件在气泡区的下标就是加入前已有的项数。即便是调料（视觉显示会延迟到
            // 撒料动画播完），这个槽位也从现在起就被预定，不会被后续投放的下一件挤占。
            int slotIndex = m_PlacedItemIds.Count;
            bool isFirstItem = slotIndex == 0; // 检查是否是第一个食材
            
            m_PlacedItemIds.Add(payload.ItemId);
            m_CandidateDishIds = m_PendingCandidates;
            m_PendingCandidates = null;

            // ★ 【已移除】第一个食材不再换贴图
            // if (isFirstItem && m_MovableSprite != null && m_PotRenderer != null)
            // {
            //     m_PotRenderer.sprite = m_MovableSprite;
            // }

            // 播放食材放置音效
            SoundManager.Instance?.PlayIngredientPlaceSound();

            if (payload.Kind == KitchenDragItemKind.Seasoning)
            {
                // 开火前每次加入调料、播放撒料动画时，锅盖都会被去掉。
                HideCover();

                if (m_PotAnimator != null)
                {
                    m_PotAnimator.SetInteger(m_SeasoningIdParam, payload.ItemId);
                    m_PotAnimator.SetTrigger(m_SprinkleAnimTrigger);
                }

                // 不同调料的撒料动画时长不同，气泡图标要等对应那一款动画播完才显示。
                StartCoroutine(RevealBubbleSlotAfterDelay(slotIndex, payload.Icon, GetSeasoningAnimDuration(payload.ItemId)));
            }
            else
            {
                // ★ 【改进】加入第一个非调料食材后盖上锅盖（不需要换贴图）
                ShowCover();
                SetBubbleSlot(slotIndex, payload.Icon);
            }

            m_MatchedDishId = DishRecipeUtility.FindExactMatch(m_CandidateDishIds, m_PlacedItemIds);
            if (m_CookButton != null)
            {
                m_CookButton.interactable = m_MatchedDishId >= 0;
            }
        }

        private IEnumerator RevealBubbleSlotAfterDelay(int slotIndex, Sprite icon, float delay)
        {
            yield return new WaitForSeconds(delay);
            SetBubbleSlot(slotIndex, icon);
        }

        private void SetBubbleSlot(int slotIndex, Sprite icon)
        {
            if (m_BubbleSlots == null || slotIndex < 0 || slotIndex >= m_BubbleSlots.Length || m_BubbleSlots[slotIndex] == null)
            {
                return;
            }

            m_BubbleSlots[slotIndex].sprite = icon;
            m_BubbleSlots[slotIndex].preserveAspect = true; // 按槽位大小等比缩放显示，避免图标被拉伸变形
            m_BubbleSlots[slotIndex].gameObject.SetActive(true);
        }

        /// <summary>
        /// 根据当前 <see cref="m_PotType"/> 显示对应锅盖（不同锅型盖子不同）。
        /// </summary>
        private void ShowCover()
        {
            if (m_CoverRenderer == null)
            {
                return;
            }

            if (m_PotCovers != null)
            {
                foreach (PotCoverEntry entry in m_PotCovers)
                {
                    if ((int)entry.Type == m_PotType)
                    {
                        m_CoverRenderer.sprite = entry.CoverSprite;
                        break;
                    }
                }
            }

            m_CoverRenderer.gameObject.SetActive(true);
        }

        private void HideCover()
        {
            if (m_CoverRenderer != null)
            {
                m_CoverRenderer.gameObject.SetActive(false);
            }
        }

        private void ClearAllBubbleSlots()
        {
            if (m_BubbleSlots == null)
            {
                return;
            }

            foreach (Image slot in m_BubbleSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                slot.sprite = null;
                slot.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 不同调料撒料动画播放时长不同，未在 <see cref="m_SeasoningAnimDurations"/> 列表中配置的调料类型
        /// 使用 <see cref="m_DefaultSeasoningAnimDuration"/>。
        /// </summary>
        private float GetSeasoningAnimDuration(int seasoningId)
        {
            if (m_SeasoningAnimDurations != null)
            {
                foreach (SeasoningAnimEntry entry in m_SeasoningAnimDurations)
                {
                    if ((int)entry.SeasoningType == seasoningId)
                    {
                        return entry.Duration;
                    }
                }
            }

            return m_DefaultSeasoningAnimDuration;
        }

        /// <summary>
        /// 安全读取 DataNode 里的 "DayCurrent.Value"（当前游戏天数）。
        /// 在直接 Play 测试等节点未初始化情况下，安全回退到第 1 天。
        /// </summary>
        private int GetCurrentDaySafely()
        {
            const string path = "DayCurrent.Value";
            if (GameEntry.DataNode.GetNode(path) == null)
            {
                return 1;
            }

            return GameEntry.DataNode.GetData<VarInt32>(path).Value;
        }

        /// <summary>
        /// 开火与关火复用同一个按钮：Idle 且已匹配完整配方时点击 = 开火；Finished（已成熟未糊）时点击 = 关火。
        /// </summary>
        private void OnCookButtonClicked()
        {
            switch (m_State)
            {
                case PotStationState.Idle:
                    StartCooking();
                    break;

                case PotStationState.Finished:
                    PowerOff();
                    break;
            }
        }

        private void StartCooking()
        {
            if (m_MatchedDishId < 0)
            {
                return;
            }

            m_State = PotStationState.Cooking;
            m_CookElapsed = 0f;

            // ★ 【新增】确保锅的贴图在开火时可见
            if (m_PotRenderer != null)
            {
                m_PotRenderer.gameObject.SetActive(true);
            }

            if (m_CookButton != null)
            {
                SetButtonGrayedOut(true);
                if (!m_IsOven)
                {
                    m_CookButton.transform.localRotation = m_CookButtonInitialRotation * Quaternion.Euler(0f, 0f, m_CookButtonFiredAngle);
                }
            }

            if (m_ProgressSlider != null)
            {
                m_ProgressSlider.gameObject.SetActive(true);
                m_ProgressSlider.value = 0f;
            }

            if (m_SliderHandleImage != null && m_HandleNormalSprite != null)
            {
                m_SliderHandleImage.sprite = m_HandleNormalSprite;
            }

            if (m_SliderFillImage != null)
            {
                m_SliderFillImage.color = m_FillNormalColor;
            }

            // 烧火时显示锅盖而不是隐藏
            ShowCover();

            if (m_StoveAnimator != null)
            {
                m_StoveAnimator.SetBool(m_IsCookingParam, true);
            }

            if (m_IsOven && m_PotRenderer != null && m_OvenCookingSprite != null)
            {
                m_PotRenderer.sprite = m_OvenCookingSprite;
            }

            // 播放开火音效
            SoundManager.Instance?.PlayStoveOnSound();
        }

        private void UpdateCooking()
        {
            m_CookElapsed += Time.deltaTime;

            if (m_ProgressSlider != null)
            {
                m_ProgressSlider.value = Mathf.Clamp01(m_CookElapsed / Constant.GameConstant.DEFAULT_COOK_TIME);
            }

            if (m_CookElapsed < Constant.GameConstant.DEFAULT_COOK_TIME)
            {
                return;
            }

            OnCookTimeReached();
        }

        private void OnCookTimeReached()
        {
            DishRecipeUtility.DishRecipe recipe = DishRecipeUtility.GetRecipe(m_MatchedDishId);

            if (recipe != null && recipe.IsIntermediate)
            {
                // 中间配料（例如番茄肉酱）：不进入成熟/装盘流程，做完直接把自己的 DishId 当成一件新食材
                // 留在锅里，继续参与后续配方匹配（例如番茄肉酱面还需要投入面团）；回到 Idle 说明这一轮
                // 烹饪已经结束，循环烹饪动画和按钮旋转都要复位。
                if (m_StoveAnimator != null)
                {
                    m_StoveAnimator.SetBool(m_IsCookingParam, false);
                }

                if (m_IsOven && m_PotRenderer != null && m_OvenDefaultSprite != null)
                {
                    m_PotRenderer.sprite = m_OvenDefaultSprite;
                }

                ClearAllBubbleSlots();
                m_PlacedItemIds.Clear();
                m_PlacedItemIds.Add(m_MatchedDishId);
                SetBubbleSlot(0, s_DishService.GetDishIcon(m_MatchedDishId));
                ShowCover(); // 中间菜品本身也算一件非调料食材，重新盖上锅盖

                m_CandidateDishIds = DishRecipeUtility.GetCandidateDishIds(m_PotType, m_MatchedDishId);

                // 同样过滤当天防匹配到未解锁的后续菜品
                int currentDay = GetCurrentDaySafely();
                List<int> unlockedDishes = s_DishService.GetUnlockedDishIds(currentDay);
                m_CandidateDishIds = m_CandidateDishIds.FindAll(dishId =>
                {
                    DishRecipeUtility.DishRecipe rec = DishRecipeUtility.GetRecipe(dishId);
                    if (rec == null)
                    {
                        return false;
                    }
                    return rec.IsIntermediate || unlockedDishes.Contains(dishId);
                });

                m_MatchedDishId = DishRecipeUtility.FindExactMatch(m_CandidateDishIds, m_PlacedItemIds);

                if (m_CookButton != null)
                {
                    if (!m_IsOven)
                    {
                        m_CookButton.transform.localRotation = m_CookButtonInitialRotation;
                    }

                    SetButtonGrayedOut(m_MatchedDishId < 0);
                }

                if (m_ProgressSlider != null)
                {
                    m_ProgressSlider.value = 0f;
                    m_ProgressSlider.gameObject.SetActive(false);
                }

                m_State = PotStationState.Idle;
                return;
            }

            // 6 秒烹饪结束但还没点关火、也还没糊锅之前，循环烹饪动画继续播放，直到 PowerOff/BecomeBurnt 才停止。
            m_State = PotStationState.Finished;
            m_FinishedElapsed = 0f;

            if (m_FoodRenderer != null)
            {
                m_FoodRenderer.sprite = s_DishService.GetDishIcon(m_MatchedDishId);
                m_FoodRenderer.gameObject.SetActive(true);
            }

            // 刚进入 Finished 缓冲期时外观保持不变，直到 COOK_FINISH_HANDLE_TIME 秒后才切换为警示样式（见 UpdateFinished）。
            if (m_SliderHandleImage != null && m_HandleNormalSprite != null)
            {
                m_SliderHandleImage.sprite = m_HandleNormalSprite;
            }

            if (m_SliderFillImage != null)
            {
                m_SliderFillImage.color = m_FillNormalColor;
            }

            if (m_CookButton != null)
            {
                // 旋转角度保持开火时的状态不变，仅恢复可点击，供玩家点击执行关火。
                SetButtonGrayedOut(false);
            }
        }

        private void UpdateFinished()
        {
            m_FinishedElapsed += Time.deltaTime;

            if (m_FinishedElapsed >= Constant.GameConstant.COOK_FINISH_HANDLE_TIME)
            {
                // 超过 2 秒还未关火/取出，切换为警示样式：handle 换图 + 进度条变红。
                if (m_SliderHandleImage != null && m_HandleFinishedSprite != null)
                {
                    m_SliderHandleImage.sprite = m_HandleFinishedSprite;
                }

                if (m_SliderFillImage != null)
                {
                    m_SliderFillImage.color = m_FillWarningColor;
                }
            }

            if (m_FinishedElapsed >= Constant.GameConstant.COOK_BUFFER_TIME)
            {
                BecomeBurnt();
            }
        }

        private void PowerOff()
        {
            m_State = PotStationState.ReadyToServe;

            if (m_StoveAnimator != null)
            {
                m_StoveAnimator.SetBool(m_IsCookingParam, false);
            }

            if (m_IsOven && m_PotRenderer != null && m_OvenDefaultSprite != null)
            {
                m_PotRenderer.sprite = m_OvenDefaultSprite;
            }

            if (m_CookButton != null)
            {
                if (!m_IsOven)
                {
                    m_CookButton.transform.localRotation = m_CookButtonInitialRotation;
                }

                SetButtonGrayedOut(true);
            }

            if (m_ProgressSlider != null)
            {
                m_ProgressSlider.gameObject.SetActive(false);
            }
        }

        private void BecomeBurnt()
        {
            m_State = PotStationState.Burnt;

            if (m_StoveAnimator != null)
            {
                m_StoveAnimator.SetBool(m_IsCookingParam, false);
            }

            if (m_IsOven && m_PotRenderer != null && m_OvenDefaultSprite != null)
            {
                m_PotRenderer.sprite = m_OvenDefaultSprite;
            }

            if (m_CookButton != null)
            {
                // 糊锅：按钮永久置灰，旋转角度保持开火时的状态，直到炉灶被重置。
                SetButtonGrayedOut(true);
            }

            if (m_ProgressSlider != null)
            {
                m_ProgressSlider.gameObject.SetActive(false);
            }

            if (m_FoodRenderer != null && m_BurntFoodSprite != null)
            {
                m_FoodRenderer.sprite = m_BurntFoodSprite;
            }

            HideCover();
        }

        /// <summary>
        /// 设置按钮置灰状态，保持透明度不变（只改变颜色，不改变alpha）
        /// </summary>
        private void SetButtonGrayedOut(bool grayed)
        {
            if (m_CookButton == null)
            {
                return;
            }

            m_CookButton.interactable = !grayed;
            
            // 手动调整颜色但保持透明度
            Image buttonImage = m_CookButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color targetColor;
                if (grayed)
                {
                    // 置灰：降低色彩饱和度
                    targetColor = Color.Lerp(m_CookButtonInitialColor, Color.gray, 0.5f);
                }
                else
                {
                    // 恢复初始颜色
                    targetColor = m_CookButtonInitialColor;
                }
                targetColor.a = m_CookButtonInitialAlpha;
                buttonImage.color = targetColor;
            }
        }

        private void BeginDragFoodOnly()
        {
            if (KitchenDragController.Instance == null || m_FoodRenderer == null)
            {
                return;
            }

            Sprite icon = m_FoodRenderer.sprite;
            // 拖拽期间隐藏原位的糊锅食物，避免和跟随鼠标的拾取物同时可见（“两个鬼影”）；未命中垃圾桶则恢复显示。
            m_FoodRenderer.gameObject.SetActive(false);

            KitchenDragPayload payload = new KitchenDragPayload(KitchenDragItemKind.Food, m_MatchedDishId, icon)
            {
                OnAccepted = ResetToEmpty,
                OnReturnToOrigin = () =>
                {
                    if (m_FoodRenderer != null)
                    {
                        m_FoodRenderer.gameObject.SetActive(true);
                    }
                },
            };
            KitchenDragController.Instance.BeginDrag(payload, icon, transform.position);
        }

        /// <summary>
        /// 拖拽整锅到垃圾桶：仅在非烹饪阶段（Idle状态）允许，用于重置已放入食材的锅。
        /// 普通锅和烤箱都可以拖拽。
        /// </summary>
        private void BeginDragPot()
        {
            if (KitchenDragController.Instance == null || m_PotRenderer == null)
            {
                return;
            }

            // 使用配置的拖拽图片，如果没配置则用锅的原始图片
            Sprite icon = m_DragSprite != null ? m_DragSprite : m_PotRenderer.sprite;
            if (icon == null)
            {
                return;
            }

            // 拖拽期间隐藏原位的锅，避免视觉混乱
            m_PotRenderer.gameObject.SetActive(false);

            KitchenDragPayload payload = new KitchenDragPayload(KitchenDragItemKind.Pot, m_PotType, icon)
            {
                OnAccepted = ResetToEmpty,
                OnReturnToOrigin = () =>
                {
                    if (m_PotRenderer != null && m_State == PotStationState.Idle)
                    {
                        m_PotRenderer.gameObject.SetActive(true);
                    }
                },
            };
            KitchenDragController.Instance.BeginDrag(payload, icon, transform.position);
        }

        /// <summary>
        /// 糊锅食物被拖去垃圾桶、或空锅（Idle 且未投料）被重新点击选锅后调用：重置为空位
        /// （烤箱没有"空位"概念，重置后直接回到 Idle 继续持有 Oven 这口锅）。
        /// </summary>
        private void ResetToEmpty()
        {
            ClearAllBubbleSlots();
            m_PlacedItemIds.Clear();
            m_CandidateDishIds.Clear();
            m_PendingCandidates = null;
            m_MatchedDishId = -1;
            m_CookElapsed = 0f;
            m_FinishedElapsed = 0f;
            m_PrepareElapsed = 0f;

            // 恢复原始的锅图片（还原移动态图片）
            if (m_OriginalPotSprite != null && m_PotRenderer != null)
            {
                m_PotRenderer.sprite = m_OriginalPotSprite;
            }

            if (m_StoveAnimator != null)
            {
                m_StoveAnimator.SetBool(m_IsCookingParam, false);
            }

            if (m_CookButton != null)
            {
                if (!m_IsOven)
                {
                    m_CookButton.transform.localRotation = m_CookButtonInitialRotation;
                }

                m_CookButton.interactable = false;
            }

            if (m_ProgressSlider != null)
            {
                m_ProgressSlider.value = 0f;
                m_ProgressSlider.gameObject.SetActive(false);
            }

            if (m_FoodRenderer != null)
            {
                m_FoodRenderer.sprite = null;
                m_FoodRenderer.gameObject.SetActive(false);
            }

            if (m_SliderFillImage != null)
            {
                m_SliderFillImage.color = m_FillNormalColor;
            }

            HideCover();

            if (m_IsOven)
            {
                if (m_PotRenderer != null && m_OvenDefaultSprite != null)
                {
                    m_PotRenderer.sprite = m_OvenDefaultSprite;
                }

                m_State = PotStationState.Idle;
                return;
            }

            m_PotType = 0;
            m_State = PotStationState.Empty;
            if (m_PotRenderer != null)
            {
                m_PotRenderer.gameObject.SetActive(false);
            }

            if (m_PotAnimator != null)
            {
                m_PotAnimator.enabled = false;
            }
        }

        /// <summary>
        /// 进入下一天时调用：重置锅具状态为空。
        /// </summary>
        public void ResetForNewDay()
        {
            ResetToEmpty();
        }

        /// <summary>
        /// 供 <see cref="KitchenManager"/> 装盘调用：仅在已关火未糊时成功，成功后立即重置为空位。
        /// </summary>
        public bool TryPlate(out int dishId, out Sprite sprite)
        {
            if (m_State != PotStationState.ReadyToServe)
            {
                dishId = 0;
                sprite = null;
                return false;
            }

            dishId = m_MatchedDishId;
            sprite = m_FoodRenderer != null ? m_FoodRenderer.sprite : null;
            ResetToEmpty();
            return true;
        }
    }
}

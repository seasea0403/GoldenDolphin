using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuController : MonoBehaviour
{
    [Header("【外部传入】菜谱总面板根节点（整套菜单容器）")]
    public RectTransform uiRoot;

    [Header("两页内容容器")]
    public GameObject recipeList1;
    public GameObject recipeList2;

    [Header("翻页按钮")]
    public Button prevButton;
    public Button nextButton;

    [Header("菜品详情弹窗")]
    public GameObject recipePanel;
    public Image recipeImage;
    public Button backButton;

    [Header("菜谱图片资源库")]
    public List<Sprite> recipeSprites = new List<Sprite>();

    [Header("所有菜品点击按钮")]
    public List<Button> dishButtons = new List<Button>();

    private int currentPageIndex = 0;
    private int totalPages = 2;
    private CanvasGroup m_MenuCanvasGroup;

    void Awake()
    {
        if (uiRoot != null)
        {
            m_MenuCanvasGroup = uiRoot.GetComponent<CanvasGroup>();
            if (m_MenuCanvasGroup == null)
                m_MenuCanvasGroup = uiRoot.gameObject.AddComponent<CanvasGroup>();
            // 默认关闭射线拦截
            m_MenuCanvasGroup.blocksRaycasts = false;
        }
    }

    void Start()
    {
        prevButton.onClick.AddListener(PrevPage);
        nextButton.onClick.AddListener(NextPage);
        backButton.onClick.AddListener(ReturnToMenu);

        // 为翻页按钮绑定音效
        LingBoCanteen.UIButtonSoundHelper.BindButtonSound(prevButton);
        LingBoCanteen.UIButtonSoundHelper.BindButtonSound(nextButton);
        LingBoCanteen.UIButtonSoundHelper.BindButtonSound(backButton);

        for (int i = 0; i < dishButtons.Count; i++)
        {
            int index = i;
            dishButtons[i].onClick.AddListener(() => ShowRecipe(index));
            // 为菜品按钮绑定音效
            LingBoCanteen.UIButtonSoundHelper.BindButtonSound(dishButtons[i]);
        }

        // 初始化详情弹窗关闭
        recipePanel.SetActive(false);
        backButton.gameObject.SetActive(false);

        // 默认加载第0页
        ShowPage(0);
    }

    /// <summary>
    /// 展示对应菜品大图详情
    /// </summary>
    public void ShowRecipe(int index)
    {
        if (index < 0 || index >= recipeSprites.Count)
            return;

        currentPageIndex = index < 12 ? 0 : 1;
        recipeList1.SetActive(false);
        recipeList2.SetActive(false);

        recipePanel.SetActive(true);
        backButton.gameObject.SetActive(true);
        recipeImage.sprite = recipeSprites[index];
    }

    /// <summary>
    /// 关闭详情弹窗，回到当前页码列表
    /// </summary>
    public void ReturnToMenu()
    {
        recipePanel.SetActive(false);
        backButton.gameObject.SetActive(false);
        ShowPage(currentPageIndex);
    }

    /// <summary>
    /// 切换页面，控制翻页按钮可交互状态
    /// </summary>
    void ShowPage(int page)
    {
        currentPageIndex = page;
        recipeList1.SetActive(page == 0);
        recipeList2.SetActive(page == 1);
        prevButton.interactable = page > 0;
        nextButton.interactable = page < totalPages - 1;
    }

    public void PrevPage()
    {
        if (currentPageIndex > 0)
            ShowPage(currentPageIndex - 1);
    }

    public void NextPage()
    {
        if (currentPageIndex < totalPages - 1)
            ShowPage(currentPageIndex + 1);
    }

    /// <summary>
    /// 打开菜谱面板，暂停游戏 + 阻断底层点击
    /// </summary>
    public void OpenMenu()
    {
        if (uiRoot == null) return;

        uiRoot.gameObject.SetActive(true);
        // 阻断下层点击
        m_MenuCanvasGroup.blocksRaycasts = true;
        m_MenuCanvasGroup.interactable = true;
        // 全局暂停游戏计时
        Time.timeScale = 0;

        if (recipePanel != null) recipePanel.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        ShowPage(0);
    }

    /// <summary>
    /// 关闭菜谱面板，恢复游戏、放行点击
    /// </summary>
    public void CloseMenu()
    {
        if (uiRoot == null) return;
        uiRoot.gameObject.SetActive(false);
        // 放行底层点击
        m_MenuCanvasGroup.blocksRaycasts = false;
        // 恢复游戏运行
        Time.timeScale = 1;
    }

    /// <summary>
    /// 切换菜谱面板显示/隐藏状态
    /// </summary>
    public void ToggleMenu()
    {
        if (uiRoot == null) return;

        bool isOpen = !uiRoot.gameObject.activeSelf;
        uiRoot.gameObject.SetActive(isOpen);

        if (isOpen)
        {
            // 打开：暂停+拦截点击
            m_MenuCanvasGroup.blocksRaycasts = true;
            m_MenuCanvasGroup.interactable = true;
            Time.timeScale = 0;

            if (recipePanel != null) recipePanel.SetActive(false);
            if (backButton != null) backButton.gameObject.SetActive(false);
            ShowPage(0);
        }
        else
        {
            // 关闭：恢复+放行点击
            m_MenuCanvasGroup.blocksRaycasts = false;
            Time.timeScale = 1;
        }
    }
}
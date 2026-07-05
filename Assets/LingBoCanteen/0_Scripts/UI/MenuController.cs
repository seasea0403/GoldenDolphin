using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuController : MonoBehaviour
{
    [Header("²Ëµ¥Ò³Ãæ")]
    public GameObject recipeList1;
    public GameObject recipeList2;

    [Header("·­Ò³°´Å¥")]
    public Button prevButton;
    public Button nextButton;

    [Header("Ê³Æ×Ãæ°å")]
    public GameObject recipePanel;      
    public Image recipeImage;         
    public Button backButton;          

    [Header("²ËÆ×")]
    public List<Sprite> recipeSprites = new List<Sprite>();

    [Header("²ËÆ×°´Å¥")]
    public List<Button> dishButtons = new List<Button>();

    private int currentPageIndex = 0;
    private int totalPages = 2;

    void Start()
    {
        prevButton.onClick.AddListener(PrevPage);
        nextButton.onClick.AddListener(NextPage);

        backButton.onClick.AddListener(ReturnToMenu);

        for (int i = 0; i < dishButtons.Count; i++)
        {
            int index = i; 
            dishButtons[i].onClick.AddListener(() => ShowRecipe(index));
        }

        recipePanel.SetActive(false);
        backButton.gameObject.SetActive(false);

        ShowPage(0);
    }

    public void ShowRecipe(int index)
    {
        if (index < 0 || index >= recipeSprites.Count)
        {
            return;
        }

        currentPageIndex = (index < 12) ? 0 : 1;

        recipeList1.SetActive(false);
        recipeList2.SetActive(false);

        recipePanel.SetActive(true);
        backButton.gameObject.SetActive(true);

        recipeImage.sprite = recipeSprites[index];
    }

    public void ReturnToMenu()
    {
        recipePanel.SetActive(false);
        backButton.gameObject.SetActive(false);

        ShowPage(currentPageIndex);
    }

    void ShowPage(int page)
    {
        currentPageIndex = page;
        recipeList1.SetActive(page == 0);
        recipeList2.SetActive(page == 1);
        prevButton.interactable = (page > 0);
        nextButton.interactable = (page < totalPages - 1);
    }

    public void PrevPage() { if (currentPageIndex > 0) ShowPage(currentPageIndex - 1); }
    public void NextPage() { if (currentPageIndex < totalPages - 1) ShowPage(currentPageIndex + 1); }
}
using UnityEngine;
using UnityEngine.UI;

public class GuideManager : MonoBehaviour
{
    [Header("全屏遮罩")]
    public GameObject guideBlocker;

    [Header("显化类目标（直接显示）")]
    public GameObject patienceBar;   // 耐心值条
    public GameObject placemat;      // 餐垫
    public GameObject bellBtn;       // 送餐铃

    [Header("引导点击类目标（高亮）")]
    public GameObject recipeBtn;           // 配方按钮
    public GameObject recipeCloseBtn;      // 配方面板关闭按钮
    public GameObject prepBtn;            // 备菜区按钮
    public GameObject cookBtn;            // 烹调区按钮
    public GameObject potSelectBtn;       // 选锅按钮

    private string _currentStep = "";
    private GameObject _highlightedObj;

    private void OnEnable()
    {
        TextManager.OnGuideCommand += HandleCommand;
    }

    private void OnDisable()
    {
        TextManager.OnGuideCommand -= HandleCommand;
    }

    void HandleCommand(string cmd)
    {
        Debug.Log("[Guide] 收到指令：" + cmd);
        string cleanCmd = cmd.Replace("【", "").Replace("】", "").Trim();

        if (!string.IsNullOrEmpty(_currentStep)) return;

        // ===== 显化类：显示 → 等1.5秒 → 隐藏 → 继续 =====
        if (cleanCmd.Contains("显化耐心值"))
        {
            _currentStep = "显化耐心值";
            ShowTemporary(patienceBar, 1.5f);
        }
        else if (cleanCmd.Contains("显化餐垫"))
        {
            _currentStep = "显化餐垫";
            ShowTemporary(placemat, 1.5f);
        }
        else if (cleanCmd.Contains("显化送餐铃"))
        {
            _currentStep = "显化送餐铃";
            ShowTemporary(bellBtn, 1.5f);
        }

        // ===== 引导点击类：高亮 → 等玩家点 =====
        else if (cleanCmd.Contains("引导点击配方Btn"))
        {
            _currentStep = "引导点击配方Btn";
            Highlight(recipeBtn);
        }
        else if (cleanCmd.Contains("引导点击关闭配方面板"))
        {
            _currentStep = "引导点击关闭配方面板";
            Highlight(recipeCloseBtn);
        }
        else if (cleanCmd.Contains("引导点击备菜区"))
        {
            _currentStep = "引导点击备菜区";
            Highlight(prepBtn);
        }
        else if (cleanCmd.Contains("引导点击烹调区"))
        {
            _currentStep = "引导点击烹调区";
            Highlight(cookBtn);
        }
        else if (cleanCmd.Contains("引导点击选锅按钮"))
        {
            _currentStep = "引导点击选锅按钮";
            Highlight(potSelectBtn);
        }

        // ===== 不认识的指令直接跳过 =====
        else
        {
            Continue();
        }
    }

    // ===== 显化：显示物体 → 延时隐藏 → 继续 =====
    void ShowTemporary(GameObject target, float delay)
    {
        if (target == null) { Continue(); return; }
        target.SetActive(true);
        StartCoroutine(HideAndContinue(target, delay));
    }

    System.Collections.IEnumerator HideAndContinue(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        target.SetActive(false);
        _currentStep = "";
        Continue();
    }

    // ===== 高亮：遮罩 + 目标浮到最前面 =====
    void Highlight(GameObject target)
    {
        if (target == null) { Debug.LogError("高亮目标为空！"); Continue(); return; }

        // 显示遮罩（挡住其他按钮）
        if (guideBlocker != null) guideBlocker.SetActive(true);

        // 给目标加 Canvas，强制渲染在最前面
        _highlightedObj = target;
        Canvas cv = target.GetComponent<Canvas>();
        if (cv == null) cv = target.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = 999;

        // 加黄色描边，让玩家一眼看出要点它
        Outline outline = target.GetComponent<Outline>();
        if (outline == null) outline = target.AddComponent<Outline>();
        outline.effectColor = Color.yellow;
        outline.effectDistance = new Vector2(6, -6);
    }

    // ===== 清除高亮 =====
    void ClearHighlight()
    {
        if (_highlightedObj != null)
        {
            Canvas cv = _highlightedObj.GetComponent<Canvas>();
            if (cv != null) Destroy(cv);
            Outline outline = _highlightedObj.GetComponent<Outline>();
            if (outline != null) Destroy(outline);
            _highlightedObj = null;
        }
        if (guideBlocker != null) guideBlocker.SetActive(false);
    }

    // ===== 外部调用：玩家点完按钮后调这个 =====
    public void CompleteStep()
    {
        if (string.IsNullOrEmpty(_currentStep)) return;
        Debug.Log("[Guide] 步骤完成：" + _currentStep);
        _currentStep = "";
        ClearHighlight();
        Continue();
    }

    void Continue()
    {
        Debug.Log("[Guide] 引导完成，恢复剧情");
        if (TextManager.Instance != null)
            TextManager.Instance.ResumeFromGuide();
    }
}
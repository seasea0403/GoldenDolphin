using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    public static TextManager Instance;
    public static event System.Action<string> OnGuideCommand; // 引导指令事件

    [Header("View 层引用")]
    public ViewDialogue view;

    [Header("打字机速度")]
    public float typeSpeed = 0.03f;

    private DialogueAsset currentAsset;
    private int currentIndex;
    private bool isTyping;
    private bool isWaitingForGuide; // 引导暂停锁
    private Coroutine typingCo;

    private void Awake() { Instance = this; }

    #region 对外接口
    public void PlayDay(int day) => LoadAndPlay("Day" + day);
    public void PlayEnding(int index) => LoadAndPlay("Ending" + index);
    #endregion

    void LoadAndPlay(string fileName)
    {
        DialogueAsset asset = Resources.Load<DialogueAsset>("DialogueAssets/" + fileName);
        if (asset == null) { Debug.LogError("找不到剧本：" + fileName); return; }

        currentAsset = asset;
        currentIndex = 0;
        isWaitingForGuide = false;
        view.gameObject.SetActive(true);
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentIndex >= currentAsset.lines.Count)
        {
            view.gameObject.SetActive(false);
            currentAsset = null;
            Debug.Log("剧情播放完毕");
            return;
        }

        DialogueLine line = currentAsset.lines[currentIndex];

        view.gameObject.SetActive(true);

        view.ShowSpeaker(line.speakerName);
        view.ShowCharImage(line.charImage);

        // 解析【】标记
        string rawText = line.speakingContent;
        string displayText = Regex.Replace(rawText, @"【.*?】", "").Trim(); 
        bool hasGuideCmd = Regex.IsMatch(rawText, @"【.*?】");

        view.SetFullText(displayText);

        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(TypeText(displayText, hasGuideCmd, rawText));
    }

    IEnumerator TypeText(string displayText, bool hasGuideCmd, string rawText)
    {
        isTyping = true;
        isWaitingForGuide = false;
        int totalChars = displayText.Length;
        int currentChar = 0;

        while (currentChar <= totalChars)
        {
            view.SetVisibleCharCount(currentChar);
            currentChar++;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;

        // 打字结束，如果有引导指令，触发并锁住
        if (hasGuideCmd)
        {
            isWaitingForGuide = true;
            view.gameObject.SetActive(false);
            // 提取所有【指令
            MatchCollection matches = Regex.Matches(rawText, @"【(.*?)】");
            foreach (Match m in matches)
            {
                string cmd = m.Groups[1].Value;
                Debug.Log($"触发引导指令：{cmd}");
                OnGuideCommand?.Invoke(cmd);
            }
        }
    }

    /// <summary>
    /// 点击屏幕
    /// </summary>
    public void OnClickScreen()
    {
        if (currentAsset == null) return;

        // 引导期间禁止点击跳过
        if (isWaitingForGuide)
        {
            Debug.Log("引导进行中，等待玩家操作...");
            return;
        }

        if (isTyping)
        {
            if (typingCo != null) StopCoroutine(typingCo);
            view.ShowAllText();
            isTyping = false;
        }
        else
        {
            currentIndex++;
            ShowCurrentLine();
        }
    }

    /// <summary>
    /// ★ 引导完成后由外部调用，继续下一句
    /// </summary>
    public void ResumeFromGuide()
    {
        if (!isWaitingForGuide) return;
        Debug.Log("引导完成，继续剧情");
        isWaitingForGuide = false;
        currentIndex++;
        ShowCurrentLine();
    }
}
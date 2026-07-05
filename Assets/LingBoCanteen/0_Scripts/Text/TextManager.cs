using System.Collections;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    public static TextManager Instance;

    [Header("View 层引用")]
    public ViewDialogue view;

    [Header("打字机速度（秒/字）")]
    public float typeSpeed = 0.03f;

    private DialogueAsset currentAsset;
    private int currentIndex;
    private bool isTyping;
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

        view.ShowSpeaker(line.speakerName);
        view.ShowCharImage(line.charImage);
        view.SetFullText(line.speakingContent);

        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(TypeText(line.speakingContent));
    }

    IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        int totalChars = fullText.Length;
        int currentChar = 0;

        while (currentChar <= totalChars)
        {
            view.SetVisibleCharCount(currentChar);
            currentChar++;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    public void OnClickScreen()
    {
        if (currentAsset == null) return;

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
}
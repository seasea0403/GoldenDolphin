using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewDialogue : MonoBehaviour
{
    public TextMeshProUGUI txtSpeaker;
    public TextMeshProUGUI txtContent;
    public Image imgChar;          

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowSpeaker(string name)
    {
        txtSpeaker.text = name;
    }

    public void SetFullText(string text)
    {
        txtContent.text = text;
        txtContent.maxVisibleCharacters = 0;
    }

    public void SetVisibleCharCount(int count)
    {
        txtContent.maxVisibleCharacters = count;
    }

    public void ShowAllText()
    {
        txtContent.maxVisibleCharacters = txtContent.text.Length;
    }

    public void ShowCharImage(Sprite sp)
    {
        imgChar.sprite = sp;
        imgChar.enabled = (sp != null);
    }

    // µã»÷ÆÁÄ»¼ÌÐø
    private void Update()
    {
        if (gameObject.activeSelf && Input.GetMouseButtonDown(0))
        {
            TextManager.Instance.OnClickScreen();
        }
    }
}
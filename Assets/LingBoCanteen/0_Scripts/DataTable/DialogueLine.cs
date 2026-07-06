using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public int id;
    public string speakerName;
    [TextArea(2, 5)]
    public string speakingContent;
    public Sprite charImage;
}
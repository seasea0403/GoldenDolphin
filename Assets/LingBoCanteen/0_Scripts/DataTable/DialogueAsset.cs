using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue_New", menuName = "MyGame/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    public List<DialogueLine> lines = new List<DialogueLine>();
}
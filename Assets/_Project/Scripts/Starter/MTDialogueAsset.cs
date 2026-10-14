using UnityEngine;

[CreateAssetMenu(menuName = "Memorias Tecidas/Dialogo", fileName = "Dialogo")]
public sealed class MTDialogueAsset : ScriptableObject
{
    public string id;
    public string characterName;
    [TextArea(2, 4)] public string context;
    [TextArea(2, 5)] public string source;
    [TextArea(3, 8)] public string[] lines;
}

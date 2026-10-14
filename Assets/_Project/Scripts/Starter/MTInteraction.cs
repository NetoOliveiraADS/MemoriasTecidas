using UnityEngine;

// Adicione a um objeto com Collider2D. O collider pode ser sólido.
public sealed class MTInteraction : MonoBehaviour
{
    public enum InteractionKind { Cotton, Spinner, Loom, Guide, Delivery, Dialogue }

    [SerializeField] private InteractionKind kind;
    [SerializeField] private MTDialogueAsset dialogueAsset;
    private bool collected;

    public bool Available => !collected && isActiveAndEnabled;

    public string Prompt
    {
        get
        {
            switch (kind)
            {
                case InteractionKind.Cotton: return "E / Enter: recolher algodão";
                case InteractionKind.Spinner: return "E / Enter: preparar fio (2 algodões)";
                case InteractionKind.Loom: return "E / Enter: montar rede (3 fios + 1 kit)";
                case InteractionKind.Guide: return "E / Enter: conversar com Ana";
                case InteractionKind.Dialogue: return "E / Enter: ouvir " + (dialogueAsset != null ? dialogueAsset.characterName : "memória");
                default: return "E / Enter: entregar rede à Mostra";
            }
        }
    }

    public void Interact(MTInventory inventory, MTHud hud, MTDialogueController dialogue)
    {
        if (!Available || inventory == null) return;
        string message;
        switch (kind)
        {
            case InteractionKind.Dialogue:
                if (dialogue != null && dialogueAsset != null) dialogue.Open(dialogueAsset);
                else Debug.LogWarning("Ligue o Dialogue do Player e o Dialogue Asset deste objeto.", this);
                return;
            case InteractionKind.Cotton:
                if (inventory.TryCollectCotton(out message))
                {
                    collected = true;
                    gameObject.SetActive(false);
                }
                break;
            case InteractionKind.Spinner:
                inventory.TrySpin(out message);
                break;
            case InteractionKind.Loom:
                inventory.TryWeave(out message);
                break;
            case InteractionKind.Guide:
                inventory.TryClaimKit(out message);
                break;
            default:
                inventory.TryDeliver(out message);
                break;
        }
        if (hud != null) hud.ShowMessage(message);
        else Debug.Log(message);
    }
}

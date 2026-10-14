using TMPro;
using UnityEngine;

public sealed class MTHud : MonoBehaviour
{
    [SerializeField] private MTInventory playerInventory;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text messageText;
    private float messageUntil;
    private string previousStatus;

    private void Start()
    {
        SetPrompt("");
        ShowMessage("Caminhe com WASD ou setas. Procure Ana e pressione E.");
    }

    private void Update()
    {
        if (playerInventory != null && statusText != null)
        {
            string status = $"Algodão: {playerInventory.Cotton}  |  Fio: {playerInventory.Yarn}" +
                $"  |  Kit: {playerInventory.Kits}  |  Rede: {playerInventory.Hammocks}";
            if (playerInventory.Delivered) status += "\nMostra concluída";
            if (status != previousStatus)
            {
                statusText.text = status;
                previousStatus = status;
            }
        }
        if (messageText != null && Time.unscaledTime > messageUntil)
            messageText.text = "";
    }

    public void SetPrompt(string text)
    {
        if (promptText != null && promptText.text != text) promptText.text = text;
    }

    public void ShowMessage(string text)
    {
        if (messageText != null) messageText.text = text;
        messageUntil = Time.unscaledTime + 8f;
    }
}

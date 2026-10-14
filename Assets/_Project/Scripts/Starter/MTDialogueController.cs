using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Este componente fica no Canvas ativo, nunca no painel que ele oculta.
public sealed class MTDialogueController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text sourceText;
    [SerializeField] private TMP_Text counterText;
    private MTDialogueAsset current;
    private int index;
    private int changedFrame = -1;
    private InputAction advance;
    private InputAction cancel;

    public bool IsOpen => current != null;
    public bool BlocksGameplay => IsOpen || changedFrame == Time.frameCount;
    public event Action<string> Completed;

    private void Awake()
    {
        advance = new InputAction("Next", InputActionType.Button);
        advance.AddBinding("<Keyboard>/e");
        advance.AddBinding("<Keyboard>/enter");
        cancel = new InputAction("Close", InputActionType.Button, "<Keyboard>/escape");
        if (panel != null) panel.SetActive(false);
    }
    private void OnEnable() { advance.Enable(); cancel.Enable(); }
    private void OnDisable()
    {
        advance.Disable(); cancel.Disable();
        current = null;
        if (panel != null && panel != gameObject) panel.SetActive(false);
    }
    private void OnDestroy() { advance?.Dispose(); cancel?.Dispose(); }
    private void Update()
    {
        if (!IsOpen || changedFrame == Time.frameCount) return;
        if (cancel.WasPressedThisFrame()) Close();
        else if (advance.WasPressedThisFrame()) Next();
    }
    public void Open(MTDialogueAsset data)
    {
        if (IsOpen || data == null) return;
        if (panel == null || panel == gameObject || nameText == null || bodyText == null || sourceText == null)
        {
            Debug.LogError("Dialogue: ligue Panel, Name Text, Body Text e Source Text. Panel deve ser filho do Canvas.", this);
            return;
        }
        if (data.lines == null || data.lines.Length == 0)
        {
            Debug.LogWarning("O diálogo não tem falas.", data);
            return;
        }
        current = data; index = 0; changedFrame = Time.frameCount;
        panel.SetActive(true);
        Draw();
    }
    public void Next()
    {
        if (!IsOpen || changedFrame == Time.frameCount) return;
        changedFrame = Time.frameCount;
        index++;
        if (index >= current.lines.Length)
        {
            string completedId = current.id;
            Close();
            Completed?.Invoke(completedId);
            Debug.Log("Conversa concluída: " + completedId);
        }
        else Draw();
    }
    public void Close()
    {
        current = null; changedFrame = Time.frameCount;
        if (panel != null) panel.SetActive(false);
    }
    private void Draw()
    {
        nameText.text = current.characterName;
        bodyText.text = current.lines[index];
        sourceText.text = current.context + "\nFonte: " + current.source;
        if (counterText != null)
            counterText.text = (index + 1) + "/" + current.lines.Length + "  E / Enter: avançar  Esc: fechar";
    }
}

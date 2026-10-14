using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(MTInventory))]
public sealed class MTPlayer : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 3.5f;
    [SerializeField, Min(0.1f)] private float interactionRadius = 1.2f;
    [SerializeField] private LayerMask interactableMask = ~0;
    [SerializeField] private MTHud hud;
    [SerializeField] private MTDialogueController dialogue;

    private Rigidbody2D body;
    private MTInventory inventory;
    private InputAction move;
    private InputAction interact;
    private Vector2 direction;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        inventory = GetComponent<MTInventory>();
        move = new InputAction("Move", InputActionType.Value);
        AddKeys("w", "s", "a", "d");
        AddKeys("upArrow", "downArrow", "leftArrow", "rightArrow");
        interact = new InputAction("Interact", InputActionType.Button);
        interact.AddBinding("<Keyboard>/e");
        interact.AddBinding("<Keyboard>/enter");
    }

    private void AddKeys(string up, string down, string left, string right)
    {
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/" + up)
            .With("Down", "<Keyboard>/" + down)
            .With("Left", "<Keyboard>/" + left)
            .With("Right", "<Keyboard>/" + right);
    }

    private void OnEnable()
    {
        move.Enable();
        interact.Enable();
    }

    private void OnDisable()
    {
        move.Disable();
        interact.Disable();
        direction = Vector2.zero;
        if (body != null) body.linearVelocity = Vector2.zero;
        if (hud != null) hud.SetPrompt("");
    }

    private void OnDestroy()
    {
        move?.Dispose();
        interact?.Dispose();
    }

    private void Update()
    {
        if (dialogue != null && dialogue.BlocksGameplay)
        {
            direction = Vector2.zero;
            if (hud != null) hud.SetPrompt("");
            return;
        }
        direction = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        MTInteraction target = FindClosestTarget();
        if (hud != null) hud.SetPrompt(target != null ? target.Prompt : "");
        if (target != null && interact.WasPressedThisFrame())
        {
            target.Interact(inventory, hud, dialogue);
            if (dialogue != null && dialogue.BlocksGameplay) direction = Vector2.zero;
            if (hud != null && !target.Available) hud.SetPrompt("");
        }
    }

    private void FixedUpdate()
    {
        // linearVelocity já usa unidades por segundo: não multiplicar por deltaTime.
        body.linearVelocity = dialogue != null && dialogue.BlocksGameplay ? Vector2.zero : direction * speed;
    }

    private MTInteraction FindClosestTarget()
    {
        MTInteraction best = null;
        float bestDistance = float.PositiveInfinity;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            body.position, interactionRadius, interactableMask);
        foreach (Collider2D hit in hits)
        {
            MTInteraction candidate = hit.GetComponentInParent<MTInteraction>();
            if (candidate == null || !candidate.Available) continue;
            Vector2 closestPoint = hit.ClosestPoint(body.position);
            float distance = (closestPoint - body.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}

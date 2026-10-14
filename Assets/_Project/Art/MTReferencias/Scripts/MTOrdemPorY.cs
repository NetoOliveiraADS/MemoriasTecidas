using UnityEngine;

// Add to the GameObject that owns the SpriteRenderer. Ground does not use this.
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MTOrdemPorY : MonoBehaviour
{
    [Tooltip("Deslocamento vertical do ponto de apoio, em unidades do mundo. Use -0.3 para um Player de altura 0.6 com pivo central.")]
    [SerializeField] private float deslocamentoY;
    [SerializeField] private int ordemBase;
    private SpriteRenderer alvo;

    private void OnEnable() { Atualizar(); }
    private void OnValidate() { Atualizar(); }
    private void LateUpdate() { Atualizar(); }

    private void Atualizar()
    {
        if (alvo == null) alvo = GetComponent<SpriteRenderer>();
        if (alvo == null) return;
        float apoioY = transform.position.y + deslocamentoY;
        int ordem = Mathf.RoundToInt(-apoioY * 100f) + ordemBase;
        alvo.sortingOrder = Mathf.Clamp(ordem, -28000, 28000);
    }
}

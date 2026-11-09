using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    /// <summary>Ponto de pesca independente do roteiro e das falas.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MTMPonto))]
    public sealed class MTMPontoPesca : MonoBehaviour
    {
        [Tooltip("Identificador único do ponto, para futuras estatísticas.")]
        public string idPonto = "rio_01";
        public SpriteRenderer brilho;
        public Color corBase = new Color(.25f, .67f, .82f, 1f);

        private void Update()
        {
            if (brilho == null) return;
            float brilhoAtual = .82f + .18f * Mathf.Sin(Time.time * 2.2f);
            brilho.color = new Color(
                corBase.r * brilhoAtual,
                corBase.g * brilhoAtual,
                corBase.b * brilhoAtual,
                corBase.a);
        }
    }
}
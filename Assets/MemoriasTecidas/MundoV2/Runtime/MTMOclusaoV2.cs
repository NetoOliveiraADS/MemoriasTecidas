using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Mantém o explorador legível ao passar atrás de volumes altos.
    // Usa o retângulo do sprite no mundo; não é detecção por pixel opaco.
    [DefaultExecutionOrder(100)]
    public sealed class MTMOclusaoV2 : MonoBehaviour
    {
        public Transform alvo;
        public SpriteRenderer desenho;
        [Range(.15f, .7f)] public float opacidadeOculta = .32f;
        [Min(.1f)] public float alturaDoJogador = 1.35f;
        [Min(.1f)] public float velocidade = 4f;
        private Color corOriginal;
        private bool inicializado;

        private void Awake()
        {
            if (desenho == null) return;
            corOriginal = desenho.color;
            inicializado = true;
        }

        private void LateUpdate()
        {
            if (!inicializado || desenho == null) return;
            bool encobre = false;
            if (alvo != null && alvo.gameObject.activeInHierarchy)
            {
                Vector3 p = alvo.position;
                Bounds b = desenho.bounds;
                bool atras = p.y > transform.position.y + .08f;
                bool cruzaX = p.x + .23f > b.min.x && p.x - .23f < b.max.x;
                bool cruzaY = p.y < b.max.y && p.y + alturaDoJogador > b.min.y;
                encobre = atras && cruzaX && cruzaY;
            }
            float destino = corOriginal.a * (encobre ? opacidadeOculta : 1f);
            Color cor = desenho.color;
            cor.a = Mathf.MoveTowards(cor.a, destino, velocidade * Time.deltaTime);
            desenho.color = cor;
        }

        private void OnDisable()
        {
            if (inicializado && desenho != null) desenho.color = corOriginal;
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [DisallowMultipleComponent]
    public sealed class MTMCaminhanteV22 : MonoBehaviour
    {
        [Tooltip("Posições locais em relação ao pai do personagem, não deslocamentos.")]
        public Vector2[] pontos;
        public MTMVidaV22 vida;
        public bool animal;
        [Min(.08f)] public float velocidade = .55f;
        [Min(.1f)] public float raio = .22f;
        [Min(.2f)] public float pausa = 2.5f;
        public LayerMask obstaculos = -1;
        public Vector2 Velocidade { get; private set; }
        private int destino;
        private float espera;
        private readonly RaycastHit2D[] acertos = new RaycastHit2D[24];
        private Rigidbody2D corpo;

        private void Awake()
        {
            corpo = GetComponent<Rigidbody2D>();
            espera = Mathf.Abs(transform.position.x * .7f) % pausa;
        }
        private void OnDisable() { Velocidade = Vector2.zero; if(corpo!=null)corpo.linearVelocity=Vector2.zero; }
        private void FixedUpdate()
        {
            Velocidade = Vector2.zero;
            if(corpo!=null)corpo.linearVelocity=Vector2.zero;
            if (vida == null || !vida.isActiveAndEnabled || pontos == null || pontos.Length < 2) return;
            if (animal ? !vida.animaisAndando : !vida.moradoresAndando) return;
            if (vida.jogo != null && vida.jogo.V22Bloqueado) return;
            if (Vector2.Distance(transform.position, vida.transform.position) < (animal ? 1.1f : 2.5f)) return;
            if (espera > 0f) { espera -= Time.fixedDeltaTime; return; }
            Vector2 alvo = transform.parent != null ? (Vector2)transform.parent.TransformPoint(pontos[destino]) : pontos[destino];
            Vector2 pos = corpo != null ? corpo.position : (Vector2)transform.position;
            Vector2 delta = alvo - pos;
            float rapidez = animal ? velocidade : vida.velocidadeMoradores;
            float passo = Mathf.Min(delta.magnitude, rapidez * Time.fixedDeltaTime);
            if (delta.magnitude < .06f) { Proximo(); return; }
            var filtro = new ContactFilter2D();
            filtro.SetLayerMask(obstaculos); filtro.useTriggers = false;
            int n = Physics2D.CircleCast(pos + Vector2.up * .1f, raio, delta.normalized, filtro, acertos, passo + .07f);
            for (int i = 0; i < n; i++)
            {
                Transform outro = acertos[i].transform;
                if (outro == null || outro == transform || outro.IsChildOf(transform)) continue;
                // Não atravessa água, edifícios ou outros personagens.
                Proximo(); return;
            }
            Velocidade = delta.normalized * rapidez;
            Vector2 chegada = pos + delta.normalized * passo;
            if (corpo != null) corpo.MovePosition(chegada);
            else transform.position = new Vector3(chegada.x, chegada.y, transform.position.z);
        }
        private void Proximo() { destino = (destino + 1) % pontos.Length; espera = pausa; }
        private void OnDrawGizmosSelected()
        {
            if (pontos == null || pontos.Length < 2) return;
            Gizmos.color = animal ? new Color(1f,.7f,.3f) : Color.cyan;
            for (int i = 0; i < pontos.Length; i++)
            {
                Vector3 a = transform.parent != null ? transform.parent.TransformPoint(pontos[i]) : (Vector3)pontos[i];
                Vector3 b = transform.parent != null ? transform.parent.TransformPoint(pontos[(i+1)%pontos.Length]) : (Vector3)pontos[(i+1)%pontos.Length];
                Gizmos.DrawWireSphere(a, raio); Gizmos.DrawLine(a, b);
            }
        }
    }
}

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
        [Min(.08f)]
        public float velocidade = .55f;
        [Min(.1f)]
        public float raio = .22f;
        [Min(.2f)]
        public float pausa = 2.5f;
        public LayerMask obstaculos = -1;
        public Vector2 Velocidade { get; private set; }

        private int destino;
        private float espera;
        private readonly RaycastHit2D[] acertos = new RaycastHit2D[24];
        private Rigidbody2D corpo;
        private float rapidezAtual, fasePausa;
        private void Awake()
        {
            corpo = GetComponent<Rigidbody2D>();
            if (corpo != null && corpo.bodyType == RigidbodyType2D.Kinematic && corpo.interpolation == RigidbodyInterpolation2D.None)
                corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            fasePausa = Mathf.Repeat(Mathf.Abs(transform.position.x * .31f + transform.position.y * .73f), 1f);
            espera = Mathf.Abs(transform.position.x * .7f) % pausa;
        }

        private void OnDisable()
        {
            rapidezAtual = 0f;
            Velocidade = Vector2.zero;
            if (corpo != null)
                corpo.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (corpo != null)
                Mover(Time.fixedDeltaTime);
        }

        private void Update()
        {
            if (corpo == null)
                Mover(Time.deltaTime);
        }

        private void Mover(float dt)
        {
            Velocidade = Vector2.zero;
            if (corpo != null)
                corpo.linearVelocity = Vector2.zero;
            if (dt <= 0f || dt > .25f)
            {
                rapidezAtual = 0f;
                return;
            }

            if (vida == null || !vida.isActiveAndEnabled || pontos == null || pontos.Length < 2)
            {
                rapidezAtual = 0f;
                return;
            }

            if (animal ? !vida.animaisAndando : !vida.moradoresAndando)
            {
                rapidezAtual = 0f;
                return;
            }

            if (vida.jogo != null && vida.jogo.V22Bloqueado)
            {
                rapidezAtual = 0f;
                return;
            }

            if ((transform.position - vida.transform.position).sqrMagnitude < (animal ? 1.21f : 6.25f))
            {
                rapidezAtual = 0f;
                return;
            }

            if (espera > 0f)
            {
                espera -= dt;
                rapidezAtual = 0f;
                return;
            }

            Vector2 alvo = transform.parent != null ? (Vector2)transform.parent.TransformPoint(pontos[destino]) : pontos[destino];
            Vector2 pos = corpo != null ? corpo.position : (Vector2)transform.position;
            Vector2 delta = alvo - pos;
            float rapidez = animal ? velocidade : vida.velocidadeMoradores;
            float distancia = delta.magnitude;
            if (distancia < .06f)
            {
                Proximo();
                return;
            }

            float desejada = Mathf.Min(rapidez, Mathf.Sqrt(2f * 2.4f * Mathf.Max(0f, distancia - .03f)));
            rapidezAtual = Mathf.MoveTowards(rapidezAtual, desejada, 2.4f * dt);
            float passo = Mathf.Min(distancia, rapidezAtual * dt);
            var filtro = new ContactFilter2D();
            filtro.SetLayerMask(obstaculos);
            filtro.useTriggers = false;
            int n = Physics2D.CircleCast(pos + Vector2.up * .1f, raio, delta.normalized, filtro, acertos, passo + .07f);
            for (int i = 0; i < n; i++)
            {
                Transform outro = acertos[i].transform;
                if (outro == null || outro == transform || outro.IsChildOf(transform))
                    continue;
                // Não atravessa água, edifícios ou outros personagens.
                Proximo();
                return;
            }

            Velocidade = delta.normalized * (passo / dt);
            Vector2 chegada = pos + delta.normalized * passo;
            if (corpo != null)
                corpo.MovePosition(chegada);
            else
                transform.position = new Vector3(chegada.x, chegada.y, transform.position.z);
        }

        private void Proximo()
        {
            destino = (destino + 1) % pontos.Length;
            rapidezAtual = 0f;
            espera = pausa * Mathf.Lerp(.85f, 1.15f, Mathf.Repeat(fasePausa + destino * .618034f, 1f));
        }

        private void OnDrawGizmosSelected()
        {
            if (pontos == null || pontos.Length < 2)
                return;
            Gizmos.color = animal ? new Color(1f, .7f, .3f) : Color.cyan;
            for (int i = 0; i < pontos.Length; i++)
            {
                Vector3 a = transform.parent != null ? transform.parent.TransformPoint(pontos[i]) : (Vector3)pontos[i];
                Vector3 b = transform.parent != null ? transform.parent.TransformPoint(pontos[(i + 1) % pontos.Length]) : (Vector3)pontos[(i + 1) % pontos.Length];
                Gizmos.DrawWireSphere(a, raio);
                Gizmos.DrawLine(a, b);
            }
        }
    }
}

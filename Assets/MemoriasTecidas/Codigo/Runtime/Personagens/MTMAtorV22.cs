using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Escreve depois dos Updates, antes da malha (ordem 200).
    [DefaultExecutionOrder(150), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTMAtorV22 : MonoBehaviour
    {
        public enum Tipo
        {
            Jogador,
            Morador,
            Animal
        }

        public Tipo tipo;
        public Sprite[] quadros;
        // Jogador: poses próprias da corrida, na mesma ordem de "quadros" (direção * 4 + passo).
        // Vazio = a corrida usa as poses da caminhada. O kit HD2D preenche ao dar Play.
        [System.NonSerialized]
        public Sprite[] quadrosCorrida;
        // Morador: caminhada de perfil (virada para a direita), usada enquanto ele anda; o jogo espelha
        // para a esquerda. Vazio = anda com a arte de frente. O kit HD2D preenche ao dar Play.
        [System.NonSerialized]
        public Sprite[] quadrosLado;
        private float faseLado;
        public MTMJogo jogo;
        public MTMVidaV22 vida;
        public string nomeDaConversa;
        [Range(2f, 14f)]
        public float fpsCaminhada = 7f, fpsCorrida = 10f;
        [Tooltip("V2.2.1: inclui apoio, passada e joelho levantado no ciclo.")]
        public bool cicloCompleto = true;
        [SerializeField]
        private int direcaoAtual, quadroAtual;
        private SpriteRenderer desenho;
        private MTMCaminhanteV22 caminhante;
        private Transform raizMovimento;
        private Vector3 posicaoAnterior;
        private float tempo, fase;
        private bool primeiraLeitura = true, andava;
        public float RapidezReal { get; private set; }
        // Deslocamento visto neste quadro (unidades por segundo): a passada dos moradores inclina para ele.
        public Vector2 VelocidadeReal { get; private set; }

        // Distância de uma passada completa (os dois pés). Moradores dão passos um pouco mais curtos que o
        // Neto, para a cadência do andar devagar não parecer flutuar. Usado também pela captura do Editor.
        public static float Passada(float altura, Tipo tipo)
        {
            return Mathf.Max(.35f, altura * (tipo == Tipo.Jogador ? .45f : .36f));
        }
        // Direção do sprite (0 baixo, 1 baixo-esquerda ... 7 baixo-direita).
        public int Direcao
        {
            get { return direcaoAtual; }
        }

        // Quadro exibido (direção * 4 + passo, no jogador).
        public int Quadro
        {
            get { return quadroAtual; }
        }
        public float FaseCaminhada { get; private set; }
        // A arte já desenha as pernas: a malha não acrescenta a passada artificial.
        public bool DesenhaPassos
        {
            get { return tipo == Tipo.Jogador || (quadrosLado != null && quadrosLado.Length > 0); }
        }

        // Jogador: posição contínua no ciclo de 4 poses (0 a 4); o quadro é a parte inteira.
        public float FasePasso
        {
            get { return tipo == Tipo.Jogador ? tempo : 0f; }
        }

        // Jogador correndo com as poses próprias da corrida (não as da caminhada).
        public bool CorrendoComQuadros { get; private set; }

        private void Awake()
        {
            desenho = GetComponent<SpriteRenderer>();
            caminhante = GetComponentInParent<MTMCaminhanteV22>();
            if (jogo == null && tipo == Tipo.Jogador)
                jogo = GetComponentInParent<MTMJogo>();
            raizMovimento = tipo == Tipo.Jogador && jogo != null ? jogo.transform : transform.parent != null ? transform.parent : transform;
            fase = Mathf.Abs(raizMovimento.position.x * .831f + raizMovimento.position.y * .53f);
            posicaoAnterior = raizMovimento.position;
        }

        private void Start()
        {
            if (tipo != Tipo.Animal && GetComponent<MTMContatoPersonagem>() == null)
            {
                var contato = gameObject.AddComponent<MTMContatoPersonagem>();
                contato.hideFlags = HideFlags.DontSave;
            }
        }

        private void OnEnable()
        {
            primeiraLeitura = true;
            andava = false;
            tempo = 0;
            RapidezReal = 0;
            VelocidadeReal = Vector2.zero;
            FaseCaminhada = 0;
        }

        // Um novo movimento já exibe a passada 1; o quadro 0 é o repouso/apoio.
        // Método puro compartilhado com a verificação no Editor.
        public static int AvancarPasso(bool andando, bool anterior, bool completo, float dt, float fps, float proporcao, ref float fasePasso)
        {
            if (!andando)
            {
                fasePasso = 0f;
                return 0;
            }

            if (!anterior)
            {
                fasePasso = completo ? 1f : 0f;
                return 1;
            }

            int quantidade = completo ? 4 : 3;
            fasePasso = Mathf.Repeat(fasePasso + Mathf.Max(0f, dt) * Mathf.Max(0f, fps) * Mathf.Clamp(proporcao, .1f, 1f), quantidade);
            return (completo ? 0 : 1) + Mathf.FloorToInt(fasePasso);
        }

        private void LateUpdate()
        {
            if (desenho == null || quadros == null || quadros.Length == 0 || raizMovimento == null)
                return;
            Vector3 pos = raizMovimento.position;
            Vector2 delta = primeiraLeitura ? Vector2.zero : (Vector2)(pos - posicaoAnterior);
            primeiraLeitura = false;
            posicaoAnterior = pos;
            if (delta.sqrMagnitude > 1f)
            {
                delta = Vector2.zero;
                tempo = 0;
                andava = false;
            }

            float limite = tipo == Tipo.Jogador && jogo != null ? jogo.velocidade * Mathf.Max(1.8f, jogo.multiplicadorCorrida + .25f) : 8f;
            Vector2 medida = MTMCalculosMovimento.MedirVelocidade(delta, Time.deltaTime, limite);
            // O Rigidbody já interpola o deslocamento visível. Outra suavização atrasava
            // a primeira passada e mantinha a animação depois de parar.
            Vector2 velocidade = medida;
            // Inclui moradores movidos pelo roteiro: a pose acompanha o deslocamento que foi visto.
            // Atingido numa luta, Neto é jogado para trás: a pose fica parada, de frente para a lenda.
            if (jogo != null && (jogo.V22Bloqueado || (tipo == Tipo.Jogador && jogo.NetoAtordoado)))
                velocidade = Vector2.zero;
            bool andando = velocidade.sqrMagnitude > (tipo == Tipo.Jogador ? .0001f : .01f);
            RapidezReal = andando ? velocidade.magnitude : 0f;
            VelocidadeReal = andando ? velocidade : Vector2.zero;
            float percurso = andando ? RapidezReal * Time.deltaTime : 0f;
            if (andando)
            {
                float altura = desenho.sprite != null ? desenho.sprite.bounds.size.y * Mathf.Abs(transform.lossyScale.y) : 1.6f;
                FaseCaminhada = Mathf.Repeat(FaseCaminhada + percurso / Passada(altura, tipo) * Mathf.PI * 2f, Mathf.PI * 2f);
            }
            else
                FaseCaminhada = 0f;
            quadroAtual = 0;
            CorrendoComQuadros = false;
            if (tipo == Tipo.Jogador && quadros.Length >= 32)
            {
                float fps = fpsCaminhada;
                float baseAndar = jogo != null ? Mathf.Max(.1f, jogo.velocidade) : 3.5f;
                float fatorCorrida = jogo != null ? Mathf.Max(1.05f, jogo.multiplicadorCorrida) : 1.75f;
                float distanciaQuadro = baseAndar / Mathf.Max(1f, fpsCaminhada);
                if (andando)
                {
                    direcaoAtual = MTMCalculosMovimento.DirecaoOito(velocidade, direcaoAtual);
                    float rapidez = velocidade.magnitude;
                    float corrida = Mathf.InverseLerp(baseAndar, baseAndar * fatorCorrida, rapidez);
                    fps = Mathf.Lerp(fpsCaminhada, fpsCorrida, corrida);
                    distanciaQuadro = Mathf.Lerp(baseAndar, baseAndar * fatorCorrida, corrida) / Mathf.Max(1f, fps);
                    CorrendoComQuadros = corrida > .6f && quadrosCorrida != null && quadrosCorrida.Length == quadros.Length;
                }

                if (!andando && jogo != null && jogo.DirecaoDoEncontro.sqrMagnitude > .01f)
                    direcaoAtual = MTMCalculosMovimento.DirecaoOito(jogo.DirecaoDoEncontro, direcaoAtual);
                int passo = MTMCalculosMovimento.PassoPorDistancia(andando, andava, cicloCompleto, percurso, distanciaQuadro, ref tempo);
                quadroAtual = direcaoAtual * 4 + passo;
                // Direção ainda sem pose de corrida: corre com a da caminhada (e a postura automática).
                if (CorrendoComQuadros && (quadroAtual >= quadrosCorrida.Length || quadrosCorrida[quadroAtual] == null))
                    CorrendoComQuadros = false;
            }
            else if (tipo == Tipo.Morador)
            {
                bool fala = jogo != null && jogo.V22FalandoCom(nomeDaConversa);
                if (fala)
                    quadroAtual = Mathf.FloorToInt(Time.unscaledTime * 3.2f) % 2 == 0 ? 2 : 0;
                else if (!andando && Mathf.Repeat(Time.time + fase, 5.7f) < .18f)
                    quadroAtual = 1;
            }
            else if (tipo == Tipo.Animal)
            {
                tempo = andando ? Mathf.Repeat(tempo + percurso / .12f, quadros.Length) : 0;
                quadroAtual = andando ? Mathf.FloorToInt(tempo) % quadros.Length : 0;
                if (Mathf.Abs(velocidade.x) > .02f)
                    desenho.flipX = velocidade.x < 0;
            }

            andava = andando;
            Sprite[] poses = CorrendoComQuadros ? quadrosCorrida : quadros;
            Sprite proximo = poses[Mathf.Clamp(quadroAtual, 0, poses.Length - 1)];
            if (proximo == null)
                proximo = quadros[Mathf.Clamp(quadroAtual, 0, quadros.Length - 1)];
            // Morador com caminhada de perfil: andando, troca de quadro pela distância e vira para onde vai;
            // parado ou conversando, volta à arte de frente, sem espelhar.
            if (tipo == Tipo.Morador && quadrosLado != null && quadrosLado.Length > 0)
            {
                bool conversando = jogo != null && jogo.V22FalandoCom(nomeDaConversa);
                if (andando && !conversando)
                {
                    // Um ciclo da folha de perfil é uma passada inteira (os dois pés), na cadência do andar de frente.
                    Sprite frente = quadros[0] != null ? quadros[0] : desenho.sprite;
                    float altura = frente != null ? frente.bounds.size.y * Mathf.Abs(transform.lossyScale.y) : 1.6f;
                    faseLado = Mathf.Repeat(faseLado + percurso / Passada(altura, tipo) * quadrosLado.Length, quadrosLado.Length);
                    var lado = quadrosLado[Mathf.Clamp(Mathf.FloorToInt(faseLado), 0, quadrosLado.Length - 1)];
                    if (lado != null)
                        proximo = lado;
                    if (Mathf.Abs(velocidade.x) > .02f)
                        desenho.flipX = velocidade.x < 0f;
                }
                else
                {
                    faseLado = 0f;
                    desenho.flipX = false;
                }
            }
            if (desenho.sprite != proximo)
                desenho.sprite = proximo;
        }
    }
}

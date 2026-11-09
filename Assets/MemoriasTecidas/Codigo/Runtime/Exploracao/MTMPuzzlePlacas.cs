using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    /// <summary>
    /// Puzzle ambiental sem diálogo: pisar nas placas na ordem para abrir a passagem.
    /// O progresso intermediário não é salvo; a solução é um marco permanente.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MTMPuzzlePlacas : MonoBehaviour
    {
        [Tooltip("Identificador único do puzzle, sem prefixo.")]
        public string idPuzzle = "praca_01";
        [Tooltip("ID da região onde está instalado.")]
        public string idRegiao = "praca";
        [Tooltip("Ordem dos índices de placas (0, 1, 2).")]
        public int[] sequencia = { 0, 1, 2 };
        public Transform[] placas = new Transform[3];
        public SpriteRenderer[] visuais = new SpriteRenderer[3];
        public SpriteRenderer visualPassagem;
        public Collider2D colisaoPassagem;
        public float raioAtivacao = 0.52f;
        public float raioPista = 3.6f;

        private static readonly Color[] CoresBase =
        {
            new Color(.20f, .53f, .95f),
            new Color(.99f, .76f, .22f),
            new Color(.26f, .74f, .43f)
        };

        private MTMJogo jogo;
        private int proxima;
        private int placaOcupada = -1;
        private bool mostrouPista;
        private bool resolvidoVisual;

        private bool ConfiguracaoValida()
        {
            if (placas == null || visuais == null || sequencia == null ||
                placas.Length != 3 || visuais.Length != 3 || sequencia.Length != 3)
                return false;

            int mascara = 0;
            for (int i = 0; i < 3; i++)
            {
                if (placas[i] == null || visuais[i] == null || sequencia[i] < 0 || sequencia[i] > 2)
                    return false;
                mascara |= 1 << sequencia[i];
            }
            return mascara == 7;
        }

        private void OnEnable()
        {
            proxima = 0;
            placaOcupada = -1;
            mostrouPista = false;
            resolvidoVisual = false;
        }

        private void Start()
        {
            if (!ConfiguracaoValida())
                Debug.LogError("Puzzle de placas incompleto ou com sequência inválida: " + name, this);
        }

        private void Update()
        {
            if (!ConfiguracaoValida()) return;

            if (jogo == null)
                jogo = FindFirstObjectByType<MTMJogo>(FindObjectsInactive.Include);
            if (jogo == null || jogo.estado == null || jogo.estado.area != idRegiao) return;

            if (jogo.estado.Tem("exploracao_puzzle_" + idPuzzle))
            {
                if (!resolvidoVisual) AplicarVisuais(true);
                return;
            }

            if (resolvidoVisual) AplicarVisuais(false);
            if (!jogo.PodeAtivarPuzzle)
            {
                // Exigir que o jogador saia e entre novamente na placa após fechar um painel.
                placaOcupada = PlacaSobJogador(jogo.transform.position);
                return;
            }

            Vector2 posicao = jogo.transform.position;
            int agora = PlacaSobJogador(posicao);
            if (agora != placaOcupada)
            {
                placaOcupada = agora;
                if (agora >= 0)
                    ProcessarPlaca(agora);
            }

            float distancia = Vector2.Distance(posicao, transform.position);
            if (distancia <= raioPista && !mostrouPista)
            {
                mostrouPista = true;
                jogo.AvisarPuzzle("Desafio das placas: AZUL, OURO, VERDE. Pise na ordem.");
            }
            else if (distancia > raioPista + 2f)
                mostrouPista = false;
        }

        private int PlacaSobJogador(Vector3 posicao)
        {
            for (int i = 0; i < placas.Length; i++)
                if (Vector2.Distance(posicao, placas[i].position) <= raioAtivacao)
                    return i;
            return -1;
        }

        private void ProcessarPlaca(int indice)
        {
            if (indice == sequencia[proxima])
            {
                proxima++;
                if (proxima == sequencia.Length)
                {
                    if (jogo.RegistrarPuzzleConcluido(idPuzzle))
                    {
                        AplicarVisuais(true);
                        jogo.AvisarPuzzle("Puzzle concluído! A passagem foi aberta.");
                    }
                    return;
                }
                jogo.AvisarPuzzle("Placa certa! " + proxima + "/3. Continue a sequência.");
            }
            else
            {
                // Acertar a primeira placa depois de errar já inicia uma nova tentativa.
                proxima = indice == sequencia[0] ? 1 : 0;
                jogo.AvisarPuzzle("Ordem incorreta! Recomece pelo AZUL.");
            }
            AplicarVisuais(false);
        }

        private void AplicarVisuais(bool resolvido)
        {
            resolvidoVisual = resolvido;
            for (int i = 0; i < visuais.Length; i++)
            {
                if (visuais[i] == null) continue;
                visuais[i].color = resolvido
                    ? Color.Lerp(CoresBase[i], Color.white, .35f)
                    : Color.Lerp(CoresBase[i], Color.white, i == sequencia[0] && proxima == 0 ? .15f : .0f);
            }
            if (visualPassagem != null) visualPassagem.enabled = !resolvido;
            if (colisaoPassagem != null) colisaoPassagem.enabled = !resolvido;
        }

        private void OnDrawGizmosSelected()
        {
            if (placas == null) return;
            Gizmos.color = Color.cyan;
            foreach (var placa in placas)
                if (placa != null) Gizmos.DrawWireSphere(placa.position, raioAtivacao);
        }
    }
}

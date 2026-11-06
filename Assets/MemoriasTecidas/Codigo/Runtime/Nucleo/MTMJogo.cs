using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [RequireComponent(typeof(Rigidbody2D)), DisallowMultipleComponent]
    public sealed partial class MTMJogo : MonoBehaviour
    {
        public Camera cameraDoMapa;
        public MTMRegiao[] regioes;
        public MTMAmbienteV2 ambienteVisual;
        [Range(5f, 30f)]
        public float tamanhoCamera = 8f;
        [Min(1f)]
        public float velocidade = 3.5f;
        [Min(60f)]
        public float segundosPorDia = 720f;
        public bool carregarProgresso = true;
        public bool importarProgressoAnterior = true;
        public MTMEstado estado = new MTMEstado();
        private Rigidbody2D corpo;
        private Vector2 movimento;
        private Vector3 velocidadeCamera;
        private float multiplicador = 1f;
        private Texture2D retratoAtual;
        private Rect recorteAtual;
        private MTMRegiao atual;
        private MTMPonto perto;
        private string[] falas;
        private string locutor, contexto;
        private int pagina, quadroDaAcao = -1;
        private Action aoConcluir;
        private string painel = "", destinoMapa = "";
        private string aviso = "";
        private float avisoAte;
        private bool pronto, confirmarNovo, migracaoBloqueadaRoteiro;
        public bool Noite
        {
            get
            {
                return estado.relogio < .25f || estado.relogio >= .75f;
            }
        }

        private bool Bloqueado
        {
            get
            {
                return inicioAbertoUI || falas != null || painel.Length > 0;
            }
        }

        private void Awake()
        {
            corpo = GetComponent<Rigidbody2D>();
            PrepararApresentacao();
        }

        private void Start()
        {
            if (cameraDoMapa == null || regioes == null || regioes.Length == 0)
            {
                Debug.LogError("Mundo V2 sem câmera ou regiões. Use o menu de criação de cena.", this);
                enabled = false;
                return;
            }

            PrepararMenusUI();
            if (carregarProgresso)
                Carregar();
            if (migracaoBloqueadaRoteiro)
            {
                enabled = false;
                return;
            }

            PrepararTramaTecida();
            if (tramaTecida == null)
            {
                enabled = false;
                return;
            }

            PrepararPrimeiroFio();
            PrepararKitHD2D();
            string inicio = RegiaoInicialPrimeiroFio();
            pronto = true;
            if (!RetomarJornadaSalva() && !IrParaPontoSeguro(inicio))
                IrParaPontoSeguro("praca");
            var localFeliciana = MTMVinculosRegiao.RegiaoDoPonto(regioes, "npc_feliciana");
            string encontro = localFeliciana == null ? "Consulte o caderno." : "Dona Feliciana: " + localFeliciana.dados.nome + ".";
            Avisar("WASD/setas: caminhar. " + encontro + " E: conversar. J: caderno. M: mapa.");
        }

        private void Update()
        {
            if (!pronto)
                return;
            AtualizarPassagemPrimeiroFio();
            if (!possuiFoco)
            {
                Parar();
                return;
            }

            if (MTMEntrada.Apertou('x'))
            {
                VoltarInterfaceUI();
                return;
            }

            if (inicioAbertoUI || painel == "pausa" || painel == "visual" || painel == "ajuda" || painel == "sobre" || painel == "mapaCidade")
            {
                Parar();
                return;
            }

            AtualizarRoteiro();
            // Digitar nomes na bolsa não aciona I/M/J/Q nem move o protagonista.
            if (painel == "bolsa" && buscaBolsaEmFoco && !MTMEntrada.Apertou('x'))
            {
                Parar();
                return;
            }

            if (MTMEntrada.ApertouF1())
            {
                interfaceVisivel = !interfaceVisivel;
            }

            AjustarCamera();
            if (atual != null)
                atual.Ambiente(Noite, false);
            if (ambienteVisual != null && atual != null)
                ambienteVisual.Atualizar(atual.dados, estado.relogio);
            if (MTMEntrada.Apertou('q'))
            {
                MTMAmbienteV2.Qualidade = (MTMAmbienteV2.Qualidade + 1) % 3;
                Avisar("Qualidade dos efeitos: " + MTMAmbienteV2.Nomes[MTMAmbienteV2.Qualidade]);
            }

            if (falas != null)
            {
                Parar();
                if (MTMEntrada.Apertou('e') && !ConfirmacaoComFocoUI)
                    Avancar();
                return;
            }

            if (painel == "primeiro_fio")
            {
                if (!ConfirmacaoComFocoUI)
                    TeclasPrimeiroFio();
                return;
            }

            if (painel == "atividade" || painel == "observacao")
            {
                Parar();
                return;
            }

            if (MTMEntrada.Apertou('m'))
                AlternarPainel("mapa");
            else if (MTMEntrada.Apertou('j'))
                AlternarPainel("diario");
            else if (MTMEntrada.Apertou('i') || MTMEntrada.Apertou('b'))
                AlternarPainel("bolsa");
            if (Bloqueado || ConfirmacaoComFocoUI)
            {
                Parar();
                return;
            }

            estado.relogio += Time.deltaTime / segundosPorDia;
            if (estado.relogio >= 1f)
            {
                estado.relogio -= 1f;
                estado.dia++;
            }

            if (MTMEntrada.Apertou('r'))
            {
                Reposicionar(new Vector2(atual.dados.seguroX, atual.dados.seguroY));
                Avisar("Você voltou ao caminho central desta região.");
                return;
            }

            movimento = MTMEntrada.Movimento();
            if (movimento.sqrMagnitude < .0001f && corpo != null)
                corpo.linearVelocity = Vector2.zero;
            multiplicador = MTMEntrada.Correndo() ? 1.55f : 1f;
            perto = MaisProximo();
            if (MTMEntrada.Apertou('e') && perto != null && quadroDaAcao != Time.frameCount)
                Interagir(perto);
        }

        private void FixedUpdate()
        {
            MoverComSuavidade();
        }

        private void LateUpdate()
        {
            AtualizarCameraFluida();
        }

        private void OnDisable()
        {
            Parar();
        }

        private void OnApplicationQuit()
        {
            SalvarPreferenciasInterface();
            SalvarPreferenciasVisuais();
            if (pronto && carregarProgresso)
                Salvar();
        }

        private void OnApplicationPause(bool pausa)
        {
            if (pausa)
            {
                Parar();
                SalvarPreferenciasInterface();
                SalvarPreferenciasVisuais();
                if (pronto && carregarProgresso)
                    Salvar();
            }
        }

        private void Parar()
        {
            movimento = Vector2.zero;
            if (corpo != null)
                corpo.linearVelocity = Vector2.zero;
        }

        private void AlternarPainel(string nome)
        {
            retornoColecaoUI = "";
            painel = painel == nome ? "" : nome;
            confirmarNovo = false;
            Parar();
        }

        private void Avisar(string texto)
        {
            aviso = texto;
            avisoAte = Time.unscaledTime + 5f;
        }

        private void AjustarCamera()
        {
            if (Screen.height <= 0)
                return;
            cameraDoMapa.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}

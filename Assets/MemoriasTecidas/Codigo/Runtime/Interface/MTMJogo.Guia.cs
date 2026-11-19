using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Orientação do jogador: seta de guia na borda da tela, marcador sobre o alvo,
    // dica por inatividade e relógio parado até a primeira conversa.
    public sealed partial class MTMJogo
    {
        [Tooltip("Mantém o relógio parado até a primeira conversa com Dona Feliciana.")]
        public bool pausarRelogioAteConversaInicial = true;

        private const string EventoPrimeiraConversa = "r24_m01_01";
        private const float SegundosParaDicaGuia = 25f;
        private bool mostrarGuiaMundo = true;
        private Texture2D setaGuia;
        private int marcosVistosGuia = -1;
        private string regiaoVistaGuia = "";
        private float inicioSemProgressoGuia, ultimaChamadaGuia;
        private bool dicaGuiaMostrada;

        // Arte do guia (Resources/MemoriasTecidas/Interface/Guia): losango bordado sobre o alvo e anel costurado no chão.
        // Sem a arte, o guia volta ao losango e às ondas desenhados em código.
        private const string PastaArteGuia = "MemoriasTecidas/Interface/Guia/";
        private static readonly Color AmbarGuia = new Color(.94f, .69f, .24f, 1f);
        private Texture2D marcadorGuiaArte;
        private bool arteGuiaProcurada;
        private GameObject anelGuiaMundo;
        private Transform anelGuiaGiro;
        private SpriteRenderer anelGuiaDesenho;
        private int anelGuiaPedido = -10;

        private void CarregarArteGuia()
        {
            if (arteGuiaProcurada)
                return;
            arteGuiaProcurada = true;
            marcadorGuiaArte = Resources.Load<Texture2D>(PastaArteGuia + "marcador_objetivo");
        }

        private bool PessoaGuia(MTMPonto alvo)
        {
            return alvo != null && alvo.dados != null && tramaTecida != null && tramaTecida.personagens != null &&
                Array.Exists(tramaTecida.personagens, x => x != null && x.id == alvo.dados.id);
        }

        // Anel costurado no chão do alvo. Fica no mundo (não no HUD) para passar por trás das pessoas.
        private bool MostrarAnelGuia(MTMPonto alvo, bool reduzir, float pisca)
        {
            if (anelGuiaMundo == null)
            {
                var sprite = Resources.Load<Sprite>(PastaArteGuia + "anel_objetivo");
                if (sprite == null)
                    return false;
                anelGuiaMundo = new GameObject("Guia · anel no chão") { hideFlags = HideFlags.DontSave };
                var giro = new GameObject("Giro") { hideFlags = HideFlags.DontSave };
                giro.transform.SetParent(anelGuiaMundo.transform, false);
                anelGuiaDesenho = giro.AddComponent<SpriteRenderer>();
                anelGuiaDesenho.sprite = sprite;
                // Como uma marca pintada no chão: acima do piso (-30000 a -29000) e abaixo das sombras (-22002 a -21980),
                // que caem por cima dele; pessoas e objetos ficam sempre na frente.
                anelGuiaDesenho.sortingOrder = -22500;
                anelGuiaGiro = giro.transform;
            }

            // 1,35 de diâmetro, achatado como o chão visto de cima; numa passagem, o mesmo tamanho do anel dela.
            float diametro = alvo.dados != null && alvo.dados.tipo == "saida" ? MTMMarcadorSaidaV221.DiametroAnel : 1.35f;
            float s = diametro / Mathf.Max(.01f, anelGuiaDesenho.sprite.bounds.size.x);
            anelGuiaMundo.transform.localScale = new Vector3(s, s * .5f, 1f);
            anelGuiaPedido = Time.frameCount;
            // O ponto de uma pessoa fica cerca de 0,35 acima dos pés.
            Vector3 p = alvo.transform.position + (PessoaGuia(alvo) ? Vector3.down * .32f : Vector3.zero);
            anelGuiaMundo.transform.position = new Vector3(p.x, p.y, 0f);
            if (!anelGuiaMundo.activeSelf)
                anelGuiaMundo.SetActive(true);
            if (!reduzir)
                anelGuiaGiro.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 18f);
            anelGuiaDesenho.color = new Color(1f, 1f, 1f, .78f + .22f * pisca);
            return true;
        }

        // Para o anel das saídas: quando o guia já marca esta passagem, o anel da saída não se repete por cima.
        public bool V22AnelGuiaEm(Vector3 posicao)
        {
            return anelGuiaMundo != null && anelGuiaMundo.activeSelf &&
                ((Vector2)(anelGuiaMundo.transform.position - posicao)).sqrMagnitude < .36f;
        }

        public bool V22ReduzirMovimento
        {
            get { return apresentacao != null && apresentacao.reduzirMovimento; }
        }

        // No LateUpdate: o anel some quando o guia não foi desenhado no quadro anterior (conversa, painel, alvo perto).
        private void EsconderAnelGuiaSeParado()
        {
            if (anelGuiaMundo != null && anelGuiaMundo.activeSelf && Time.frameCount - anelGuiaPedido > 1)
                anelGuiaMundo.SetActive(false);
        }

        // O tempo só corre depois que o jogador aprendeu a andar e falou com Feliciana.
        private bool RelogioPausado()
        {
            return pausarRelogioAteConversaInicial && tramaTecida != null && !estado.Tem(EventoPrimeiraConversa);
        }

        // Alvo atual da missão nesta região: a saída que leva ao destino ou o ponto da etapa.
        private MTMPonto AlvoGuiaAtual(out bool ehSaida)
        {
            ehSaida = false;
            if (atual == null || tramaTecida == null)
                return null;
            bool primeiro = GuiaPrimeiroFioAtivo();
            var missao = MissaoAtualTecido();
            var local = primeiro ? LocalPrimeiroFio() : null;
            string destino = !string.IsNullOrEmpty(destinoGuiadoTecido) ? destinoGuiadoTecido :
                local != null ? local.dados.id : RegiaoDaEtapaTecido(EtapaAtualTecido(missao));
            MTMPonto alvo = ProximaSaidaTecido(destino);
            if (alvo == null && destino == atual.dados.id)
                alvo = primeiro && string.IsNullOrEmpty(destinoGuiadoTecido) ? AlvoPrimeiroFio() : AlvoDaEtapaTecido(EtapaAtualTecido(missao));
            if (alvo == null || alvo.dados == null || !alvo.gameObject.activeInHierarchy)
                return null;
            ehSaida = alvo.dados.tipo == "saida";
            return alvo;
        }

        // Terceira linha do cartão de objetivo: passo, lugar e distância em passagens.
        // Passos da etapa atual, para os losangos do cartão de objetivo.
        private void PassosObjetivoHUD(bool primeiro, MTMMissaoTecido missao, MTMEtapaTecido etapa, out int feitos, out int total)
        {
            feitos = total = 0;
            if (primeiro && LocalPrimeiroFio() != null)
            {
                total = 4;
                feitos = Mathf.Clamp(MTMPrimeiroFio.Etapa(estado), 0, 4);
                return;
            }

            if (missao == null || missao.etapas == null || missao.etapas.Length < 2)
                return;
            total = missao.etapas.Length;
            int i = etapa == null ? -1 : Array.IndexOf(missao.etapas, etapa);
            feitos = i < 0 ? total : i;
        }

        private string LinhaOndeHUD(bool primeiro, MTMMissaoTecido missao, MTMEtapaTecido etapa, bool comPasso = true)
        {
            string destino;
            string passo = "";
            if (primeiro && LocalPrimeiroFio() != null)
            {
                destino = LocalPrimeiroFio().dados.id;
                passo = "Passo " + Mathf.Min(4, MTMPrimeiroFio.Etapa(estado) + 1) + "/4 · ";
            }
            else
            {
                destino = RegiaoDaEtapaTecido(etapa);
                if (missao != null && etapa != null && missao.etapas != null && missao.etapas.Length > 1)
                {
                    int i = Array.IndexOf(missao.etapas, etapa);
                    if (i >= 0)
                        passo = "Passo " + (i + 1) + "/" + missao.etapas.Length + " · ";
                }
            }

            if (!comPasso)
                passo = "";
            if (string.IsNullOrEmpty(destino))
                return passo.TrimEnd(' ', '·');
            string nome = NomeCurtoTecido(destino);
            if (atual != null && destino == atual.dados.id)
                return passo + nome + " · você está aqui";
            int passagens = CaminhoTecido(destino).Count - 1;
            return passo + nome + (passagens > 0 ? " · " + passagens + (passagens == 1 ? " passagem" : " passagens") : "");
        }

        // GUIUtility.RotateAroundPivot recebe o pivô em pixels de tela, não nas coordenadas
        // virtuais 1280x720 do HUD. Converte antes; sem isso, em telas de outro tamanho,
        // setas e faixas giram em volta do ponto errado e aparecem deslocadas.
        private static void GirarEmVolta(float graus, Vector2 pivo)
        {
            GUIUtility.RotateAroundPivot(graus, GUI.matrix.MultiplyPoint3x4(new Vector3(pivo.x, pivo.y, 0f)));
        }

        // Altura do losango acima do alvo: sobre a cabeça de pessoas, rente ao chão em saídas.
        private float AlturaMarcadorGuia(MTMPonto alvo)
        {
            if (alvo == null || alvo.dados == null)
                return 1.6f;
            if (alvo.dados.tipo == "saida")
                return .7f;
            // Com quem prepara o jogador ao lado, o ponto da luta é a própria pessoa.
            MTMConfrontoDados lenda;
            if (PontoConfronto(alvo) && confrontosPorPonto.TryGetValue(alvo.dados.id, out lenda) && GuiaRecebe(lenda))
                return 2.3f;
            if (PontoConfronto(alvo) || PontoVivo(alvo))
                return 1.4f;
            bool pessoa = tramaTecida != null && tramaTecida.personagens != null &&
                Array.Exists(tramaTecida.personagens, x => x != null && x.id == alvo.dados.id);
            // Logo acima da cabeça: os personagens têm cerca de 1,7 de altura e o ponto fica 0,35 acima dos pés.
            // Mais alto, o losango parecia marcar o chão vazio atrás da pessoa.
            return pessoa ? 2.3f : 1.8f;
        }

        private void PrepararSetaGuia()
        {
            if (setaGuia != null)
                return;
            const int n = 64;
            setaGuia = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "Seta_de_guia",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[n * n];
            // Seta apontando para a direita: haste fina (x 6 a 34) e ponta comprida (x 28 a 58).
            // O triângulo antigo tinha os três lados quase iguais e parecia apontar para baixo.
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dy = Mathf.Abs(y - 31.5f);
                    float haste = x >= 6 && x <= 34 ? Mathf.Clamp01(6.5f - dy) : 0f;
                    float ponta = x >= 28 && x <= 58 ? Mathf.Clamp01((58f - x) * .6f - dy) : 0f;
                    pixels[y * n + x] = new Color(1, 1, 1, Mathf.Max(haste, ponta));
                }

            setaGuia.SetPixels(pixels);
            setaGuia.Apply(false, true);
        }

        private void AtualizarProgressoGuia()
        {
            float agora = Time.unscaledTime;
            // Painéis e conversas não desenham o HUD: o relógio de inatividade recomeça.
            if (agora - ultimaChamadaGuia > 1f)
                inicioSemProgressoGuia = agora;
            ultimaChamadaGuia = agora;
            int marcos = estado.marcos == null ? 0 : estado.marcos.Count;
            string regiao = atual == null ? "" : atual.dados.id;
            if (marcos != marcosVistosGuia || regiao != regiaoVistaGuia)
            {
                marcosVistosGuia = marcos;
                regiaoVistaGuia = regiao;
                inicioSemProgressoGuia = agora;
                dicaGuiaMostrada = false;
            }
        }

        // Desenha o marcador (alvo visível) ou a seta de borda (alvo fora da tela).
        private void GuiaMundoHUD(MTMPonto alvo, bool ehSaida, Color dourado, Color fundo, Color claro)
        {
            AtualizarProgressoGuia();
            if (!mostrarGuiaMundo || alvo == null || cameraDoMapa == null)
                return;
            bool parado = Time.unscaledTime - inicioSemProgressoGuia > SegundosParaDicaGuia;
            if (parado && !dicaGuiaMostrada)
            {
                dicaGuiaMostrada = true;
                Avisar("Dica: siga a seta dourada. Aperte M para ver o caminho no mapa.");
            }

            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            float pulso = reduzir ? 1f : 1f + (parado ? .25f : .1f) * Mathf.Sin(Time.unscaledTime * (parado ? 7f : 4f));
            Vector3 proj = cameraDoMapa.WorldToScreenPoint(alvo.transform.position + Vector3.up * AlturaMarcadorGuia(alvo));
            if (proj.z <= 0)
                return;
            Vector2 pos = CoordenadaHUDCompacta(proj);
            // Visível na tela = losango sobre o alvo; fora dela = seta na borda.
            Rect tela = new Rect(28, 28, 1224, 664);
            Rect area = new Rect(70, 150, 1140, 430);
            float distancia = corpo == null ? 0f : Vector2.Distance(corpo.position, alvo.transform.position);
            string rotulo = ehSaida ? NomeMapaGuia(alvo.dados.destino) : Mathf.Max(1, Mathf.RoundToInt(distancia)) + " m";
            // Pisca: mais rápido e mais forte quando o jogador está parado há um tempo.
            float pisca = reduzir ? 1f : .5f + .5f * Mathf.Sin(Time.unscaledTime * (parado ? 9f : 5.5f));
            if (distancia > alvo.alcance * 1.4f)
                RastroGuiaHUD(alvo, distancia, dourado, reduzir);
            // Se a pessoa aparece na tela mas a cabeça passou da borda de cima (ou está sob o cartão),
            // continua sendo "na tela": o losango desce para dentro, em vez de virar a seta da borda.
            Vector3 chaoProj = cameraDoMapa.WorldToScreenPoint(alvo.transform.position);
            if (!tela.Contains(pos) && chaoProj.z > 0 && tela.Contains(CoordenadaHUDCompacta(chaoProj)))
                pos.y = Mathf.Max(pos.y, tela.yMin + 34f);
            if (tela.Contains(pos))
            {
                // Perto do alvo, o aviso [E] já orienta; o losango some para não poluir.
                if (distancia <= alvo.alcance * 1.4f)
                    return;
                // Marca no chão do alvo: o lugar exato, não só a direção. Com a arte, um anel costurado no mundo.
                if (!MostrarAnelGuia(alvo, reduzir, pisca))
                {
                    Vector3 chaoTela = cameraDoMapa.WorldToScreenPoint(alvo.transform.position);
                    if (chaoTela.z > 0)
                        OndaGuiaHUD(CoordenadaHUDCompacta(chaoTela), dourado, reduzir);
                }

                // Não deixa o losango se esconder atrás do cartão de objetivo ou do relógio.
                foreach (var bloqueio in new[] { cartaoObjetivoHUD, new Rect(1084, 20, 176, 96) })
                    if (bloqueio.width > 0f && new Rect(pos.x - 22f, pos.y - 22f, 44f, 44f).Overlaps(bloqueio))
                        pos.y = bloqueio.yMax + 26f;
                float oscila = reduzir ? 0f : Mathf.Sin(Time.unscaledTime * 3f) * 5f;
                CarregarArteGuia();
                if (marcadorGuiaArte != null)
                {
                    // Losango bordado com ponta: a ponta fica onde ficava o pé do losango antigo, logo acima da cabeça.
                    float larguraMarca = 46f * pulso, alturaMarca = larguraMarca * 1.25f;
                    float ponta = pos.y + 15f + oscila;
                    float centroMarca = ponta - alturaMarca * (74f / 160f);
                    DiscoHUD(new Rect(pos.x - larguraMarca * .78f, centroMarca - larguraMarca * .78f, larguraMarca * 1.56f, larguraMarca * 1.56f),
                        AlfaTecido(AmbarGuia, .10f + .18f * pisca));
                    Color antigaCor = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, .9f + .1f * pisca);
                    GUI.DrawTexture(new Rect(pos.x - larguraMarca * .5f, ponta - alturaMarca * (134f / 160f), larguraMarca, alturaMarca), marcadorGuiaArte);
                    GUI.color = antigaCor;
                    return;
                }

                float lado = 30f * pulso;
                // Halo que pisca atrás do losango.
                DiscoHUD(new Rect(pos.x - lado - 6f, pos.y - lado - 6f + oscila, lado * 2f + 12f, lado * 2f + 12f), AlfaTecido(dourado, .12f + .22f * pisca));
                // Contorno escuro: o dourado sozinho some sobre a terra clara do sertão.
                LosangoCompacta(new Rect(pos.x - lado * .5f - 4f, pos.y - lado * .5f + oscila - 4f, lado + 8f, lado + 8f), AlfaTecido(TintaAtualHUD, .9f));
                LosangoCompacta(new Rect(pos.x - lado * .5f, pos.y - lado * .5f + oscila, lado, lado), Color.Lerp(dourado, claro, .35f * pisca));
                LosangoCompacta(new Rect(pos.x - lado * .22f, pos.y - lado * .22f + oscila, lado * .44f, lado * .44f), claro);
                return;
            }

            PrepararSetaGuia();
            Vector2 centro = new Vector2(area.center.x, area.center.y);
            Vector2 d = pos - centro;
            float sx = Mathf.Abs(d.x) < .01f ? float.MaxValue : area.width * .5f / Mathf.Abs(d.x);
            float sy = Mathf.Abs(d.y) < .01f ? float.MaxValue : area.height * .5f / Mathf.Abs(d.y);
            Vector2 p = centro + d * Mathf.Min(1f, Mathf.Min(sx, sy));
            // A seta "empurra" para fora, na direção do alvo, e pisca.
            if (!reduzir)
                p += d.normalized * (4f + 4f * Mathf.Sin(Time.unscaledTime * 6f));
            // Disco escuro com borda dourada e a seta dentro: lê bem em qualquer chão.
            float disco = 50f * pulso;
            DiscoHUD(new Rect(p.x - disco * .5f - 12f, p.y - disco * .5f - 12f, disco + 24f, disco + 24f), AlfaTecido(dourado, .1f + .28f * pisca));
            DiscoHUD(new Rect(p.x - disco * .5f, p.y - disco * .5f + 3f, disco, disco), new Color(0, 0, 0, .35f));
            DiscoHUD(new Rect(p.x - disco * .5f - 2f, p.y - disco * .5f - 2f, disco + 4f, disco + 4f), dourado);
            DiscoHUD(new Rect(p.x - disco * .5f, p.y - disco * .5f, disco, disco), AlfaTecido(TintaAtualHUD, .95f));
            float tamanho = 30f * pulso;
            Matrix4x4 matriz = GUI.matrix;
            Color antiga = GUI.color;
            GirarEmVolta(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, p);
            GUI.color = Color.Lerp(dourado, claro, .45f * pisca);
            GUI.DrawTexture(new Rect(p.x - tamanho * .5f + 2f, p.y - tamanho * .5f, tamanho, tamanho), setaGuia);
            GUI.color = antiga;
            GUI.matrix = matriz;
            float largura = Mathf.Clamp(LarguraTextoHUD(rotulo, 13, true) + 26f, 90f, 240f);
            Rect r = new Rect(Mathf.Clamp(p.x - largura * .5f, 16, 1264 - largura), Mathf.Clamp(p.y + 32, 140, 600), largura, 28);
            PainelHUD(r);
            TextoTecido(r, ResumoHUDClaroUI(rotulo, largura - 16, 22, 13, true), 13, claro, true, TextAnchor.MiddleCenter);
        }

        // Onda que se abre no chão do alvo, duas por vez, defasadas.
        private void OndaGuiaHUD(Vector2 chao, Color dourado, bool reduzir)
        {
            var anel = TexturaAnelVivo();
            Color antiga = GUI.color;
            for (int k = 0; k < 2; k++)
            {
                float fase = reduzir ? .5f : Mathf.Repeat(Time.unscaledTime / 1.4f + k * .5f, 1f);
                float tamanho = Mathf.Lerp(18f, 78f, fase);
                GUI.color = AlfaTecido(dourado, (1f - fase) * .85f);
                // Achatado: o chão é visto de cima, em perspectiva leve.
                GUI.DrawTexture(new Rect(chao.x - tamanho * .5f, chao.y - tamanho * .25f, tamanho, tamanho * .5f), anel);
            }

            GUI.color = antiga;
        }

        // Rastro de pontos no chão, do Neto em direção ao alvo, correndo para a frente.
        private void RastroGuiaHUD(MTMPonto alvo, float distancia, Color dourado, bool reduzir)
        {
            if (corpo == null || distancia < 2.6f)
                return;
            Vector2 origem = corpo.position;
            Vector2 direcao = ((Vector2)alvo.transform.position - origem).normalized;
            float fase = reduzir ? 0f : Mathf.Repeat(Time.unscaledTime * 1.3f, 1f);
            const float espaco = .85f;
            for (int k = 0; k < 6; k++)
            {
                float d = 1.1f + (k + fase) * espaco;
                if (d > distancia - 1f)
                    break;
                Vector2 ponto = origem + direcao * d;
                Vector3 tela = cameraDoMapa.WorldToScreenPoint(new Vector3(ponto.x, ponto.y, 0f));
                if (tela.z <= 0)
                    continue;
                Vector2 p = CoordenadaHUDCompacta(tela);
                // Some na ponta e nasce devagar perto do Neto.
                float alfa = Mathf.Clamp01((k + fase) / 1.2f) * Mathf.Clamp01((6f - k - fase) / 2.5f) * .9f;
                LosangoCompacta(new Rect(p.x - 7f, p.y - 5f, 14f, 10f), AlfaTecido(TintaAtualHUD, alfa * .7f));
                LosangoCompacta(new Rect(p.x - 5f, p.y - 3.5f, 10f, 7f), AlfaTecido(AmbarGuia, alfa));
                LosangoCompacta(new Rect(p.x - 1.8f, p.y - 1.3f, 3.6f, 2.6f), AlfaTecido(TerracotaHUD, alfa));
            }
        }

        private string bannerNome = "", bannerDescricao = "", bannerRotulo = "";
        private float bannerInicio = -100f, bannerDuracao;
        private bool bannerAbertura, bannerSomTocado;
        // Área do banner neste quadro: o aviso desce para baixo dela.
        private Rect bannerRectHUD;

        private bool BannerAberturaAtivo
        {
            get { return bannerAbertura && Time.unscaledTime - bannerInicio < bannerDuracao; }
        }

        // Título de lugar ao entrar numa região; a descrição só aparece na primeira visita.
        // "abertura" = a chegada depois da abertura: maior, com a bandeira de Jaguaruana e "A aventura começa".
        private void MostrarBannerRegiao(string id, bool primeiraVisita, bool abertura = false)
        {
            var ficha = MTMAutoriaRoteiro.Lugar(tramaTecida, id);
            bannerNome = NomeMapaGuia(id);
            bannerDescricao = primeiraVisita && ficha != null ? ficha.descricao ?? "" : "";
            bannerAbertura = abertura;
            bannerRotulo = abertura ? "A AVENTURA COMEÇA" : bannerDescricao.Length > 0 ? "NOVO LUGAR" : "";
            // Na chegada, o banner espera a tela clarear.
            bannerInicio = Time.unscaledTime + (abertura ? .9f : 0f);
            bannerDuracao = abertura ? 7f : bannerDescricao.Length > 0 ? 6f : 3.4f;
            bannerSomTocado = false;
        }

        // Banner de lugar: o fio dourado corre do meio para as pontas, o pano abre a partir dele e o nome
        // sobe letra a letra. Na saída o pano enrola de volta no fio, que recolhe para o meio.
        private void BannerRegiaoHUD(Color fundo, Color claro, Color dourado)
        {
            bannerRectHUD = new Rect(0, 0, 0, 0);
            float t = Time.unscaledTime - bannerInicio;
            if (string.IsNullOrEmpty(bannerNome) || t < 0f || t > bannerDuracao)
                return;
            if (bannerAbertura && !bannerSomTocado)
            {
                bannerSomTocado = true;
                TocarSom("objetivo", .75f, 0f);
            }

            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            bool descricao = bannerDescricao.Length > 0, rotulo = bannerRotulo.Length > 0;
            int tamanhoNome = bannerAbertura ? 40 : 32;
            float bandeira = bannerAbertura ? 108f : 0f;
            float largura = Mathf.Clamp(Mathf.Max(LarguraSerifUI(bannerNome, tamanhoNome) + 160f, descricao ? 540f : 0f) + bandeira, 440f, bannerAbertura ? 780f : 660f);
            float altura = 20f + (rotulo ? 24f : 0f) + tamanhoNome * 1.3f + (descricao ? 48f : 0f) + 12f;
            float meio = 640f, topo = bannerAbertura ? 148f : 140f;
            float yFio = topo + altura * .5f;
            bannerRectHUD = new Rect(meio - largura * .5f, topo, largura, altura);

            // Tempos: fio (0 a .35 s), pano (.15 a .7 s), conteúdo a partir de .45 s; saída nos últimos .6 s.
            float sair = reduzir ? 0f : Suave((t - (bannerDuracao - .6f)) / .6f);
            float alfa = reduzir ? Mathf.Clamp01(Mathf.Min(t / .4f, (bannerDuracao - t) / .6f)) : 1f - Suave((t - (bannerDuracao - .3f)) / .3f);
            float fio = reduzir ? 1f : SaidaSuave(t / .35f) * (1f - sair);
            float abre = reduzir ? 1f : Mathf.Max(0f, Salto((t - .15f) / .55f));
            float alto = reduzir ? 1f : SaidaSuave((t - .2f) / .5f) * (1f - sair);
            float w = largura * abre, h = altura * alto;
            if (h > 6f && w > 24f)
                PainelHUD(new Rect(meio - w * .5f, yFio - h * .5f, w, h), alfa, bannerAbertura);
            // Fio dourado com um nó em cada ponta; some por trás do pano aberto.
            float meioFio = (largura * .5f + 24f) * fio;
            if (meioFio > 1f)
            {
                Fundo(new Rect(meio - meioFio, yFio - 1f, meioFio * 2f, 2f), AlfaTecido(dourado, .9f * alfa * (h > altura * .5f ? .25f : 1f)));
                float no = 14f * (reduzir ? 1f : Mathf.Max(0f, Salto((t - .25f) / .45f))) * (1f - sair);
                LosangoCompacta(new Rect(meio - meioFio - no * .5f, yFio - no * .5f, no, no), AlfaTecido(dourado, alfa));
                LosangoCompacta(new Rect(meio + meioFio - no * .5f, yFio - no * .5f, no, no), AlfaTecido(dourado, alfa));
            }

            float conteudo = alfa * (reduzir ? 1f : Mathf.Clamp01((t - .45f) / .3f) * (1f - Mathf.Clamp01(sair * 2.5f)));
            if (conteudo <= .01f || h < altura * .6f)
                return;
            Rect caixa = bannerRectHUD;
            float xTexto = caixa.x + bandeira, larguraTexto = caixa.width - bandeira;
            float y = caixa.y + 12f;
            if (bannerAbertura)
                BandeiraAoVentoUI(new Vector2(caixa.x + 30f, caixa.y + 28f), 72f, reduzir ? 1.3f : Time.unscaledTime, conteudo, reduzir ? .25f : 1f, caixa.height - 44f);
            if (rotulo)
            {
                // Rótulo entre dois fios curtos.
                float lr = LarguraTextoHUD(bannerRotulo, 13, true);
                float cx = xTexto + larguraTexto * .5f;
                TextoTecido(new Rect(cx - lr * .5f, y, lr + 4f, 18f), bannerRotulo, 13, AlfaTecido(dourado, conteudo), true);
                Fundo(new Rect(cx - lr * .5f - 48f, y + 9f, 36f, 1f), AlfaTecido(dourado, .7f * conteudo));
                Fundo(new Rect(cx + lr * .5f + 12f, y + 9f, 36f, 1f), AlfaTecido(dourado, .7f * conteudo));
                y += 24f;
            }

            TituloLetraALetraUI(new Rect(xTexto, y, larguraTexto, tamanhoNome * 1.3f), bannerNome, tamanhoNome, AlfaTecido(claro, conteudo), reduzir ? 9f : t - .5f, reduzir, true);
            y += tamanhoNome * 1.3f;
            if (descricao)
            {
                float e = reduzir ? 1f : SaidaSuave((t - .95f) / .6f);
                TextoTecido(new Rect(xTexto + 24f, y + 2f + (1f - e) * 8f, larguraTexto - 48f, 44f),
                    ResumoHUDClaroUI(bannerDescricao, larguraTexto - 48f, 44f, 16), 16, AlfaTecido(CremeFracoHUD, conteudo * e), false, TextAnchor.UpperCenter);
            }

            // Um brilho corre pelas bordas de tempos em tempos.
            if (!reduzir && t > 1.1f)
            {
                float ciclo = Mathf.Repeat(t - 1.1f, 3.2f) / 1.2f;
                if (ciclo < 1f)
                {
                    float bx = Mathf.Lerp(caixa.x + 10f, caixa.xMax - 10f, ciclo);
                    BrilhoAbertura(new Rect(bx - 44f, caixa.y - 6f, 88f, 12f), new Color(1f, .92f, .7f, .55f * conteudo));
                    BrilhoAbertura(new Rect(caixa.xMax - (bx - caixa.x) - 44f, caixa.yMax - 6f, 88f, 12f), new Color(1f, .92f, .7f, .4f * conteudo));
                }
            }
        }
    }
}

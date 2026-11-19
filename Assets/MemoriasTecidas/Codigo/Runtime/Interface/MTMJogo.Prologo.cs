using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Uma lenda que atravessa a tela no quadro da noite. "imagem" é a folha de animação (linha 0 no alto);
    // "linha" é a direção da corrida; de/para = x no HUD (1280 x 720); chao = y dos pés; altura = do desenho.
    [Serializable]
    public sealed class MTMCorredorPrologo
    {
        public string imagem;
        public int colunas = 1, linhas = 1, linha;
        public float de, para, chao, altura, inicio, duracao, fps = 10f;
    }

    [Serializable]
    public sealed class MTMMarcoPrologo
    {
        public string ano, nome;
    }

    [Serializable]
    public sealed class MTMQuadroPrologo
    {
        // "bandeira": título com a bandeira de Jaguaruana ao vento; "quadro": ilustração;
        // "linha": linha do tempo com os marcos; "lendas": a noite com as lendas correndo;
        // "aventura": o mapa com o caminho, o Neto e o botão para começar (sempre o último).
        public string tipo, imagem, rotulo, titulo, subtitulo, texto, retrato;
        // "foto": fotografia mostrada numa moldura à direita, por cima de "imagem" (o fundo).
        public string foto;
        // Câmera lenta sobre a arte: foco (0 a 1, a partir do canto de cima) e aproximação no começo e no fim do quadro.
        public float[] focoDe, focoPara;
        public float zoomDe, zoomPara;
        // "antiga": foto velha em tela cheia, com grão, poeira e a luz tremendo como filme antigo.
        public bool noite, fibras, passaros, antiga;
        public MTMMarcoPrologo[] marcos;
        public MTMCorredorPrologo[] corredores;
        // Lugares do mapa ligados por um fio, na ordem (ids de Interface/mapa_jaguaruana.json).
        public string[] rota;
        // Versão 1 ("preencher", "conter" ou "direita"). A abertura de cinema não usa.
        public string ajuste;
    }

    [Serializable]
    public sealed class MTMPrologo
    {
        public int versao;
        public MTMQuadroPrologo[] quadros;
    }

    // Abertura de cinema: a história de Jaguaruana em quadros de tela cheia, com câmera lenta sobre as artes,
    // a bandeira ao vento, a linha do tempo, as lendas da noite e o começo da aventura no mapa.
    // Depois dela vem a chegada: a tela clareia, a câmera desce sobre o Neto e o banner de abertura
    // apresenta o primeiro lugar (MTMJogo.Guia.cs).
    public sealed partial class MTMJogo
    {
        // Chave nova: quem viu a abertura antiga assiste à de cinema uma vez.
        private const string ChavePrologoVisto = ChaveInterface + "AberturaVista";
        private const string ArteBandeira = "MemoriasTecidas/Interface/BandeiraJaguaruana";
        private const float TrocaQuadroPrologo = .9f, EntradaPrologo = .9f, SaidaPrologo = .6f, FaixaCinema = 54f;
        private MTMPrologo prologo;
        private Texture2D[] artesPrologo, retratosPrologo, fotosPrologo;
        private Texture2D[][] corredoresPrologo;
        private Texture2D bandeiraJaguaruana, texDegradeAbertura, texBrilhoAbertura, texVinhetaAbertura, texAnelAbertura, texPlacaAbertura, texGraoAbertura;
        private int quadroPrologo, quadroAnteriorPrologo = -1;
        private float tempoQuadroPrologo, tempoAnteriorPrologo, tempoAberturaPrologo, saidaPrologo = -1f;
        private bool aberturaRevista;
        private GUIStyle estiloSerifAbertura;
        private readonly GUIContent conteudoAbertura = new GUIContent();

        private void PrepararPrologo()
        {
            prologo = null;
            try
            {
                var arquivo = Resources.Load<TextAsset>("MemoriasTecidas/prologo");
                if (arquivo == null)
                    return;
                var dados = JsonUtility.FromJson<MTMPrologo>(arquivo.text);
                if (dados == null || dados.quadros == null || dados.quadros.Length == 0)
                    throw new InvalidOperationException("prologo.json sem quadros.");
                foreach (var q in dados.quadros)
                    if (q == null || string.IsNullOrWhiteSpace(q.texto))
                        throw new InvalidOperationException("Todo quadro da abertura precisa de texto.");
                prologo = dados;
            }
            catch (Exception erro)
            {
                prologo = null;
                Debug.LogWarning("A abertura não foi carregada: " + erro.Message + " O jogo começa direto na cidade.", this);
            }
        }

        // As artes grandes só são carregadas quando a abertura vai passar.
        private void CarregarArtesPrologo()
        {
            if (prologo == null || artesPrologo != null)
                return;
            int n = prologo.quadros.Length;
            artesPrologo = new Texture2D[n];
            retratosPrologo = new Texture2D[n];
            fotosPrologo = new Texture2D[n];
            corredoresPrologo = new Texture2D[n][];
            for (int i = 0; i < n; i++)
            {
                var q = prologo.quadros[i];
                artesPrologo[i] = CarregarArteAbertura(q.imagem);
                retratosPrologo[i] = CarregarArteAbertura(q.retrato);
                fotosPrologo[i] = CarregarArteAbertura(q.foto);
                int c = q.corredores == null ? 0 : q.corredores.Length;
                corredoresPrologo[i] = new Texture2D[c];
                for (int k = 0; k < c; k++)
                    corredoresPrologo[i][k] = q.corredores[k] == null ? null : CarregarArteAbertura(q.corredores[k].imagem);
            }

            if (bandeiraJaguaruana == null)
                bandeiraJaguaruana = Resources.Load<Texture2D>(ArteBandeira);
        }

        private Texture2D CarregarArteAbertura(string caminho)
        {
            if (string.IsNullOrEmpty(caminho))
                return null;
            var arte = Resources.Load<Texture2D>(caminho);
            if (arte == null)
                Debug.LogWarning("Abertura: a arte '" + caminho + "' não está em Resources.", this);
            return arte;
        }

        // Chamado quando o jogador entra na jornada. "explicita" = escolheu Nova jornada.
        private void ComecoDaJornada(bool explicita)
        {
            // JornadaVazia() não serve aqui: visitar a Entrada já grava um marco.
            if (estado.Tem(EventoPrimeiraConversa))
            {
                // Jornada em andamento: só o nome do lugar, para situar quem volta.
                if (atual != null)
                    MostrarBannerRegiao(atual.dados.id, false);
                return;
            }

            bool jaViu = PlayerPrefs.GetInt(ChavePrologoVisto, 0) == 1;
            if (prologo != null && (explicita || !jaViu))
            {
                IniciarPrologo();
                return;
            }

            IniciarChegadaAventura();
        }

        private void IniciarPrologo()
        {
            CarregarArtesPrologo();
            quadroPrologo = 0;
            quadroAnteriorPrologo = -1;
            tempoQuadroPrologo = tempoAnteriorPrologo = tempoAberturaPrologo = 0f;
            saidaPrologo = -1f;
            painel = "prologo";
            quadroDaAcao = Time.frameCount;
            Parar();
            LimparFocoUI();
        }

        // Pela página "Sobre o jogo": assiste de novo e volta para ela no fim, sem mexer na jornada.
        private void ReverAbertura()
        {
            if (prologo == null)
                return;
            aberturaRevista = true;
            IniciarPrologo();
        }

        private void AvancarPrologo()
        {
            if (prologo != null && quadroPrologo + 1 < prologo.quadros.Length)
            {
                TocarSom("tear_fio", .5f, .04f);
                quadroAnteriorPrologo = quadroPrologo;
                tempoAnteriorPrologo = tempoQuadroPrologo;
                quadroPrologo++;
                tempoQuadroPrologo = 0f;
                return;
            }

            SairPrologo();
        }

        // O primeiro toque termina a entrada do texto; o seguinte passa o quadro.
        private void PedirAvancoPrologo()
        {
            if (tempoQuadroPrologo < 1.2f)
            {
                tempoQuadroPrologo = 1.2f;
                return;
            }

            AvancarPrologo();
        }

        // Escurece e encerra; pedir de novo durante o escuro sai na hora.
        private void SairPrologo()
        {
            if (saidaPrologo >= 0f)
            {
                EncerrarPrologo();
                return;
            }

            saidaPrologo = 0f;
            quadroDaAcao = Time.frameCount;
        }

        private void EncerrarPrologo()
        {
            bool revendo = aberturaRevista;
            aberturaRevista = false;
            saidaPrologo = -1f;
            quadroAnteriorPrologo = -1;
            // Solta as artes: as que outras telas não usam podem ser descarregadas.
            artesPrologo = retratosPrologo = fotosPrologo = null;
            corredoresPrologo = null;
            quadroDaAcao = Time.frameCount;
            Parar();
            LimparFocoUI();
            if (revendo)
            {
                painel = "sobre";
                return;
            }

            PlayerPrefs.SetInt(ChavePrologoVisto, 1);
            PlayerPrefs.Save();
            painel = "";
            IniciarChegadaAventura();
        }

        private static float DuracaoQuadroPrologo(MTMQuadroPrologo q)
        {
            if (q == null)
                return 8f;
            if (q.tipo == "aventura")
                return 10f;
            int palavras = 1;
            foreach (char c in q.texto ?? "")
                if (c == ' ')
                    palavras++;
            return Mathf.Clamp(3.5f + palavras * .28f, 8f, 12.5f);
        }

        private bool AvancoAutomaticoPrologo
        {
            // Com "reduzir movimento", a abertura espera o jogador em cada quadro.
            get { return apresentacao == null || !apresentacao.reduzirMovimento; }
        }

        private void TeclasPrologo()
        {
            Parar();
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            tempoQuadroPrologo += dt;
            tempoAberturaPrologo += dt;
            if (prologo == null)
            {
                EncerrarPrologo();
                return;
            }

            if (saidaPrologo >= 0f)
            {
                saidaPrologo += dt;
                if (saidaPrologo >= SaidaPrologo)
                    EncerrarPrologo();
                return;
            }

            if (quadroDaAcao == Time.frameCount)
                return;
            if (MTMEntrada.Apertou('e') || MTMEntrada.ConfirmouMenu())
            {
                quadroDaAcao = Time.frameCount;
                PedirAvancoPrologo();
                return;
            }

            var q = prologo.quadros[Mathf.Clamp(quadroPrologo, 0, prologo.quadros.Length - 1)];
            if (AvancoAutomaticoPrologo && q.tipo != "aventura" && tempoQuadroPrologo >= DuracaoQuadroPrologo(q))
                AvancarPrologo();
        }

        // ---------------------------------------------------------------- desenho da abertura

        private void PrologoUI()
        {
            if (prologo == null || prologo.quadros == null || prologo.quadros.Length == 0)
            {
                EncerrarPrologo();
                return;
            }

            PrepararTexturasAbertura();
            Rect tela = TelaInteiraUI();
            Fundo(tela, Color.black);
            int total = prologo.quadros.Length;
            int i = Mathf.Clamp(quadroPrologo, 0, total - 1);
            // Troca de quadro: o anterior se desfaz por cima do novo.
            float troca = quadroAnteriorPrologo >= 0 ? Mathf.Clamp01(tempoQuadroPrologo / TrocaQuadroPrologo) : 1f;
            DesenharQuadroPrologo(i, tempoQuadroPrologo, 1f, tela);
            if (troca < 1f)
                DesenharQuadroPrologo(quadroAnteriorPrologo, tempoAnteriorPrologo + tempoQuadroPrologo, 1f - Suave(troca), tela);
            if (troca < 1f && quadroAnteriorPrologo >= 0)
                LegendaPrologo(prologo.quadros[quadroAnteriorPrologo], 99f, 1f - Mathf.Clamp01(tempoQuadroPrologo / .35f));
            LegendaPrologo(prologo.quadros[i], tempoQuadroPrologo, 1f);

            float faixa = FaixaCinema * SaidaSuave(tempoAberturaPrologo / .9f);
            FaixasCinemaUI(tela, faixa);
            ControlesPrologo(i, total, faixa);
            float escuro = Mathf.Max(1f - Mathf.Clamp01(tempoAberturaPrologo / EntradaPrologo), saidaPrologo >= 0f ? Mathf.Clamp01(saidaPrologo / SaidaPrologo) : 0f);
            if (escuro > .001f)
                Fundo(tela, new Color(0f, 0f, 0f, escuro));
        }

        private void DesenharQuadroPrologo(int indice, float t, float alfa, Rect tela)
        {
            if (alfa <= .001f || indice < 0 || indice >= prologo.quadros.Length)
                return;
            var q = prologo.quadros[indice];
            var arte = artesPrologo != null && indice < artesPrologo.Length ? artesPrologo[indice] : null;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            Rect recorte = new Rect(0f, 0f, 1f, 1f);
            if (arte != null)
            {
                recorte = CameraDoQuadro(q, arte, tela, t);
                // A noite tinge a arte de azul; no título a cidade fica mais escura para a bandeira aparecer.
                Color tom = q.noite ? new Color(.28f, .33f, .56f, alfa)
                    : q.tipo == "bandeira" ? new Color(.70f, .66f, .62f, alfa)
                    : q.tipo == "aventura" ? new Color(.84f, .80f, .76f, alfa) : new Color(1f, 1f, 1f, alfa);
                if (q.antiga)
                {
                    // Foto velha: um pouco mais escura e quente, com a luz do projetor tremendo.
                    float treme = reduzir ? 0f : .06f * Acaso(QuadroDoFilme() * 5 + 3);
                    tom = new Color(.92f - treme, .88f - treme, .80f - treme, alfa);
                }

                DesenharRecorte(tela, arte, recorte, tom);
            }
            else
                Fundo(tela, AlfaTecido(profundoUI, alfa));

            switch (q.tipo)
            {
                case "bandeira":
                    TituloBandeiraPrologo(q, t, alfa, tela, reduzir);
                    break;
                case "linha":
                    LinhaDoTempoPrologo(q, t, alfa, tela);
                    break;
                case "lendas":
                    NoiteDasLendasPrologo(q, indice, t, alfa, tela, reduzir);
                    break;
                case "aventura":
                    AventuraPrologo(q, indice, t, alfa, tela, recorte, arte != null, reduzir);
                    break;
            }

            var foto = fotosPrologo != null && indice < fotosPrologo.Length ? fotosPrologo[indice] : null;
            if (foto != null)
                FotoEmolduradaUI(foto, t, alfa, reduzir);
            if (q.antiga)
                FilmeAntigoUI(tela, alfa, reduzir);
            if (q.fibras && !reduzir)
                FibrasAbertura(tela, tempoAberturaPrologo, alfa, new Color(1f, .97f, .9f, .75f), 28, false);
            // Escuro embaixo para a legenda e bordas escuras de cinema.
            DegradeAbertura(new Rect(tela.x, 330f, tela.width, tela.yMax - 330f), new Color(.02f, .015f, .01f, .9f * alfa), false);
            Color antiga = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .75f * alfa);
            GUI.DrawTexture(tela, texVinhetaAbertura);
            GUI.color = antiga;
        }

        // O filme anda a 12 quadros por segundo: grão, poeira e riscos mudam nesse ritmo.
        private int QuadroDoFilme()
        {
            return Mathf.FloorToInt(tempoAberturaPrologo * 12f);
        }

        // Filme antigo por cima das fotos velhas: grão que pisca, poeira e um risco de vez em quando.
        private void FilmeAntigoUI(Rect tela, float alfa, bool reduzir)
        {
            int quadro = reduzir ? 0 : QuadroDoFilme();
            Color antiga = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, .55f * alfa);
            GUI.DrawTextureWithTexCoords(tela, texGraoAbertura, new Rect(Acaso(quadro * 7 + 1), Acaso(quadro * 7 + 2), tela.width / 160f, tela.height / 160f));
            GUI.color = antiga;
            if (reduzir)
                return;
            for (int k = 0; k < 4; k++)
            {
                float a = Acaso(quadro * 13 + k * 3), b = Acaso(quadro * 13 + k * 3 + 1), c = Acaso(quadro * 13 + k * 3 + 2);
                if (c < .5f)
                    continue;
                float tam = 1.6f + 3.4f * c;
                DiscoHUD(new Rect(tela.x + a * tela.width, tela.y + b * tela.height, tam, tam * (.6f + .8f * a)), new Color(.12f, .09f, .06f, .5f * alfa));
            }

            if (Acaso(quadro * 31) > .8f)
                Fundo(new Rect(tela.x + Acaso(quadro * 31 + 5) * tela.width, tela.y, 1.3f, tela.height), new Color(1f, .97f, .9f, .16f * alfa));
        }

        // Fotografia impressa numa moldura branca à direita, com sombra; dentro dela a câmera se aproxima devagar.
        private void FotoEmolduradaUI(Texture2D foto, float t, float alfa, bool reduzir)
        {
            float e = reduzir ? 1f : SaidaSuave((t - .25f) / 1.1f);
            float a = alfa * e;
            if (a <= .001f)
                return;
            const float alto = 448f;
            float largo = alto * foto.width / Mathf.Max(1f, foto.height);
            Rect imagem = new Rect(1004f - largo * .5f, 106f + (1f - e) * 26f, largo, alto);
            Rect papel = new Rect(imagem.x - 12f, imagem.y - 12f, imagem.width + 24f, imagem.height + 40f);
            BrilhoAbertura(new Rect(papel.x - 46f, papel.y - 6f, papel.width + 92f, papel.height + 84f), new Color(0f, 0f, 0f, .6f * a));
            Fundo(papel, new Color(.95f, .93f, .88f, a));
            float z = reduzir ? 1f : Mathf.Lerp(1f, 1.07f, Suave(t / 12f));
            float w = 1f / z;
            // Aproxima em direção à torre, um pouco acima do meio da foto.
            Rect uv = new Rect((1f - w) * .5f, (1f - w) * .62f, w, w);
            Color antiga = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTextureWithTexCoords(imagem, foto, uv);
            GUI.color = antiga;
        }

        // Quadro de título: a cidade ao fundo, a bandeira no mastro e o nome subindo letra a letra.
        private void TituloBandeiraPrologo(MTMQuadroPrologo q, float t, float alfa, Rect tela, bool reduzir)
        {
            if (q.passaros && !reduzir)
                PassarosAbertura(tela, tempoAberturaPrologo, alfa);
            // Sombra macia atrás do título e do texto, para ler sobre a cidade.
            BrilhoAbertura(new Rect(540f, 60f, 820f, 560f), new Color(0f, 0f, 0f, .7f * alfa));
            float entra = reduzir ? 1f : SaidaSuave((t - .15f) / 1.3f);
            BandeiraAoVentoUI(new Vector2(178f, 104f + (1f - entra) * 36f), 430f, reduzir ? 1.3f : tempoAberturaPrologo, alfa * entra, reduzir ? .25f : 1f, 600f);
            const float x = 654f;
            float e1 = reduzir ? 1f : SaidaSuave((t - .55f) / .7f);
            TextoSombraHUD(new Rect(x + 2f, 184f + (1f - e1) * 12f, 560f, 22f), (q.rotulo ?? "").ToUpperInvariant(), 15, AlfaTecido(ouro, alfa * e1), true, TextAnchor.UpperLeft);
            TituloLetraALetraUI(new Rect(x - 4f, 206f, 620f, 112f), q.titulo ?? "", 88, AlfaTecido(textoSuperficieUI, alfa), t - .8f, reduzir, false);
            float e2 = reduzir ? 1f : SaidaSuave((t - 1.7f) / .8f);
            TextoSerifUI(new Rect(x + 2f, 320f + (1f - e2) * 10f, 560f, 48f), q.subtitulo ?? "", 34, new Color(0f, 0f, 0f, .55f * alfa * e2), TextAnchor.UpperLeft);
            TextoSerifUI(new Rect(x, 318f + (1f - e2) * 10f, 560f, 48f), q.subtitulo ?? "", 34, AlfaTecido(ouro, alfa * e2), TextAnchor.UpperLeft);
            float fio = reduzir ? 1f : SaidaSuave((t - 2.1f) / 1.1f);
            if (fio > 0f)
            {
                Fundo(new Rect(x + 2f, 376f, 400f * fio, 2f), AlfaTecido(ouro, .85f * alfa));
                LosangoCompacta(new Rect(x + 2f + 400f * fio - 6f, 371f, 12f, 12f), AlfaTecido(ouro, alfa));
            }
        }

        // Linha do tempo: um fio dourado corre pela tela e cada data estoura quando ele chega.
        private void LinhaDoTempoPrologo(MTMQuadroPrologo q, float t, float alfa, Rect tela)
        {
            DegradeAbertura(new Rect(tela.x, tela.y, tela.width, 370f - tela.y), new Color(.02f, .015f, .01f, .85f * alfa), true);
            var marcos = q.marcos;
            if (marcos == null || marcos.Length == 0)
                return;
            const float y = 232f, x0 = 190f, x1 = 1090f;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            float prog = reduzir ? 1f : Suave((t - .5f) / 3.4f);
            Fundo(new Rect(x0, y - .5f, x1 - x0, 1f), AlfaTecido(textoSuperficieUI, .2f * alfa));
            Fundo(new Rect(x0, y - 1.5f, (x1 - x0) * prog, 3f), AlfaTecido(ouro, .95f * alfa));
            if (prog > 0f && prog < 1f)
                BrilhoAbertura(new Rect(x0 + (x1 - x0) * prog - 18f, y - 18f, 36f, 36f), new Color(1f, .9f, .6f, .85f * alfa));
            for (int k = 0; k < marcos.Length; k++)
            {
                var m = marcos[k];
                if (m == null)
                    continue;
                float f = (k + .5f) / marcos.Length;
                float xk = Mathf.Lerp(x0, x1, f);
                float e = reduzir ? 1f : Salto((prog - f) * 4.5f);
                float a = Mathf.Clamp01((prog - f) * 6f + (reduzir ? 1f : 0f)) * alfa;
                if (a <= .001f)
                    continue;
                float tam = 22f * e;
                BrilhoAbertura(new Rect(xk - 44f, y - 44f, 88f, 88f), new Color(1f, .85f, .5f, .35f * a));
                LosangoCompacta(new Rect(xk - tam * .5f, y - tam * .5f, tam, tam), AlfaTecido(ouro, a));
                LosangoCompacta(new Rect(xk - tam * .2f, y - tam * .2f, tam * .4f, tam * .4f), new Color(.18f, .12f, .08f, a));
                TextoSerifUI(new Rect(xk - 160f + 2f, y - 98f + (1f - e) * 14f + 2f, 320f, 72f), m.ano ?? "", 58, new Color(0f, 0f, 0f, .5f * a), TextAnchor.LowerCenter);
                TextoSerifUI(new Rect(xk - 160f, y - 98f + (1f - e) * 14f, 320f, 72f), m.ano ?? "", 58, AlfaTecido(ouro, a), TextAnchor.LowerCenter);
                TextoSombraHUD(new Rect(xk - 150f, y + 22f, 300f, 26f), m.nome ?? "", 19, AlfaTecido(CremeHUD, a), true, TextAnchor.UpperCenter);
            }
        }

        // A noite do sertão (a lua e a serra estão na arte): névoa e vaga-lumes; as lendas atravessam o caminho.
        private void NoiteDasLendasPrologo(MTMQuadroPrologo q, int indice, float t, float alfa, Rect tela, bool reduzir)
        {
            BrilhoAbertura(new Rect(tela.x - 120f, 396f, tela.width + 240f, 110f), new Color(.55f, .62f, .92f, .12f * alfa));
            if (!reduzir)
                FibrasAbertura(tela, tempoAberturaPrologo, alfa, new Color(.95f, .95f, .55f, .9f), 18, true);
            var corredores = q.corredores;
            var artes = corredoresPrologo != null && indice < corredoresPrologo.Length ? corredoresPrologo[indice] : null;
            if (corredores == null || artes == null)
                return;
            for (int k = 0; k < corredores.Length && k < artes.Length; k++)
                CorredorPrologo(corredores[k], artes[k], t, alfa, reduzir);
        }

        private void CorredorPrologo(MTMCorredorPrologo c, Texture2D arte, float t, float alfa, bool reduzir)
        {
            if (c == null || arte == null)
                return;
            float duracao = c.duracao > 0f ? c.duracao : 4f;
            float p = (t - c.inicio) / duracao;
            if (p < 0f || p > 1f)
                return;
            int colunas = Mathf.Max(1, c.colunas), linhas = Mathf.Max(1, c.linhas);
            float altura = c.altura > 0f ? c.altura : 200f;
            float largura = altura * (arte.width / (float)colunas) / (arte.height / (float)linhas);
            float x = Mathf.Lerp(c.de, c.para, p);
            int quadro = reduzir ? 0 : (int)((t - c.inicio) * (c.fps > 0f ? c.fps : 10f)) % colunas;
            int linha = Mathf.Clamp(c.linha, 0, linhas - 1);
            Rect uv = new Rect(quadro / (float)colunas, 1f - (linha + 1f) / linhas, 1f / colunas, 1f / linhas);
            float a = alfa * Mathf.Clamp01(p * 6f) * Mathf.Clamp01((1f - p) * 6f);
            Rect r = new Rect(x - largura * .5f, c.chao - altura, largura, altura);
            BrilhoAbertura(new Rect(x - largura * .42f, c.chao - 12f, largura * .84f, 22f), new Color(0f, 0f, 0f, .5f * a));
            Color antiga = GUI.color;
            // Contorno de luar (a cópia clara um pouco acima, do lado da lua) e o corpo escurecido pela noite.
            GUI.color = new Color(.62f, .72f, 1f, .55f * a);
            GUI.DrawTextureWithTexCoords(new Rect(r.x + 2f, r.y - 2.5f, r.width, r.height), arte, uv);
            GUI.color = new Color(.42f, .46f, .68f, a);
            GUI.DrawTextureWithTexCoords(r, arte, uv);
            GUI.color = antiga;
        }

        // O começo: o mapa se abre a partir da Entrada, um fio costura o caminho e o Neto aparece.
        private void AventuraPrologo(MTMQuadroPrologo q, int indice, float t, float alfa, Rect tela, Rect recorte, bool temArte, bool reduzir)
        {
            if (temArte && q.rota != null && q.rota.Length > 1)
                RotaNoMapaPrologo(q.rota, reduzir ? 99f : t, alfa, tela, recorte);
            var retrato = retratosPrologo != null && indice < retratosPrologo.Length ? retratosPrologo[indice] : null;
            if (retrato == null)
                return;
            float e = reduzir ? 1f : SaidaSuave((t - .6f) / 1.1f);
            const float alto = 440f;
            float largo = alto * retrato.width / Mathf.Max(1f, retrato.height);
            Rect r = new Rect(1214f - largo + (1f - e) * 70f, 668f - alto, largo, alto);
            BrilhoAbertura(new Rect(r.x - 70f, r.y + 30f, r.width + 140f, r.height + 40f), new Color(1f, .8f, .45f, .32f * alfa * e));
            Color antiga = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alfa * e);
            GUI.DrawTexture(r, retrato, ScaleMode.ScaleToFit, true);
            GUI.color = antiga;
        }

        private void RotaNoMapaPrologo(string[] rota, float t, float alfa, Rect tela, Rect recorte)
        {
            var pontos = new Vector2[rota.Length];
            int n = 0;
            foreach (var id in rota)
            {
                MTMNoMapaGuia no;
                if (id != null && locaisMapaGuia.TryGetValue(id, out no))
                    pontos[n++] = PontoNoRecorte(tela, recorte, no.x, no.y);
            }

            if (n < 2)
                return;
            float total = 0f;
            for (int i = 1; i < n; i++)
                total += Vector2.Distance(pontos[i - 1], pontos[i]);
            float feito = total * Suave((t - 1.4f) / 5.5f);
            // Pontos de costura ao longo do caminho, até onde o fio já chegou.
            const float passo = 9f;
            float proximo = 0f, inicio = 0f;
            Vector2 ponta = pontos[0];
            for (int i = 1; i < n; i++)
            {
                Vector2 a = pontos[i - 1], b = pontos[i];
                float seg = Vector2.Distance(a, b);
                while (proximo <= inicio + seg && proximo <= feito)
                {
                    Vector2 p = Vector2.Lerp(a, b, (proximo - inicio) / Mathf.Max(.001f, seg));
                    DiscoHUD(new Rect(p.x - 3.6f, p.y - 3.6f, 7.2f, 7.2f), new Color(.22f, .12f, .05f, .85f * alfa));
                    DiscoHUD(new Rect(p.x - 2.4f, p.y - 2.4f, 4.8f, 4.8f), AlfaTecido(ouro, alfa));
                    ponta = p;
                    proximo += passo;
                }

                inicio += seg;
            }

            if (feito > 0f && feito < total)
                BrilhoAbertura(new Rect(ponta.x - 20f, ponta.y - 20f, 40f, 40f), new Color(1f, .9f, .55f, .9f * alfa));
            // Um nó em cada lugar quando o fio chega; no primeiro (onde a aventura começa) dois anéis pulsam.
            float andado = 0f;
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                    andado += Vector2.Distance(pontos[i - 1], pontos[i]);
                float chegou = i == 0 ? (t - 1f) * 2f : (feito - andado) / 40f;
                float e = Salto(chegou), a = Mathf.Clamp01(chegou * 3f) * alfa;
                if (a <= .001f)
                    continue;
                Vector2 p = pontos[i];
                if (i == 0)
                    for (int k = 0; k < 2; k++)
                    {
                        float fase = Mathf.Repeat(tempoAberturaPrologo / 1.6f + k * .5f, 1f);
                        float raio = Mathf.Lerp(14f, 64f, fase);
                        AnelAbertura(p, raio, AlfaTecido(ouro, (1f - fase) * .9f * a));
                    }

                float tam = 22f * e;
                BrilhoAbertura(new Rect(p.x - 36f, p.y - 36f, 72f, 72f), new Color(1f, .85f, .45f, .5f * a));
                LosangoCompacta(new Rect(p.x - tam * .5f, p.y - tam * .5f, tam, tam), AlfaTecido(ouro, a));
                LosangoCompacta(new Rect(p.x - tam * .2f, p.y - tam * .2f, tam * .4f, tam * .4f), new Color(.2f, .12f, .06f, a));
            }
        }

        // Legenda no terço de baixo: rótulo, título e texto entram em sequência, subindo de leve.
        private void LegendaPrologo(MTMQuadroPrologo q, float t, float alfa)
        {
            if (q == null || alfa <= .001f)
                return;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            // No quadro de título o nome grande já está na tela: a legenda é só o texto, embaixo dele, à direita.
            bool soTexto = q.tipo == "bandeira";
            // Com uma foto emoldurada à direita, a legenda fica mais estreita para não passar por cima dela.
            bool comFoto = !string.IsNullOrEmpty(q.foto);
            float x = soTexto ? 656f : 96f, largura = soTexto ? 540f : comFoto ? 600f : 760f;
            float y = soTexto ? 402f : 470f;
            if (!soTexto)
            {
                // Placa escura atrás da legenda (borda de cima e lado direito macios): lê bem até sobre o mapa claro.
                Rect tela = TelaInteiraUI();
                Color antiga = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, .62f * alfa);
                GUI.DrawTexture(new Rect(tela.x, 396f, (comFoto ? 800f : 1020f) - tela.x, tela.yMax - 396f), texPlacaAbertura);
                GUI.color = antiga;
                float e1 = reduzir ? 1f : SaidaSuave((t - .35f) / .6f);
                if (!string.IsNullOrEmpty(q.rotulo))
                {
                    LosangoCompacta(new Rect(x, y + 5f + (1f - e1) * 10f, 10f, 10f), AlfaTecido(ouro, alfa * e1));
                    TextoSombraHUD(new Rect(x + 20f, y + (1f - e1) * 10f, largura, 20f), q.rotulo.ToUpperInvariant(), 14, AlfaTecido(ouro, alfa * e1), true, TextAnchor.UpperLeft);
                }

                float e2 = reduzir ? 1f : SaidaSuave((t - .5f) / .7f);
                Rect titulo = new Rect(x - 2f, y + 22f + (1f - e2) * 14f, largura + 200f, 56f);
                TextoSerifUI(new Rect(titulo.x + 2f, titulo.y + 2f, titulo.width, titulo.height), q.titulo ?? "", 42, new Color(0f, 0f, 0f, .55f * alfa * e2), TextAnchor.UpperLeft);
                TextoSerifUI(titulo, q.titulo ?? "", 42, AlfaTecido(textoSuperficieUI, alfa * e2), TextAnchor.UpperLeft);
                y += 82f;
            }

            float e3 = reduzir ? 1f : SaidaSuave((t - (soTexto ? 2.4f : .8f)) / .8f);
            TextoSombraHUD(new Rect(x, y + (1f - e3) * 12f, largura, 96f), q.texto ?? "", 21, AlfaTecido(CremeHUD, alfa * e3), false, TextAnchor.UpperLeft);
        }

        // Fio de progresso e teclas nas faixas de cinema: um nó por quadro; o trecho atual enche com o tempo.
        private void ControlesPrologo(int indice, int total, float faixa)
        {
            float a = Mathf.Clamp01(faixa / FaixaCinema);
            if (a <= .01f)
                return;
            const float x0 = 96f, x1 = 820f, y = 693f;
            var q = prologo.quadros[indice];
            bool ultimo = indice + 1 >= total;
            float parcial = ultimo || !AvancoAutomaticoPrologo ? 0f : Mathf.Clamp01(tempoQuadroPrologo / DuracaoQuadroPrologo(q));
            float passo = total > 1 ? (x1 - x0) / (total - 1) : 0f;
            Fundo(new Rect(x0, y - .5f, x1 - x0, 1f), AlfaTecido(textoSuperficieUI, .25f * a));
            Fundo(new Rect(x0, y - 1f, passo * (indice + parcial), 2f), AlfaTecido(ouro, .9f * a));
            for (int k = 0; k < total; k++)
            {
                float tam = k == indice ? 12f + 2f * Mathf.Sin(tempoAberturaPrologo * 4f) : 9f;
                LosangoCompacta(new Rect(x0 + passo * k - tam * .5f, y - tam * .5f, tam, tam), k <= indice ? AlfaTecido(ouro, a) : AlfaTecido(textoSuperficieUI, .4f * a));
            }

            if (DicaAberturaUI(new Rect(1030f, 13f, 220f, 28f), "Esc", "Pular abertura", a, "abertura:pular") && quadroDaAcao != Time.frameCount)
            {
                quadroDaAcao = Time.frameCount;
                SairPrologo();
            }

            if (ultimo)
            {
                if (BotaoTecido(new Rect(990f, 672f, 260f, 40f), "Começar a aventura  →", true) && quadroDaAcao != Time.frameCount)
                {
                    quadroDaAcao = Time.frameCount;
                    SairPrologo();
                }
            }
            else if (DicaAberturaUI(new Rect(1030f, 679f, 220f, 28f), "E", "Continuar", a, "abertura:continuar") && quadroDaAcao != Time.frameCount)
            {
                quadroDaAcao = Time.frameCount;
                PedirAvancoPrologo();
            }
        }

        // Tecla + rótulo alinhados à direita de "r", clicáveis.
        private bool DicaAberturaUI(Rect r, string tecla, string rotulo, float alfa, string chave)
        {
            string[] teclas = { tecla }, rotulos = { rotulo };
            float largura = DicasHUD(0f, 0f, teclas, rotulos, true);
            float x = r.xMax - largura;
            DicasHUD(x, r.y + 2f, teclas, rotulos, false, alfa);
            return AcaoTecido(new Rect(x - 6f, r.y - 2f, largura + 12f, r.height + 4f), chave);
        }

        private void FaixasCinemaUI(Rect tela, float faixa)
        {
            if (faixa <= .5f)
                return;
            var preto = new Color(.012f, .009f, .007f, 1f);
            Fundo(new Rect(tela.x, tela.y, tela.width, faixa - tela.y), preto);
            Fundo(new Rect(tela.x, 720f - faixa, tela.width, tela.yMax - (720f - faixa)), preto);
        }

        // ---------------------------------------------------------------- peças de desenho

        // Bandeira de Jaguaruana presa no mastro, ondulando em faixas verticais (cada faixa sobe, desce e
        // ganha luz ou sombra conforme a onda). "topo" = ponta do mastro; "forca" 0-1 = quanto o vento mexe;
        // "mastro" = altura do mastro desenhado.
        private void BandeiraAoVentoUI(Vector2 topo, float largura, float tempo, float alfa, float forca, float mastro)
        {
            if (alfa <= .001f)
                return;
            if (bandeiraJaguaruana == null)
                bandeiraJaguaruana = Resources.Load<Texture2D>(ArteBandeira);
            float espessura = Mathf.Max(3f, largura * .022f);
            if (mastro > 0f)
            {
                Fundo(new Rect(topo.x - espessura * .5f + 3f, topo.y + 5f, espessura, mastro), new Color(0f, 0f, 0f, .28f * alfa));
                Fundo(new Rect(topo.x - espessura * .5f, topo.y, espessura, mastro), new Color(.33f, .23f, .15f, alfa));
                Fundo(new Rect(topo.x - espessura * .5f + espessura * .55f, topo.y, espessura * .25f, mastro), new Color(.60f, .46f, .31f, alfa));
                float ponta = espessura * 2.4f;
                DiscoHUD(new Rect(topo.x - ponta * .5f, topo.y - ponta * .8f, ponta, ponta), new Color(.64f, .48f, .2f, alfa));
                DiscoHUD(new Rect(topo.x - ponta * .3f, topo.y - ponta * .66f, ponta * .42f, ponta * .42f), new Color(1f, .9f, .62f, alfa));
            }

            if (bandeiraJaguaruana == null)
                return;
            float altura = largura * .7f;
            float x0 = topo.x + espessura * .5f, y0 = topo.y + espessura * 1.2f;
            const int faixas = 44;
            float amplitude = largura * .05f * forca;
            Color antiga = GUI.color;
            // Primeiro a sombra do pano (deslocada), depois o pano.
            for (int passada = 0; passada < 2; passada++)
            {
                float x = x0;
                for (int i = 0; i < faixas; i++)
                {
                    float u0 = i / (float)faixas, u1 = (i + 1f) / faixas, um = (u0 + u1) * .5f;
                    // Perto do mastro o pano quase não mexe; a onda corre para a ponta solta.
                    float preso = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(um * 2.2f));
                    float fase = um * 8.5f - tempo * 4.6f, fase2 = fase * 1.7f + 1.3f + tempo * 1.1f;
                    float onda = Mathf.Sin(fase) + .35f * Mathf.Sin(fase2);
                    float inclina = Mathf.Cos(fase) + .6f * Mathf.Cos(fase2);
                    float largFaixa = largura / faixas * (1f - .04f * forca * preso * inclina * inclina);
                    float alt = altura * (1f - .05f * forca * preso * (.5f + .5f * Mathf.Sin(fase + .8f)));
                    float y = y0 + onda * amplitude * preso + (altura - alt) * .5f + um * um * altura * .05f * forca;
                    Rect r = new Rect(x, y, largFaixa + .75f, alt);
                    x += largFaixa;
                    Rect uv = new Rect(u0, 0f, u1 - u0, 1f);
                    if (passada == 0)
                    {
                        GUI.color = new Color(0f, 0f, 0f, .22f * alfa);
                        GUI.DrawTextureWithTexCoords(new Rect(r.x + largura * .024f, r.y + largura * .032f, r.width, r.height), bandeiraJaguaruana, uv);
                        continue;
                    }

                    float luz = .78f + .22f * (.5f + .5f * Mathf.Clamp(-inclina * preso, -1f, 1f));
                    GUI.color = new Color(luz, luz, luz, alfa);
                    GUI.DrawTextureWithTexCoords(r, bandeiraJaguaruana, uv);
                    float brilho = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(fase + 2.1f)), 6f) * .16f * preso * forca;
                    if (brilho > .005f)
                    {
                        GUI.color = new Color(1f, 1f, .95f, brilho * alfa);
                        GUI.DrawTexture(r, Texture2D.whiteTexture);
                    }
                }
            }

            GUI.color = antiga;
        }

        // Fiapos de algodão subindo devagar (ou vaga-lumes piscando, à noite).
        private void FibrasAbertura(Rect tela, float t, float alfa, Color cor, int quantidade, bool vagalumes)
        {
            for (int i = 0; i < quantidade; i++)
            {
                float a = Acaso(i * 3 + 1), b = Acaso(i * 3 + 2), c = Acaso(i * 3 + 3);
                float velocidade = Mathf.Lerp(14f, 40f, b) * (vagalumes ? .45f : 1f);
                float y = tela.yMax + 20f - Mathf.Repeat(t * velocidade + c * tela.height * 1.3f, tela.height + 40f);
                float x = tela.x + Mathf.Repeat(a * tela.width + Mathf.Sin(t * (.4f + b) + c * 6.28f) * 28f + t * (vagalumes ? 3f : 9f), tela.width);
                float tam = Mathf.Lerp(2.5f, 6.5f, c) * (vagalumes ? 1.4f : 1f);
                float vida = Mathf.Clamp01((tela.yMax - y) / 120f) * Mathf.Clamp01((y - tela.y) / 120f);
                float pisca = vagalumes ? Mathf.Max(0f, Mathf.Sin(t * (1.4f + 2f * a) + c * 9f)) : 1f;
                BrilhoAbertura(new Rect(x - tam, y - tam, tam * 2f, tam * 2f), AlfaTecido(cor, cor.a * alfa * vida * pisca));
            }
        }

        // Pássaros cruzando o céu do título: duas asas finas batendo.
        private void PassarosAbertura(Rect tela, float t, float alfa)
        {
            var cor = new Color(.13f, .10f, .10f, .6f * alfa);
            for (int i = 0; i < 5; i++)
            {
                float a = Acaso(40 + i), b = Acaso(60 + i);
                float velocidade = 34f + 26f * a;
                float x = tela.x - 60f + Mathf.Repeat(t * velocidade + b * (tela.width + 120f), tela.width + 120f);
                float y = 86f + 70f * a + Mathf.Sin(t * .8f + b * 6f) * 8f;
                float asa = 8f + 5f * b;
                float bate = Mathf.Sin(t * (7f + 3f * a) + b * 10f);
                AveAbertura(new Vector2(x, y), asa, 18f + 22f * bate, cor);
            }
        }

        private void AveAbertura(Vector2 p, float asa, float angulo, Color cor)
        {
            Matrix4x4 antes = GUI.matrix;
            // RotateAroundPivot espera o pivô na tela; o HUD está escalado.
            Vector2 pivo = GUI.matrix.MultiplyPoint3x4(p);
            GUIUtility.RotateAroundPivot(angulo, pivo);
            Fundo(new Rect(p.x - asa, p.y - .9f, asa, 1.8f), cor);
            GUI.matrix = antes;
            GUIUtility.RotateAroundPivot(-angulo, pivo);
            Fundo(new Rect(p.x, p.y - .9f, asa, 1.8f), cor);
            GUI.matrix = antes;
        }

        // Título letra a letra: cada uma sobe e aparece um pouco depois da anterior.
        private void TituloLetraALetraUI(Rect r, string texto, int tamanho, Color cor, float t, bool reduzir, bool centralizar)
        {
            if (string.IsNullOrEmpty(texto) || cor.a <= .001f)
                return;
            var estilo = EstiloSerifAbertura(tamanho, TextAnchor.UpperLeft);
            float x = r.x;
            if (centralizar)
            {
                conteudoAbertura.text = texto;
                x = r.center.x - estilo.CalcSize(conteudoAbertura).x * .5f;
            }

            for (int i = 0; i < texto.Length; i++)
            {
                conteudoAbertura.text = texto[i].ToString();
                float w = estilo.CalcSize(conteudoAbertura).x;
                float e = reduzir ? Mathf.Clamp01(t / .5f) : SaidaSuave((t - i * .055f) / .55f);
                if (e > .001f)
                {
                    float y = r.y + (reduzir ? 0f : (1f - e) * tamanho * .3f);
                    CorTextoUI(estilo, new Color(0f, 0f, 0f, .5f * cor.a * e));
                    GUI.Label(new Rect(x + 2f, y + 3f, w + 6f, r.height), conteudoAbertura, estilo);
                    CorTextoUI(estilo, AlfaTecido(cor, cor.a * e));
                    GUI.Label(new Rect(x, y, w + 6f, r.height), conteudoAbertura, estilo);
                }

                x += w;
            }
        }

        private GUIStyle EstiloSerifAbertura(int tamanho, TextAnchor alinhamento)
        {
            if (estiloSerifAbertura == null)
                estiloSerifAbertura = new GUIStyle(GUI.skin.label)
                {
                    richText = false,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    clipping = TextClipping.Overflow
                };
            if (fonteTituloUI != null)
                estiloSerifAbertura.font = fonteTituloUI;
            estiloSerifAbertura.fontSize = tamanho;
            estiloSerifAbertura.fontStyle = FontStyle.Normal;
            estiloSerifAbertura.alignment = alinhamento;
            estiloSerifAbertura.wordWrap = false;
            return estiloSerifAbertura;
        }

        private void TextoSerifUI(Rect r, string texto, int tamanho, Color cor, TextAnchor alinhamento)
        {
            if (string.IsNullOrEmpty(texto) || cor.a <= .001f)
                return;
            var estilo = EstiloSerifAbertura(tamanho, alinhamento);
            CorTextoUI(estilo, cor);
            GUI.Label(r, texto, estilo);
        }

        private float LarguraSerifUI(string texto, int tamanho)
        {
            conteudoAbertura.text = texto ?? "";
            return EstiloSerifAbertura(tamanho, TextAnchor.UpperLeft).CalcSize(conteudoAbertura).x;
        }

        private void PrepararTexturasAbertura()
        {
            if (texBrilhoAbertura != null)
                return;
            // Brilho redondo e macio: partículas, lua, halos e sombras.
            const int n = 64;
            texBrilhoAbertura = NovaTexturaHUD("Abertura_brilho", n, n);
            var p = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n * .5f, n * .5f)) / (n * .5f);
                    float a = Mathf.Clamp01(1f - d);
                    p[y * n + x] = new Color(1f, 1f, 1f, a * a);
                }

            texBrilhoAbertura.SetPixels(p);
            texBrilhoAbertura.Apply(false, true);

            // Degradê: transparente em cima, cheio embaixo (a linha 0 da textura fica embaixo).
            texDegradeAbertura = NovaTexturaHUD("Abertura_degrade", 1, 128);
            p = new Color[128];
            for (int y = 0; y < 128; y++)
                p[y] = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, 1f - y / 127f));
            texDegradeAbertura.SetPixels(p);
            texDegradeAbertura.Apply(false, true);

            // Vinheta: bordas escuras, centro limpo.
            const int vw = 160, vh = 90;
            texVinhetaAbertura = NovaTexturaHUD("Abertura_vinheta", vw, vh);
            p = new Color[vw * vh];
            for (int y = 0; y < vh; y++)
                for (int x = 0; x < vw; x++)
                {
                    float dx = (x + .5f) / vw * 2f - 1f, dy = (y + .5f) / vh * 2f - 1f;
                    p[y * vw + x] = new Color(1f, 1f, 1f, Mathf.SmoothStep(.55f, 1.35f, Mathf.Sqrt(dx * dx * .9f + dy * dy * 1.1f)));
                }

            texVinhetaAbertura.SetPixels(p);
            texVinhetaAbertura.Apply(false, true);

            // Anel fino (pulso no começo do caminho).
            texAnelAbertura = NovaTexturaHUD("Abertura_anel", n, n);
            p = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n * .5f, n * .5f));
                    p[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(2.2f - Mathf.Abs(d - 28.5f)));
                }

            texAnelAbertura.SetPixels(p);
            texAnelAbertura.Apply(false, true);

            // Placa da legenda: cheia embaixo à esquerda, some para cima e para a direita (linha 0 fica embaixo).
            const int pw = 128, ph = 64;
            texPlacaAbertura = NovaTexturaHUD("Abertura_placa", pw, ph);
            p = new Color[pw * ph];
            for (int y = 0; y < ph; y++)
                for (int x = 0; x < pw; x++)
                {
                    float doAlto = 1f - (y + .5f) / ph, lado = (x + .5f) / pw;
                    p[y * pw + x] = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, doAlto / .35f) * (1f - Mathf.SmoothStep(.5f, 1f, lado)));
                }

            texPlacaAbertura.SetPixels(p);
            texPlacaAbertura.Apply(false, true);

            // Grão de filme (repete pela tela): pontos claros e escuros bem fracos.
            const int g = 128;
            texGraoAbertura = NovaTexturaHUD("Abertura_grao", g, g, TextureWrapMode.Repeat);
            p = new Color[g * g];
            var acaso = new System.Random(19);
            for (int i = 0; i < p.Length; i++)
            {
                float v = (float)acaso.NextDouble();
                p[i] = v > .5f ? new Color(1f, .97f, .9f, (v - .5f) * .55f) : new Color(.05f, .04f, .03f, (.5f - v) * .55f);
            }

            texGraoAbertura.SetPixels(p);
            texGraoAbertura.Apply(false, true);
        }

        private void AnelAbertura(Vector2 centro, float raio, Color cor)
        {
            if (cor.a <= .001f)
                return;
            PrepararTexturasAbertura();
            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTexture(new Rect(centro.x - raio, centro.y - raio, raio * 2f, raio * 2f), texAnelAbertura);
            GUI.color = antiga;
        }

        private void DegradeAbertura(Rect r, Color cor, bool doAlto)
        {
            if (cor.a <= .001f || r.height <= 0f)
                return;
            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTextureWithTexCoords(r, texDegradeAbertura, doAlto ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f));
            GUI.color = antiga;
        }

        private void BrilhoAbertura(Rect r, Color cor)
        {
            if (cor.a <= .001f)
                return;
            PrepararTexturasAbertura();
            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTexture(r, texBrilhoAbertura);
            GUI.color = antiga;
        }

        // Tela inteira em coordenadas do HUD (1280 x 720), incluindo as sobras fora do 16:9.
        private Rect TelaInteiraUI()
        {
            Rect px = cameraDoMapa != null ? cameraDoMapa.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
            float s = Mathf.Min(px.width / 1280f, px.height / 720f);
            if (s <= 0f)
                return new Rect(0f, 0f, 1280f, 720f);
            float ox = px.x + (px.width - 1280f * s) * .5f;
            float oy = Screen.height - px.yMax + (px.height - 720f * s) * .5f;
            return new Rect(-ox / s, -oy / s, Screen.width / s, Screen.height / s);
        }

        // Câmera lenta do quadro: vai do foco/zoom inicial ao final durante o quadro (e a troca).
        private Rect CameraDoQuadro(MTMQuadroPrologo q, Texture arte, Rect tela, float t)
        {
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            float p = reduzir ? 0f : Suave(t / (DuracaoQuadroPrologo(q) + TrocaQuadroPrologo));
            Vector2 de = FocoPrologo(q.focoDe, new Vector2(.5f, .5f)), para = FocoPrologo(q.focoPara, de);
            float zDe = q.zoomDe > 0f ? q.zoomDe : 1.1f, zPara = q.zoomPara > 0f ? q.zoomPara : zDe;
            return RecorteArte(tela, arte, Vector2.Lerp(de, para, p), Mathf.Lerp(zDe, zPara, p));
        }

        private static Vector2 FocoPrologo(float[] v, Vector2 padrao)
        {
            return v != null && v.Length >= 2 ? new Vector2(Mathf.Clamp01(v[0]), Mathf.Clamp01(v[1])) : padrao;
        }

        // Recorte da arte (0 a 1, a partir do canto de cima) que cobre "destino" com o foco e a aproximação pedidos.
        private static Rect RecorteArte(Rect destino, Texture arte, Vector2 foco, float zoom)
        {
            float aspectoTela = destino.width / Mathf.Max(1f, destino.height);
            float aspectoArte = arte.width / (float)Mathf.Max(1, arte.height);
            float w = 1f, h = 1f;
            if (aspectoArte > aspectoTela)
                w = aspectoTela / aspectoArte;
            else
                h = aspectoArte / aspectoTela;
            zoom = Mathf.Max(1f, zoom);
            w /= zoom;
            h /= zoom;
            return new Rect(Mathf.Clamp(foco.x - w * .5f, 0f, 1f - w), Mathf.Clamp(foco.y - h * .5f, 0f, 1f - h), w, h);
        }

        private static void DesenharRecorte(Rect destino, Texture arte, Rect recorte, Color cor)
        {
            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTextureWithTexCoords(destino, arte, new Rect(recorte.x, 1f - recorte.y - recorte.height, recorte.width, recorte.height));
            GUI.color = antiga;
        }

        private static Vector2 PontoNoRecorte(Rect destino, Rect recorte, float x, float y)
        {
            return new Vector2(destino.x + (x - recorte.x) / recorte.width * destino.width, destino.y + (y - recorte.y) / recorte.height * destino.height);
        }

        private static float Suave(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float SaidaSuave(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        // Entrada com uma pequena passada do ponto (estouro).
        private static float Salto(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private static float Acaso(int n)
        {
            float s = Mathf.Sin(n * 12.9898f + 78.233f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        // ---------------------------------------------------------------- chegada

        // Depois da abertura: a tela clareia sobre o letreiro EU AMO JAGUARUANA e as esculturas da Entrada
        // (a primeira imagem da cidade, com o banner do lugar), as faixas de cinema recolhem e a câmera desce
        // até o Neto. Numa região sem esses marcos, a câmera só desce de longe até ele.
        private const float DuracaoChegada = 4.6f, SeguraChegada = 2.4f;
        private float chegadaInicio = -100f;
        private bool avisoChegadaPendente, brilhoChegadaPendente, temMarcoChegada;
        private Vector3 marcoChegada;
        private float tamanhoMarcoChegada;

        private bool ChegadaAtiva
        {
            get { return Time.unscaledTime - chegadaInicio < DuracaoChegada; }
        }

        // Enquanto a câmera mostra a Entrada, o Neto espera; E pula para a descida.
        private bool ChegadaSegurando
        {
            get { return temMarcoChegada && Time.unscaledTime - chegadaInicio < SeguraChegada + .4f; }
        }

        private void IniciarChegadaAventura()
        {
            if (atual != null)
                MostrarBannerRegiao(atual.dados.id, true, true);
            chegadaInicio = Time.unscaledTime;
            avisoChegadaPendente = brilhoChegadaPendente = true;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            temMarcoChegada = !reduzir && MarcoDaEntrada(out marcoChegada, out tamanhoMarcoChegada);
            if (cameraDoMapa == null || atual == null || reduzir)
                return;
            // A câmera já começa no lugar: a tela clareia sobre ele.
            cameraDoMapa.orthographicSize = Mathf.Min(TamanhoMaximoCamera(), tamanhoCamera * zoomPreferido * FatorZoomChegada());
            velocidadeZoom = 0f;
            if (temMarcoChegada)
            {
                Vector3 p = marcoChegada;
                p.z = -10f;
                cameraDoMapa.transform.position = ConterCamera(p);
                velocidadeCamera = Vector3.zero;
            }
        }

        // Letreiro e esculturas da região atual: a moldura que a câmera mostra primeiro.
        private bool MarcoDaEntrada(out Vector3 centro, out float tamanho)
        {
            centro = Vector3.zero;
            tamanho = 0f;
            if (atual == null || cameraDoMapa == null)
                return false;
            bool achou = false;
            var caixa = new Bounds();
            foreach (var t in atual.GetComponentsInChildren<Transform>())
            {
                string n = t.name;
                if (n.IndexOf("letreiro", StringComparison.OrdinalIgnoreCase) < 0 && n.IndexOf("escultura", StringComparison.OrdinalIgnoreCase) < 0
                    && n.IndexOf("monumento_entrada", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var b = new Bounds(t.position, Vector3.one);
                foreach (var r in t.GetComponentsInChildren<SpriteRenderer>())
                    if (r.enabled && r.bounds.size.x < 30f && r.bounds.size.y < 30f)
                        b.Encapsulate(r.bounds);
                if (achou)
                    caixa.Encapsulate(b);
                else
                {
                    caixa = b;
                    achou = true;
                }
            }

            if (!achou)
                return false;
            centro = caixa.center;
            float aspecto = Mathf.Max(.5f, cameraDoMapa.aspect);
            tamanho = Mathf.Max(caixa.extents.y + 1.6f, (caixa.extents.x + 2.2f) / aspecto);
            tamanho = Mathf.Clamp(tamanho, tamanhoCamera * zoomPreferido, TamanhoMaximoCamera());
            return true;
        }

        // 0 enquanto a câmera mostra a Entrada (ou está de longe); chega a 1 quando ela termina de descer até o Neto.
        private float MisturaChegada()
        {
            float t = Time.unscaledTime - chegadaInicio;
            if (t >= DuracaoChegada)
                return 1f;
            return temMarcoChegada ? Suave((t - SeguraChegada) / (DuracaoChegada - SeguraChegada)) : Suave(t / DuracaoChegada);
        }

        private float FatorZoomChegada()
        {
            if ((apresentacao != null && apresentacao.reduzirMovimento) || !ChegadaAtiva)
                return 1f;
            float normal = Mathf.Max(.1f, tamanhoCamera * zoomPreferido);
            float inicio = temMarcoChegada ? tamanhoMarcoChegada / normal : 1.7f;
            return Mathf.Lerp(inicio, 1f, MisturaChegada());
        }

        // Para onde a câmera olha na chegada: o letreiro e as esculturas, depois o Neto.
        private bool AlvoDaChegada(ref Vector3 alvo)
        {
            if (!temMarcoChegada || !ChegadaAtiva)
                return false;
            alvo = Vector3.Lerp(marcoChegada, alvo, MisturaChegada());
            return true;
        }

        private void PularEsperaChegada()
        {
            float t = Time.unscaledTime - chegadaInicio;
            if (t < SeguraChegada)
                chegadaInicio = Time.unscaledTime - SeguraChegada;
        }

        // O objetivo volta brilhando quando a câmera chega no Neto; a dica espera o banner terminar.
        private void AtualizarChegada()
        {
            if (ChegadaAtiva)
                return;
            if (brilhoChegadaPendente)
            {
                brilhoChegadaPendente = false;
                objetivoBrilhoAte = Time.unscaledTime + 2.4f;
            }

            if (avisoChegadaPendente && !BannerAberturaAtivo)
            {
                avisoChegadaPendente = false;
                Avisar("Siga a seta dourada para encontrar a primeira história.");
            }
        }

        private void ChegadaHUD()
        {
            float t = Time.unscaledTime - chegadaInicio;
            if (t < 0f || t > DuracaoChegada)
                return;
            Rect tela = TelaInteiraUI();
            float escuro = 1f - Suave(t / 1.2f);
            if (escuro > .001f)
                Fundo(tela, new Color(0f, 0f, 0f, escuro));
            // As faixas ficam enquanto a câmera mostra a Entrada e recolhem quando ela desce até o Neto.
            float recolhe = temMarcoChegada ? SeguraChegada - .2f : .9f;
            FaixasCinemaUI(tela, FaixaCinema * (1f - Suave((t - recolhe) / 1.8f)));
        }
    }
}

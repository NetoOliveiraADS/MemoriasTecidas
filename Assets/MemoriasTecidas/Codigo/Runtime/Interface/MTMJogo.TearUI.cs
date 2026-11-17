using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // O primeiro fio: tear de pedais. Regras em MTMTear.cs; amostras em
    // Resources/MemoriasTecidas/tear_primeiro_fio.json.
    public sealed partial class MTMJogo
    {
        private const float DuracaoPassagemTear = .55f, DuracaoErroTear = 1.4f;
        private MTMTearDados tearDados;
        private MTMSessaoTear sessaoTear;
        private bool tearCarregado;
        private bool passandoTear;
        private float inicioPassagemTear, erroTearAte, acertoTearAte;
        private int pedalPassagemTear, corPassagemTear, linhaPassagemTear;
        private bool nivelTearPronto;
        private Texture2D texFioVertical, texFioHorizontal, texMadeiraTear;

        private static readonly Color MadeiraClaraTear = new Color(.55f, .36f, .22f);
        private static readonly Color MadeiraEscuraTear = new Color(.30f, .19f, .12f);
        private static readonly Color FundoTear = new Color(.17f, .12f, .09f, .97f);

        private bool CarregarTear()
        {
            if (tearCarregado)
                return tearDados != null;
            tearCarregado = true;
            try
            {
                var arquivo = Resources.Load<TextAsset>("MemoriasTecidas/tear_primeiro_fio");
                if (arquivo == null)
                    throw new InvalidOperationException("Resources/MemoriasTecidas/tear_primeiro_fio.json não encontrado.");
                var dados = JsonUtility.FromJson<MTMTearDados>(arquivo.text);
                MTMTear.Validar(dados);
                tearDados = dados;
                sessaoTear = new MTMSessaoTear(dados);
            }
            catch (Exception erro)
            {
                tearDados = null;
                Debug.LogWarning("O tear do primeiro fio não foi carregado: " + erro.Message, this);
            }

            return tearDados != null;
        }

        private Color CorTear(int indice)
        {
            return indice < 0 || tearDados == null ? new Color(.5f, .5f, .5f) : CorTecido(tearDados.cores[indice], ouro);
        }

        // ---------- texturas ----------

        private void PrepararTexturasTear()
        {
            if (texFioVertical != null)
                return;
            const int n = 16;
            texFioVertical = NovaTexturaHUD("Tear_fio_vertical", n, n, TextureWrapMode.Repeat);
            texFioHorizontal = NovaTexturaHUD("Tear_fio_horizontal", n, n, TextureWrapMode.Repeat);
            var v = new Color[n * n];
            var h = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Fio arredondado (claro no meio, escuro nas bordas) com torção em diagonal.
                    float bordaV = Mathf.Abs(x + .5f - n * .5f) / (n * .5f);
                    float torcaoV = .06f * Mathf.Sin((x + y * .9f) * 1.6f);
                    float luzV = Mathf.Lerp(1.12f, .62f, bordaV * bordaV) + torcaoV;
                    v[y * n + x] = new Color(luzV, luzV, luzV, 1f);
                    float bordaH = Mathf.Abs(y + .5f - n * .5f) / (n * .5f);
                    float torcaoH = .06f * Mathf.Sin((y + x * .9f) * 1.6f);
                    float luzH = Mathf.Lerp(1.12f, .62f, bordaH * bordaH) + torcaoH;
                    h[y * n + x] = new Color(luzH, luzH, luzH, 1f);
                }

            texFioVertical.SetPixels(v);
            texFioVertical.Apply(false, true);
            texFioHorizontal.SetPixels(h);
            texFioHorizontal.Apply(false, true);

            // Veio de madeira para as traves e os pedais.
            const int mw = 128, mh = 32;
            texMadeiraTear = NovaTexturaHUD("Tear_madeira", mw, mh, TextureWrapMode.Repeat);
            var m = new Color[mw * mh];
            for (int y = 0; y < mh; y++)
                for (int x = 0; x < mw; x++)
                {
                    float veio = Mathf.Sin(y * .9f + Mathf.Sin(x * .05f) * 2.2f + Mathf.Sin(x * .013f + y * .3f) * 1.5f);
                    float luz = .82f + .14f * veio + .04f * Mathf.Sin(x * .7f + y);
                    m[y * mw + x] = new Color(luz, luz, luz, 1f);
                }

            texMadeiraTear.SetPixels(m);
            texMadeiraTear.Apply(false, true);
        }

        private void MadeiraTear(Rect r, Color cor)
        {
            PrepararTexturasTear();
            ArredondadoHUD(new Rect(r.x, r.y + 3, r.width, r.height), new Color(0, 0, 0, .35f));
            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTextureWithTexCoords(r, texMadeiraTear, new Rect(r.x / 128f, 0, r.width / 128f, Mathf.Max(.25f, r.height / 32f)));
            GUI.color = antiga;
            Fundo(new Rect(r.x, r.y, r.width, 2), new Color(1, 1, 1, .12f));
            Fundo(new Rect(r.x, r.yMax - 2, r.width, 2), new Color(0, 0, 0, .25f));
        }

        // Um ponto do tecido: urdidura por cima (fio em pé) ou trama por cima (fio deitado).
        private void PontoTear(Rect r, Color cor, bool urdiduraPorCima, float alfa = 1f)
        {
            PrepararTexturasTear();
            Color antiga = GUI.color;
            GUI.color = new Color(cor.r, cor.g, cor.b, alfa);
            if (urdiduraPorCima)
            {
                GUI.DrawTextureWithTexCoords(new Rect(r.x + r.width * .14f, r.y, r.width * .72f, r.height), texFioVertical, new Rect(0, 0, 1, r.height / 18f));
                GUI.color = new Color(0, 0, 0, .22f * alfa);
                GUI.DrawTexture(new Rect(r.x + r.width * .14f, r.yMax - 2, r.width * .72f, 2), Texture2D.whiteTexture);
            }
            else
            {
                GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y + r.height * .12f, r.width, r.height * .76f), texFioHorizontal, new Rect(0, 0, r.width / 18f, 1));
                GUI.color = new Color(0, 0, 0, .18f * alfa);
                GUI.DrawTexture(new Rect(r.xMax - 2, r.y + r.height * .12f, 2, r.height * .76f), Texture2D.whiteTexture);
            }

            GUI.color = antiga;
        }

        // Grade de tecido. linhas = cores por linha; pedais = pedal de cada linha (para saber o que está por cima).
        private void TecidoTear(Rect area, MTMTearNivel nivel, int totalLinhas, Func<int, int[]> corDaLinha, Func<int, int> pedalDaLinha, float alfa = 1f)
        {
            int colunas = nivel.urdidura.Length;
            float w = area.width / colunas, h = area.height / Mathf.Max(1, totalLinhas);
            for (int l = 0; l < totalLinhas; l++)
            {
                int[] cores = corDaLinha(l);
                if (cores == null)
                    continue;
                int pedal = pedalDaLinha(l);
                for (int c = 0; c < colunas; c++)
                    PontoTear(new Rect(area.x + c * w, area.y + l * h, w, h), CorTear(cores[c]), pedal >= 0 && MTMTear.Levanta(nivel, pedal, c), alfa);
            }
        }

        // ---------- tela ----------

        private void PrimeiroFioUI()
        {
            if (aberturaMemorias == null)
            {
                painel = "";
                return;
            }

            Fundo(new Rect(0, 0, 1280, 720), new Color(.06f, .04f, .03f, .62f));
            if (!CarregarTear())
            {
                TearIndisponivelUI();
                return;
            }

            var s = sessaoTear;
            var nivel = s.NivelAtual;
            Rect fundo = new Rect(24, 20, 1232, 680);
            ArredondadoHUD(new Rect(fundo.x, fundo.y + 4, fundo.width, fundo.height), new Color(0, 0, 0, .4f));
            ArredondadoHUD(fundo, FundoTear);
            TramaHUD(new Rect(fundo.x + 1, fundo.y + 14, 5, fundo.height - 28));

            // Cabeçalho.
            TextoTecido(new Rect(52, 34, 600, 20), "O PRIMEIRO FIO · AMOSTRA " + (s.Nivel + 1) + " DE " + tearDados.niveis.Length, 12, OuroHUD, true);
            TextoTecido(new Rect(52, 54, 700, 36), nivel.nome, 28, CremeHUD, true);
            for (int i = 0; i < tearDados.niveis.Length; i++)
            {
                Rect l = new Rect(760 + i * 22, 52, 14, 14);
                bool feita = i < s.Nivel || (i == s.Nivel && (s.NivelPronto || s.Terminou));
                LosangoCompacta(l, i <= s.Nivel ? OuroHUD : AlfaTecido(OuroHUD, .35f));
                if (!feita)
                    LosangoCompacta(new Rect(l.x + 3, l.y + 3, 8, 8), FundoTear);
            }

            if (BotaoTecido(new Rect(1096, 38, 136, 40), "Voltar"))
            {
                LimparPrimeiroFio();
                painel = "";
                return;
            }

            TextoTecido(new Rect(52, 94, 1180, 40), ResumoHUDClaroUI(nivel.explicacao, 1180, 40, 15), 15, CremeFracoHUD);

            Rect tear = new Rect(52, 140, 720, 404);
            TearUI(tear, s, nivel);
            AmostraReferenciaUI(new Rect(796, 140, 436, 404), new Rect(tear.x + 104, tear.y + 52, tear.width - 208, tear.height - 104), s, nivel);
            if (nivelTearPronto)
            {
                NivelProntoTearUI(tear, s, nivel);
                return;
            }

            ControlesTearUI(s, nivel);
        }

        private void TearIndisponivelUI()
        {
            Rect r = new Rect(340, 240, 600, 220);
            PainelHUD(r);
            TextoTecido(new Rect(r.x + 30, r.y + 30, r.width - 60, 100),
                "O tear não abriu (o Console explica o motivo). Você pode seguir a jornada e voltar a ele depois.", 17, CremeHUD, false, TextAnchor.MiddleCenter);
            if (BotaoTecido(new Rect(r.x + 180, r.yMax - 66, 240, 42), "Seguir assim", true))
                ConcluirTearPrimeiroFio();
        }

        // O tear: traves de madeira, urdidura em pé, tecido já feito, linha atual com o "vão" e a lançadeira.
        private void TearUI(Rect area, MTMSessaoTear s, MTMTearNivel nivel)
        {
            int colunas = nivel.urdidura.Length, total = nivel.solucao.Length;
            ArredondadoHUD(area, new Color(.11f, .08f, .06f, 1f));
            MadeiraTear(new Rect(area.x + 8, area.y + 10, area.width - 16, 26), MadeiraClaraTear);
            MadeiraTear(new Rect(area.x + 8, area.yMax - 36, area.width - 16, 26), MadeiraClaraTear);
            Rect tecido = new Rect(area.x + 104, area.y + 52, area.width - 208, area.height - 104);
            float w = tecido.width / colunas, h = tecido.height / total;

            // Fios da urdidura, de trave a trave.
            for (int c = 0; c < colunas; c++)
                PontoTear(new Rect(tecido.x + c * w, area.y + 30, w, area.height - 60), Color.Lerp(CorTear(nivel.urdidura[c]), Color.black, .25f), true, .9f);

            // Linhas já tecidas.
            int feitas = s.LinhasFeitas;
            TecidoTear(new Rect(tecido.x, tecido.y, tecido.width, h * total), nivel, total,
                l =>
                {
                    if (l >= feitas)
                        return null;
                    int p, c;
                    s.Feita(l, out p, out c);
                    return MTMTear.Linha(nivel, p, c);
                },
                l =>
                {
                    if (l >= feitas)
                        return -1;
                    int p, c;
                    s.Feita(l, out p, out c);
                    return p;
                });

            // Números das linhas e marcas de conferido.
            for (int l = 0; l < total; l++)
            {
                Rect num = new Rect(area.x + 14, tecido.y + l * h + h * .5f - 13, 26, 26);
                bool feita = l < feitas, atual = l == s.LinhaAtual && !s.NivelPronto;
                // A linha da vez já tem moldura e lançadeira; o número sairia por baixo dela.
                if (atual)
                    continue;
                DiscoHUD(num, feita ? OuroHUD : atual ? AlfaTecido(OuroHUD, .25f) : new Color(1, 1, 1, .06f));
                TextoTecido(num, (l + 1).ToString(), 13, feita ? FundoTear : atual ? OuroHUD : CremeFracoHUD, true, TextAnchor.MiddleCenter);
            }

            if (s.NivelPronto || s.Terminou)
                return;

            // Linha atual: o vão aberto pelo pedal, a prévia do fio e a lançadeira.
            int linha = s.LinhaAtual;
            Rect faixa = new Rect(tecido.x, tecido.y + linha * h, tecido.width, h);
            bool pulso = !(apresentacao != null && apresentacao.reduzirMovimento);
            float brilho = pulso ? .5f + .5f * Mathf.Sin(Time.unscaledTime * 4f) : .7f;
            var alvo = MTMTear.Alvo(nivel, linha);
            var previa = passandoTear ? null : s.Previa();
            bool combina = !passandoTear && s.Combina;

            // Liga a linha da vez à mesma linha da amostra, do outro lado.
            for (float px = faixa.xMax + 8; px < 796; px += 11)
                Fundo(new Rect(px, faixa.center.y - 1.5f, 6, 3), AlfaTecido(OuroHUD, .35f + .45f * brilho));

            int pedalVisto = passandoTear ? pedalPassagemTear : s.Pedal;
            if (pedalVisto >= 0)
                for (int c = 0; c < colunas; c++)
                {
                    bool levanta = MTMTear.Levanta(nivel, pedalVisto, c);
                    Rect fio = new Rect(tecido.x + c * w, faixa.y - (levanta ? 6 : 0), w, faixa.height);
                    // Fio levantado sobe e clareia; o baixo fica na sombra do vão.
                    PontoTear(fio, levanta ? Color.Lerp(CorTear(nivel.urdidura[c]), Color.white, .12f) : Color.Lerp(CorTear(nivel.urdidura[c]), Color.black, .55f), true, levanta ? 1f : .8f);
                }

            // Prévia: como a linha vai sair com o pedal e o fio escolhidos.
            if (previa != null)
            {
                bool mostrarErro = Time.unscaledTime < erroTearAte;
                for (int c = 0; c < colunas; c++)
                {
                    Rect p = new Rect(tecido.x + c * w, faixa.y, w, faixa.height);
                    PontoTear(p, CorTear(previa[c]), MTMTear.Levanta(nivel, s.Pedal, c), combina ? 1f : .72f + .14f * brilho);
                    if (previa[c] == alvo[c])
                        continue;
                    // Ponto que ainda não bate com a amostra.
                    float lado = mostrarErro ? 16f : 11f;
                    LosangoCompacta(new Rect(p.center.x - lado * .5f - 2f, p.yMax - lado - 4f, lado + 4f, lado + 4f), new Color(.1f, .05f, .04f, .85f));
                    LosangoCompacta(new Rect(p.center.x - lado * .5f, p.yMax - lado - 2f, lado, lado), PerigoHUD);
                    if (mostrarErro)
                        BordaTecido(new Rect(p.x + 2, p.y + 2, p.width - 4, p.height - 4), AlfaTecido(PerigoHUD, .5f + .5f * brilho), 2);
                }
            }

            if (combina)
            {
                Fundo(faixa, new Color(1f, .92f, .7f, .10f + .08f * brilho));
                BordaTecido(new Rect(faixa.x - 4, faixa.y - 2, faixa.width + 8, faixa.height + 4), OuroHUD, 3);
            }
            else
                BordaTecido(new Rect(faixa.x - 3, faixa.y - 1, faixa.width + 6, faixa.height + 2), AlfaTecido(OuroHUD, .45f + .4f * brilho), 2);

            // Lançadeira: vai e volta, uma linha para cada lado, como no tear de verdade.
            float progresso = passandoTear ? Mathf.Clamp01((Time.unscaledTime - inicioPassagemTear) / DuracaoPassagemTear) : 0f;
            bool vaiParaDireita = linha % 2 == 0;
            float inicioX = vaiParaDireita ? tecido.x - 46 : tecido.xMax + 46;
            float fimX = vaiParaDireita ? tecido.xMax + 46 : tecido.x - 46;
            float x = Mathf.Lerp(inicioX, fimX, Mathf.SmoothStep(0f, 1f, progresso));

            // A trama aparece atrás da lançadeira, ponto por ponto.
            if (passandoTear)
            {
                var resultado = MTMTear.Linha(nivel, pedalPassagemTear, corPassagemTear);
                for (int c = 0; c < colunas; c++)
                {
                    float x0 = tecido.x + c * w;
                    float fracao = Mathf.Clamp01(vaiParaDireita ? (x - x0) / w : (x0 + w - x) / w);
                    if (fracao > 0f)
                        PontoTear(new Rect(x0, faixa.y, w, faixa.height), CorTear(resultado[c]), MTMTear.Levanta(nivel, pedalPassagemTear, c), fracao);
                }
            }

            int corVista = passandoTear ? corPassagemTear : s.Cor;
            Rect barco = new Rect(x - 40, faixa.center.y - 13, 80, 26);
            if (combina)
            {
                // Pronta para passar: a lançadeira brilha e mostra a tecla.
                float halo = 6f + 4f * brilho;
                ArredondadoHUD(new Rect(barco.x - halo, barco.y - halo, barco.width + halo * 2, barco.height + halo * 2), AlfaTecido(OuroHUD, .25f + .3f * brilho));
                float sobe = pulso ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)) * 5f : 0f;
                TeclaHUD(barco.center.x - 12, barco.y - 34 - sobe, "E", 1f, 13, 24f);
            }

            ArredondadoHUD(new Rect(barco.x, barco.y + 3, barco.width, barco.height), new Color(0, 0, 0, .4f));
            ArredondadoHUD(barco, MadeiraEscuraTear);
            ArredondadoHUD(new Rect(barco.x + 4, barco.y + 3, barco.width - 8, barco.height - 8), MadeiraClaraTear);
            DiscoHUD(new Rect(barco.x + 1, barco.center.y - 6, 12, 12), MadeiraEscuraTear);
            DiscoHUD(new Rect(barco.xMax - 13, barco.center.y - 6, 12, 12), MadeiraEscuraTear);
            Rect carretel = new Rect(barco.center.x - 18, barco.center.y - 7, 36, 14);
            if (corVista >= 0)
                PontoTear(carretel, CorTear(corVista), false);
            else
                ArredondadoHUD(carretel, new Color(.2f, .15f, .11f, 1f));

            if (passandoTear && progresso >= 1f)
                TerminarPassagemTear();

            // Clique na lançadeira também passa o fio.
            if (!passandoTear && AcaoTecido(new Rect(barco.x - 6, barco.y - 6, barco.width + 12, barco.height + 12), "tear:lancadeira"))
                PassarOuConcluirPrimeiroFio();

            // Acerto: brilho rápido na linha recém-tecida.
            if (Time.unscaledTime < acertoTearAte && linha > 0)
            {
                float a = Mathf.Clamp01((acertoTearAte - Time.unscaledTime) / .6f);
                Fundo(new Rect(tecido.x, tecido.y + (linha - 1) * h, tecido.width, h), new Color(1f, .95f, .8f, .35f * a));
            }
        }

        // A amostra de Dona Feliciana: o modelo, com as linhas na mesma altura das do tear.
        private void AmostraReferenciaUI(Rect area, Rect tecidoTear, MTMSessaoTear s, MTMTearNivel nivel)
        {
            ArredondadoHUD(area, algodaoUI);
            TextoTecido(new Rect(area.x + 22, area.y + 14, area.width - 44, 20), "AMOSTRA DE DONA FELICIANA", 12, acentoTextoUI, true);
            TextoTecido(new Rect(area.x + 22, area.y + 32, area.width - 44, 20), "Copie linha por linha, de cima para baixo", 13, textoPapelUI);
            int total = nivel.solucao.Length, colunas = nivel.urdidura.Length;
            Rect grade = new Rect(area.x + 64, tecidoTear.y, area.width - 110, tecidoTear.height);
            ArredondadoHUD(new Rect(grade.x - 8, grade.y - 8, grade.width + 16, grade.height + 16), new Color(.25f, .18f, .13f, 1f));
            TecidoTear(grade, nivel, total, l => MTMTear.Alvo(nivel, l), l =>
            {
                int p, c;
                MTMTear.Solucao(nivel, l, out p, out c);
                return p;
            });
            float h = grade.height / total;
            bool pulso = !(apresentacao != null && apresentacao.reduzirMovimento);
            float brilho = pulso ? .5f + .5f * Mathf.Sin(Time.unscaledTime * 4f) : .7f;
            for (int l = 0; l < total; l++)
            {
                Rect marca = new Rect(area.x + 22, grade.y + l * h + h * .5f - 11, 22, 22);
                if (l < s.LinhasFeitas)
                {
                    DiscoHUD(marca, terracotaUI);
                    IconeTecido("check", new Rect(marca.x + 4, marca.y + 4, 14, 14), claroUI);
                    // Linha já copiada fica apagada: o olho vai direto para a da vez.
                    Fundo(new Rect(grade.x, grade.y + l * h, grade.width, h), AlfaTecido(algodaoUI, .45f));
                }
                else if (l == s.LinhaAtual && !s.NivelPronto)
                {
                    Fundo(new Rect(area.x, marca.center.y - 1.5f, marca.x - area.x, 3), AlfaTecido(OuroHUD, .35f + .45f * brilho));
                    LosangoCompacta(marca, OuroHUD);
                    BordaTecido(new Rect(grade.x - 4, grade.y + l * h - 2, grade.width + 8, h + 4), AlfaTecido(OuroHUD, .7f + .3f * brilho), 3);
                }
                else
                    TextoTecido(marca, (l + 1).ToString(), 13, AlfaTecido(textoPapelUI, .55f), true, TextAnchor.MiddleCenter);
            }

            TextoTecido(new Rect(area.x + 22, grade.yMax + 12, area.width - 44, area.yMax - grade.yMax - 16),
                ResumoHUDClaroUI(retornoPrimeiroFio, area.width - 44, area.yMax - grade.yMax - 16, 14), 14, acentoTextoUI, true);
        }

        // Passo da vez: 1 pedal, 2 fio, 3 passar a lançadeira.
        private static int PassoTear(MTMSessaoTear s)
        {
            if (s.Pedal < 0)
                return 1;
            if (s.Cor < 0)
                return 2;
            if (!s.Combina)
                return s.PedalAutomatico || s.PedalCombina() ? 2 : 1;
            return 3;
        }

        private string OrientacaoTear(MTMSessaoTear s, MTMTearNivel nivel)
        {
            if (s.Pedal < 0)
                return (MTMEntrada.UsandoControle ? "Escolha um pedal (LB e RB)." : "Escolha um pedal (1 a " + nivel.pedais.Length + ").") + " No tear, veja quais fios em pé ele levanta.";
            if (s.Cor < 0)
                return (MTMEntrada.UsandoControle ? "Escolha a cor do fio (X troca)." : "Escolha a cor do fio (A, S ou D).") + " A linha da vez mostra como vai ficar.";
            int d = s.Diferencas();
            if (d > 0)
                return "Ainda não combina: " + d + (d == 1 ? " ponto diferente" : " pontos diferentes") + " da amostra, marcados em vermelho. " +
                       (s.PedalAutomatico || s.PedalCombina() ? "Troque a cor do fio." : "Experimente outro pedal.");
            return "Combina com a amostra! Passe a lançadeira: aperte E ou clique nela.";
        }

        // Rótulo de grupo; o do passo da vez vira um selo dourado com uma seta piscando.
        private void RotuloPassoTear(float x, float y, string texto, bool ativo, bool apagado)
        {
            float largura = LarguraTextoHUD(texto, 12, true);
            if (!ativo)
            {
                TextoTecido(new Rect(x, y, largura + 10, 18), texto, 12, apagado ? AlfaTecido(OuroHUD, .5f) : OuroHUD, true);
                return;
            }

            bool pulso = !(apresentacao != null && apresentacao.reduzirMovimento);
            float pisca = pulso ? .5f + .5f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            ArredondadoHUD(new Rect(x - 8, y - 3, largura + 16, 22), Color.Lerp(OuroHUD, CremeHUD, .25f * pisca));
            TextoTecido(new Rect(x, y, largura + 10, 18), texto, 12, FundoTear, true);
            PrepararTexturasHUD();
            float desce = pulso ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)) * 4f : 0f;
            Color antiga = GUI.color;
            GUI.color = AlfaTecido(OuroHUD, .55f + .45f * pisca);
            GUI.DrawTexture(new Rect(x + largura + 16, y + 2 + desce, 20, 11), texPontaHUD);
            GUI.color = antiga;
        }

        private void ControlesTearUI(MTMSessaoTear s, MTMTearNivel nivel)
        {
            bool livre = !passandoTear;
            bool automatico = s.PedalAutomatico;
            int passo = PassoTear(s);

            // 1 · Pedais: cada um mostra a linha que sai com ele e com o fio escolhido.
            bool controle = MTMEntrada.UsandoControle;
            RotuloPassoTear(52, 556, automatico ? "PEDAIS · automáticos" : controle ? "1 · PEDAL  LB / RB" : "1 · PEDAL", !automatico && passo == 1, automatico);
            int n = nivel.pedais.Length;
            for (int p = 0; p < n; p++)
            {
                bool escolhido = s.Pedal == p;
                bool sugerido = !automatico && s.DicaNaLinha >= 1 && s.PedalDaDica() == p;
                float alfa = automatico && !escolhido ? .4f : 1f;
                Rect r = new Rect(52 + p * 132, 580 + (escolhido ? 4 : 0), 122, 76);
                MadeiraTear(r, AlfaTecido(escolhido ? Color.Lerp(MadeiraClaraTear, Color.white, .15f) : MadeiraClaraTear, alfa));
                if (escolhido || sugerido)
                    BordaTecido(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), sugerido && !escolhido ? CremeHUD : OuroHUD, 3);
                TextoTecido(new Rect(r.x + 10, r.y + 5, 80, 20), "Pedal " + (p + 1), 14, AlfaTecido(CremeHUD, alfa), true);
                if (!automatico && !controle)
                    TeclaHUD(r.xMax - 30, r.y + 5, (p + 1).ToString(), 1f, 12, 20f);

                // Miniatura da linha: fio em pé onde o pedal levanta, fio da lançadeira no resto.
                float passoX = (r.width - 20) / nivel.urdidura.Length;
                Rect fundoMini = new Rect(r.x + 8, r.y + 30, r.width - 16, 38);
                ArredondadoHUD(fundoMini, new Color(.12f, .08f, .06f, .9f * alfa));
                for (int c = 0; c < nivel.urdidura.Length; c++)
                {
                    bool levanta = MTMTear.Levanta(nivel, p, c);
                    Rect cel = new Rect(r.x + 10 + c * passoX, r.y + 34, passoX, 30);
                    if (levanta)
                        PontoTear(new Rect(cel.x, cel.y - 2, cel.width, cel.height + 2), Color.Lerp(CorTear(nivel.urdidura[c]), Color.white, .1f), true, alfa);
                    else if (s.Cor >= 0)
                        PontoTear(cel, CorTear(s.Cor), false, alfa);
                    else
                        PontoTear(cel, Color.Lerp(CorTear(nivel.urdidura[c]), Color.black, .6f), true, .7f * alfa);
                }

                if (sugerido)
                    TextoTecido(new Rect(r.x, r.yMax + 1, r.width, 16), "este", 12, CremeHUD, true, TextAnchor.MiddleCenter);
                if (livre && !automatico && AcaoTecido(r, "tear:pedal:" + p))
                {
                    s.EscolherPedal(p);
                    retornoPrimeiroFio = "";
                }
            }

            // 2 · Carretéis de cor.
            float x0 = 52 + n * 132 + 24;
            RotuloPassoTear(x0, 556, (automatico ? "1 · FIO" : "2 · FIO") + (controle ? "  X troca" : ""), passo == 2, false);
            string[] teclas = { "A", "S", "D" };
            for (int c = 0; c < tearDados.cores.Length; c++)
            {
                bool escolhido = s.Cor == c;
                bool sugerido = s.DicaNaLinha >= (automatico ? 1 : 2) && s.CorDaDica() == c;
                Rect r = new Rect(x0 + c * 92, 580, 84, 76);
                ArredondadoHUD(r, escolhido ? new Color(.30f, .22f, .16f, 1f) : new Color(.22f, .16f, .12f, 1f));
                if (escolhido || sugerido)
                    BordaTecido(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), sugerido && !escolhido ? CremeHUD : OuroHUD, 3);
                // Carretel: abas de madeira e o fio enrolado.
                Rect fio = new Rect(r.x + 22, r.y + 14, 40, 30);
                MadeiraTear(new Rect(fio.x - 4, fio.y - 4, fio.width + 8, 5), MadeiraClaraTear);
                MadeiraTear(new Rect(fio.x - 4, fio.yMax - 1, fio.width + 8, 5), MadeiraClaraTear);
                PontoTear(fio, CorTear(c), false);
                TextoTecido(new Rect(r.x, r.y + 50, r.width, 20), tearDados.nomesCores[c], 12, CremeHUD, true, TextAnchor.MiddleCenter);
                if (c < teclas.Length && !controle)
                    TeclaHUD(r.xMax - 22, r.y + 4, teclas[c], 1f, 11, 18f);
                if (sugerido)
                    TextoTecido(new Rect(r.x, r.yMax + 1, r.width, 16), "este", 12, CremeHUD, true, TextAnchor.MiddleCenter);
                if (livre && AcaoTecido(r, "tear:cor:" + c))
                {
                    s.EscolherCor(c);
                    retornoPrimeiroFio = "";
                }
            }

            // 3 · Passar a lançadeira.
            RotuloPassoTear(984, 556, automatico ? "2 · TECER" : "3 · TECER", passo == 3, false);
            Rect passar = new Rect(984, 580, 248, 46);
            bool combina = livre && s.Combina;
            if (combina)
            {
                bool pulso = !(apresentacao != null && apresentacao.reduzirMovimento);
                float brilho = pulso ? .5f + .5f * Mathf.Sin(Time.unscaledTime * 4f) : .7f;
                ArredondadoHUD(new Rect(passar.x - 5, passar.y - 5, passar.width + 10, passar.height + 10), AlfaTecido(OuroHUD, .3f + .35f * brilho));
            }

            if (BotaoTecido(passar, "Passar a lançadeira  " + AtalhoUI("E"), combina, livre && s.Pedal >= 0 && s.Cor >= 0))
                PassarOuConcluirPrimeiroFio();
            if (BotaoTecido(new Rect(984, 634, 120, 34), "Dica " + AtalhoUI("H"), false, livre && !s.NivelPronto))
                DicaTear();
            if (BotaoTecido(new Rect(1112, 634, 120, 34), "Desfazer " + AtalhoUI("U"), false, livre && s.LinhasFeitas > 0))
                DesfazerPrimeiroFio();

            // Orientação do passo da vez, sempre visível.
            if (livre)
                TextoTecido(new Rect(52, 676, 920, 22), OrientacaoTear(s, nivel), 14, combina ? OuroHUD : s.Diferencas() > 0 ? Color.Lerp(PerigoHUD, CremeHUD, .45f) : CremeHUD, true);
        }

        private void NivelProntoTearUI(Rect tear, MTMSessaoTear s, MTMTearNivel nivel)
        {
            Rect r = new Rect(tear.x + 90, tear.center.y - 70, tear.width - 180, 140);
            PainelHUD(r, 1f, true);
            TextoTecido(new Rect(r.x + 20, r.y + 18, r.width - 40, 30), nivel.nome + " pronto!", 22, OuroHUD, true, TextAnchor.MiddleCenter);
            TextoTecido(new Rect(r.x + 30, r.y + 52, r.width - 60, 40), ResumoHUDClaroUI(nivel.conclusao, r.width - 60, 40, 14), 14, CremeHUD, false, TextAnchor.MiddleCenter);
            bool ultima = s.Terminou;
            if (BotaoTecido(new Rect(r.center.x - 130, r.yMax - 48, 260, 38), (ultima ? "Mostrar a Dona Feliciana  " : "Próxima amostra  ") + AtalhoUI("E"), true))
                AvancarNivelTear();
        }

        // ---------- ações ----------

        private void DicaTear()
        {
            if (sessaoTear == null || passandoTear)
                return;
            int nivelDica = sessaoTear.PedirDica();
            retornoPrimeiroFio = sessaoTear.PedalAutomatico
                ? "A cor certa está marcada com \"este\"."
                : nivelDica == 1
                    ? "O pedal certo está marcado com \"este\". Agora acerte a cor."
                    : "Pedal e cor certos estão marcados com \"este\".";
        }

        private void TerminarPassagemTear()
        {
            passandoTear = false;
            var s = sessaoTear;
            s.EscolherPedal(pedalPassagemTear);
            s.EscolherCor(corPassagemTear);
            var resultado = s.Passar();
            switch (resultado)
            {
                case MTMResultadoTear.Errada:
                    erroTearAte = Time.unscaledTime + DuracaoErroTear;
                    retornoPrimeiroFio = s.ErrosNaLinha >= 2
                        ? "Ainda não bate. Que tal pedir uma dica (H)?"
                        : "O fio não bateu com a amostra nos pontos marcados. Tente outro pedal ou outra cor.";
                    break;
                case MTMResultadoTear.Certa:
                    TocarSom("tear_acerto", .6f, .02f);
                    acertoTearAte = Time.unscaledTime + .6f;
                    retornoPrimeiroFio = "Linha " + s.LinhasFeitas + " igual à amostra.";
                    break;
                case MTMResultadoTear.NivelCompleto:
                case MTMResultadoTear.TudoCompleto:
                    TocarSom("vitoria", .7f, 0f);
                    acertoTearAte = Time.unscaledTime + .6f;
                    nivelTearPronto = true;
                    retornoPrimeiroFio = "";
                    break;
            }

            GuardarPassagensPrimeiroFio();
        }

        private void AvancarNivelTear()
        {
            if (quadroDaAcao == Time.frameCount)
                return;
            quadroDaAcao = Time.frameCount;
            nivelTearPronto = false;
            if (sessaoTear.Terminou)
            {
                ConcluirTearPrimeiroFio();
                return;
            }

            sessaoTear.ProximoNivel();
            retornoPrimeiroFio = "";
            GuardarPassagensPrimeiroFio();
        }

        // Fim do tear: registra o primeiro fio com a amostra de referência da abertura.
        private void ConcluirTearPrimeiroFio()
        {
            if (aberturaMemorias == null)
                return;
            var amostra = new List<int>(aberturaMemorias.padrao);
            if (!MTMPrimeiroFio.Avancar(estado, aberturaMemorias, MTMPrimeiroFio.Teceu, amostra))
                return;
            estado.amostraPrimeiroFio = amostra;
            Salvar();
            AtualizarAmostraPrimeiroFio();
            LimparPrimeiroFio();
            painel = "";
            Abrir("O primeiro fio", aberturaMemorias.fonte, aberturaMemorias.conclusao, null);
        }
    }
}

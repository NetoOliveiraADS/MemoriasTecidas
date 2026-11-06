using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private void SoltarArrastoPrimeiroFioSeInativo()
        {
            if (controleArrastoPrimeiroFio != 0 &&
                (painel != "primeiro_fio" || !possuiFoco || !tearPrimeiroFio.Passando))
            {
                if (GUIUtility.hotControl == controleArrastoPrimeiroFio) GUIUtility.hotControl = 0;
                controleArrastoPrimeiroFio = 0;
            }
        }

        private void GestoBastidorUI(Rect r)
        {
            int controle = GUIUtility.GetControlID("MTM.Bastidor".GetHashCode(), FocusType.Passive, r);
            Event e = Event.current;
            int linha = tearPrimeiroFio.LinhaAtiva;
            if (linha >= 4 || !possuiFoco) return;
            float x = linha % 2 == 0 ? r.x : r.xMax;
            float y = r.y + (linha + .5f) * r.height / 4f;
            Rect lancadeira = new Rect(x - 28, y - 20, 56, 40);
            if (e.type == EventType.MouseDown && e.button == 0 && lancadeira.Contains(e.mousePosition) &&
                GUIUtility.hotControl == 0 && tearPrimeiroFio.IniciarPassagem())
            {
                passagemAutomaticaPrimeiroFio = false;
                controleArrastoPrimeiroFio = controle;
                GUIUtility.hotControl = controle;
                e.Use();
            }
            else if (controleArrastoPrimeiroFio == controle && GUIUtility.hotControl == controle)
            {
                if (e.type == EventType.MouseDrag)
                {
                    float progresso = MTMSessaoTecido.ProgressoDoGesto(e.mousePosition.x, r.x, r.width, linha);
                    if (tearPrimeiroFio.Percorrer(progresso))
                    {
                        GUIUtility.hotControl = 0;
                        controleArrastoPrimeiroFio = 0;
                        AoTerminarPassagemPrimeiroFio();
                    }
                    e.Use();
                }
                else if (e.rawType == EventType.MouseUp)
                {
                    tearPrimeiroFio.Cancelar();
                    GUIUtility.hotControl = 0;
                    controleArrastoPrimeiroFio = 0;
                    retornoPrimeiroFio = "Continue até a outra borda. Você também pode usar E para passar o fio.";
                    e.Use();
                }
            }
        }

        private void PrimeiroFioUI()
        {
            if (aberturaMemorias == null) { painel = ""; return; }
            Fundo(new Rect(0, 0, 1280, 720), new Color(.16f, .12f, .09f, .45f));
            PainelTecido(new Rect(28, 32, 1224, 656), algodaoUI);
            TextoTecido(new Rect(58, 54, 900, 42), aberturaMemorias.titulo, 30, acentoTextoUI, true);
            TramaFaixa(new Rect(28, 112, 1224, 5));
            if (BotaoTecido(new Rect(1080, 56, 138, 40), "Voltar"))
            {
                LimparPrimeiroFio();
                painel = "";
                return;
            }
            TextoTecido(new Rect(58, 138, 1150, 34),
                "Escolha um fio. Arraste a lançadeira até a outra borda ou pressione E.", 18, textoPapelUI);
            Fundo(new Rect(58, 188, 692, 344), CorTecido("#6A4E38", textoPapelUI));
            TextoTecido(new Rect(82, 204, 640, 24),
                "SUA AMOSTRA · " + passagensPrimeiroFio.Count + " / 4 PASSAGENS", 13, algodaoUI, true);
            Rect bastidor = new Rect(122, 250, 556, 224);
            GestoBastidorUI(bastidor);
            DesenharTramaInterativaUI(bastidor, passagensPrimeiroFio, true);
            TextoTecido(new Rect(82, 498, 640, 24),
                "Toque uma linha pronta ou use Tab para corrigir essa passagem.", 13, algodaoUI, false, TextAnchor.MiddleCenter);

            Fundo(new Rect(774, 188, 444, 344), papelUI);
            TextoTecido(new Rect(798, 204, 396, 28), "Amostra de referência", 22, textoPapelUI, true);
            DesenharTramaInterativaUI(new Rect(832, 258, 320, 180), aberturaMemorias.padrao, false);
            TextoTecido(new Rect(798, 464, 396, 48),
                "Observe de cima para baixo. O modelo fica visível durante toda a experiência.", 14, textoPapelUI);

            for (int i = 0; i < 3; i++)
            {
                Rect botao = new Rect(58 + i * 232, 548, 216, 46);
                if (BotaoTecido(botao, (i + 1) + " · " + aberturaMemorias.nomes[i],
                    tearPrimeiroFio.CorSelecionada == i, !tearPrimeiroFio.Passando))
                    EscolherPassagemPrimeiroFio(i);
                Fundo(new Rect(botao.x + 1, botao.yMax - 5, botao.width - 2, 4),
                    CorTecido(aberturaMemorias.cores[i], ouro));
            }
            TextoTecido(new Rect(774, 548, 444, 42),
                "Sem tempo limite. As passagens completas ficam salvas ao sair.", 14, textoPapelUI);
            TextoTecido(new Rect(58, 600, 1160, 26), retornoPrimeiroFio, 14, acentoTextoUI, true);
            if (BotaoTecido(new Rect(58, 636, 216, 36), "Desfazer [U]", false, passagensPrimeiroFio.Count > 0))
                DesfazerPrimeiroFio();
            if (BotaoTecido(new Rect(290, 636, 216, 36), "Recomeçar"))
                RecomecarAmostraPrimeiroFio();
            bool completa = passagensPrimeiroFio.Count == 4 && tearPrimeiroFio.LinhaEmEdicao < 0;
            if (BotaoTecido(new Rect(774, 632, 444, 40),
                completa ? "Conferir a amostra [E / Enter]" : "Passar o fio [E / Enter]",
                true, !tearPrimeiroFio.Passando))
                PassarOuConcluirPrimeiroFio();
            TextoTecido(new Rect(526, 638, 226, 30), "1 / 2 / 3 · escolher fio", 12, textoPapelUI, false, TextAnchor.MiddleCenter);
        }

        private void DesenharTramaInterativaUI(Rect r, IList<int> linhas, bool interativa)
        {
            Color madeira = CorTecido("#8A472E", terracotaUI);
            Fundo(new Rect(r.x - 12, r.y - 12, r.width + 24, 8), madeira);
            Fundo(new Rect(r.x - 12, r.yMax + 6, r.width + 24, 8), madeira);
            for (int coluna = 0; coluna < 22; coluna++)
                Fundo(new Rect(r.x + coluna * r.width / 21f, r.y - 5, 2, r.height + 16), AlfaTecido(algodaoUI, .75f));
            float passo = r.height / 4f;
            for (int linha = 0; linha < 4; linha++)
            {
                float y = r.y + linha * passo + 5f;
                Rect faixa = new Rect(r.x - 4, y, r.width + 8, passo - 10);
                bool ativa = interativa && tearPrimeiroFio.LinhaAtiva == linha;
                bool animando = ativa && tearPrimeiroFio.Passando;
                int cor = animando ? tearPrimeiroFio.CorSelecionada : linha < linhas.Count ? linhas[linha] : -1;
                float progresso = animando ? tearPrimeiroFio.Progresso : cor >= 0 ? 1f : 0f;
                float inicio = linha % 2 == 0 ? faixa.x : faixa.xMax - faixa.width * progresso;
                Color tintaFio = cor >= 0 ? CorTecido(aberturaMemorias.cores[cor], ouro) : algodaoUI;
                Fundo(faixa, AlfaTecido(algodaoUI, .08f));
                if (progresso > 0f)
                {
                    Rect tecido = new Rect(inicio, y, faixa.width * progresso, faixa.height);
                    Fundo(tecido, tintaFio);
                    for (int fio = 1; fio < 6; fio++)
                        Fundo(new Rect(tecido.x, y + fio * faixa.height / 6f, tecido.width, 1), AlfaTecido(textoPapelUI, .13f));
                    for (int coluna = 0; coluna < 22; coluna++)
                    {
                        float x = r.x + coluna * r.width / 21f;
                        if ((linha + coluna) % 2 == 0 && x >= tecido.x && x < tecido.xMax)
                            Fundo(new Rect(x, y, 2, faixa.height), AlfaTecido(algodaoUI, .85f));
                    }
                }
                TextoTecido(new Rect(r.x - 40, y + 4, 28, 26), (linha + 1).ToString(), 16,
                    interativa ? algodaoUI : textoPapelUI, true, TextAnchor.MiddleCenter);
                if (!interativa && cor >= 0)
                    TextoTecido(new Rect(r.x + 8, y + 2, r.width - 16, faixa.height - 4),
                        aberturaMemorias.nomes[cor], 14, cor == 2 ? algodaoUI : textoPapelUI, true, TextAnchor.MiddleCenter);
                if (interativa && linha < linhas.Count)
                {
                    if (linha == linhaDiferentePrimeiroFio || tearPrimeiroFio.LinhaEmEdicao == linha)
                        BordaTecido(faixa, ouro, 3);
                    bool habilitado = GUI.enabled;
                    GUI.enabled = habilitado && !tearPrimeiroFio.Passando;
                    if (AcaoTecido(faixa, "primeirofio:linha:" + linha))
                    {
                        tearPrimeiroFio.EditarLinha(linha);
                        retornoPrimeiroFio = "Linha " + (linha + 1) + ": escolha o fio e faça a passagem novamente.";
                    }
                    GUI.enabled = habilitado;
                }
                if (ativa && tearPrimeiroFio.CorSelecionada >= 0)
                {
                    float x = linha % 2 == 0 ? Mathf.Lerp(r.x, r.xMax, tearPrimeiroFio.Progresso) :
                        Mathf.Lerp(r.xMax, r.x, tearPrimeiroFio.Progresso);
                    Rect lancadeira = new Rect(x - 24, y + faixa.height * .5f - 12, 48, 24);
                    Fundo(lancadeira, madeira);
                    BordaTecido(lancadeira, ouro, 2);
                    Fundo(new Rect(x - 12, lancadeira.y + 5, 24, 14),
                        CorTecido(aberturaMemorias.cores[tearPrimeiroFio.CorSelecionada], ouro));
                }
            }
        }

        private void RedeMemoriasUI()
        {
            if (aberturaMemorias == null)
            {
                TextoTecido(new Rect(80, 300, 1120, 120), "A rede de memórias não foi carregada. Confira abertura_memorias.json. As missões e o caderno continuam disponíveis.", 22, textoPapelUI);
                return;
            }

            var motivos = aberturaMemorias.motivos;
            if (!Array.Exists(motivos, m => m.id == motivoSelecionadoRede))
                motivoSelecionadoRede = motivos[0].id;
            PainelTecido(new Rect(58, 252, 736, 378), tinta);
            TextoTecido(new Rect(84, 270, 682, 24), "SUA REDE  ·  " + MTMPrimeiroFio.Quantidade(estado, aberturaMemorias) + " / " + motivos.Length + " MOTIVOS REVELADOS", 13, acentoTextoUI, true);
            // Linhas de urdidura fixas; as cores dos motivos registram a progressão.
            for (int i = 0; i < 30; i++)
                Fundo(new Rect(88 + i * 23, 302, 1, 272), AlfaTecido(algodaoUI, .13f));
            Fundo(new Rect(80, 300, 692, 4), terracotaUI);
            Fundo(new Rect(80, 574, 692, 4), terracotaUI);
            for (int i = 0; i < motivos.Length; i++)
            {
                var motivo = motivos[i];
                bool revelado = MTMPrimeiroFio.Revelado(estado, motivo), selecionado = motivo.id == motivoSelecionadoRede;
                Rect r = new Rect(88 + i % 3 * 230, 312 + i / 3 * 88, 214, 76);
                Color cor = CorTecido(motivo.cor, ouro);
                Fundo(r, revelado ? Color.Lerp(tinta, cor, .16f) : profundoUI);
                if (revelado)
                {
                    PanoTecido(r, AlfaTecido(cor, .48f));
                    for (int fio = 0; fio < 5; fio++)
                        Fundo(new Rect(r.x + 7, r.yMax - 14 + fio * 2, r.width - 14, 1), AlfaTecido(cor, .6f));
                    IconeTecido(motivo.icone, new Rect(r.x + 12, r.y + 15, 38, 38), cor);
                }
                else
                {
                    NoTecido(new Vector2(r.x + 30, r.y + 34), AlfaTecido(algodaoUI, .35f), 6);
                    NoTecido(new Vector2(r.x + 30, r.y + 34), profundoUI, 3);
                }

                TextoTecido(new Rect(r.x + 60, r.y + 12, 142, 44), revelado ? motivo.titulo : "Ainda por descobrir", 14, revelado ? textoSuperficieUI : AlfaTecido(textoSuperficieUI, .60f), revelado);
                BordaTecido(r, selecionado ? ouro : AlfaTecido(algodaoUI, .16f), selecionado ? 2 : 1);
                if (AcaoTecido(r, "motivo:" + motivo.id))
                {
                    motivoSelecionadoRede = motivo.id;
                    rolagemDetalhe = Vector2.zero;
                }
            }

            bool terminou = estado.Tem(MTMPrimeiroFio.Memoria);
            if (BotaoTecido(new Rect(84, 590, 332, 28), terminou ? "Revisitar Feliciana no mapa" : "Acompanhar o primeiro fio"))
            {
                if (terminou)
                {
                    var local = LocalPrimeiroFio();
                    if (local != null)
                        AbrirMapaTecido(local.dados.id);
                }
                else
                    AcompanharPrimeiroFio();
            }

            if (BotaoTecido(new Rect(432, 590, 336, 28), "Continuar pela campanha", true))
            {
                AdiarPrimeiroFio();
                painel = "";
                Avisar(ObjetivoTecido());
            }

            var escolhido = Array.Find(motivos, m => m.id == motivoSelecionadoRede);
            bool aberto = MTMPrimeiroFio.Revelado(estado, escolhido);
            Fundo(new Rect(818, 252, 400, 378), papelUI);
            TextoTecido(new Rect(844, 272, 348, 20), aberto ? "UMA HISTÓRIA NA TRAMA" : "UM ESPAÇO PARA NOVOS ENCONTROS", 11, terracotaUI, true);
            TextoTecido(new Rect(844, 310, 348, 72), aberto ? escolhido.titulo : "A rede cresce com a jornada", 25, textoPapelUI, true);
            string resumo = aberto ? escolhido.resumo : "Converse, observe e participe das atividades da cidade. Os encontros importantes vão preencher estes espaços, sem precisar trocar materiais por pontos.";
            float altura = AlturaTecido(resumo, 320, 18);
            rolagemDetalhe = IniciarRolagemUI(new Rect(844, 394, 348, 132), rolagemDetalhe, new Rect(0, 0, 320, Mathf.Max(132, altura)));
            TextoTecido(new Rect(0, 0, 320, altura), resumo, 18, textoPapelUI);
            TerminarRolagemUI();
            if (BotaoTecido(new Rect(844, 574, 348, 36), aberto ? "Ler o registro desse encontro" : "Ver minhas missões", true))
            {
                if (aberto)
                {
                    abaCaderno = "memorias";
                    registroSelecionadoRoteiro = escolhido.registro;
                }
                else
                    abaCaderno = "missoes";
                rolagemLista = rolagemDetalhe = Vector2.zero;
            }
        }

        private void RedeMemoriasHUD()
        {
            if (aberturaMemorias == null)
                return;
            Rect r = new Rect(1090, 90, 166, 32);
            PainelTecido(r, FundoHUDClaroUI, false);
            TextoTecido(new Rect(1098, 94, 150, 24), "Rede · " + MTMPrimeiroFio.Quantidade(estado, aberturaMemorias) + " / " + aberturaMemorias.motivos.Length, 13, textoSuperficieUI, true, TextAnchor.MiddleCenter);
            if (AcaoTecido(r, "hud:rede"))
                AbrirRedeMemorias();
        }
    }
}

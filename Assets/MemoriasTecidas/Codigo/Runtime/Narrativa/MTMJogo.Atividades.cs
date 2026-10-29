using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private MTMPuzzleTecido atividadeRoteiro;
        private MTMMissaoTecido missaoAtividade;
        private MTMEtapaTecido etapaAtividade;
        private readonly List<int> respostaAtividade = new List<int>();
        private string retornoAtividade = "", pistaSelecionadaRoteiro = "", registroSelecionadoRoteiro = "";
        private bool referenciasAtividade;
        private Vector2 rolagemAtividade;
        private void AbrirAtividadeRoteiro(MTMMissaoTecido m, MTMEtapaTecido e)
        {
            atividadeRoteiro = Array.Find(tramaTecida.atividades, a => a.id == e.atividade);
            if (atividadeRoteiro == null)
            {
                Avisar("Atividade ausente no roteiro. Confira o pacote completo.");
                return;
            }

            missaoAtividade = m;
            etapaAtividade = e;
            respostaAtividade.Clear();
            retornoAtividade = "";
            referenciasAtividade = false;
            rolagemAtividade = Vector2.zero;
            painel = "atividade";
            Parar();
        }

        private void LimparAtividadeRoteiro()
        {
            atividadeRoteiro = null;
            missaoAtividade = null;
            etapaAtividade = null;
            respostaAtividade.Clear();
            referenciasAtividade = false;
        }

        private void EscolherOpcaoRoteiro(int i)
        {
            if (atividadeRoteiro == null)
                return;
            retornoAtividade = "";
            if (atividadeRoteiro.tipo == "conjunto" || atividadeRoteiro.tipo == "grade")
            {
                if (respostaAtividade.Contains(i))
                    respostaAtividade.Remove(i);
                else
                    respostaAtividade.Add(i);
            }
            else if (atividadeRoteiro.tipo == "escolha")
            {
                respostaAtividade.Clear();
                respostaAtividade.Add(i);
            }
            else if (respostaAtividade.Count < atividadeRoteiro.resposta.Length)
                respostaAtividade.Add(i);
        }

        private void AtividadeRoteiroUI()
        {
            var a = atividadeRoteiro;
            if (a == null)
            {
                painel = "";
                return;
            }

            Fundo(new Rect(0, 0, 1280, 720), new Color(.16f, .12f, .09f, .60f));
            PainelTecido(new Rect(28, 32, 1224, 656), algodaoUI);
            Fundo(new Rect(28, 32, 1224, 142), tinta);
            TextoTecido(new Rect(58, 53, 1010, 24), "MEMÓRIAS TECIDAS  /  " + missaoAtividade.numero.ToString("00") + " · " + missaoAtividade.titulo, 13, acentoTextoUI, true);
            TextoTecido(new Rect(58, 90, 1050, 48), a.titulo, 29, textoSuperficieUI, true);
            TramaFaixa(new Rect(28, 174, 1224, 8));
            if (BotaoTecido(new Rect(1134, 54, 90, 38), "Sair"))
            {
                LimparAtividadeRoteiro();
                painel = "";
                return;
            }

            TextoTecido(new Rect(58, 201, 1160, 62), a.instrucao, 19, textoPapelUI);
            bool sequencia = a.tipo == "sequencia", grade = a.tipo == "grade";
            if (referenciasAtividade)
            {
                string texto = a.dica + "\n\n";
                foreach (string id in a.requerPistas)
                {
                    var p = Array.Find(tramaTecida.pistas, x => x.id == id);
                    if (p != null && MTMMotorMissoes.TemPista(estado, tramaTecida, id))
                        texto += p.titulo + "\n" + p.texto + "\n\n";
                }

                float h = AlturaTecido(texto, 1090, 18);
                Fundo(new Rect(58, 278, 1160, 280), papelUI);
                rolagemAtividade = IniciarRolagemUI(new Rect(76, 294, 1124, 246), rolagemAtividade, new Rect(0, 0, 1098, Mathf.Max(246, h)));
                TextoTecido(new Rect(0, 0, 1090, h), texto, 18, textoPapelUI);
                TerminarRolagemUI();
            }
            else if (grade)
            {
                for (int i = 0; i < 9; i++)
                {
                    Rect r = new Rect(90 + i % 3 * 80, 286 + i / 3 * 80, 68, 68);
                    bool sel = respostaAtividade.Contains(i);
                    if (BotaoTecido(r, sel ? "CIMA" : "BAIXO", sel))
                        EscolherOpcaoRoteiro(i);
                }

                TextoTecido(new Rect(390, 308, 760, 128), "Cada quadrado é um cruzamento. Clique para alternar a passagem por cima ou por baixo.\n\nO desenho representa uma atividade didática de entrelaçamento.", 21, textoPapelUI);
            }
            else
            {
                float y = 286;
                if (sequencia)
                {
                    if (a.mostrarModelo)
                    {
                        var nomes = new List<string>();
                        foreach (int n in a.resposta)
                            nomes.Add(a.opcoes[n]);
                        TextoTecido(new Rect(58, 274, 1160, 30), "Modelo: " + string.Join(" · ", nomes.ToArray()), 16, textoPapelUI, true);
                    }

                    float largura = Mathf.Min(178, 1140f / a.resposta.Length);
                    for (int i = 0; i < a.resposta.Length; i++)
                    {
                        Rect r = new Rect(58 + i * largura, 318, largura - 10, 54);
                        Fundo(r, papelUI);
                        BordaTecido(r, AlfaTecido(tinta, .3f));
                        string nome = i < respostaAtividade.Count ? a.opcoes[respostaAtividade[i]] : (i + 1).ToString("00");
                        if (i < respostaAtividade.Count && a.cores != null && a.cores.Length > respostaAtividade[i])
                            Fundo(new Rect(r.x, r.y, r.width, 5), CorTecido(a.cores[respostaAtividade[i]], ouro));
                        TextoTecido(new Rect(r.x + 6, r.y + 10, r.width - 12, 38), nome, 15, textoPapelUI, true, TextAnchor.MiddleCenter);
                    }

                    y = 394;
                }

                int colunas = sequencia ? 4 : 3;
                float passo = sequencia ? 290 : 390;
                for (int i = 0; i < a.opcoes.Length; i++)
                {
                    Rect r = new Rect(58 + i % colunas * passo, y + i / colunas * 76, passo - 16, 64);
                    if (BotaoTecido(r, a.opcoes[i], !sequencia && respostaAtividade.Contains(i)))
                        EscolherOpcaoRoteiro(i);
                }
            }

            TextoTecido(new Rect(58, 566, 1160, 35), retornoAtividade, 16, terracotaUI, true);
            if (BotaoTecido(new Rect(58, 613, 270, 40), referenciasAtividade ? "Voltar à atividade" : "Consultar referências"))
                referenciasAtividade = !referenciasAtividade;
            if (BotaoTecido(new Rect(344, 613, 180, 40), "Desfazer"))
            {
                if (respostaAtividade.Count > 0)
                    respostaAtividade.RemoveAt(respostaAtividade.Count - 1);
                retornoAtividade = "";
            }

            if (BotaoTecido(new Rect(540, 613, 180, 40), "Recomeçar"))
            {
                respostaAtividade.Clear();
                retornoAtividade = "";
            }

            if (BotaoTecido(new Rect(740, 609, 478, 44), "Conferir e concluir", true, !referenciasAtividade) && quadroDaAcao != Time.frameCount)
            {
                quadroDaAcao = Time.frameCount;
                if (!MTMMotorMissoes.RespostaCorreta(estado, tramaTecida, a, respostaAtividade))
                {
                    retornoAtividade = "Ainda não corresponde às referências. Você pode corrigir e tentar novamente.";
                    return;
                }

                if (ConcluirEtapaRoteiro(missaoAtividade, etapaAtividade, respostaAtividade, false))
                {
                    var concluida = etapaAtividade;
                    string titulo = a.titulo;
                    LimparAtividadeRoteiro();
                    painel = "";
                    if (concluida.falasConclusao != null && concluida.falasConclusao.Length > 0)
                        Abrir(titulo, FonteRoteiro, concluida.falasConclusao, null);
                    return;
                }

                retornoAtividade = "Confira o horário e os materiais necessários para concluir.";
            }

            TextoTecido(new Rect(58, 666, 1150, 16), "Esc sai sem concluir esta atividade. As etapas anteriores permanecem salvas.", 11, AlfaTecido(textoPapelUI, .75f));
        }

        private void ObservacaoRoteiroUI()
        {
            PainelTecido(new Rect(340, 590, 600, 66), tinta);
            TextoTecido(new Rect(364, 608, 552, 35), "Uma forma aparece ao longe e desaparece…", 20, textoSuperficieUI, true, TextAnchor.MiddleCenter);
        }

        private void PistasRoteiroUI()
        {
            var lista = new List<MTMPistaTecida>();
            foreach (var p in tramaTecida.pistas)
                if (estado.Tem(p.evento))
                    lista.Add(p);
            if (lista.Count == 0)
            {
                TextoTecido(new Rect(270, 345, 740, 120), "As pistas serão registradas conforme a investigação avançar. Primeiro, conheça os lugares e as pessoas.", 22, textoPapelUI, false, TextAnchor.MiddleCenter);
                return;
            }

            if (!lista.Exists(p => p.id == pistaSelecionadaRoteiro))
                pistaSelecionadaRoteiro = lista[0].id;
            rolagemLista = IniciarRolagemUI(new Rect(58, 252, 350, 378), rolagemLista, new Rect(0, 0, 326, Mathf.Max(378, lista.Count * 74)));
            for (int i = 0; i < lista.Count; i++)
            {
                var p = lista[i];
                Rect r = new Rect(0, i * 74, 324, 64);
                bool sel = p.id == pistaSelecionadaRoteiro;
                Fundo(r, sel ? tinta : papelUI);
                TextoTecido(new Rect(14, r.y + 10, 296, 44), p.ordem.ToString("00") + " · " + p.titulo, 16, sel ? textoSuperficieUI : textoPapelUI, true);
                if (AcaoTecido(r, "pista:" + p.id))
                {
                    pistaSelecionadaRoteiro = p.id;
                    rolagemDetalhe = Vector2.zero;
                }
            }

            TerminarRolagemUI();
            var escolhido = lista.Find(p => p.id == pistaSelecionadaRoteiro);
            Fundo(new Rect(434, 252, 784, 378), papelUI);
            TextoTecido(new Rect(460, 272, 730, 50), escolhido.titulo, 25, textoPapelUI, true);
            TextoTecido(new Rect(460, 326, 730, 24), escolhido.lugar, 15, terracotaUI, true);
            float h = AlturaTecido(escolhido.texto, 700, 20);
            rolagemDetalhe = IniciarRolagemUI(new Rect(460, 370, 730, 178), rolagemDetalhe, new Rect(0, 0, 704, Mathf.Max(178, h)));
            TextoTecido(new Rect(0, 0, 700, h), escolhido.texto, 20, textoPapelUI);
            TerminarRolagemUI();
            TextoTecido(new Rect(460, 576, 730, 38), "Pista ficcional da investigação. O significado final do símbolo ainda não foi revelado.", 13, terracotaUI);
        }

        private void RegistrosRoteiroUI()
        {
            var etapas = new List<MTMEtapaTecido>();
            foreach (var m in tramaTecida.missoes)
                foreach (var e in m.etapas)
                    if (MTMTrama.Concluida(estado, e))
                        etapas.Add(e);
            if (etapas.Count == 0)
            {
                TextoTecido(new Rect(260, 346, 760, 120), "Converse com Dona Feliciana para começar a registrar a jornada.", 23, textoPapelUI, false, TextAnchor.MiddleCenter);
                return;
            }

            if (!etapas.Exists(e => e.eventos[0] == registroSelecionadoRoteiro))
                registroSelecionadoRoteiro = etapas[etapas.Count - 1].eventos[0];
            rolagemLista = IniciarRolagemUI(new Rect(58, 252, 350, 378), rolagemLista, new Rect(0, 0, 326, Mathf.Max(378, etapas.Count * 78)));
            for (int i = 0; i < etapas.Count; i++)
            {
                var e = etapas[i];
                Rect r = new Rect(0, i * 78, 324, 68);
                bool sel = e.eventos[0] == registroSelecionadoRoteiro;
                Fundo(r, sel ? tinta : papelUI);
                TextoTecido(new Rect(14, r.y + 9, 296, 50), e.texto, 15, sel ? textoSuperficieUI : textoPapelUI, true);
                if (AcaoTecido(r, "registro:" + e.eventos[0]))
                {
                    registroSelecionadoRoteiro = e.eventos[0];
                    rolagemDetalhe = Vector2.zero;
                }
            }

            TerminarRolagemUI();
            var registro = etapas.Find(e => e.eventos[0] == registroSelecionadoRoteiro);
            Fundo(new Rect(434, 252, 784, 378), papelUI);
            TextoTecido(new Rect(460, 270, 730, 52), registro.texto, 23, textoPapelUI, true);
            string conteudo = registro.registro + "\n\n" + string.Join("\n\n", registro.falas ?? new string[0]);
            if (registro.falasConclusao != null && registro.falasConclusao.Length > 0)
                conteudo += "\n\n" + string.Join("\n\n", registro.falasConclusao);
            float altura = AlturaTecido(conteudo, 700, 18);
            rolagemDetalhe = IniciarRolagemUI(new Rect(460, 338, 730, 212), rolagemDetalhe, new Rect(0, 0, 704, Mathf.Max(212, altura)));
            TextoTecido(new Rect(0, 0, 700, altura), conteudo, 18, textoPapelUI);
            TerminarRolagemUI();
            TextoTecido(new Rect(460, 576, 730, 38), FonteRoteiro, 12, AlfaTecido(textoPapelUI, .8f));
        }
    }
}

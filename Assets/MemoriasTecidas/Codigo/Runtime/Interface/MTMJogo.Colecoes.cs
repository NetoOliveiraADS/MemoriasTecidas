using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private void PanoTecido(Rect r, Color cor)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Color antes = GUI.color;
            GUI.color = cor;
            GUI.DrawTextureWithTexCoords(r, arteTecida.Tecido, new Rect(0, 0, r.width / 48f, r.height / 48f), true);
            GUI.color = antes;
        }

        private void NoTecido(Vector2 centro, Color cor, float raio = 4)
        {
            CirculoTecido(new Rect(centro.x - raio, centro.y - raio, raio * 2, raio * 2), cor);
        }

        private void NosProgressoTecido(Rect r, int feitos, int total, Color cor, Color fundo)
        {
            if (total <= 0)
                return;
            float cy = r.center.y;
            float passo = total == 1 ? 0 : r.width / (total - 1);
            Linha(new Vector2(r.x, cy), new Vector2(r.xMax, cy), AlfaTecido(cor, .24f));
            if (feitos > 1)
                Linha(new Vector2(r.x, cy), new Vector2(r.x + passo * (feitos - 1), cy), cor);
            for (int i = 0; i < total; i++)
            {
                var centro = new Vector2(r.x + passo * i, cy);
                NoTecido(centro, i < feitos ? cor : AlfaTecido(cor, .48f), 4);
                if (i >= feitos)
                    NoTecido(centro, fundo, 2);
            }
        }

        private void RedeDecorativaTecido(Rect r, Color cor)
        {
            // Pontos estáveis: cache limitado, desenho dentro do próprio retângulo.
            Vector2[] cima = new Vector2[9], baixo = new Vector2[9];
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f, s = 4 * t * (1 - t);
                cima[i] = new Vector2(r.x + t * r.width, r.y + 4 + s * r.height * .28f);
                baixo[i] = new Vector2(r.x + t * r.width, r.y + 4 + s * r.height * .76f);
                if (i > 0)
                {
                    Linha(cima[i - 1], cima[i], cor);
                    Linha(baixo[i - 1], baixo[i], cor);
                }
            }

            for (int i = 1; i < 8; i++)
            {
                Linha(cima[i - 1], baixo[i], AlfaTecido(cor, cor.a * .75f));
                Linha(cima[i + 1], baixo[i], AlfaTecido(cor, cor.a * .75f));
                Linha(baixo[i], baixo[i] + Vector2.up * 9, cor);
                NoTecido(baixo[i], cor, 2);
            }

            NoTecido(cima[0], cor, 3);
            NoTecido(cima[8], cor, 3);
        }

        private string CategoriaBolsaTecido(string id)
        {
            return id == "material" ? "MATERIAIS" : id == "pista" ? "PISTAS" : id == "missao" ? "TAREFAS" : "LEMBRANÇAS";
        }

        private Color CorCategoriaTecido(string id)
        {
            return id == "material" ? terracotaUI : id == "pista" ? textoPapelUI : id == "missao" ? folhaUI : ouro;
        }

        private void BolsaUI()
        {
            if (tramaTecida == null)
            {
                MensagemSemDadosTecido();
                return;
            }

            string[] ids =
            {
                "todos",
                "material",
                "lembranca",
                "pista",
                "missao"
            }, rotulos =
            {
                "Todos",
                "Materiais",
                "Lembranças",
                "Pistas",
                "Tarefas"
            };
            for (int i = 0; i < ids.Length; i++)
                if (AbaTecido(new Rect(58 + i * 136, 204, 130, 34), rotulos[i], abaBolsa == ids[i]))
                {
                    abaBolsa = ids[i];
                    rolagemBolsa = rolagemDetalhe = Vector2.zero;
                }

            Fundo(new Rect(58, 248, 435, 36), papelUI);
            TextoTecido(new Rect(70, 256, 60, 18), "BUSCAR", 10, terracotaUI, true);
            var campo = new GUIStyle(EstiloTecido(15, false, TextAnchor.MiddleLeft));
            CorTextoUI(campo, textoPapelUI);
            campo.padding = new RectOffset(6, 6, 0, 0);
            Rect buscaRect = new Rect(137, 250, 290, 30);
            if (Event.current.type == EventType.MouseDown && !buscaRect.Contains(Event.current.mousePosition) && GUI.GetNameOfFocusedControl() == "MT_BuscaBolsa")
                GUI.FocusControl(null);
            RegistrarFocoUI(buscaRect, "campo:busca", "MT_BuscaBolsa");
            GUI.SetNextControlName("MT_BuscaBolsa");
            string busca = GUI.TextField(buscaRect, buscaBolsa, 80, campo);
            buscaBolsaEmFoco = GUI.GetNameOfFocusedControl() == "MT_BuscaBolsa";
            if (busca != buscaBolsa)
            {
                buscaBolsa = busca;
                rolagemBolsa = Vector2.zero;
            }

            if (BotaoTecido(new Rect(437, 250, 46, 30), "×", false, buscaBolsa.Length > 0))
            {
                buscaBolsa = "";
                rolagemBolsa = Vector2.zero;
                GUI.FocusControl("MT_BuscaBolsa");
            }

            if (BotaoTecido(new Rect(508, 248, 234, 36), somenteItensNaBolsa ? "Só o que tenho" : "Mostrar todos os itens", somenteItensNaBolsa))
            {
                somenteItensNaBolsa = !somenteItensNaBolsa;
                rolagemBolsa = Vector2.zero;
            }

            var itens = new List<MTMItemTecido>();
            foreach (var item in tramaTecida.itens)
            {
                int qtd = MTMTrama.Quantidade(estado, item.id);
                if ((abaBolsa == "todos" || item.categoria == abaBolsa) && (!item.ocultoAteObter || qtd > 0) && (!somenteItensNaBolsa || qtd > 0) && (string.IsNullOrWhiteSpace(buscaBolsa) || item.nome.IndexOf(buscaBolsa.Trim(), StringComparison.OrdinalIgnoreCase) >= 0))
                    itens.Add(item);
            }

            if (itens.Count == 0)
            {
                IconeTecido("bolsa", new Rect(330, 365, 96, 96), terracotaUI);
                TextoTecido(new Rect(84, 480, 632, 70), "Nenhum objeto neste filtro.\nExperimente outra categoria ou limpe a busca.", 18, textoPapelUI, false, TextAnchor.MiddleCenter);
                return;
            }

            if (!itens.Exists(i => i.id == itemSelecionado))
            {
                itemSelecionado = itens[0].id;
                rolagemDetalhe = Vector2.zero;
            }

            float altura = Mathf.Ceil(itens.Count / 3f) * 148;
            rolagemBolsa = IniciarRolagemUI(new Rect(58, 292, 684, 338), rolagemBolsa, new Rect(0, 0, 660, Mathf.Max(338, altura)));
            for (int i = 0; i < itens.Count; i++)
            {
                var item = itens[i];
                int qtd = MTMTrama.Quantidade(estado, item.id);
                bool ativo = item.id == itemSelecionado;
                Rect r = new Rect(i % 3 * 222, i / 3 * 148, 208, 136);
                Color destaque = CorCategoriaTecido(item.categoria);
                Fundo(r, ativo ? tinta : papelUI);
                BordaTecido(r, ativo ? ouro : AlfaTecido(tinta, .15f), ativo ? 2 : 1);
                Fundo(new Rect(r.x + 12, r.y + 12, 56, 56), ativo ? profundoUI : algodaoUI);
                IconeTecido(item.id, new Rect(r.x + 16, r.y + 16, 48, 48), ativo ? acentoTextoUI : destaque);
                TextoTecido(new Rect(r.x + 100, r.y + 19, 90, 28), qtd.ToString("00"), 25, ativo ? textoSuperficieUI : textoPapelUI, true, TextAnchor.MiddleRight);
                TextoTecido(new Rect(r.x + 78, r.y + 51, 112, 16), qtd == 0 ? "A REUNIR" : CategoriaBolsaTecido(item.categoria), 9, ativo ? acentoTextoUI : terracotaUI, true, TextAnchor.MiddleRight);
                TextoTecido(new Rect(r.x + 14, r.y + 80, 180, 44), item.nome, 16, ativo ? textoSuperficieUI : textoPapelUI, true);
                if (AcaoTecido(r, "item:" + item.id))
                {
                    itemSelecionado = item.id;
                    rolagemDetalhe = Vector2.zero;
                }
            }

            TerminarRolagemUI();
            var selecionado = itens.Find(i => i.id == itemSelecionado);
            if (selecionado == null)
                return;
            PainelTecido(new Rect(772, 204, 446, 426), tinta);
            TextoTecido(new Rect(796, 224, 390, 18), CategoriaBolsaTecido(selecionado.categoria) + "  /  OBJETOS QUE CONTAM HISTÓRIAS", 10, acentoTextoUI, true);
            Fundo(new Rect(796, 258, 84, 84), profundoUI);
            CosturaTecido(new Rect(796, 258, 84, 84), AlfaTecido(ouro, .3f));
            IconeTecido(selecionado.id, new Rect(805, 267, 66, 66), acentoTextoUI);
            TextoTecido(new Rect(900, 260, 292, 52), selecionado.nome, 22, textoSuperficieUI, true);
            int quantidade = MTMTrama.Quantidade(estado, selecionado.id), necessario = MTMAutoriaRoteiro.Receita(selecionado.id);
            string uso = necessario > 0 ? (estado.rede ? "Já teceu sua rede" : quantidade + " / " + necessario + " para a rede") : quantidade + " na bolsa";
            TextoTecido(new Rect(900, 320, 292, 24), uso, 14, acentoTextoUI, true);
            string texto = selecionado.descricao + "\n\nONDE ENCONTRAR\n" + selecionado.origem;
            if (!string.IsNullOrEmpty(selecionado.dica))
                texto += "\n\n" + selecionado.dica;
            if (selecionado.categoria == "pista")
                texto += "\n\nA descoberta também fica registrada no caderno de pistas.";
            float h = Mathf.Max(176, AlturaTecido(texto, 370, 16));
            rolagemDetalhe = IniciarRolagemUI(new Rect(796, 364, 398, 180), rolagemDetalhe, new Rect(0, 0, 370, h));
            TextoTecido(new Rect(0, 0, 370, h), texto, 16, textoSuperficieUI);
            TerminarRolagemUI();
            if (BotaoTecido(new Rect(796, 574, 190, 36), "Ver no mapa", false, RegiaoTecido(selecionado.regiao) != null))
                AbrirMapaTecido(selecionado.regiao);
            if (BotaoTecido(new Rect(998, 574, 196, 36), selecionado.categoria == "pista" ? "Ler pistas" : "Receita da rede"))
            {
                painel = selecionado.categoria == "pista" ? "diario" : "receita";
                if (painel == "diario")
                    abaCaderno = "pistas";
                rolagemDetalhe = Vector2.zero;
            }
        }

        private void ReceitaTecido()
        {
            string[] ids =
            {
                "fio",
                "corda",
                "pano",
                "punho",
                "enfeite"
            };
            TextoTecido(new Rect(60, 208, 1140, 34), estado.rede ? "Uma rede, muitas histórias" : "Da matéria-prima à sua primeira rede", 26, textoPapelUI, true);
            TextoTecido(new Rect(60, 250, 1140, 24), "Cinco materiais se encontram no tear. Cada nó guarda um aprendizado.", 16, textoPapelUI);
            Linha(new Vector2(116, 316), new Vector2(1164, 316), terracotaUI);
            for (int i = 0; i < ids.Length; i++)
            {
                var item = Array.Find(tramaTecida.itens, a => a.id == ids[i]);
                int precisa = MTMAutoriaRoteiro.Receita(ids[i]), qtd = MTMTrama.Quantidade(estado, ids[i]);
                bool prontoItem = estado.rede || qtd >= precisa;
                Rect r = new Rect(60 + i * 232, 300, 216, 192);
                PainelTecido(r, prontoItem ? tinta : papelUI);
                NoTecido(new Vector2(r.center.x, r.y), terracotaUI, 4);
                IconeTecido(ids[i], new Rect(r.x + 76, r.y + 20, 64, 64), prontoItem ? acentoTextoUI : terracotaUI);
                TextoTecido(new Rect(r.x + 14, r.y + 101, 188, 43), item == null ? ids[i] : item.nome, 18, prontoItem ? textoSuperficieUI : textoPapelUI, true, TextAnchor.MiddleCenter);
                TextoTecido(new Rect(r.x + 14, r.y + 151, 188, 24), estado.rede ? "TECIDO NA REDE" : qtd + " / " + precisa + (prontoItem ? "  ·  PRONTO" : "  ·  FALTA"), 13, prontoItem ? acentoTextoUI : terracotaUI, true, TextAnchor.MiddleCenter);
            }

            TextoTecido(new Rect(60, 514, 1140, 45), estado.rede ? "Sua rede está pronta. Ela permanece na bolsa como lembrança da jornada." : estado.PodeTecelagem ? "Materiais reunidos. Termine o tutorial e a encomenda para tecer a rede nas cinco fases da oficina." : "Abra a bolsa para consultar a origem dos materiais. A encomenda completa apenas o que faltar.", 18, textoPapelUI);
            if (BotaoTecido(new Rect(60, 578, 548, 38), "Consultar minha bolsa"))
                painel = "bolsa";
            if (BotaoTecido(new Rect(632, 578, 588, 38), estado.rede ? "Continuar pelo caderno" : "Encontrar a oficina", true))
            {
                if (estado.rede)
                    painel = "diario";
                else
                    AbrirMapaTecido("oficina");
            }

            TextoTecido(new Rect(60, 633, 1140, 20), "Receita didática simplificada. A fabricação real envolve outros saberes e etapas.", 12, AlfaTecido(textoPapelUI, .75f));
        }

        private void MissoesTecido()
        {
            var missoes = tramaTecida.missoes;
            if (missoes.Length == 0)
                return;
            if (MissaoTecido(missaoSelecionadaTecido) == null)
            {
                var atualM = MissaoAtualTecido();
                missaoSelecionadaTecido = atualM == null ? missoes[0].id : atualM.id;
            }

            float total = 0;
            string cap = "";
            foreach (var m in missoes)
            {
                string c = MTMAutoriaRoteiro.Capitulo(m);
                if (c != cap)
                {
                    total += 40;
                    cap = c;
                }

                total += 90;
            }

            rolagemLista = IniciarRolagemUI(new Rect(58, 252, 350, 378), rolagemLista, new Rect(0, 0, 326, Mathf.Max(378, total)));
            float y = 0;
            cap = "";
            foreach (var m in missoes)
            {
                string c = MTMAutoriaRoteiro.Capitulo(m);
                if (c != cap)
                {
                    TextoTecido(new Rect(4, y + 8, 316, 30), c.ToUpperInvariant(), 11, terracotaUI, true);
                    y += 40;
                    cap = c;
                }

                bool concluida = MTMTrama.Concluida(estado, m), disponivel = DisponivelTecido(m), sel = m.id == missaoSelecionadaTecido;
                Rect r = new Rect(0, y, 324, 80);
                Fundo(r, sel ? tinta : papelUI);
                NoTecido(new Vector2(16, y + 19), sel ? ouro : terracotaUI, 4);
                TextoTecido(new Rect(30, y + 10, 280, 16), concluida ? "TECIDO" : !disponivel ? "POR DESCOBRIR" : m.opcional ? "FIO OPCIONAL" : "EM ABERTO", 10, sel ? acentoTextoUI : terracotaUI, true);
                TextoTecido(new Rect(14, y + 32, 296, 44), m.numero.ToString("00") + " · " + m.titulo, 16, sel ? textoSuperficieUI : textoPapelUI, true);
                if (AcaoTecido(r, "missao:" + m.id))
                {
                    missaoSelecionadaTecido = m.id;
                    rolagemDetalhe = Vector2.zero;
                }

                y += 90;
            }

            TerminarRolagemUI();
            var escolhida = MissaoTecido(missaoSelecionadaTecido);
            if (escolhida == null)
                return;
            Fundo(new Rect(434, 252, 784, 378), papelUI);
            if (!DisponivelTecido(escolhida))
            {
                IconeTecido("novelo", new Rect(758, 310, 96, 96), terracotaUI);
                TextoTecido(new Rect(460, 430, 730, 130), "Este fio ainda será descoberto.\n\nContinue os encontros anteriores para conhecer esta parte da história.", 21, textoPapelUI, false, TextAnchor.MiddleCenter);
                return;
            }

            TextoTecido(new Rect(460, 268, 730, 18), MTMAutoriaRoteiro.Capitulo(escolhida).ToUpperInvariant(), 11, terracotaUI, true);
            TextoTecido(new Rect(460, 298, 730, 63), escolhida.titulo, 25, textoPapelUI, true);
            int feitas = MTMTrama.Progresso(estado, escolhida);
            NosProgressoTecido(new Rect(464, 374, 492, 12), feitas, escolhida.etapas.Length, terracotaUI, papelUI);
            TextoTecido(new Rect(978, 368, 212, 24), feitas + " / " + escolhida.etapas.Length + " nós tecidos", 13, textoPapelUI, true, TextAnchor.MiddleRight);
            float descH = AlturaTecido(escolhida.descricao, 700, 16);
            float conteudo = descH + 28;
            foreach (var e in escolhida.etapas)
                conteudo += Mathf.Max(48, AlturaTecido(e.texto, 654, 16) + 18);
            rolagemDetalhe = IniciarRolagemUI(new Rect(460, 404, 730, 152), rolagemDetalhe, new Rect(0, 0, 704, Mathf.Max(152, conteudo)));
            TextoTecido(new Rect(0, 0, 700, descH), escolhida.descricao, 16, textoPapelUI);
            float ey = descH + 24;
            foreach (var e in escolhida.etapas)
            {
                bool ok = MTMTrama.Concluida(estado, e);
                float eh = Mathf.Max(48, AlturaTecido(e.texto, 654, 16) + 18);
                NoTecido(new Vector2(11, ey + 12), ok ? folhaUI : terracotaUI, 5);
                if (!ok)
                    NoTecido(new Vector2(11, ey + 12), papelUI, 3);
                TextoTecido(new Rect(36, ey, 654, eh - 10), e.texto, 16, textoPapelUI, ok);
                ey += eh;
            }

            TerminarRolagemUI();
            bool pode = !MTMTrama.Concluida(estado, escolhida);
            if (BotaoTecido(new Rect(460, 578, 342, 36), pode ? "Acompanhar este fio" : "Missão concluída", true, pode))
            {
                AdiarPrimeiroFio();
                missaoFixadaTecido = escolhida.id;
                destinoMapa = "";
                destinoGuiadoTecido = "";
                painel = "";
            }

            if (BotaoTecido(new Rect(818, 578, 372, 36), "Ver próximo passo no mapa", false, pode))
            {
                AdiarPrimeiroFio();
                missaoFixadaTecido = escolhida.id;
                destinoGuiadoTecido = "";
                AbrirMapaTecido(RegiaoDaEtapaTecido(EtapaAtualTecido(escolhida)));
            }
        }
    }
}

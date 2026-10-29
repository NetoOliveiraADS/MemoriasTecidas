using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        // Compatibilidade com o módulo Vida V22. A renderização passa a ter uma única entrada.
        private GUIStyle tituloUI, textoUI, pequenoUI, centroUI;
        private bool interfaceVisivel = true, mostrarObjetivoTecido = true;
        private Color tinta, ouro, algodaoUI, papelUI, terracotaUI, folhaUI, textoPapelUI, profundoUI;
        private readonly Dictionary<int, GUIStyle> estilosTecido = new Dictionary<int, GUIStyle>();
        private readonly Dictionary<Vector2Int, Texture2D> linhasTecido = new Dictionary<Vector2Int, Texture2D>();
        private Texture2D circuloTecido;
        private Vector2 rolagemBolsa, rolagemMapa, rolagemLista, rolagemDetalhe, rolagemDialogo;
        private string itemSelecionado = "algodao", abaBolsa = "todos", abaCaderno = "missoes";
        private string pessoaSelecionada = "", memoriaSelecionada = "", ultimaFalaTecido = "";
        private string destinoGuiadoTecido = "";
        private int filtroMapa;
        private bool mapaEmCartoes;
        private string buscaBolsa = "";
        private bool somenteItensNaBolsa = true, buscaBolsaEmFoco;
        private readonly MTMIconesTecido arteTecida = new MTMIconesTecido();
        private static Color CorTecido(string hex, Color padrao)
        {
            Color c;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c) ? c : padrao;
        }

        private void Estilos()
        {
            if (tituloUI != null)
                return;
            AplicarTemaAreiaUI();
            tituloUI = EstiloTecido(28, true, TextAnchor.UpperLeft);
            CorTextoUI(tituloUI, acentoTextoUI);
            textoUI = EstiloTecido(20, false, TextAnchor.UpperLeft);
            CorTextoUI(textoUI, textoSuperficieUI);
            pequenoUI = EstiloTecido(16, false, TextAnchor.UpperLeft);
            CorTextoUI(pequenoUI, textoSuperficieUI);
            centroUI = EstiloTecido(16, false, TextAnchor.MiddleCenter);
            CorTextoUI(centroUI, textoPapelUI);
            if (circuloTecido != null)
                return;
            circuloTecido = new Texture2D(48, 48, TextureFormat.RGBA32, false)
            {
                name = "MT_Circulo_UI",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color[48 * 48];
            for (int y = 0; y < 48; y++)
                for (int x = 0; x < 48; x++)
                    pixels[y * 48 + x] = new Color(1, 1, 1, Mathf.Clamp01(24f - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(24, 24))));
            circuloTecido.SetPixels(pixels);
            circuloTecido.Apply(false, true);
        }

        private GUIStyle EstiloTecido(int tamanho, bool negrito, TextAnchor alinhamento)
        {
            tamanho = Mathf.Max(12, tamanho);
            int chave = tamanho * 100 + (negrito ? 50 : 0) + (int)alinhamento;
            GUIStyle estilo;
            if (!estilosTecido.TryGetValue(chave, out estilo))
            {
                estilo = new GUIStyle(GUI.skin.label)
                {
                    fontSize = tamanho,
                    fontStyle = negrito ? FontStyle.Bold : FontStyle.Normal,
                    alignment = alinhamento,
                    wordWrap = true,
                    richText = false,
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0)
                };
                Font fonte = negrito ? fonteForteUI : fonteLeituraUI;
                if (fonte != null)
                {
                    estilo.font = fonte;
                    estilo.fontStyle = FontStyle.Normal;
                }

                CorTextoUI(estilo, textoPapelUI);
                estilosTecido.Add(chave, estilo);
            }

            return estilo;
        }

        // IMGUI pode usar hover/active mesmo em Labels. Nunca herdar o branco da skin.
        public static void CorTextoUI(GUIStyle estilo, Color cor)
        {
            estilo.normal.textColor = estilo.hover.textColor = estilo.active.textColor = estilo.focused.textColor = cor;
            estilo.onNormal.textColor = estilo.onHover.textColor = estilo.onActive.textColor = estilo.onFocused.textColor = cor;
        }

        private void TextoTecido(Rect r, string texto, int tamanho, Color cor, bool negrito = false, TextAnchor alinhamento = TextAnchor.UpperLeft)
        {
            var e = EstiloTecido(tamanho, negrito, alinhamento);
            if (cor == terracotaUI)
                cor = acentoTextoUI;
            r.height = Mathf.Max(r.height, e.fontSize * 1.25f);
            CorTextoUI(e, cor);
            GUI.Label(r, texto ?? "", e);
        }

        private float AlturaTecido(string texto, float largura, int tamanho)
        {
            return EstiloTecido(tamanho, false, TextAnchor.UpperLeft).CalcHeight(new GUIContent(texto ?? ""), largura);
        }

        private static void Fundo(Rect r, Color c)
        {
            Color anterior = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = anterior;
        }

        private static Color AlfaTecido(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        private void CirculoTecido(Rect r, Color cor)
        {
            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTexture(r, circuloTecido);
            GUI.color = antiga;
        }

        private void Linha(Vector2 a, Vector2 b, Color cor)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Vector2 d = b - a;
            if (d.sqrMagnitude < .0001f)
                return;
            if (Mathf.Abs(d.y) < .01f)
            {
                Fundo(new Rect(Mathf.Min(a.x, b.x), a.y - 1f, Mathf.Abs(d.x), 2f), cor);
                return;
            }

            if (Mathf.Abs(d.x) < .01f)
            {
                Fundo(new Rect(a.x - 1f, Mathf.Min(a.y, b.y), 2f, Mathf.Abs(d.y)), cor);
                return;
            }

            // Desenha a linha dentro de uma textura local, sem girar GUI.matrix.
            // Assim a escala 4K e o recorte das listas continuam a cargo do IMGUI.
            if (d.x < 0f)
            {
                Vector2 troca = a;
                a = b;
                b = troca;
                d = b - a;
            }

            var chave = new Vector2Int(Mathf.RoundToInt(d.x * 4f), Mathf.RoundToInt(d.y * 4f));
            Vector2 delta = new Vector2(chave.x * .25f, chave.y * .25f);
            if (delta.sqrMagnitude < .0001f)
                return;
            Vector2 inicio = new Vector2(2f + Mathf.Max(0f, -delta.x), 2f + Mathf.Max(0f, -delta.y));
            Texture2D textura;
            if (!linhasTecido.TryGetValue(chave, out textura))
            {
                int largura = Mathf.CeilToInt(Mathf.Abs(delta.x)) + 4, altura = Mathf.CeilToInt(Mathf.Abs(delta.y)) + 4;
                textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
                {
                    name = "MT_Linha_UI",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };
                var pixels = new Color32[largura * altura];
                float comprimento2 = delta.sqrMagnitude;
                for (int y = 0; y < altura; y++)
                    for (int x = 0; x < largura; x++)
                    {
                        Vector2 p = new Vector2(x + .5f, y + .5f);
                        float t = Mathf.Clamp01(Vector2.Dot(p - inicio, delta) / comprimento2);
                        float alpha = Mathf.Clamp01(1.5f - Vector2.Distance(p, inicio + t * delta));
                        // Texture2D começa embaixo; as coordenadas da GUI começam em cima.
                        pixels[(altura - 1 - y) * largura + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }

                textura.SetPixels32(pixels);
                textura.Apply(false, true);
                linhasTecido.Add(chave, textura);
            }

            Color antiga = GUI.color;
            try
            {
                GUI.color = cor;
                GUI.DrawTexture(new Rect(a.x - inicio.x, a.y - inicio.y, textura.width, textura.height), textura, ScaleMode.StretchToFill, true);
            }
            finally
            {
                GUI.color = antiga;
            }
        }

        private void BordaTecido(Rect r, Color cor, float largura = 1f)
        {
            Fundo(new Rect(r.x, r.y, r.width, largura), cor);
            Fundo(new Rect(r.x, r.yMax - largura, r.width, largura), cor);
            Fundo(new Rect(r.x, r.y, largura, r.height), cor);
            Fundo(new Rect(r.xMax - largura, r.y, largura, r.height), cor);
        }

        private void CosturaTecido(Rect r, Color cor)
        {
            for (float x = r.x + 10; x < r.xMax - 10; x += 12)
            {
                Fundo(new Rect(x, r.y + 8, 5, 1), cor);
                Fundo(new Rect(x, r.yMax - 9, 5, 1), cor);
            }

            for (float y = r.y + 18; y < r.yMax - 17; y += 12)
            {
                Fundo(new Rect(r.x + 8, y, 1, 5), cor);
                Fundo(new Rect(r.xMax - 9, y, 1, 5), cor);
            }
        }

        private void PainelTecido(Rect r, Color fundo, bool costura = true)
        {
            Fundo(new Rect(r.x + 3, r.y + 4, r.width, r.height), new Color(0, 0, 0, .16f));
            Fundo(r, fundo);
            BordaTecido(r, AlfaTecido(ouro, .24f));
            // A trama fica na borda; o miolo permanece limpo para leitura.
            if (costura)
                Fundo(new Rect(r.x, r.y, 3, r.height), ouro);
        }

        private void TramaFaixa(Rect r)
        {
            Fundo(r, terracotaUI);
            for (float x = r.x + 5; x < r.xMax - 14; x += 18)
            {
                Linha(new Vector2(x, r.y + 1), new Vector2(x + 9, r.yMax - 1), ouro);
                Linha(new Vector2(x, r.yMax - 1), new Vector2(x + 9, r.y + 1), AlfaTecido(algodaoUI, .7f));
            }
        }

        private bool BotaoTecido(Rect r, string texto, bool destaque = false, bool habilitado = true)
        {
            string chave = "botao:" + r.x + ":" + r.y + ":" + r.width;
            bool antes = GUI.enabled, interativo = antes && habilitado;
            int estadoAcao = EstadoAcaoUI(r, chave, interativo);
            Color fundo = FundoAcaoUI(destaque, interativo, estadoAcao);
            Color textoAcao = interativo ? (destaque ? algodaoUI : textoPapelUI) : textoDesativadoUI;
            // Desabilitado tem sua própria paleta legível; GUI.enabled só governa a ação.
            GUI.enabled = true;
            Fundo(r, fundo);
            BordaTecido(r, destaque && interativo ? fundo : AlfaTecido(textoPapelUI, .28f));
            TextoTecido(new Rect(r.x + 12, r.y + 4, r.width - 24, r.height - 8), texto, 16, textoAcao, true, TextAnchor.MiddleCenter);
            GUI.enabled = interativo;
            bool clicou = AcaoTecido(r, chave);
            GUI.enabled = antes;
            return clicou;
        }

        private bool AbaTecido(Rect r, string texto, bool ativa, bool escura = false)
        {
            string chave = "aba:" + texto;
            int estadoAcao = EstadoAcaoUI(r, chave, GUI.enabled);
            if (ativa || estadoAcao > 0)
                Fundo(r, FundoAcaoUI(false, GUI.enabled, estadoAcao));
            if (ativa)
                Fundo(new Rect(r.x, r.yMax - 3, r.width, 3), escura ? ouro : terracotaUI);
            TextoTecido(r, texto, 16, escura ? textoSuperficieUI : textoPapelUI, ativa, TextAnchor.MiddleCenter);
            return AcaoTecido(r, chave);
        }

        private void BarraTecido(Rect r, float valor, Color cor)
        {
            Fundo(r, AlfaTecido(cor, .18f));
            Fundo(new Rect(r.x, r.y, r.width * Mathf.Clamp01(valor), r.height), cor);
        }

        private void IconeTecido(string id, Rect r, Color cor)
        {
            var item = tramaTecida == null || tramaTecida.itens == null ? null : Array.Find(tramaTecida.itens, i => i.id == id);
            string nome = item != null && !string.IsNullOrEmpty(item.icone) ? item.icone : id;
            Color anterior = GUI.color;
            GUI.color = cor;
            GUI.DrawTexture(r, arteTecida.Obter(nome), ScaleMode.ScaleToFit, true);
            GUI.color = anterior;
        }

        private void OnGUI()
        {
            if (!pronto || atual == null || cameraDoMapa == null || (!interfaceVisivel && !Bloqueado))
                return;
            if (painel != "bolsa")
                buscaBolsaEmFoco = false;
            Estilos();
            if (vidaV22 == null)
                vidaV22 = GetComponent<MTMVidaV22>();
            Matrix4x4 matriz = GUI.matrix;
            Color cor = GUI.color, conteudo = GUI.contentColor;
            bool habilitado = GUI.enabled;
            Rect px = cameraDoMapa.pixelRect;
            float escala = Mathf.Min(px.width / 1280f, px.height / 720f);
            if (escala <= 0)
                return;
            GUI.matrix = Matrix4x4.identity;
            if (inicioAbertoUI)
                Fundo(new Rect(0, 0, Screen.width, Screen.height), profundoUI);
            GUI.matrix = Matrix4x4.TRS(new Vector3(px.x + (px.width - 1280f * escala) * .5f, Screen.height - px.yMax + (px.height - 720f * escala) * .5f, 0), Quaternion.identity, Vector3.one * escala);
            GUI.color = GUI.contentColor = Color.white;
            try
            {
                if ((painel != "mapaCidade" && painel != "mapa") || !possuiFoco)
                    SoltarArrastoMapaUI();
                if (painel != "mapa")
                    mapaGuiaAbertoUI = false;
                IniciarFocoUI();
                if (inicioAbertoUI && painel.Length == 0)
                    InicioUI();
                else if (falas != null)
                    DialogoUI();
                else if (painel.Length > 0)
                    PainelUI();
                else
                    HUDCompacta();
                FinalizarFocoUI();
            }
            finally
            {
                GUI.matrix = matriz;
                GUI.color = cor;
                GUI.contentColor = conteudo;
                GUI.enabled = habilitado;
            }
        }

        private bool JanelaTecido(string titulo, string subtitulo)
        {
            Fundo(new Rect(0, 0, 1280, 720), new Color(.16f, .12f, .09f, .70f));
            PainelTecido(new Rect(28, 32, 1224, 656), algodaoUI, false);
            Fundo(new Rect(28, 32, 1224, 148), tinta);
            TextoTecido(new Rect(58, 49, 840, 22), "MEMÓRIAS TECIDAS  /  SUA JORNADA", 12, acentoTextoUI, true);
            TituloEditorial(new Rect(56, 79, 840, 48), titulo, 32, textoSuperficieUI);
            if (BotaoTecido(new Rect(1072, 62, 146, 42), "Voltar · Esc"))
            {
                VoltarInterfaceUI();
                return false;
            }

            string[] ids =
            {
                "mapa",
                "bolsa",
                "diario"
            }, nomes =
            {
                "Mapa  [M]",
                "Bolsa  [I]",
                "Caderno  [J]"
            };
            for (int i = 0; i < 3; i++)
                if (AbaTecido(new Rect(58 + i * 212, 134, 196, 40), nomes[i], painel == ids[i] || (i == 1 && painel == "receita"), true))
                {
                    painel = ids[i];
                    rolagemLista = rolagemDetalhe = Vector2.zero;
                    confirmarNovo = false;
                }

            Fundo(new Rect(28, 180, 1224, 3), ouro);
            TextoTecido(new Rect(58, 658, 1140, 20), "Tab · selecionar    Enter / espaço · abrir    M · mapa    I · bolsa    J · caderno    Esc · voltar", 12, textoPapelUI);
            return true;
        }

        private void PainelUI()
        {
            if (painel == "mapa")
            {
                MapaGuiadoUI();
                return;
            }

            if (painel == "arteLugar")
            {
                LugarIlustradoUI();
                return;
            }

            if (painel == "mapaCidade")
            {
                MapaCidadeUI();
                return;
            }

            if (painel == "primeiro_fio")
            {
                PrimeiroFioUI();
                return;
            }

            if (painel == "atividade")
            {
                AtividadeRoteiroUI();
                return;
            }

            if (painel == "observacao")
            {
                ObservacaoRoteiroUI();
                return;
            }

            if (painel == "pausa")
            {
                PausaEditorialUI();
                return;
            }

            if (painel == "ajuda")
            {
                AjudaUI();
                return;
            }

            if (painel == "sobre")
            {
                SobreUI();
                return;
            }

            if (painel == "visual")
            {
                OpcoesVisuaisTecido();
                return;
            }

            string titulo = painel == "mapa" ? "Redes de Memórias" : painel == "bolsa" ? "Bolsa de viagem" : painel == "receita" ? "Receita da rede" : "Caderno de memórias";
            if (!JanelaTecido(titulo, "ENTRE MEMÓRIAS E LENDAS"))
                return;
            if (painel == "mapa")
                return;
            if (painel == "bolsa")
                BolsaUI();
            else
                DiarioUI();
        }

        private void DialogoUI()
        {
            PrepararFalaLegivelUI();
            PainelTecido(new Rect(24, 402, 1232, 294), tinta);
            TramaFaixa(new Rect(24, 402, 1232, 8));
            Fundo(new Rect(48, 432, 162, 220), profundoUI);
            CosturaTecido(new Rect(48, 432, 162, 220), AlfaTecido(ouro, .55f));
            if (retratoExibidoDialogo != null)
                DesenharRetratoTecido(new Rect(61, 447, 136, 188), retratoExibidoDialogo, recorteExibidoDialogo);
            else
                IconeTecido("diario", new Rect(87, 482, 84, 84), acentoTextoUI);
            TextoTecido(new Rect(238, 428, 970, 36), nomeExibidoDialogo, 26, acentoTextoUI, true);
            Fundo(new Rect(238, 470, 970, 1), AlfaTecido(ouro, .35f));
            float altura = Mathf.Max(126, AlturaTecido(textoExibidoDialogo, 946, tamanhoDialogoUI));
            rolagemDialogo = IniciarRolagemUI(new Rect(238, 482, 970, 126), rolagemDialogo, new Rect(0, 0, 946, altura));
            TextoTecido(new Rect(0, 0, 946, altura), textoExibidoDialogo, tamanhoDialogoUI, textoSuperficieUI);
            TerminarRolagemUI();
            TextoTecido(new Rect(238, 614, 760, 22), MTMTextoDialogo.Assinatura, 13, AlfaTecido(textoSuperficieUI, .85f));
            if (altura > 127)
                TextoTecido(new Rect(1020, 614, 188, 22), "Role para ler", 12, acentoTextoUI, false, TextAnchor.UpperRight);
            TextoTecido(new Rect(238, 652, 560, 24), "E / Enter · avançar    Esc · fechar    " + (pagina + 1) + " de " + falas.Length, 13, textoSuperficieUI);
            if (pagina > 0 && BotaoTecido(new Rect(824, 646, 148, 34), "Anterior"))
            {
                pagina--;
                quadroDaAcao = Time.frameCount;
            }

            if (BotaoTecido(new Rect(988, 642, 220, 40), pagina + 1 == falas.Length ? "Concluir" : "Continuar →"))
                Avancar();
        }

        private Texture2D ImagemTecido(string id)
        {
            var imagem = kitHD2D == null ? null : kitHD2D.Imagem(id);
            return imagem != null ? imagem : vidaV22 == null ? null : vidaV22.Imagem(id);
        }

        private void DiarioUI()
        {
            if (painel == "receita")
            {
                ReceitaTecido();
                return;
            }

            if (tramaTecida == null)
            {
                MensagemSemDadosTecido();
                return;
            }

            string[] ids =
            {
                "missoes",
                "rede",
                "memorias",
                "pessoas",
                "pistas"
            }, nomes =
            {
                "Missões",
                "Rede",
                "Memórias",
                "Pessoas",
                "Pistas"
            };
            for (int i = 0; i < ids.Length; i++)
                if (AbaTecido(new Rect(58 + i * 184, 204, 172, 34), nomes[i], abaCaderno == ids[i]))
                {
                    abaCaderno = ids[i];
                    rolagemLista = rolagemDetalhe = Vector2.zero;
                }

            if (abaCaderno == "missoes")
                MissoesTecido();
            else if (abaCaderno == "rede")
                RedeMemoriasUI();
            else if (abaCaderno == "pessoas")
                PessoasTecido();
            else if (abaCaderno == "pistas")
                PistasRoteiroUI();
            else
                RegistrosRoteiroUI();
        }

        private void PessoasTecido()
        {
            var lista = tramaTecida.personagens;
            if (lista.Length == 0)
                return;
            if (!Array.Exists(lista, p => p.id == pessoaSelecionada))
                pessoaSelecionada = lista[0].id;
            rolagemLista = IniciarRolagemUI(new Rect(58, 252, 350, 378), rolagemLista, new Rect(0, 0, 326, Mathf.Max(378, lista.Length * 66)));
            for (int i = 0; i < lista.Length; i++)
            {
                var p = lista[i];
                bool sel = p.id == pessoaSelecionada;
                Rect r = new Rect(0, i * 66, 324, 56);
                Fundo(r, sel ? tinta : papelUI);
                TextoTecido(new Rect(14, r.y + 10, 296, 24), p.nome, 17, sel ? textoSuperficieUI : textoPapelUI, true);
                TextoTecido(new Rect(14, r.y + 35, 296, 15), estado.Tem(p.evento) ? "ENCONTRO REGISTRADO" : "A CONHECER", 10, sel ? acentoTextoUI : terracotaUI, true);
                if (AcaoTecido(r, "pessoa:" + p.id))
                {
                    pessoaSelecionada = p.id;
                    rolagemDetalhe = Vector2.zero;
                }
            }

            TerminarRolagemUI();
            var pessoa = Array.Find(lista, p => p.id == pessoaSelecionada);
            var ponto = PontoTecido(pessoa.regiao, pessoa.id);
            Fundo(new Rect(434, 252, 784, 378), papelUI);
            Fundo(new Rect(460, 274, 136, 158), tinta);
            if (ponto != null && ponto.retrato != null)
                DesenharRetratoTecido(new Rect(466, 280, 124, 146), ponto.retrato, ponto.recorteRosto);
            else
                IconeTecido("pessoa", new Rect(490, 310, 76, 76), acentoTextoUI);
            TextoTecido(new Rect(620, 276, 570, 40), pessoa.nome, 26, textoPapelUI, true);
            TextoTecido(new Rect(620, 325, 570, 32), pessoa.papel, 18, textoPapelUI);
            TextoTecido(new Rect(620, 366, 570, 48), pessoa.natureza + "\n" + NomeCurtoTecido(pessoa.regiao), 14, terracotaUI, true);
            TextoTecido(new Rect(460, 456, 730, 83), pessoa.descricao, 18, textoPapelUI);
            if (BotaoTecido(new Rect(460, 574, 730, 40), "Encontrar no mapa", true))
                AbrirMapaTecido(pessoa.regiao);
        }

        private void MensagemSemDadosTecido()
        {
            TextoTecido(new Rect(80, 290, 1120, 120), "A narrativa não foi encontrada. Confira se a pasta Resources veio junto com a atualização. O Console informa o arquivo esperado.", 22, textoPapelUI, false, TextAnchor.MiddleCenter);
        }

        private void OnDestroy()
        {
            if (losangoCompacta != null)
                Destroy(losangoCompacta);
            if (acabamentoOriginal != null)
            {
                acabamentoOriginal.Dispose();
                acabamentoOriginal = null;
            }

            if (kitHD2D != null)
            {
                kitHD2D.Dispose();
                kitHD2D = null;
            }

            arteTecida.Dispose();
            if (circuloTecido != null)
                Destroy(circuloTecido);
            foreach (var textura in linhasTecido.Values)
                if (textura != null)
                    Destroy(textura);
            linhasTecido.Clear();
            LimparRecursosRoteiro();
        }
    }
}

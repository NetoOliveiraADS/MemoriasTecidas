using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Peças de desenho do HUD no mundo, no mesmo idioma de tecido dos menus:
    // painel escuro com cantos arredondados, contorno dourado e costura tracejada,
    // teclas em creme, discos e o arco do dia. Tudo gerado em código, sem arte externa.
    public sealed partial class MTMJogo
    {
        private static readonly Color TintaHUD = new Color(.13f, .10f, .08f, .9f);
        private static readonly Color OuroHUD = new Color(.85f, .69f, .41f, 1f);
        private static readonly Color CremeHUD = new Color(.96f, .91f, .84f, 1f);
        private static readonly Color CremeFracoHUD = new Color(.96f, .91f, .84f, .72f);
        private static readonly Color TerracotaHUD = new Color(.73f, .42f, .30f, 1f);
        private static readonly Color PerigoHUD = new Color(.88f, .34f, .24f, 1f);
        private static readonly Color SombraTeclaHUD = new Color(.61f, .48f, .31f, 1f);

        private Texture2D texArredondadoHUD, texDiscoHUD, texTracoHUD, texPontaHUD, texArcoHUD, texSetasHUD;
        private GUIStyle estiloArredondadoHUD;
        private float alfaAtalhosHUD = 1f;
        // Alvo da seta de guia e área do cartão de objetivo neste quadro (para outros desenhos desviarem).
        private MTMPonto alvoGuiaHUD;
        private Rect cartaoObjetivoHUD;
        private float objetivoBrilhoAte;

        private bool ContrasteHUD
        {
            get { return contrasteUI; }
        }

        private Color TintaAtualHUD
        {
            get { return ContrasteHUD ? new Color(.06f, .05f, .04f, 1f) : TintaHUD; }
        }

        private static Texture2D NovaTexturaHUD(string nome, int w, int h, TextureWrapMode modo = TextureWrapMode.Clamp)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = nome,
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = modo
            };
        }

        private void PrepararTexturasHUD()
        {
            if (texArredondadoHUD != null)
                return;
            // Retângulo branco de cantos arredondados (raio 8), para 9-slice.
            const int n = 32;
            const float raio = 8f;
            texArredondadoHUD = NovaTexturaHUD("HUD_arredondado", n, n);
            var p = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, raio, n - raio), cy = Mathf.Clamp(y + .5f, raio, n - raio);
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, cy));
                    p[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(raio + .5f - d));
                }

            texArredondadoHUD.SetPixels(p);
            texArredondadoHUD.Apply(false, true);
            estiloArredondadoHUD = new GUIStyle { border = new RectOffset(10, 10, 10, 10) };
            estiloArredondadoHUD.normal.background = texArredondadoHUD;

            const int m = 64;
            texDiscoHUD = NovaTexturaHUD("HUD_disco", m, m);
            p = new Color[m * m];
            for (int y = 0; y < m; y++)
                for (int x = 0; x < m; x++)
                    p[y * m + x] = new Color(1, 1, 1, Mathf.Clamp01(32f - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32))));
            texDiscoHUD.SetPixels(p);
            texDiscoHUD.Apply(false, true);

            // Traço da costura: 5 px cheios, 4 vazios, repetido.
            texTracoHUD = NovaTexturaHUD("HUD_traco", 9, 1, TextureWrapMode.Repeat);
            texTracoHUD.filterMode = FilterMode.Point;
            p = new Color[9];
            for (int x = 0; x < 9; x++)
                p[x] = new Color(1, 1, 1, x < 5 ? 1f : 0f);
            texTracoHUD.SetPixels(p);
            texTracoHUD.Apply(false, true);

            // Ponta do balão (triângulo para baixo).
            texPontaHUD = NovaTexturaHUD("HUD_ponta", 32, 16);
            p = new Color[32 * 16];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 32; x++)
                {
                    float meia = (y + .5f);
                    p[y * 32 + x] = new Color(1, 1, 1, Mathf.Clamp01(meia - Mathf.Abs(x + .5f - 16f)));
                }

            texPontaHUD.SetPixels(p);
            texPontaHUD.Apply(false, true);

            // Arco do dia: meia-volta tracejada e a linha do horizonte.
            const int aw = 132, ah = 80;
            texArcoHUD = NovaTexturaHUD("HUD_arco", aw, ah);
            p = new Color[aw * ah];
            var centro = new Vector2(66f, 8f);
            for (int y = 0; y < ah; y++)
                for (int x = 0; x < aw; x++)
                {
                    var q = new Vector2(x + .5f, y + .5f);
                    float d = Mathf.Abs(Vector2.Distance(q, centro) - 56f);
                    float ang = Mathf.Atan2(q.y - centro.y, q.x - centro.x) * Mathf.Rad2Deg;
                    float traco = Mathf.Repeat(ang, 12f) < 6.5f ? 1f : 0f;
                    float a = q.y >= centro.y ? Mathf.Clamp01(2.2f - d) * traco * .55f : 0f;
                    if (Mathf.Abs(q.y - centro.y) < 1.4f && x > 4 && x < aw - 4)
                        a = Mathf.Max(a, .4f);
                    p[y * aw + x] = new Color(1, 1, 1, a);
                }

            texArcoHUD.SetPixels(p);
            texArcoHUD.Apply(false, true);

            // Setas da faixa de investida, para repetir ao longo do caminho.
            const int s = 32;
            texSetasHUD = NovaTexturaHUD("HUD_setas", s, s, TextureWrapMode.Repeat);
            p = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Abs((x - 22f) + Mathf.Abs(y - 15.5f) * .8f);
                    float dentro = Mathf.Abs(y - 15.5f) < 11f ? 1f : 0f;
                    p[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(2.6f - d) * dentro);
                }

            texSetasHUD.SetPixels(p);
            texSetasHUD.Apply(false, true);
        }

        // Retângulo arredondado de uma cor.
        private void ArredondadoHUD(Rect r, Color cor)
        {
            PrepararTexturasHUD();
            if (Event.current.type != EventType.Repaint)
                return;
            Color antiga = GUI.color, fundoAntigo = GUI.backgroundColor;
            GUI.color = cor;
            GUI.backgroundColor = Color.white;
            estiloArredondadoHUD.Draw(r, false, false, false, false);
            GUI.color = antiga;
            GUI.backgroundColor = fundoAntigo;
        }

        // Linha tracejada horizontal ou vertical.
        private void TracoHUD(Rect r, Color cor, bool vertical)
        {
            PrepararTexturasHUD();
            Color antiga = GUI.color;
            GUI.color = cor;
            float comprimento = vertical ? r.height : r.width;
            GUI.DrawTextureWithTexCoords(r, texTracoHUD, vertical ? new Rect(0, 0, 1, comprimento / 9f) : new Rect(0, 0, comprimento / 9f, 1));
            GUI.color = antiga;
        }

        // Painel de tecido: sombra, contorno dourado, fundo escuro e costura por dentro.
        private void PainelHUD(Rect r, float alfa = 1f, bool destaque = false)
        {
            if (alfa <= .01f)
                return;
            ArredondadoHUD(new Rect(r.x, r.y + 3, r.width, r.height), new Color(0, 0, 0, .3f * alfa));
            var borda = destaque ? OuroHUD : new Color(OuroHUD.r, OuroHUD.g, OuroHUD.b, .5f);
            borda.a *= alfa;
            ArredondadoHUD(new Rect(r.x - (destaque ? 1 : 0), r.y - (destaque ? 1 : 0), r.width + (destaque ? 2 : 0), r.height + (destaque ? 2 : 0)), borda);
            var tinta = TintaAtualHUD;
            tinta.a *= alfa;
            ArredondadoHUD(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), tinta);
            if (r.width < 40 || r.height < 26)
                return;
            var costura = new Color(OuroHUD.r, OuroHUD.g, OuroHUD.b, .28f * alfa);
            TracoHUD(new Rect(r.x + 9, r.y + 4, r.width - 18, 1), costura, false);
            TracoHUD(new Rect(r.x + 9, r.yMax - 5, r.width - 18, 1), costura, false);
            TracoHUD(new Rect(r.x + 4, r.y + 9, 1, r.height - 18), costura, true);
            TracoHUD(new Rect(r.xMax - 5, r.y + 9, 1, r.height - 18), costura, true);
        }

        // Faixa trançada na borda esquerda do cartão de objetivo.
        private void TramaHUD(Rect r, float alfa = 1f)
        {
            for (float y = r.y; y < r.yMax; y += 6f)
            {
                float h = Mathf.Min(6f, r.yMax - y);
                bool par = Mathf.RoundToInt((y - r.y) / 6f) % 2 == 0;
                Fundo(new Rect(r.x, y, r.width, h), AlfaTecido(par ? TerracotaHUD : OuroHUD, alfa));
            }
        }

        private void DiscoHUD(Rect r, Color cor)
        {
            PrepararTexturasHUD();
            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTexture(r, texDiscoHUD);
            GUI.color = antiga;
        }

        // Rótulo da tecla no dispositivo em uso: com o controle, o botão equivalente (nomes do Xbox).
        private static string TeclaAtual(string tecla)
        {
            if (!MTMEntrada.UsandoControle)
                return tecla;
            switch (tecla)
            {
                case "E":
                case "Enter":
                case "E / Enter":
                    return "A";
                case "Espaço":
                    return "X";
                case "Shift":
                    return "RT";
                case "L":
                    return "LT";
                case "M":
                case "U":
                    return "View";
                case "J":
                case "H":
                    return "Y";
                case "B":
                case "I":
                    return "LB";
                case "Esc":
                    return "B";
                case "R":
                    return "R3";
                case "WASD":
                case "WASD / setas":
                    return "Analógico";
                default:
                    return tecla;
            }
        }

        private static string AtalhoUI(string tecla)
        {
            return "[" + TeclaAtual(tecla) + "]";
        }

        // Textos que citam teclas ("aperte M", "J → Achados", "segure L"...) passam a citar o botão do controle.
        private static readonly string[] TrocasTextoControle =
        {
            "aperte M", "aperte View", "aperte E", "aperte A", "J → ", "Y → ", "segure L", "segure LT", "Segure L", "Segure LT",
            "com Espaço", "com X", "com E.", "com A.", "com E,", "com A,", "com E ", "com A ", "(H)", "(Y)", "(E)", "(A)"
        };

        private static string TextoParaControle(string texto)
        {
            if (!MTMEntrada.UsandoControle || string.IsNullOrEmpty(texto))
                return texto;
            for (int i = 0; i < TrocasTextoControle.Length; i += 2)
                if (texto.IndexOf(TrocasTextoControle[i], StringComparison.Ordinal) >= 0)
                    texto = texto.Replace(TrocasTextoControle[i], TrocasTextoControle[i + 1]);
            return texto;
        }

        // Tecla em creme com "degrau" embaixo. Devolve a largura usada.
        private float TeclaHUD(float x, float y, string tecla, float alfa = 1f, int tamanho = 13, float altura = 24f)
        {
            tecla = TeclaAtual(tecla);
            float largura = Mathf.Max(altura, EstiloTecido(tamanho, true, TextAnchor.MiddleCenter).CalcSize(new GUIContent(tecla)).x + 14f);
            ArredondadoHUD(new Rect(x, y + 3, largura, altura), AlfaTecido(SombraTeclaHUD, alfa));
            ArredondadoHUD(new Rect(x, y, largura, altura), AlfaTecido(CremeHUD, alfa));
            TextoTecido(new Rect(x, y, largura, altura), tecla, tamanho, AlfaTecido(new Color(.16f, .12f, .09f), alfa), true, TextAnchor.MiddleCenter);
            return largura;
        }

        // Tecla redonda dourada (interação principal).
        private void TeclaRedondaHUD(Rect r, string tecla)
        {
            tecla = TeclaAtual(tecla);
            DiscoHUD(new Rect(r.x, r.y + 3, r.width, r.height), new Color(.54f, .40f, .19f, 1f));
            DiscoHUD(r, OuroHUD);
            TextoTecido(r, tecla, 14, new Color(.16f, .12f, .09f), true, TextAnchor.MiddleCenter);
        }

        // Sequência "tecla + rótulo"; devolve a largura total, para centralizar.
        private float DicasHUD(float x, float y, string[] teclas, string[] rotulos, bool medir, float alfa = 1f)
        {
            float inicio = x;
            for (int i = 0; i < teclas.Length; i++)
            {
                if (i > 0)
                    x += 18f;
                float largura = Mathf.Max(24f, EstiloTecido(13, true, TextAnchor.MiddleCenter).CalcSize(new GUIContent(TeclaAtual(teclas[i]))).x + 14f);
                if (!medir)
                    TeclaHUD(x, y, teclas[i], alfa);
                x += largura + 8f;
                float texto = EstiloTecido(14, false, TextAnchor.MiddleLeft).CalcSize(new GUIContent(rotulos[i])).x;
                if (!medir)
                    TextoTecido(new Rect(x, y, texto + 4, 24), rotulos[i], 14, AlfaTecido(CremeHUD, alfa), false, TextAnchor.MiddleLeft);
                x += texto;
            }

            return x - inicio;
        }

        // Texto solto sobre o cenário, com sombra para ler em qualquer chão.
        private void TextoSombraHUD(Rect r, string texto, int tamanho, Color cor, bool negrito, TextAnchor alinhamento)
        {
            var sombra = new Color(0, 0, 0, .75f * cor.a);
            TextoTecido(new Rect(r.x + 1, r.y + 1, r.width, r.height), texto, tamanho, sombra, negrito, alinhamento);
            TextoTecido(new Rect(r.x - 1, r.y, r.width, r.height), texto, tamanho, new Color(0, 0, 0, .35f * cor.a), negrito, alinhamento);
            TextoTecido(r, texto, tamanho, cor, negrito, alinhamento);
        }

        private float LarguraTextoHUD(string texto, int tamanho, bool negrito)
        {
            return EstiloTecido(tamanho, negrito, TextAnchor.UpperLeft).CalcSize(new GUIContent(texto ?? "")).x;
        }

        // Balão preso a um ponto do mundo, com ponta para baixo. "base" = ponto de apoio no HUD.
        private Rect BalaoHUD(Vector2 apoio, float largura, float altura, bool destaque)
        {
            Rect r = new Rect(Mathf.Clamp(apoio.x - largura * .5f, 16, 1264 - largura), Mathf.Clamp(apoio.y - altura - 10f, 130, 600), largura, altura);
            PainelHUD(r, 1f, destaque);
            PrepararTexturasHUD();
            Color antiga = GUI.color;
            float px = Mathf.Clamp(apoio.x, r.x + 14, r.xMax - 14);
            GUI.color = destaque ? OuroHUD : new Color(OuroHUD.r, OuroHUD.g, OuroHUD.b, .5f);
            GUI.DrawTexture(new Rect(px - 9, r.yMax - 1, 18, 10), texPontaHUD);
            GUI.color = TintaAtualHUD;
            GUI.DrawTexture(new Rect(px - 7.5f, r.yMax - 2, 15, 8), texPontaHUD);
            GUI.color = antiga;
            return r;
        }

        private static string PeriodoDoDia(float relogio)
        {
            float h = Mathf.Repeat(relogio, 1f) * 24f;
            return h < 5f ? "madrugada" : h < 12f ? "manhã" : h < 18f ? "tarde" : "noite";
        }

        // Relógio: arco do dia com o sol (6h-18h) ou a lua (18h-6h) andando sobre ele.
        private void RelogioHUD()
        {
            PrepararTexturasHUD();
            Rect r = new Rect(1084, 20, 176, 70);
            PainelHUD(r);
            Rect arco = new Rect(r.x + 12, r.y + 12, 66, 40);
            bool noite = Noite;
            Color corArco = noite ? new Color(.6f, .68f, .92f, 1f) : OuroHUD;
            Color antiga = GUI.color;
            GUI.color = corArco;
            // Linha 0 da textura fica embaixo: o horizonte está na base e o arco sobe.
            GUI.DrawTexture(arco, texArcoHUD);
            GUI.color = antiga;
            float h = Mathf.Repeat(estado.relogio, 1f) * 24f;
            float t = noite ? Mathf.Repeat(h - 18f, 24f) / 12f : Mathf.Clamp01((h - 6f) / 12f);
            float ang = Mathf.PI * (1f - Mathf.Clamp01(t));
            Vector2 centro = new Vector2(arco.center.x, arco.yMax - 4f);
            Vector2 astro = centro + new Vector2(Mathf.Cos(ang) * 28f, -Mathf.Sin(ang) * 28f);
            if (noite)
            {
                DiscoHUD(new Rect(astro.x - 7, astro.y - 7, 14, 14), new Color(.86f, .89f, 1f, 1f));
                DiscoHUD(new Rect(astro.x - 3.5f, astro.y - 9f, 12, 12), TintaAtualHUD);
            }
            else
            {
                DiscoHUD(new Rect(astro.x - 10, astro.y - 10, 20, 20), new Color(.95f, .77f, .42f, .35f));
                DiscoHUD(new Rect(astro.x - 6.5f, astro.y - 6.5f, 13, 13), new Color(.95f, .77f, .42f, 1f));
            }

            int minutos = Mathf.FloorToInt(Mathf.Repeat(estado.relogio, 1f) * 1440f);
            TextoTecido(new Rect(r.x + 86, r.y + 8, 84, 30), (minutos / 60).ToString("00") + ":" + (minutos % 60).ToString("00"), 22, CremeHUD, true);
            string linha = RelogioPausado() ? "tempo parado" : "Dia " + Mathf.Max(1, estado.dia) + " · " + PeriodoDoDia(estado.relogio);
            TextoTecido(new Rect(r.x + 86, r.y + 40, 86, 18), ResumoHUDClaroUI(linha, 86, 18, 12), 12, CremeFracoHUD);
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        // A aparência da interface fica separada dos dados narrativos da campanha.
        private Color textoSuperficieUI, acentoTextoUI;
        private Color acaoHoverUI, acaoPressionadaUI, papelHoverUI, papelPressionadoUI, textoDesativadoUI, fundoDesativadoUI;
        // Texto e detalhes claros sobre fundos escuros ou sobre o botão terracota.
        private Color claroUI;

        // Mesmo tecido escuro do HUD: tinta, costura dourada e texto creme. O nome do método ficou do tema antigo.
        private void AplicarTemaAreiaUI()
        {
            profundoUI = CorTecido("#17110D", Color.black);
            algodaoUI = CorTecido("#241A14", Color.black);
            tinta = CorTecido("#30231B", Color.black);
            papelUI = CorTecido("#3B2C22", Color.black);
            ouro = CorTecido("#D9AF68", Color.yellow);
            terracotaUI = CorTecido("#A8553A", Color.red);
            folhaUI = CorTecido("#9DB06F", Color.green);
            claroUI = CorTecido("#F5E9D6", Color.white);
            textoPapelUI = textoSuperficieUI = claroUI;
            acentoTextoUI = ouro;
            acaoHoverUI = CorTecido("#BC6347", Color.red);
            acaoPressionadaUI = CorTecido("#8E452E", Color.red);
            papelHoverUI = CorTecido("#503C2E", Color.gray);
            papelPressionadoUI = CorTecido("#5E4838", Color.gray);
            textoDesativadoUI = CorTecido("#8C7F70", Color.gray);
            fundoDesativadoUI = CorTecido("#2A2019", Color.black);
            if (contrasteUI)
            {
                profundoUI = algodaoUI = Color.black;
                tinta = CorTecido("#141414", Color.black);
                papelUI = CorTecido("#262626", Color.black);
                papelHoverUI = CorTecido("#3A3A3A", Color.gray);
                papelPressionadoUI = CorTecido("#4A4A4A", Color.gray);
                claroUI = textoPapelUI = textoSuperficieUI = Color.white;
                ouro = acentoTextoUI = CorTecido("#FFD98A", Color.yellow);
                terracotaUI = CorTecido("#B5502F", Color.red);
                acaoHoverUI = CorTecido("#CC6340", Color.red);
                acaoPressionadaUI = CorTecido("#963E22", Color.red);
                folhaUI = CorTecido("#C2DA8A", Color.green);
                textoDesativadoUI = CorTecido("#B0B0B0", Color.gray);
                fundoDesativadoUI = CorTecido("#1C1C1C", Color.black);
            }
        }

        private GUIStyle rolagemTrilhoUI, rolagemPegadorUI;
        private Color corRolagemUI;

        private static Texture2D TexturaPilulaUI(Color cor)
        {
            const int w = 8, h = 16;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "MT_Rolagem", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            var p = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cy = Mathf.Clamp(y + .5f, 4f, h - 4f);
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(4f, cy));
                    p[y * w + x] = new Color(cor.r, cor.g, cor.b, cor.a * Mathf.Clamp01(4.2f - d));
                }

            t.SetPixels(p);
            t.Apply(false, true);
            return t;
        }

        // Barra de rolagem fina e dourada, no lugar da barra preta padrão do Unity.
        private void PrepararRolagemUI()
        {
            if (rolagemTrilhoUI == null || corRolagemUI != ouro)
            {
                corRolagemUI = ouro;
                rolagemTrilhoUI = new GUIStyle { name = "verticalscrollbar", fixedWidth = 8, border = new RectOffset(4, 4, 6, 6), margin = new RectOffset(4, 0, 0, 0) };
                rolagemTrilhoUI.normal.background = TexturaPilulaUI(AlfaTecido(claroUI, .10f));
                rolagemPegadorUI = new GUIStyle { name = "verticalscrollbarthumb", fixedWidth = 8, border = new RectOffset(4, 4, 6, 6) };
                rolagemPegadorUI.normal.background = TexturaPilulaUI(AlfaTecido(ouro, .8f));
                rolagemPegadorUI.hover.background = rolagemPegadorUI.active.background = TexturaPilulaUI(ouro);
            }

            var pele = GUI.skin;
            if (pele.verticalScrollbar == rolagemTrilhoUI)
                return;
            pele.verticalScrollbar = rolagemTrilhoUI;
            pele.verticalScrollbarThumb = rolagemPegadorUI;
            pele.verticalScrollbarUpButton = pele.verticalScrollbarDownButton = GUIStyle.none;
        }

        private Color FundoAcaoUI(bool destaque, bool habilitado, int estadoAcao)
        {
            if (!habilitado)
                return fundoDesativadoUI;
            if (destaque)
                return estadoAcao == 2 ? acaoPressionadaUI : estadoAcao == 1 ? acaoHoverUI : terracotaUI;
            return estadoAcao == 2 ? papelPressionadoUI : estadoAcao == 1 ? papelHoverUI : papelUI;
        }

        private Color FundoHUDClaroUI
        {
            get
            {
                return AlfaTecido(algodaoUI, contrasteUI ? 1f : .97f);
            }
        }

        private string ResumoHUDClaroUI(string texto, float largura, float altura, int tamanho, bool negrito = false)
        {
            if (string.IsNullOrEmpty(texto))
                return "";
            texto = texto.Replace('\n', ' ').Replace('\r', ' ').Trim();
            var estilo = EstiloTecido(tamanho, negrito, TextAnchor.UpperLeft);
            bool umaLinha = altura < tamanho * 2f;
            var conteudo = new GUIContent(texto);
            if ((umaLinha ? estilo.CalcSize(conteudo).x <= largura : estilo.CalcHeight(conteudo, largura) <= altura))
                return texto;
            int inicio = 0, fim = texto.Length;
            while (inicio < fim)
            {
                int meio = (inicio + fim + 1) / 2;
                conteudo.text = texto.Substring(0, meio).TrimEnd() + "…";
                bool cabe = umaLinha ? estilo.CalcSize(conteudo).x <= largura : estilo.CalcHeight(conteudo, largura) <= altura;
                if (cabe)
                    inicio = meio;
                else
                    fim = meio - 1;
            }

            return texto.Substring(0, inicio).TrimEnd() + "…";
        }
    }
}

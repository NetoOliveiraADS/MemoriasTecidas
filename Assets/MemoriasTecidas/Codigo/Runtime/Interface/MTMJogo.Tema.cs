using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        // A aparência da interface fica separada dos dados narrativos da campanha.
        private Color textoSuperficieUI, acentoTextoUI;
        private Color acaoHoverUI, acaoPressionadaUI, papelHoverUI, papelPressionadoUI, textoDesativadoUI, fundoDesativadoUI;
        private void AplicarTemaAreiaUI()
        {
            tinta = CorTecido("#E7D3B0", Color.white);
            profundoUI = CorTecido("#F7EFDE", Color.white);
            algodaoUI = CorTecido("#FFF9EE", Color.white);
            papelUI = CorTecido("#EFE1C8", Color.white);
            ouro = CorTecido("#BA8446", Color.gray);
            terracotaUI = CorTecido("#8A472E", Color.black);
            folhaUI = CorTecido("#53623A", Color.black);
            textoPapelUI = CorTecido("#403128", Color.black);
            textoSuperficieUI = textoPapelUI;
            acentoTextoUI = CorTecido("#7B432B", Color.black);
            acaoHoverUI = CorTecido("#713820", Color.black);
            acaoPressionadaUI = CorTecido("#5E2C1A", Color.black);
            papelHoverUI = CorTecido("#DCC7A4", Color.white);
            papelPressionadoUI = CorTecido("#CFB58B", Color.white);
            textoDesativadoUI = CorTecido("#746653", Color.black);
            fundoDesativadoUI = CorTecido("#EFE7D8", Color.white);
            if (contrasteUI)
            {
                tinta = CorTecido("#F0E1C4", Color.white);
                profundoUI = algodaoUI = Color.white;
                papelUI = CorTecido("#EFE7D8", Color.white);
                textoPapelUI = textoSuperficieUI = Color.black;
                acentoTextoUI = terracotaUI = CorTecido("#66321D", Color.black);
                folhaUI = CorTecido("#344323", Color.black);
                acaoHoverUI = CorTecido("#512513", Color.black);
                acaoPressionadaUI = CorTecido("#3D1B0D", Color.black);
                textoDesativadoUI = CorTecido("#584A3A", Color.black);
            }
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

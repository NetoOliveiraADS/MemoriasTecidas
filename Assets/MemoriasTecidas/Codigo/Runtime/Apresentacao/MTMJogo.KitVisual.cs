using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private MTMKitHD2D kitHD2D;
        private MTMAcabamentoOriginal acabamentoOriginal;
        private void PrepararKitHD2D()
        {
            if (acabamentoOriginal != null)
                acabamentoOriginal.Dispose();
            acabamentoOriginal = null;
            if (kitHD2D != null)
                kitHD2D.Dispose();
            kitHD2D = new MTMKitHD2D();
            try
            {
                kitHD2D.Aplicar(this);
            }
            catch (Exception e)
            {
                kitHD2D.Dispose();
                kitHD2D = null;
                Debug.LogWarning("HD2D: a aparência anterior foi preservada. " + e.Message + " Use Memorias Tecidas > Visual HD2D > Validar integração.", this);
            }

            acabamentoOriginal = new MTMAcabamentoOriginal();
            try
            {
                acabamentoOriginal.Aplicar(this);
            }
            catch (Exception e)
            {
                acabamentoOriginal.Dispose();
                acabamentoOriginal = null;
                Debug.LogWarning("Acabamento original: " + e.Message + " As imagens do cenário foram mantidas.", this);
            }
        }

        private void DesenharRetratoTecido(Rect area, Texture2D textura, Rect recorte)
        {
            if (textura == null || recorte.width <= 0 || recorte.height <= 0)
                return;
            float largura = textura.width * recorte.width, altura = textura.height * recorte.height;
            float escala = Mathf.Min(area.width / largura, area.height / altura);
            var tamanho = new Vector2(largura, altura) * escala;
            GUI.DrawTextureWithTexCoords(new Rect(area.center - tamanho * .5f, tamanho), textura, recorte, true);
        }

        private void LugarIlustradoUI()
        {
            Fundo(new Rect(0, 0, 1280, 720), profundoUI);
            TextoTecido(new Rect(32, 22, 880, 20), "CADERNO DE LUGARES", 12, acentoTextoUI, true);
            TituloEditorial(new Rect(30, 50, 1050, 46), NomeMapaGuia(destinoMapa), 28, textoSuperficieUI);
            if (BotaoTecido(new Rect(1116, 34, 132, 42), "Voltar · Esc"))
            {
                VoltarInterfaceUI();
                return;
            }

            Rect area = new Rect(32, 118, 1216, 518);
            PainelTecido(area, tinta, false);
            var imagem = ImagemTecido(destinoMapa);
            if (imagem != null)
                GUI.DrawTexture(new Rect(42, 128, 1196, 498), imagem, ScaleMode.ScaleToFit, true);
            TextoTecido(new Rect(32, 654, 1216, 40), "Uma ilustração deste lugar. Volte ao mapa para acompanhar as passagens da sua jornada.", 15, textoSuperficieUI, false, TextAnchor.MiddleCenter);
        }
    }
}

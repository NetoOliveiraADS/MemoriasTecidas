using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Atmosfera: luzes que acompanham o jogo (lamparina do Neto, fogueiras e lendas)
    // e a vinheta que escurece as bordas da tela, mais forte à noite.
    public sealed partial class MTMJogo
    {
        private static readonly Color CorLamparina = new Color(1f, .74f, .42f);
        private static readonly Color CorFogueira = new Color(1f, .58f, .24f);
        private static readonly Color CorCurupira = new Color(1f, .45f, .18f);
        private static readonly Color CorLuar = new Color(.62f, .74f, 1f);
        private Texture2D texturaVinheta;
        private float noiteSuave;

        // 0 de dia, 1 em plena noite, com transição no entardecer e no amanhecer.
        private float Escuridao
        {
            get { return atual != null && atual.dados.interior ? 0f : 1f - MTMCalculosVisuais.Sol(estado.relogio); }
        }

        private void ColetarLuzesDinamicas()
        {
            var luzes = MTMAmbienteV2.LuzesDinamicas;
            luzes.Clear();
            if (atual == null || corpo == null)
                return;
            float escuro = Escuridao;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            float tremor = reduzir ? 1f : 1f + .06f * Mathf.Sin(Time.time * 9.3f) + .04f * Mathf.Sin(Time.time * 15.7f + 1.3f);
            // A lamparina do Neto: só à noite, para ele nunca sumir no escuro.
            if (escuro > .05f)
                luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = corpo.position + Vector2.up * .6f, raio = 3.2f, intensidade = .55f * escuro * tremor, cor = CorLamparina });
            if (!EmConfronto)
                return;
            for (int i = 0; i < fogueirasConfronto.Count; i++)
            {
                var f = fogueirasConfronto[i];
                if (f == null || sessaoConfronto == null)
                    continue;
                bool acesa = sessaoConfronto.Fase != MTMFaseConfronto.Cerco || sessaoConfronto.Acesas.Contains(i);
                if (!acesa)
                    continue;
                float chama = reduzir ? 1f : 1f + .1f * Mathf.Sin(Time.time * 11f + i * 1.7f) + .05f * Mathf.Sin(Time.time * 23f + i);
                luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = (Vector2)f.transform.position + Vector2.up * .4f, raio = 3.4f, intensidade = .62f * chama, cor = CorFogueira });
            }

            if (inimigoConfronto == null || confrontoAtual == null)
                return;
            Vector2 lenda = (Vector2)inimigoConfronto.transform.position + Vector2.up * 1.2f;
            // O cabelo em chama do Curupira ilumina um pouco em volta dele.
            if (confrontoAtual.tipo == "encanto")
                luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = lenda, raio = 2.2f, intensidade = .45f * escuro * tremor, cor = CorCurupira });
            // A Mulher-Cavalo traz um brilho frio, de luar, para não sumir fora da lamparina.
            else
                luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = lenda, raio = 2.6f, intensidade = .55f * escuro, cor = CorLuar });
        }

        private Texture2D TexturaVinheta()
        {
            if (texturaVinheta != null)
                return texturaVinheta;
            const int w = 128, h = 72;
            texturaVinheta = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Vinheta", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + .5f) / w * 2f - 1f, dy = (y + .5f) / h * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * .85f + dy * dy * 1.1f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, 1.35f, d));
                    pixels[y * w + x] = new Color(0f, 0f, 0f, a);
                }

            texturaVinheta.SetPixels(pixels);
            texturaVinheta.Apply(false, true);
            return texturaVinheta;
        }

        // Desenhada antes do HUD: escurece só a cena, nunca os textos.
        private void VinhetaUI()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;
            noiteSuave = Mathf.Lerp(noiteSuave, Escuridao, 1f - Mathf.Exp(-3f * Mathf.Min(Time.unscaledDeltaTime, .1f)));
            float forca = .16f + .5f * noiteSuave;
            Color antiga = GUI.color;
            // Tom azulado da noite nas bordas; de dia, só um leve fechamento.
            GUI.color = new Color(.02f * (1f - noiteSuave), .03f, .08f * noiteSuave, forca);
            GUI.DrawTexture(new Rect(-40, -24, 1360, 768), TexturaVinheta(), ScaleMode.StretchToFill);
            GUI.color = antiga;
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Mede quanto a compressão BC7 (qualidade máxima) muda cada imagem, sem alterar nenhum arquivo nem
    // configuração: lê o PNG/JPG original, comprime uma cópia em memória, decodifica as duas pela placa de
    // vídeo e compara pixel a pixel. Grava um relatório e recortes ampliados (original | BC7 | diferença x10).
    // Linha de comando (Unity fechado):
    //   unity run <projeto> -- -executeMethod MemoriasTecidas.MundoV2.MTMQualidadeTexturas.Medir
    //     -texturas "Assets/a.png;Assets/b.jpg" -saida <pasta>
    public static class MTMQualidadeTexturas
    {
        public static void Medir()
        {
            try
            {
                string[] caminhos = (Argumento("-texturas") ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                string saida = Argumento("-saida") ?? Path.Combine("Builds", "Qualidade");
                Directory.CreateDirectory(saida);
                var rel = new StringBuilder();
                rel.AppendLine("imagem\tmedida\tPSNR_dB\tdif_max\tpx_dif>2_%\tpx_dif>4_%\tpx_dif>8_%\talfa_dif_max");
                foreach (var caminho in caminhos)
                    rel.AppendLine(MedirUma(caminho.Trim(), saida));
                File.WriteAllText(Path.Combine(saida, "relatorio.tsv"), rel.ToString());
                Debug.Log("Qualidade das texturas: relatório em " + Path.GetFullPath(Path.Combine(saida, "relatorio.tsv")));
            }
            catch (Exception erro)
            {
                Debug.LogError("Qualidade das texturas: " + erro);
                EditorApplication.Exit(1);
            }
        }

        private static string MedirUma(string caminho, string saida)
        {
            var lida = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            lida.LoadImage(File.ReadAllBytes(caminho));
            // BC7 trabalha em blocos de 4x4: mede na maior área múltipla de 4, sem redimensionar nada.
            int w = lida.width / 4 * 4, h = lida.height / 4 * 4;
            var original = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            original.SetPixels(lida.GetPixels(0, 0, w, h));
            original.Apply(false, false);
            var bc7 = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            bc7.SetPixels(original.GetPixels());
            bc7.Apply(false, false);
            EditorUtility.CompressTexture(bc7, TextureFormat.BC7, TextureCompressionQuality.Best);

            Color32[] a = Decodificar(original), b = Decodificar(bc7);
            double soma = 0;
            long contados = 0, acima2 = 0, acima4 = 0, acima8 = 0;
            int maximo = 0, alfaMax = 0;
            var erroPorPixel = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                alfaMax = Math.Max(alfaMax, Math.Abs(a[i].a - b[i].a));
                if (a[i].a < 8 && b[i].a < 8)
                    continue; // totalmente transparente: a cor não aparece
                int dr = Math.Abs(a[i].r - b[i].r), dg = Math.Abs(a[i].g - b[i].g), db = Math.Abs(a[i].b - b[i].b);
                int d = Math.Max(dr, Math.Max(dg, db));
                maximo = Math.Max(maximo, d);
                soma += dr * dr + dg * dg + db * db;
                contados++;
                if (d > 2) acima2++;
                if (d > 4) acima4++;
                if (d > 8) acima8++;
                erroPorPixel[i] = d;
            }

            double mse = contados > 0 ? soma / (contados * 3.0) : 0;
            double psnr = mse > 0 ? 10 * Math.Log10(255.0 * 255.0 / mse) : 99;
            SalvarComparacao(caminho, saida, a, b, erroPorPixel, w, h);
            UnityEngine.Object.DestroyImmediate(lida);
            UnityEngine.Object.DestroyImmediate(original);
            UnityEngine.Object.DestroyImmediate(bc7);
            double t = Math.Max(1, contados) / 100.0;
            return string.Format(CultureInfo.InvariantCulture, "{0}\t{1}x{2}\t{3:0.0}\t{4}\t{5:0.00}\t{6:0.00}\t{7:0.000}\t{8}",
                Path.GetFileName(caminho), w, h, psnr, maximo, acima2 / t, acima4 / t, acima8 / t, alfaMax);
        }

        // Decodifica pela placa de vídeo (como o jogo vê) para um RGBA de 8 bits.
        private static Color32[] Decodificar(Texture2D tex)
        {
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var anterior = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var leitura = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, true);
            leitura.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            leitura.Apply();
            RenderTexture.active = anterior;
            RenderTexture.ReleaseTemporary(rt);
            var px = leitura.GetPixels32();
            UnityEngine.Object.DestroyImmediate(leitura);
            return px;
        }

        // Recorte de 160x160 onde a diferença é maior, ampliado 3x: original | BC7 | diferença x10.
        private static void SalvarComparacao(string caminho, string saida, Color32[] a, Color32[] b, float[] erro, int w, int h)
        {
            const int lado = 160, zoom = 3;
            int melhorX = Math.Max(0, w / 2 - lado / 2), melhorY = Math.Max(0, h / 2 - lado / 2);
            double melhor = -1;
            for (int y = 0; y + lado <= h; y += lado / 2)
                for (int x = 0; x + lado <= w; x += lado / 2)
                {
                    double s = 0;
                    for (int yy = y; yy < y + lado; yy += 4)
                        for (int xx = x; xx < x + lado; xx += 4)
                            s += erro[yy * w + xx];
                    if (s > melhor)
                    {
                        melhor = s;
                        melhorX = x;
                        melhorY = y;
                    }
                }

            int lw = Math.Min(lado, w), lh = Math.Min(lado, h);
            var tela = new Texture2D(lw * zoom * 3 + 8, lh * zoom, TextureFormat.RGB24, false);
            var fundo = new Color32[tela.width * tela.height];
            for (int i = 0; i < fundo.Length; i++)
                fundo[i] = new Color32(30, 30, 30, 255);
            tela.SetPixels32(fundo);
            for (int y = 0; y < lh * zoom; y++)
                for (int x = 0; x < lw * zoom; x++)
                {
                    int i = (melhorY + y / zoom) * w + (melhorX + x / zoom);
                    tela.SetPixel(x, y, Sobre(a[i]));
                    tela.SetPixel(x + lw * zoom + 4, y, Sobre(b[i]));
                    float d = Mathf.Clamp01(Math.Max(Math.Abs(a[i].r - b[i].r), Math.Max(Math.Abs(a[i].g - b[i].g), Math.Abs(a[i].b - b[i].b))) * 10f / 255f);
                    tela.SetPixel(x + 2 * (lw * zoom + 4), y, new Color(d, d, d));
                }

            tela.Apply();
            File.WriteAllBytes(Path.Combine(saida, Path.GetFileNameWithoutExtension(caminho) + "_comparacao.png"), tela.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tela);
        }

        // Mostra a cor sobre um cinza médio, para a transparência não esconder a diferença.
        private static Color Sobre(Color32 c)
        {
            float al = c.a / 255f;
            return new Color(c.r / 255f * al + .45f * (1 - al), c.g / 255f * al + .45f * (1 - al), c.b / 255f * al + .45f * (1 - al));
        }

        private static string Argumento(string nome)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == nome)
                    return args[i + 1];
            return null;
        }
    }
}

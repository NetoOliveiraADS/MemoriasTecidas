using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Campo de distância visual. Não modifica vértices, colisões ou limites navegáveis.
    public static class MTMContornoVisual
    {
        public const float Alcance = .6f;
        public static Rect Limites(Vector2[] pontos)
        {
            Vector2 minimo = pontos[0], maximo = pontos[0];
            for (int i = 1; i < pontos.Length; i++)
            {
                minimo = Vector2.Min(minimo, pontos[i]);
                maximo = Vector2.Max(maximo, pontos[i]);
            }

            return new Rect(minimo, Vector2.Max(maximo - minimo, Vector2.one * .001f));
        }

        public static float DistanciaSegmento(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.00000001f, ab.sqrMagnitude));
            return (p - (a + ab * u)).sqrMagnitude;
        }

        public static Texture2D Criar(Vector2[] pontos, Transform dono, Rect limites)
        {
            Color32[] dados;
            return Criar(pontos, dono, limites, out dados);
        }

        public static Texture2D Criar(Vector2[] pontos, Transform dono, Rect limites, out Color32[] dados, Color32[] cache = null)
        {
            Vector2 origem = dono.TransformPoint(new Vector3(limites.xMin, limites.yMin));
            Vector2 eixoX = dono.TransformVector(new Vector3(limites.width, 0));
            Vector2 eixoY = dono.TransformVector(new Vector3(0, limites.height));
            int largura = Mathf.Clamp(Mathf.CeilToInt(eixoX.magnitude * 16), 32, 256);
            int altura = Mathf.Clamp(Mathf.CeilToInt(eixoY.magnitude * 16), 32, 256);
            var mundo = new Vector2[pontos.Length];
            for (int i = 0; i < pontos.Length; i++)
                mundo[i] = dono.TransformPoint(pontos[i]);
            bool reaproveitar = cache != null && cache.Length == largura * altura;
            var pixels = reaproveitar ? cache : new Color32[largura * altura];
            if (!reaproveitar)
                for (int y = 0; y < altura; y++)
                    for (int x = 0; x < largura; x++)
                    {
                        Vector2 p = origem + eixoX * ((x + .5f) / largura) + eixoY * ((y + .5f) / altura);
                        float distancia = Alcance * Alcance;
                        for (int k = 0; k < mundo.Length; k++)
                            distancia = Mathf.Min(distancia, DistanciaSegmento(p, mundo[k], mundo[(k + 1) % mundo.Length]));
                        byte valor = (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.Sqrt(distancia) / Alcance) * 255);
                        pixels[y * largura + x] = new Color32(valor, valor, valor, 255);
                    }

            var textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false, true)
            {
                name = "MTM_DistanciaDaMargem",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            textura.SetPixels32(pixels);
            textura.Apply(false, true);
            dados = pixels;
            return textura;
        }
    }
}

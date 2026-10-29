using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Cálculos sem estado de cena, usados pelo movimento, animação e efeitos.
    public static class MTMCalculosMovimento
    {
        public static Vector2 MedirVelocidade(Vector2 deslocamento, float dt, float limite)
        {
            if (dt <= .00001f || dt > .25f || deslocamento.sqrMagnitude > 4f)
                return Vector2.zero;
            return Vector2.ClampMagnitude(deslocamento / dt, Mathf.Max(.1f, limite));
        }

        public static int DirecaoOito(Vector2 velocidade, int anterior)
        {
            anterior = (anterior % 8 + 8) % 8;
            if (velocidade.sqrMagnitude < .0001f)
                return anterior;
            float angulo = Mathf.Atan2(-velocidade.x, -velocidade.y) * Mathf.Rad2Deg;
            // Evita alternar os sprites repetidamente ao deslizar junto de uma parede.
            if (Mathf.Abs(Mathf.DeltaAngle(anterior * 45f, angulo)) <= 28.5f)
                return anterior;
            return (Mathf.RoundToInt(angulo / 45f) + 8) % 8;
        }

        public static int PassoPorDistancia(bool andando, bool anterior, bool completo, float percurso, float distanciaPorQuadro, ref float fase)
        {
            if (!andando)
            {
                fase = 0;
                return 0;
            }

            if (!anterior)
                fase = completo ? 1f : 0f;
            int quantidade = completo ? 4 : 3;
            fase = Mathf.Repeat(fase + Mathf.Max(0, percurso) / Mathf.Max(.01f, distanciaPorQuadro), quantidade);
            return (completo ? 0 : 1) + Mathf.FloorToInt(fase);
        }

        public static void LimitarInerciaCamera(Vector3 proposta, Vector3 contida, ref Vector3 velocidade)
        {
            if ((proposta.x - contida.x) * velocidade.x > .000001f)
                velocidade.x = 0;
            if ((proposta.y - contida.y) * velocidade.y > .000001f)
                velocidade.y = 0;
        }

        public static bool DentroPoligono(Vector2 p, Vector2[] vertices)
        {
            if (vertices == null || vertices.Length < 3)
                return false;
            bool dentro = false;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                Vector2 a = vertices[j], b = vertices[i], ab = b - a;
                float u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.00000001f, ab.sqrMagnitude));
                if ((p - a - ab * u).sqrMagnitude < .00000001f)
                    return true;
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    dentro = !dentro;
            }

            return dentro;
        }

        public static float PoeiraDoPiso(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return 0;
            // Chamado ao montar o cache de uma região, não por quadro.
            nome = nome.ToLowerInvariant();
            if (nome.Contains("agua") || nome.Contains("rio") || nome.Contains("ponte") || nome.Contains("madeira") || nome.Contains("asfalto") || nome.Contains("mosaico") || nome.Contains("pedra") || nome.Contains("calcamento"))
                return 0;
            if (nome.Contains("areia") || nome.Contains("terra"))
                return 1f;
            return nome.Contains("grama") ? .35f : 0;
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMAmbienteV2 : MonoBehaviour
    {
        public static int Qualidade = 1;
        public static float Sol = 1f, Fase = .33f;
        public static bool Interior { get; private set; }

        // Relógio das animações do cenário (água, copas, plantas). No jogo é Time.time; as capturas
        // do editor fixam um instante para gravar o movimento quadro a quadro.
        public static float TempoFixo = float.NaN;
        public static float Tempo => float.IsNaN(TempoFixo) ? Time.time : TempoFixo;

        // Rajadas que atravessam o lugar de oeste para leste, de .55 (calmaria) a 1 (rajada).
        // A mesma conta está em MTMRajada (MTMIluminacao.cginc), para plantas e copas balançarem juntas.
        public static float Rajada(float x, float fase, float t)
        {
            float frente = .5f + .5f * Mathf.Sin(x * .12f - t * .45f);
            float pulso = .5f + .5f * Mathf.Sin(t * .23f + fase * .7f);
            return .55f + .45f * frente * (.55f + .45f * pulso);
        }

        // Luzes que mudam a cada quadro (fogueiras, lamparina). Quem usa limpa e preenche
        // antes de Atualizar; elas entram na frente das luzes fixas da região.
        public struct LuzDinamica
        {
            public Vector2 posicao;
            public float raio, intensidade;
            public Color cor;
        }

        public static readonly System.Collections.Generic.List<LuzDinamica> LuzesDinamicas = new System.Collections.Generic.List<LuzDinamica>();

        public static readonly string[] Nomes =
        {
            "Leve",
            "Equilibrada",
            "Alta"
        };
        private readonly Vector4[] posicoes = new Vector4[16], cores = new Vector4[16];
        private bool[] usadas;
        private Camera cameraPrincipal;
        private MTMAreaDados anterior;
        private Color luzAtual = Color.white;
        public void Atualizar(MTMAreaDados area, float relogio)
        {
            if (area == null)
                return;
            Interior = area.interior;
            var arte = MTMConfiguracaoVisual.Atual;
            Qualidade = Mathf.Clamp(Qualidade, 0, 2);
            Fase = Mathf.Repeat(relogio, 1f);
            Sol = MTMCalculosVisuais.Sol(Fase);
            float hora = Fase * 24f;
            // Sertão: manhã dourada até perto das 9h e tarde alaranjada a partir das 15h.
            float calor = Mathf.Clamp01(1f - Mathf.Abs(hora - 17f) / 2.8f) * Sol;
            calor = Mathf.Max(calor, Mathf.Clamp01(1f - Mathf.Abs(hora - 7f) / 2.4f) * Sol * .75f);
            Color dia = Color.Lerp(arte.luzDia, arte.luzEntardecer, calor * .72f);
            Color alvo = area.interior ? arte.luzInterior : Color.Lerp(arte.luzNoite, dia, Sol);
            if (anterior != area)
            {
                luzAtual = alvo;
                anterior = area;
            }
            else
                luzAtual = Color.Lerp(luzAtual, alvo, 1f - Mathf.Exp(-5f * Mathf.Min(Time.unscaledDeltaTime, .1f)));
            float vento = arte.reduzirMovimento || area.interior ? 0f : arte.vento;
            Shader.SetGlobalFloat("_MTV_Active", 1f);
            Shader.SetGlobalFloat("_MTV_Time", Tempo);
            Shader.SetGlobalFloat("_MTV_Quality", Qualidade);
            Shader.SetGlobalFloat("_MTV_Motion", arte.reduzirMovimento ? 0f : 1f);
            Shader.SetGlobalColor("_MTV_Ambient", luzAtual);
            Shader.SetGlobalVector("_MTV_Art", new Vector4(arte.exposicao, arte.saturacao, arte.detalhe, vento));
            // Direção horizontal troca de lado ao meio-dia; Sol controla a intensidade, não a altura.
            Shader.SetGlobalVector("_MTV_Sun", new Vector4(Mathf.Sin((Fase - .5f) * Mathf.PI * 2f), Sol, area.interior ? 1f : 0f, arte.sombrasContato));
            int limite = Qualidade == 0 ? 4 : Qualidade == 1 ? 8 : 16, count = 0;
            for (int i = 0; i < LuzesDinamicas.Count && count < 16; i++)
            {
                var l = LuzesDinamicas[i];
                if (l.raio <= 0f || l.intensidade <= 0f)
                    continue;
                posicoes[count] = new Vector4(l.posicao.x, l.posicao.y, l.raio, l.intensidade);
                cores[count] = new Vector4(l.cor.r, l.cor.g, l.cor.b, 1f);
                count++;
            }

            limite = Mathf.Min(16, limite + count);
            if (cameraPrincipal == null)
                cameraPrincipal = Camera.main;
            Vector2 centro = cameraPrincipal != null ? (Vector2)cameraPrincipal.transform.position : Vector2.zero;
            float alcance = cameraPrincipal != null ? cameraPrincipal.orthographicSize * Mathf.Sqrt(1f + cameraPrincipal.aspect * cameraPrincipal.aspect) + 2f : 100f;
            if (area.luzes != null)
            {
                if (usadas == null || usadas.Length != area.luzes.Length)
                    usadas = new bool[area.luzes.Length];
                System.Array.Clear(usadas, 0, usadas.Length);
                for (int passo = 0; passo < limite; passo++)
                {
                    int melhor = -1;
                    float prioridade = float.PositiveInfinity;
                    for (int i = 0; i < area.luzes.Length; i++)
                    {
                        var l = area.luzes[i];
                        if (usadas[i] || l == null || l.raio <= 0f || l.intensidade <= 0f || (!l.sempre && Sol > .995f))
                            continue;
                        float dist = (new Vector2(l.x, l.y) - centro).sqrMagnitude;
                        if (dist > (alcance + l.raio) * (alcance + l.raio))
                            continue;
                        float p = dist / Mathf.Max(1f, l.raio * l.raio) - l.intensidade * .15f;
                        if (p < prioridade)
                        {
                            prioridade = p;
                            melhor = i;
                        }
                    }

                    if (melhor < 0)
                        break;
                    usadas[melhor] = true;
                    var fonte = area.luzes[melhor];
                    float forca = fonte.intensidade * (fonte.sempre ? 1f : 1f - Sol);
                    float variacao = arte.reduzirMovimento ? 1f : 1f + Mathf.Sin(Time.time * 1.8f + fonte.x * .73f + fonte.y * .31f) * .012f;
                    posicoes[count] = new Vector4(fonte.x, fonte.y, fonte.raio, forca * variacao);
                    cores[count] = new Vector4(fonte.r, fonte.g, fonte.b, 1f);
                    count++;
                }
            }

            Shader.SetGlobalVectorArray("_MTV_Lights", posicoes);
            Shader.SetGlobalVectorArray("_MTV_Colors", cores);
            Shader.SetGlobalInt("_MTV_Count", count);
        }

        private void OnDisable()
        {
            Interior = false;
            anterior = null;
            LuzesDinamicas.Clear();
            Shader.SetGlobalFloat("_MTV_Active", 0f);
            Shader.SetGlobalInt("_MTV_Count", 0);
            Shader.SetGlobalFloat("_MTV_Time", 0f);
            Shader.SetGlobalFloat("_MTV_Motion", 0f);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Atmosfera: luzes que acompanham o jogo (lanterna do Neto, postes, fogueiras e lendas)
    // e a vinheta que escurece as bordas da tela, mais forte à noite.
    public sealed partial class MTMJogo
    {
        private static readonly Color CorLamparina = new Color(1f, .74f, .42f);
        private static readonly Color CorLanterna = new Color(1f, .92f, .76f);
        private static readonly Color CorLampadaPoste = new Color(1f, .84f, .55f);
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
            if (escuro > .05f)
            {
                if (LanternaAcesa)
                {
                    // A luz da noite sai da lanterna: um pouco na mão e o facho no chão, à frente de Neto.
                    float f = escuro * alfaLanterna;
                    luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = pontaLanterna, raio = 1.6f, intensidade = .34f * f, cor = CorLanterna });
                    luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = pontaLanterna + direcaoLanterna * 1.6f, raio = 2.4f, intensidade = .55f * f, cor = CorLanterna });
                    luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = pontaLanterna + direcaoLanterna * 3.2f, raio = 2.8f, intensidade = .45f * f, cor = CorLanterna });
                }
                else if (EmConfronto)
                    // Na luta a mão está no pau de jucá: a luz fica em volta de Neto, para ele nunca sumir no escuro.
                    luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = corpo.position + Vector2.up * .6f, raio = 3.2f, intensidade = .55f * escuro * tremor, cor = CorLamparina });
                else
                    // Lanterna apagada: só um pouco de luar em volta, para Neto não sumir de todo.
                    luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = corpo.position + Vector2.up * .6f, raio = 2.4f, intensidade = .24f * escuro, cor = CorLuar });
            }

            AtualizarPostes(escuro, reduzir, luzes);
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

        // ---------- postes ----------
        // À noite cada lâmpada dos postes ganha um brilho (a luz no chão vem das luzes guardadas na região).
        // Postes sem luz guardada por perto (os do Sítio) ganham a luz no chão aqui, só os perto da câmera.
        private struct BrilhoPoste
        {
            public SpriteRenderer brilho, poste;
            public float fase;
        }

        private readonly List<BrilhoPoste> brilhosPostes = new List<BrilhoPoste>();
        private readonly List<Vector2> postesSemLuz = new List<Vector2>();
        private readonly List<MTMRegiao> regioesPostesSemLuz = new List<MTMRegiao>();
        private readonly List<KeyValuePair<float, Vector2>> postesPerto = new List<KeyValuePair<float, Vector2>>();
        private bool postesPreparados;
        private Sprite spriteBrilho;

        // Onde fica cada lâmpada na arte de cada poste, em unidades a partir do pé (medido na própria arte).
        private static readonly Dictionary<string, Vector2[]> LampadasDosPostes = new Dictionary<string, Vector2[]>
        {
            { "poste", new[] { new Vector2(.84f, 3.42f) } },
            { "poste_praca", new[] { new Vector2(-.78f, 3.47f), new Vector2(.78f, 3.47f), new Vector2(-.74f, 2.9f), new Vector2(.74f, 2.9f) } },
            { "poste_amarelo", new[] { new Vector2(-.97f, 5.2f), new Vector2(.98f, 4.93f) } },
            { "urb_poste_curvo_praca", new[] { new Vector2(-1.61f, 5.76f), new Vector2(1.61f, 5.76f) } }
        };

        private void PrepararPostes()
        {
            postesPreparados = true;
            if (regioes == null)
                return;
            foreach (var regiao in regioes)
            {
                if (regiao == null)
                    continue;
                foreach (var sr in regiao.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Vector2[] lampadas;
                    if (sr == null || sr.sprite == null || sr.gameObject.name == "Sombra" || !LampadasDosPostes.TryGetValue(sr.sprite.name, out lampadas))
                        continue;
                    Vector3 escala = sr.transform.lossyScale;
                    // A sombra usa a mesma arte achatada no chão: fica de fora.
                    if (Mathf.Abs(escala.y) < .3f * Mathf.Abs(escala.x))
                        continue;
                    foreach (var l in lampadas)
                    {
                        var go = new GameObject("Brilho da lâmpada") { hideFlags = HideFlags.DontSave, layer = sr.gameObject.layer };
                        go.transform.SetParent(sr.transform, false);
                        go.transform.localPosition = new Vector3(sr.flipX ? -l.x : l.x, l.y, 0f);
                        // 1,15 unidade de diâmetro no mundo, qualquer que seja a escala do poste.
                        go.transform.localScale = new Vector3(1.15f / Mathf.Max(.01f, Mathf.Abs(escala.x)), 1.15f / Mathf.Max(.01f, Mathf.Abs(escala.y)), 1f);
                        var b = go.AddComponent<SpriteRenderer>();
                        b.sprite = SpriteBrilho();
                        b.sortingLayerID = sr.sortingLayerID;
                        b.sortingOrder = sr.sortingOrder + 1;
                        b.color = new Color(CorLampadaPoste.r, CorLampadaPoste.g, CorLampadaPoste.b, 0f);
                        b.enabled = false;
                        brilhosPostes.Add(new BrilhoPoste { brilho = b, poste = sr, fase = Random.value * 10f });
                    }

                    Vector2 pe = sr.transform.position;
                    bool temLuz = false;
                    if (regiao.dados != null && regiao.dados.luzes != null)
                        foreach (var luz in regiao.dados.luzes)
                            if (luz != null && luz.intensidade > 0f && (new Vector2(luz.x, luz.y) - pe).sqrMagnitude < 6.25f)
                            {
                                temLuz = true;
                                break;
                            }

                    if (!temLuz)
                    {
                        postesSemLuz.Add(pe);
                        regioesPostesSemLuz.Add(regiao);
                    }
                }
            }
        }

        private void AtualizarPostes(float escuro, bool reduzir, List<MTMAmbienteV2.LuzDinamica> luzes)
        {
            if (!postesPreparados)
                PrepararPostes();
            bool aceso = escuro > .02f;
            for (int i = 0; i < brilhosPostes.Count; i++)
            {
                var b = brilhosPostes[i];
                if (b.brilho == null || b.poste == null || !b.poste.gameObject.activeInHierarchy)
                    continue;
                if (b.brilho.enabled != aceso)
                    b.brilho.enabled = aceso;
                if (!aceso)
                    continue;
                float pisca = reduzir ? 1f : 1f + .05f * Mathf.Sin(Time.time * 2.1f + b.fase);
                b.brilho.color = new Color(CorLampadaPoste.r, CorLampadaPoste.g, CorLampadaPoste.b, Mathf.Clamp01(.8f * escuro * pisca));
                b.brilho.sortingOrder = b.poste.sortingOrder + 1;
            }

            if (escuro <= .05f || postesSemLuz.Count == 0 || cameraDoMapa == null || atual == null)
                return;
            Vector2 cam = cameraDoMapa.transform.position;
            float alcance = cameraDoMapa.orthographicSize * (1f + cameraDoMapa.aspect) + 6f;
            postesPerto.Clear();
            for (int i = 0; i < postesSemLuz.Count; i++)
            {
                if (regioesPostesSemLuz[i] != atual)
                    continue;
                float d = (postesSemLuz[i] - cam).sqrMagnitude;
                if (d <= alcance * alcance)
                    postesPerto.Add(new KeyValuePair<float, Vector2>(d, postesSemLuz[i]));
            }

            postesPerto.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < postesPerto.Count && i < 6; i++)
                luzes.Add(new MTMAmbienteV2.LuzDinamica { posicao = postesPerto[i].Value, raio = 6f, intensidade = .72f * escuro, cor = new Color(1f, .73f, .4f) });
        }

        // Brilho redondo e macio de 1 unidade de diâmetro: miolo forte e auréola fraca.
        private Sprite SpriteBrilho()
        {
            if (spriteBrilho != null)
                return spriteBrilho;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Brilho_luz", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var p = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                    float r2 = dx * dx + dy * dy;
                    float a = (Mathf.Exp(-r2 / .035f) + .42f * Mathf.Exp(-r2 / .22f)) * Mathf.Clamp01((1f - Mathf.Sqrt(r2)) * 4f);
                    p[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }

            tex.SetPixels(p);
            tex.Apply(false, true);
            spriteBrilho = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n, 0, SpriteMeshType.FullRect);
            spriteBrilho.hideFlags = HideFlags.DontSave;
            return spriteBrilho;
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

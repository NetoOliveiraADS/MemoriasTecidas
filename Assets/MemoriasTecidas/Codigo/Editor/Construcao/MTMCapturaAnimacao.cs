using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Grava quadros de uma ou mais regiões em movimento (água, copas, redes, plantas) sem rodar o jogo:
    // liga a região, monta as superfícies e as malhas vivas e avança o relógio das animações quadro a quadro.
    // A cena não é salva. Linha de comando (Unity fechado):
    //   unity run <projeto> -- -executeMethod MemoriasTecidas.MundoV2.MTMCapturaAnimacao.Capturar
    //     -regiao rio,beira -saida Builds/Capturas/animacao [-quadros 36 -fps 12 -zoom 1.6 -foco 0,0
    //     -hora .40 -largura 960 -altura 540]
    // Cada região grava <saida>/<regiao>/quadro_000.png, quadro_001.png...
    public static class MTMCapturaAnimacao
    {
        private const string Cena = "Assets/MemoriasTecidas/Cenas/Jaguaruana.unity";
        private const BindingFlags Membro = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static void Capturar()
        {
            try
            {
                var cultura = CultureInfo.InvariantCulture;
                // "rio,beira" usa o mesmo enquadramento; "rio@-14,0@2.6;carnaubal@-12,6@2.4" dá foco e zoom a cada uma.
                string textoRegioes = Argumento("-regiao") ?? "rio";
                string[] regioes = textoRegioes.Contains("@") || textoRegioes.Contains(";") ? textoRegioes.Split(';') : textoRegioes.Split(',');
                string saida = Argumento("-saida") ?? Path.Combine("Builds", "Capturas", "animacao");
                int quadros = int.TryParse(Argumento("-quadros"), NumberStyles.Integer, cultura, out var q) ? Mathf.Clamp(q, 1, 240) : 36;
                float fps = float.TryParse(Argumento("-fps"), NumberStyles.Float, cultura, out var f) ? Mathf.Max(1f, f) : 12f;
                float zoom = float.TryParse(Argumento("-zoom"), NumberStyles.Float, cultura, out var z) ? Mathf.Max(.1f, z) : 1f;
                float hora = float.TryParse(Argumento("-hora"), NumberStyles.Float, cultura, out var h) ? Mathf.Repeat(h, 1f) : .40f;
                int largura = int.TryParse(Argumento("-largura"), NumberStyles.Integer, cultura, out var w) ? w : 960;
                int altura = int.TryParse(Argumento("-altura"), NumberStyles.Integer, cultura, out var a) ? a : 540;
                Vector2 foco = Vector2.zero;
                var textoFoco = Argumento("-foco");
                if (!string.IsNullOrEmpty(textoFoco))
                {
                    var partes = textoFoco.Split(',');
                    foco = new Vector2(float.Parse(partes[0], cultura), float.Parse(partes[1], cultura));
                }

                EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
                using var kit = Argumento("-kit") != null ? AplicarKit() : null;
                var todas = UnityEngine.Object.FindObjectsByType<MTMRegiao>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var entrada in regioes)
                {
                    var partes = entrada.Split('@');
                    string id = partes[0].Trim();
                    Vector2 focoRegiao = foco;
                    float zoomRegiao = zoom;
                    if (partes.Length > 1)
                    {
                        var xy = partes[1].Split(',');
                        focoRegiao = new Vector2(float.Parse(xy[0], cultura), float.Parse(xy[1], cultura));
                    }

                    if (partes.Length > 2)
                        zoomRegiao = Mathf.Max(.1f, float.Parse(partes[2], cultura));
                    MTMRegiao alvo = null;
                    foreach (var r in todas)
                    {
                        bool eh = r.dados != null && r.dados.id == id;
                        r.gameObject.SetActive(eh);
                        if (eh)
                            alvo = r;
                    }

                    if (alvo == null)
                        throw new InvalidOperationException("região não encontrada: " + id);
                    Gravar(alvo, Path.Combine(saida, alvo.dados.id), quadros, fps, zoomRegiao, focoRegiao, hora, largura, altura);
                }
            }
            catch (Exception erro)
            {
                Debug.LogError("Captura animada: " + erro);
                EditorApplication.Exit(1);
            }
            finally
            {
                MTMAmbienteV2.TempoFixo = float.NaN;
            }
        }

        private static void Gravar(MTMRegiao regiao, string pasta, int quadros, float fps, float zoom, Vector2 foco, float hora, int largura, int altura)
        {
            Directory.CreateDirectory(pasta);
            foreach (var antigo in Directory.GetFiles(pasta, "quadro_*.png"))
                File.Delete(antigo);

            // Ambiente de um dia claro, como o jogo calcula em MTMAmbienteV2.
            var arte = MTMConfiguracaoVisual.Atual;
            float sol = MTMCalculosVisuais.Sol(hora);
            bool interior = regiao.dados.interior;
            MTMAmbienteV2.Qualidade = 2;
            MTMAmbienteV2.Fase = hora;
            MTMAmbienteV2.Sol = sol;
            Shader.SetGlobalFloat("_MTV_Active", 1f);
            Shader.SetGlobalFloat("_MTV_Quality", 2f);
            Shader.SetGlobalFloat("_MTV_Motion", 1f);
            Shader.SetGlobalColor("_MTV_Ambient", interior ? arte.luzInterior : Color.Lerp(arte.luzNoite, arte.luzDia, sol));
            Shader.SetGlobalVector("_MTV_Art", new Vector4(arte.exposicao, arte.saturacao, arte.detalhe, interior ? 0f : arte.vento));
            Shader.SetGlobalVector("_MTV_Sun", new Vector4(Mathf.Sin((hora - .5f) * Mathf.PI * 2f), sol, interior ? 1f : 0f, arte.sombrasContato));
            Shader.SetGlobalInt("_MTV_Count", 0);

            // Superfícies (água e pisos) e malhas vivas (copas e redes) só existem em jogo: monta aqui.
            foreach (var s in regiao.GetComponentsInChildren<MTMSuperficieV23>(false))
                s.Reconstruir();
            var visuais = regiao.GetComponentsInChildren<MTMVisualV2>(false);
            foreach (var v in visuais)
                Chamar(v, "Awake");
            var boias = regiao.GetComponentsInChildren<MTMFlutuanteV25>(false);
            foreach (var b in boias)
                Chamar(b, "Awake");
            var malhas = regiao.GetComponentsInChildren<MTMDeformacaoV22>(false);
            foreach (var m in malhas)
                Chamar(m, "OnEnable");
            // -andando: os moradores caminham no lugar, para ver o ciclo da passada quadro a quadro.
            bool andando = Argumento("-andando") != null;
            var atores = regiao.GetComponentsInChildren<MTMAtorV22>(false);
            var fases = new float[atores.Length];

            if (Argumento("-diagnostico") != null)
                Diagnosticar(malhas);
            float tamanhoBase = Mathf.Max(regiao.dados.altura * .5f, regiao.dados.largura * .5f / (largura / (float)altura)) * 1.04f;
            var go = new GameObject("Captura_Animada");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = tamanhoBase / zoom;
            // -vista N: altura visível em unidades (enquadramento fixo, independente do tamanho da região).
            if (float.TryParse(Argumento("-vista"), NumberStyles.Float, CultureInfo.InvariantCulture, out var vista) && vista > .5f)
                cam.orthographicSize = vista * .5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.12f, .11f, .1f);
            cam.transform.position = regiao.transform.position + new Vector3(foco.x, foco.y, -10f);
            var rt = new RenderTexture(largura, altura, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var tex = new Texture2D(largura, altura, TextureFormat.RGB24, false);
            try
            {
                for (int i = 0; i < quadros; i++)
                {
                    float t = 20f + i / fps;
                    MTMAmbienteV2.TempoFixo = t;
                    Shader.SetGlobalFloat("_MTV_Time", t);
                    foreach (var v in visuais)
                        Chamar(v, "LateUpdate");
                    foreach (var b in boias)
                        Chamar(b, "LateUpdate");
                    if (andando)
                        AndarNoLugar(atores, fases, 1f / fps);
                    foreach (var m in malhas)
                    {
                        Definir(m, "posePendente", true);
                        Chamar(m, "LateUpdate");
                    }
                    foreach (var o in regiao.GetComponentsInChildren<MTMOrdemY>(false))
                    {
                        var sr = o.GetComponent<SpriteRenderer>();
                        sr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-o.transform.position.y * 100f), -28000, 28000);
                    }

                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(pasta, "quadro_" + i.ToString("000") + ".png"), tex.EncodeToPNG());
                }
            }
            finally
            {
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
                foreach (var m in malhas)
                    Chamar(m, "OnDisable");
            }

            Debug.Log("Captura animada: " + quadros + " quadros de " + regiao.dados.id + " em " + Path.GetFullPath(pasta));
        }

        // Faz cada morador "andar" para a direita na velocidade dos moradores, sem sair do lugar:
        // avança a fase da passada pela distância, como MTMAtorV22 faz em jogo.
        private static void AndarNoLugar(MTMAtorV22[] atores, float[] fases, float dt)
        {
            for (int i = 0; i < atores.Length; i++)
            {
                var a = atores[i];
                if (a == null || a.tipo != MTMAtorV22.Tipo.Morador)
                    continue;
                var sr = a.GetComponent<SpriteRenderer>();
                float rapidez = a.vida != null ? a.vida.velocidadeMoradores : .48f;
                float alturaArte = sr != null && sr.sprite != null ? sr.sprite.bounds.size.y * Mathf.Abs(a.transform.lossyScale.y) : 1.6f;
                var metodo = typeof(MTMAtorV22).GetMethod("Passada", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                float passada = metodo != null ? (float)metodo.Invoke(null, new object[] { alturaArte, a.tipo }) : Mathf.Max(.35f, alturaArte * .45f);
                fases[i] = Mathf.Repeat(fases[i] + rapidez * dt / passada * Mathf.PI * 2f, Mathf.PI * 2f);
                Definir(a, "RapidezReal", rapidez);
                Definir(a, "FaseCaminhada", fases[i]);
                Definir(a, "VelocidadeReal", new Vector2(rapidez, 0f));
            }
        }

        // Escreve um campo ou uma propriedade (mesmo com set privado), se existir nesta versão do código.
        private static void Definir(object alvo, string nome, object valor)
        {
            var tipo = alvo.GetType();
            var p = tipo.GetProperty(nome, Membro);
            if (p != null)
            {
                p.GetSetMethod(true)?.Invoke(alvo, new[] { valor });
                return;
            }

            tipo.GetField(nome, Membro)?.SetValue(alvo, valor);
        }

        // Aplica o kit HD2D (arte revisada dos moradores e do Neto), como o jogo faz ao começar.
        private static IDisposable AplicarKit()
        {
            var jogo = UnityEngine.Object.FindFirstObjectByType<MTMJogo>(FindObjectsInactive.Include);
            if (jogo == null)
                return null;
            var kit = new MTMKitHD2D();
            try
            {
                kit.Aplicar(jogo);
                return kit;
            }
            catch (Exception erro)
            {
                Debug.LogWarning("Captura animada: kit HD2D não aplicado (" + erro.Message + ").");
                kit.Dispose();
                return null;
            }
        }

        // Mostra no log o que chega ao shader das primeiras malhas vivas (copas e redes).
        private static void Diagnosticar(MTMDeformacaoV22[] malhas)
        {
            int n = 0;
            foreach (var m in malhas)
            {
                if (m.tipo == MTMDeformacaoV22.Tipo.Pessoa || n++ >= 6)
                    continue;
                Chamar(m, "LateUpdate");
                var fonte = m.GetComponent<SpriteRenderer>();
                var filho = m.transform.Find("Malha_Viva_V22");
                var mr = filho != null ? filho.GetComponent<MeshRenderer>() : null;
                var mf = filho != null ? filho.GetComponent<MeshFilter>() : null;
                var bloco = new MaterialPropertyBlock();
                if (mr != null)
                    mr.GetPropertyBlock(bloco);
                var uv2 = new System.Collections.Generic.List<Vector2>();
                if (mf != null && mf.sharedMesh != null)
                    mf.sharedMesh.GetUVs(1, uv2);
                float minV = 1f, maxV = 0f;
                foreach (var u in uv2)
                {
                    minV = Mathf.Min(minV, u.y);
                    maxV = Mathf.Max(maxV, u.y);
                }

                Debug.Log(string.Format(CultureInfo.InvariantCulture,
                    "Diagnóstico: {0} tipo={1} amp={2} inicio={3} spriteOculto={4} malha={5} renderiza={6} verts={7} uv2={8} ({9:0.00}..{10:0.00}) deform={11} shader={12} sprite={13} ppu={14:0.0} alturaU={15:0.00} arte={16}",
                    Caminho(m.transform), m.tipo, m.amplitude, m.inicioDoMovimento, fonte != null && fonte.forceRenderingOff,
                    filho != null, mr != null && mr.enabled, mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount : -1,
                    uv2.Count, minV, maxV, bloco.GetVector("_MTM_Deform"), mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.shader.name : "-",
                    fonte != null && fonte.sprite != null ? fonte.sprite.name : "-", fonte != null && fonte.sprite != null ? fonte.sprite.pixelsPerUnit : 0f,
                    fonte != null && fonte.sprite != null ? fonte.sprite.bounds.size.y : 0f, Shader.GetGlobalVector("_MTV_Art")));
            }
        }

        private static string Caminho(Transform t)
        {
            string s = t.name;
            for (var p = t.parent; p != null && p.GetComponent<MTMRegiao>() == null; p = p.parent)
                s = p.name + "/" + s;
            return s;
        }

        private static void Chamar(object alvo, string metodo)
        {
            var m = alvo.GetType().GetMethod(metodo, Membro, null, Type.EmptyTypes, null);
            m?.Invoke(alvo, null);
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

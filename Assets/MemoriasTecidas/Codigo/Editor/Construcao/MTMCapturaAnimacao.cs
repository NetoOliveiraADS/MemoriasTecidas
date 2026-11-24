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

            Iluminar(regiao.dados.interior, hora);

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

        // Ambiente de um dia claro, como o jogo calcula em MTMAmbienteV2.
        private static void Iluminar(bool interior, float hora)
        {
            var arte = MTMConfiguracaoVisual.Atual;
            float sol = MTMCalculosVisuais.Sol(hora);
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
        }

        // Cada pessoa de perto, como aparece no jogo (arte revisada, malha viva e luz do dia), parada e andando.
        //   unity run <projeto> -- -executeMethod MemoriasTecidas.MundoV2.MTMCapturaAnimacao.Elenco
        //     -saida Builds/Capturas/elenco [-quadros 14 -fps 10 -largura 220 -altura 340 -vista 2.3]
        // Grava <saida>/<arte>_parado.png, <arte>_andar_000.png... e elenco.txt com as medidas de cada um.
        public static void Elenco()
        {
            try
            {
                var cultura = CultureInfo.InvariantCulture;
                string saida = Argumento("-saida") ?? Path.Combine("Builds", "Capturas", "elenco");
                int quadros = int.TryParse(Argumento("-quadros"), NumberStyles.Integer, cultura, out var q) ? Mathf.Clamp(q, 0, 120) : 14;
                float fps = float.TryParse(Argumento("-fps"), NumberStyles.Float, cultura, out var f) ? Mathf.Max(1f, f) : 10f;
                int largura = int.TryParse(Argumento("-largura"), NumberStyles.Integer, cultura, out var w) ? w : 220;
                int altura = int.TryParse(Argumento("-altura"), NumberStyles.Integer, cultura, out var a) ? a : 340;
                float vista = float.TryParse(Argumento("-vista"), NumberStyles.Float, cultura, out var v) ? Mathf.Max(.5f, v) : 2.3f;
                // -filtro Zeca,Cida: só as artes cujo nome contém um dos textos.
                filtroElenco = Argumento("-filtro")?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                Directory.CreateDirectory(saida);
                foreach (var antigo in Directory.GetFiles(saida, "*.png"))
                    File.Delete(antigo);

                EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
                using var kit = AplicarKit();
                var regioes = UnityEngine.Object.FindObjectsByType<MTMRegiao>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Array.Sort(regioes, (x, y) => string.CompareOrdinal(x.dados != null ? x.dados.id : "", y.dados != null ? y.dados.id : ""));
                var relatorio = new System.Text.StringBuilder("arte\tregião\tobjeto\tcabeça\tcorpo\tpivôX(px)\tlargura(px)\tppu\taltura(u)\n");
                var feitos = new System.Collections.Generic.HashSet<string>();
                // O Neto entra como referência de altura.
                foreach (var ator in UnityEngine.Object.FindObjectsByType<MTMAtorV22>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (ator.tipo == MTMAtorV22.Tipo.Jogador && ator.GetComponentInParent<MTMRegiao>(true) == null)
                    {
                        foreach (var r in regioes)
                            r.gameObject.SetActive(false);
                        Retratar(null, ator, saida, quadros, fps, largura, altura, vista, relatorio, feitos);
                        break;
                    }

                foreach (var regiao in regioes)
                {
                    if (regiao.dados == null)
                        continue;
                    foreach (var r in regioes)
                        r.gameObject.SetActive(r == regiao);
                    foreach (var ator in regiao.GetComponentsInChildren<MTMAtorV22>(true))
                        if (ator.tipo == MTMAtorV22.Tipo.Morador)
                            Retratar(regiao, ator, saida, quadros, fps, largura, altura, vista, relatorio, feitos);
                }

                File.WriteAllText(Path.Combine(saida, "elenco.txt"), relatorio.ToString());
                Debug.Log("Elenco: " + feitos.Count + " pessoas em " + Path.GetFullPath(saida));
            }
            catch (Exception erro)
            {
                Debug.LogError("Elenco: " + erro);
                EditorApplication.Exit(1);
            }
            finally
            {
                MTMAmbienteV2.TempoFixo = float.NaN;
            }
        }

        private static string[] filtroElenco;

        // Fotografa uma pessoa sozinha (numa camada só dela, com a sombra), a primeira de cada arte.
        private static void Retratar(MTMRegiao regiao, MTMAtorV22 ator, string pasta, int quadros, float fps, int largura, int altura, float vista,
            System.Text.StringBuilder relatorio, System.Collections.Generic.HashSet<string> feitos)
        {
            const int Camada = 31;
            var sr = ator.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null ||
                (filtroElenco != null && !Array.Exists(filtroElenco, f => sr.sprite.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                !feitos.Add(sr.sprite.name))
                return;
            var spriteParado = sr.sprite;
            // Pessoas que só entram mais tarde na história ficam desligadas: liga só durante a foto.
            var ligados = new System.Collections.Generic.List<GameObject>();
            for (var t = ator.transform; t != null && (regiao == null || t != regiao.transform); t = t.parent)
                if (!t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(true);
                    ligados.Add(t.gameObject);
                }

            var malha = ator.GetComponent<MTMDeformacaoV22>();
            var visual = ator.GetComponentInParent<MTMVisualV2>();
            var camadas = new System.Collections.Generic.Dictionary<GameObject, int>();
            void Isolar(GameObject g)
            {
                if (g != null && !camadas.ContainsKey(g))
                {
                    camadas[g] = g.layer;
                    g.layer = Camada;
                }
            }

            Iluminar(regiao != null && regiao.dados.interior, .40f);
            if (visual != null)
                Chamar(visual, "Awake");
            if (malha != null)
                Chamar(malha, "OnEnable");
            Isolar(sr.gameObject);
            var filho = malha != null ? malha.transform.Find("Malha_Viva_V22") : null;
            if (filho != null)
                Isolar(filho.gameObject);
            if (visual != null && visual.sombra != null)
                Isolar(visual.sombra.gameObject);

            var go = new GameObject("Captura_Elenco");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = vista * .5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.23f, .21f, .19f);
            cam.cullingMask = 1 << Camada;
            Vector3 pe = sr.transform.position;
            cam.transform.position = new Vector3(pe.x, pe.y + vista * .43f, pe.z - 10f);
            var rt = new RenderTexture(largura, altura, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = largura / (float)altura;
            var tex = new Texture2D(largura, altura, TextureFormat.RGB24, false);
            string nome = sr.sprite.name.Replace("HD2D_", "");
            var atores = new[] { ator };
            var fases = new float[1];
            try
            {
                // -1: parado; depois, a passada no lugar.
                for (int i = -1; i < quadros; i++)
                {
                    float tempo = 20f + Mathf.Max(0, i) / fps;
                    MTMAmbienteV2.TempoFixo = tempo;
                    Shader.SetGlobalFloat("_MTV_Time", tempo);
                    if (i >= 0)
                        AndarNoLugar(atores, fases, 1f / fps);
                    if (visual != null)
                        Chamar(visual, "LateUpdate");
                    if (malha != null)
                    {
                        Definir(malha, "posePendente", true);
                        Chamar(malha, "LateUpdate");
                    }

                    cam.Render();
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(pasta, nome + (i < 0 ? "_parado" : "_andar_" + i.ToString("000")) + ".png"), tex.EncodeToPNG());
                }

                var s = sr.sprite;
                relatorio.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}\t{4}\t{5:0.0}\t{6:0}\t{7:0.0}\t{8:0.000}",
                    nome, regiao != null ? regiao.dados.id : "-", Caminho(ator.transform), malha != null ? malha.escalaCabeca : 1f, malha != null ? malha.alongamentoCorpo : 1f,
                    s.pivot.x, s.rect.width, s.pixelsPerUnit, s.bounds.size.y * Mathf.Abs(ator.transform.lossyScale.y)));
            }
            finally
            {
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
                sr.sprite = spriteParado;
                if (malha != null)
                    Chamar(malha, "OnDisable");
                foreach (var par in camadas)
                    if (par.Key != null)
                        par.Key.layer = par.Value;
                for (int i = ligados.Count - 1; i >= 0; i--)
                    ligados[i].SetActive(false);
            }
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
                // Quem tem caminhada de perfil troca de pose como no jogo: a folha inteira a cada passada.
                if (sr != null && a.quadrosLado != null && a.quadrosLado.Length > 0)
                {
                    int n = a.quadrosLado.Length;
                    sr.sprite = a.quadrosLado[Mathf.Clamp(Mathf.FloorToInt(fases[i] / (Mathf.PI * 2f) * n), 0, n - 1)];
                    sr.flipX = false;
                }
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    // Reparo pontual e diagnostico. Nao recria o mapa e nao troca assets de arte.
    [InitializeOnLoad]
    public static class MTMReparoVisualV21
    {
        private const string Menu = "Memorias Tecidas/Mundo V2/Reparo visual/";
        private const string Montador = "Assets/MemoriasTecidas/MundoV2/Editor/MTMMontador.cs";
        private const string Marcador = "// MTM_REPARO_PARTICULAS_V21";
        private static readonly Dictionary<SpriteRenderer, Material[]> originais =
            new Dictionary<SpriteRenderer, Material[]>();
        private static Material materialTeste;

        static MTMReparoVisualV21()
        {
            EditorApplication.playModeStateChanged += AoMudarPlay;
            AssemblyReloadEvents.beforeAssemblyReload += Restaurar;
        }

        [MenuItem(Menu + "1 - Corrigir particulas da cena")]
        public static void CorrigirParticulas()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Pare o Play antes de corrigir e salvar as particulas.");
                return;
            }
            try
            {
                MTMJogo jogo = JogoDaCena();
                Scene cena = jogo.gameObject.scene;
                if (string.IsNullOrEmpty(cena.path))
                    throw new InvalidOperationException("Salve a cena antes de executar o reparo.");

                var alvos = new List<ParticleSystem>();
                foreach (ParticleSystem ps in Componentes<ParticleSystem>(jogo))
                {
                    var v = ps.velocityOverLifetime;
                    // Corrige somente o defeito observado no pacote V2.1.
                    // Preserva outros efeitos que tenham sido personalizados.
                    if (v.enabled && v.x.mode == ParticleSystemCurveMode.TwoConstants &&
                        v.y.mode == ParticleSystemCurveMode.TwoConstants &&
                        v.z.mode == ParticleSystemCurveMode.Constant)
                        alvos.Add(ps);
                }

                string fonte = File.Exists(Montador) ? File.ReadAllText(Montador) : null;
                string fonteNova = PrepararCorrecaoMontador(fonte, out string statusMontador);
                bool mudaFonte = fonteNova != null && fonteNova != fonte;
                if (alvos.Count == 0 && !mudaFonte)
                {
                    Debug.Log("Nenhuma particula com o defeito conhecido. " + statusMontador, jogo);
                    GravarRelatorio(jogo, "Nada alterado. " + statusMontador);
                    return;
                }

                // Salva primeiro para incluir as edicoes atuais no backup.
                if (!EditorSceneManager.SaveScene(cena))
                    throw new IOException("Nao foi possivel salvar a cena antes do backup.");
                string pasta = Path.Combine(RaizProjeto(), "Backups_MemoriasTecidas", Momento());
                Directory.CreateDirectory(pasta);
                CopiarComMeta(cena.path, Path.Combine(pasta, Path.GetFileName(cena.path)));
                if (mudaFonte)
                    CopiarComMeta(Montador, Path.Combine(pasta, "MTMMontador.cs"));

                Undo.IncrementCurrentGroup();
                int grupo = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Corrigir particulas de Memorias Tecidas");
                foreach (ParticleSystem ps in alvos)
                {
                    Undo.RecordObject(ps, "Alinhar modo de velocidade XYZ");
                    var v = ps.velocityOverLifetime;
                    float z = v.z.constant;
                    v.enabled = false;
                    v.z = new ParticleSystem.MinMaxCurve(z, z);
                    v.enabled = true;
                    if (PrefabUtility.IsPartOfPrefabInstance(ps))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(ps);
                    EditorUtility.SetDirty(ps);
                }
                Undo.CollapseUndoOperations(grupo);
                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena))
                    throw new IOException("Reparo aplicado, mas a cena precisa ser salva manualmente.");
                if (mudaFonte)
                    File.WriteAllText(Montador, fonteNova, new UTF8Encoding(false));

                string resultado = "Particulas corrigidas: " + alvos.Count + ". " + statusMontador
                    + "\nBackup: " + pasta;
                Debug.Log(resultado, jogo);
                GravarRelatorio(jogo, resultado);
                SceneView.RepaintAll();
                if (mudaFonte) AssetDatabase.ImportAsset(Montador);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        // Uma insercao localizada; conserva o restante do arquivo do usuario.
        internal static string PrepararCorrecaoMontador(string fonte, out string status)
        {
            if (fonte == null)
            {
                status = "Montador nao encontrado no caminho esperado; fonte nao alterada.";
                return null;
            }
            if (fonte.Contains(Marcador))
            {
                status = "Montador ja contem a correcao identificada.";
                return fonte;
            }
            if (Regex.IsMatch(fonte, @"\bvel\s*\.\s*z\s*="))
            {
                status = "Montador ja configura Z; preservei a implementacao existente.";
                return fonte;
            }
            var padrao = new Regex(@"\bvel\s*\.\s*y\s*=\s*new\s+ParticleSystem\s*\.\s*MinMaxCurve\s*\(\s*-\s*0?\.02f\s*,\s*0?\.07f\s*\)\s*;");
            MatchCollection achados = padrao.Matches(fonte);
            if (achados.Count != 1 || !fonte.Contains("velocityOverLifetime"))
            {
                status = "Montador diferente do padrao V2.1; fonte preservada para revisao manual.";
                return fonte;
            }
            Match m = achados[0];
            string nl = fonte.Contains("\r\n") ? "\r\n" : "\n";
            status = "Montador corrigido para futuras cenas.";
            return fonte.Insert(m.Index + m.Length,
                nl + "            " + Marcador + nl +
                "            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);");
        }

        [MenuItem(Menu + "2 - Gerar diagnostico da cena")]
        public static void Diagnosticar()
        {
            try { GravarRelatorio(JogoDaCena(), "Diagnostico solicitado pelo menu."); }
            catch (Exception e) { Debug.LogException(e); }
        }

        [MenuItem(Menu + "3 - Testar material padrao durante Play")]
        public static void TestarMaterial()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Entre no Play e depois execute o teste. Ele e temporario.");
                return;
            }
            try
            {
                MTMJogo jogo = JogoDaCena();
                if (materialTeste != null)
                {
                    Debug.Log("O teste ja esta ativo. Pare o Play ou use Restaurar materiais.", jogo);
                    return;
                }
                RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
                bool urp = pipeline != null && pipeline.GetType().Name.Contains("Universal");
                if (pipeline != null && !urp)
                    throw new InvalidOperationException("Teste preparado para Built-in e URP. O relatorio identifica seu pipeline.");
                Shader shader = urp
                    ? AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.render-pipelines.universal/Shaders/2D/Sprite-Unlit-Default.shader")
                    : Shader.Find("Sprites/Default");
                if (shader == null && urp)
                    shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                    throw new InvalidOperationException("O shader padrao nao esta disponivel ou tem erro. Gere o diagnostico.");

                GravarRelatorio(jogo, "Antes do teste de material padrao.");
                materialTeste = new Material(shader)
                {
                    name = "MTM_Teste_Temporario_Nao_Salvar",
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (materialTeste.HasProperty("_Color")) materialTeste.SetColor("_Color", Color.white);
                foreach (SpriteRenderer sr in Componentes<SpriteRenderer>(jogo))
                {
                    originais.Add(sr, sr.sharedMaterials);
                    sr.sharedMaterials = new[] { materialTeste };
                }
                Debug.Log("Teste temporario em " + originais.Count + " SpriteRenderers com " + shader.name
                    + ". Observe se o cenario aparece. A iluminacao personalizada e a ondulacao ficam fora deste teste. "
                    + "Parar o Play restaura os materiais.", jogo);
                SceneView.RepaintAll();
            }
            catch (Exception e) { Restaurar(); Debug.LogException(e); }
        }

        [MenuItem(Menu + "4 - Restaurar materiais do teste")]
        public static void Restaurar()
        {
            foreach (var item in originais)
                if (item.Key != null) item.Key.sharedMaterials = item.Value;
            originais.Clear();
            if (materialTeste != null) UnityEngine.Object.DestroyImmediate(materialTeste);
            materialTeste = null;
        }

        private static void AoMudarPlay(PlayModeStateChange estado)
        {
            if (estado == PlayModeStateChange.ExitingPlayMode ||
                estado == PlayModeStateChange.EnteredEditMode) Restaurar();
        }

        private static MTMJogo JogoDaCena()
        {
            Scene cena = SceneManager.GetActiveScene();
            var jogos = new List<MTMJogo>();
            foreach (GameObject raiz in cena.GetRootGameObjects())
                jogos.AddRange(raiz.GetComponentsInChildren<MTMJogo>(true));
            if (jogos.Count != 1)
                throw new InvalidOperationException("Abra a cena do Mundo V2 com exatamente um MTMJogo. Encontrados: " + jogos.Count);
            return jogos[0];
        }

        private static List<T> Componentes<T>(MTMJogo jogo) where T : Component
        {
            var todos = new HashSet<T>(jogo.GetComponentsInChildren<T>(true));
            if (jogo.regioes != null) foreach (MTMRegiao regiao in jogo.regioes)
                if (regiao != null && regiao.gameObject.scene == jogo.gameObject.scene)
                    foreach (T c in regiao.GetComponentsInChildren<T>(true)) todos.Add(c);
            return new List<T>(todos);
        }

        private static void GravarRelatorio(MTMJogo jogo, string observacao)
        {
            var texto = new StringBuilder();
            texto.AppendLine("MEMORIAS TECIDAS - DIAGNOSTICO VISUAL V2.1");
            texto.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            texto.AppendLine(observacao);
            texto.AppendLine("Cena: " + jogo.gameObject.scene.path);
            texto.AppendLine("Unity: " + Application.unityVersion + " | Play: " + EditorApplication.isPlaying);
            texto.AppendLine("GPU: " + SystemInfo.graphicsDeviceName + " | API: " + SystemInfo.graphicsDeviceType);
            var pipeline = GraphicsSettings.currentRenderPipeline;
            texto.AppendLine("Pipeline ativo: " + (pipeline == null ? "Built-in ou ainda nao inicializado no editor" : pipeline.GetType().FullName));
            if (pipeline != null)
            {
                texto.AppendLine("Asset pipeline: " + AssetDatabase.GetAssetPath(pipeline));
                var pipelineSerializado = new SerializedObject(pipeline);
                var indice = pipelineSerializado.FindProperty("m_DefaultRendererIndex");
                if (indice != null) texto.AppendLine("Indice do renderizador padrao: " + indice.intValue);
                var lista = pipelineSerializado.FindProperty("m_RendererDataList");
                if (lista != null && lista.isArray)
                    for (int i = 0; i < lista.arraySize; i++)
                    {
                        var dado = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                        texto.AppendLine("  Renderizador " + i + ": " + (dado == null ? "NULL" :
                            dado.GetType().FullName + " | " + AssetDatabase.GetAssetPath(dado)));
                    }
            }
            texto.AppendLine("Qualidade Unity: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
            texto.AppendLine("Teste de material ativo: " + (materialTeste != null));
            Camera cam = jogo.cameraDoMapa;
            if (cam == null) texto.AppendLine("CAMERA AUSENTE no MTMJogo");
            else
            {
                texto.AppendLine("Camera: " + cam.name + " | ativa: " + cam.isActiveAndEnabled
                    + " | posicao: " + cam.transform.position + " | rotacao: " + cam.transform.eulerAngles
                    + " | mascara: " + cam.cullingMask + " | ortografica: " + cam.orthographic);
                foreach (Component c in cam.GetComponents<Component>())
                    if (c != null) texto.AppendLine("  Componente camera: " + c.GetType().FullName);
            }
            if (jogo.regioes != null) foreach (MTMRegiao r in jogo.regioes)
                texto.AppendLine(r == null ? "REGIAO COM REFERENCIA AUSENTE" :
                    "Regiao: " + r.name + " | ativa: " + r.gameObject.activeInHierarchy);

            var sprites = Componentes<SpriteRenderer>(jogo);
            var materiais = new HashSet<Material>();
            int ativos = 0, semSprite = 0, semMaterial = 0, alfaZero = 0;
            texto.AppendLine("\nSPRITES");
            foreach (SpriteRenderer sr in sprites)
            {
                bool ativo = sr.gameObject.activeInHierarchy && sr.enabled;
                if (ativo) ativos++;
                if (sr.sprite == null) semSprite++;
                if (sr.sharedMaterial == null) semMaterial++;
                if (sr.color.a <= 0f) alfaZero++;
                if (sr.sharedMaterial != null) materiais.Add(sr.sharedMaterial);
                if (ativo)
                    texto.AppendLine(Caminho(sr.transform) + " | sprite=" + (sr.sprite == null ? "NULL" : sr.sprite.name)
                        + " | material=" + (sr.sharedMaterial == null ? "NULL" : sr.sharedMaterial.name)
                        + " | cor=" + sr.color + " | forceOff=" + sr.forceRenderingOff
                        + " | bounds=" + sr.bounds + " | layer=" + sr.gameObject.layer
                        + " | sorting=" + sr.sortingLayerName + "/" + sr.sortingOrder);
            }
            texto.AppendLine("Total=" + sprites.Count + "; ativos=" + ativos + "; sprite nulo=" + semSprite
                + "; material nulo=" + semMaterial + "; alpha zero=" + alfaZero);
            texto.AppendLine("Fora do Play, jogador e sombra podem ter sprite nulo: os recortes sao criados em runtime.");
            texto.AppendLine("\nMATERIAIS");
            var shadersRegistrados = new HashSet<string>();
            foreach (Material m in materiais)
            {
                texto.AppendLine(m.name + " | arquivo=" + AssetDatabase.GetAssetPath(m)
                    + " | shader=" + (m.shader == null ? "NULL" : m.shader.name)
                    + " | queue=" + m.renderQueue + " | passes=" + m.passCount);
                if (m.HasProperty("_Color")) texto.AppendLine("  Cor do material: " + m.GetColor("_Color"));
                if (m.shader != null)
                {
                    texto.AppendLine("  shaderAsset=" + AssetDatabase.GetAssetPath(m.shader)
                        + " | suportado=" + m.shader.isSupported + " | erro=" + ShaderUtil.ShaderHasError(m.shader));
                    string arquivo = AssetDatabase.GetAssetPath(m.shader);
                    if (arquivo.StartsWith("Assets/MemoriasTecidas/MundoV2/Gerados/", StringComparison.Ordinal)
                        && File.Exists(arquivo) && shadersRegistrados.Add(arquivo))
                    {
                        texto.AppendLine("  INICIO DO SHADER DO MODULO");
                        texto.AppendLine(File.ReadAllText(arquivo));
                        texto.AppendLine("  FIM DO SHADER DO MODULO");
                    }
                }
            }
            texto.AppendLine("\nPARTICULAS");
            foreach (ParticleSystem ps in Componentes<ParticleSystem>(jogo))
            {
                var v = ps.velocityOverLifetime;
                texto.AppendLine(Caminho(ps.transform) + " | velocity enabled=" + v.enabled
                    + " | XYZ=" + v.x.mode + "," + v.y.mode + "," + v.z.mode);
            }
            texto.AppendLine("\nARQUIVOS PARA CONFERIR NO PROJETO");
            texto.AppendLine("Assets/MemoriasTecidas/MundoV2/Gerados/Pintado_V2.mat");
            texto.AppendLine("Assets/MemoriasTecidas/MundoV2/Gerados/Shaders/PintadoV2.shader");
            texto.AppendLine("ProjectSettings/GraphicsSettings.asset e QualitySettings.asset; Packages/manifest.json");
            texto.AppendLine("O diagnostico nao instala pacotes nem altera a configuracao de renderizacao.");
            string pasta = Path.Combine(RaizProjeto(), "Diagnosticos_MemoriasTecidas");
            Directory.CreateDirectory(pasta);
            string destino = Path.Combine(pasta, "Diagnostico_Visual_" + Momento() + ".txt");
            File.WriteAllText(destino, texto.ToString(), new UTF8Encoding(false));
            Debug.Log("Relatorio visual salvo em: " + destino, jogo);
            EditorUtility.RevealInFinder(destino);
        }

        private static string Caminho(Transform t)
        {
            string nome = t.name;
            while (t.parent != null) { t = t.parent; nome = t.name + "/" + nome; }
            return nome;
        }
        private static string RaizProjeto() { return Directory.GetParent(Application.dataPath).FullName; }
        private static string Momento() { return DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6); }
        private static void CopiarComMeta(string origem, string destino)
        {
            File.Copy(origem, destino, false);
            if (File.Exists(origem + ".meta")) File.Copy(origem + ".meta", destino + ".meta", false);
        }
    }
}

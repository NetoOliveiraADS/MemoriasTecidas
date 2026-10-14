using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.ExpansaoV1.Editor
{
    public static class MTEXMontador
    {
        private const string Raiz = "Assets/MemoriasTecidas/ExpansaoV1";
        private const string Gerados = Raiz + "/Gerados";
        [Serializable] public sealed class Dados { public Arte[] assets; public Area[] areas; public Ponto[] agua; }
        [Serializable] public sealed class Arte
        {
            public string id, arquivo;
            public float ppu, pivotX, pivotY;
            public bool piso;
            public float colisorLargura, colisorAltura, colisorY;
        }
        [Serializable] public sealed class Area { public string nome; public float centroX; public Item[] itens; }
        [Serializable] public sealed class Item { public string id; public float x, y; public bool luz; }
        [Serializable] public sealed class Ponto { public float x, y; }
        // Leitura explícita dos recortes da Praça V1, sem dependência de seus scripts.
        [Serializable] private sealed class Praca { public Folha[] assets; }
        [Serializable] private sealed class Folha
        {
            public string id, file;
            public int width, height;
            public float ppu;
            public Quadro[] frames;
        }
        [Serializable] private sealed class Quadro { public Recorte rect; public Ponto pivot; public float pixelsPorUnidade; }
        [Serializable] private sealed class Recorte { public float x, y, width, height; }

        [MenuItem("Memorias Tecidas/Expansao V1/1 - Preparar arte e prefabs")]
        public static void PrepararMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Saia do Play para preparar os assets."); return; }
            try { Preparar(Ler()); Debug.Log("Expansão visual: prefabs prontos em " + Gerados + "/Prefabs. Arraste-os para sua cena ou crie a demonstração pelo menu 2."); }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static Dados Ler()
        {
            string caminho = Raiz + "/Data/expansao.json";
            if (!File.Exists(caminho)) throw new FileNotFoundException("Mantenha o complemento em " + Raiz, caminho);
            Dados d = JsonUtility.FromJson<Dados>(File.ReadAllText(caminho));
            if (d == null || d.assets == null || d.areas == null) throw new InvalidDataException("Manifesto da expansão incompleto.");
            foreach (Arte a in d.assets)
                if (string.IsNullOrEmpty(a.id) || a.ppu <= 0f || a.pivotX < 0f || a.pivotX > 1f || a.pivotY < 0f || a.pivotY > 1f)
                    throw new InvalidDataException("Configuração de sprite inválida: " + a.id);
            return d;
        }

        private static Material MaterialPrincipal()
        {
            string path = Gerados + "/Expansao_Unlit.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") : Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Shader de sprite não encontrado. Use um projeto 2D compatível.");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void Preparar(Dados dados)
        {
            Directory.CreateDirectory(Gerados + "/Prefabs");
            Directory.CreateDirectory(Gerados + "/Cenas");
            AssetDatabase.Refresh();
            Material mat = MaterialPrincipal();
            foreach (Arte a in dados.assets)
            {
                string path = Raiz + "/" + a.arquivo;
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) throw new FileNotFoundException("Imagem ausente: " + path);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = a.ppu;
                imp.alphaIsTransparency = !a.piso;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Bilinear;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.maxTextureSize = 4096;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); imp.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(a.pivotX, a.pivotY);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(settings); imp.SaveAndReimport();
                string prefab = Gerados + "/Prefabs/" + a.id + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefab) != null) continue;
                Scene previa = EditorSceneManager.NewPreviewScene();
                GameObject obj = null;
                try
                {
                    obj = new GameObject(a.id);
                    SceneManager.MoveGameObjectToScene(obj, previa);
                    var sr = obj.AddComponent<SpriteRenderer>();
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    sr.sharedMaterial = mat; sr.spriteSortPoint = SpriteSortPoint.Pivot;
                    if (sr.sprite == null) throw new InvalidOperationException("Sprite não carregado: " + a.id);
                    if (a.piso) sr.sortingOrder = -30000;
                    else obj.AddComponent<MTEXOrdemY>();
                    if (a.colisorLargura > 0f && a.colisorAltura > 0f)
                    {
                        var col = obj.AddComponent<BoxCollider2D>();
                        col.size = new Vector2(a.colisorLargura, a.colisorAltura);
                        col.offset = new Vector2(0f, a.colisorY);
                    }
                    PrefabUtility.SaveAsPrefabAsset(obj, prefab);
                }
                finally
                {
                    if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
                    EditorSceneManager.ClosePreviewScene(previa);
                }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Memorias Tecidas/Expansao V1/2 - Criar cena de demonstracao")]
        public static void CriarCena()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Saia do Play antes de criar a demonstração."); return; }
            try
            {
                Dados dados = Ler();
                Folha folha = LerJogador(); // Confere a dependência antes de mudar de cena.
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Preparar(dados);
                Sprite brilho = CriarMascaraDeLuz();
                Material mat = MaterialPrincipal();
                Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var raiz = new GameObject("Expansao_Visual_V1");
                var iluminacao = new GameObject("Halos_Noturnos");
                iluminacao.transform.SetParent(raiz.transform, false);
                GameObject mulher = null, centaura = null;
                foreach (Area a in dados.areas)
                {
                    var setor = new GameObject(a.nome);
                    setor.transform.SetParent(raiz.transform, false);
                    setor.transform.position = new Vector3(a.centroX, 0f, 0f);
                    foreach (Item i in a.itens)
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Gerados + "/Prefabs/" + i.id + ".prefab");
                        if (prefab == null) throw new FileNotFoundException("Prefab ausente: " + i.id);
                        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cena);
                        obj.transform.SetParent(setor.transform, false);
                        obj.transform.localPosition = new Vector3(i.x, i.y, 0f);
                        Arte arte = Array.Find(dados.assets, x => x.id == i.id);
                        if (arte != null && arte.piso)
                        {
                            Vector2 size = obj.GetComponent<SpriteRenderer>().sprite.bounds.size;
                            obj.transform.localScale = new Vector3(32f / size.x, 18f / size.y, 1f);
                        }
                        if (i.id == "mulher_cavalo_relato") mulher = obj;
                        if (i.id == "mulher_cavalo_centaura") centaura = obj;
                        if (i.luz) Halo(iluminacao.transform, obj.transform.position + new Vector3(0f, -.4f, 0f), brilho, mat);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
                    }
                    Parede(setor.transform, "Limite_Norte", new Vector2(0f, 8.3f), new Vector2(33f, .7f));
                    Parede(setor.transform, "Limite_Sul", new Vector2(0f, -8.8f), new Vector2(33f, .7f));
                    Parede(setor.transform, "Limite_Oeste", new Vector2(-15.8f, 0f), new Vector2(.7f, 18f));
                    Parede(setor.transform, "Limite_Leste", new Vector2(15.8f, 0f), new Vector2(.7f, 18f));
                    if (a.nome == "03_Rio_Carnaubal" && dados.agua != null && dados.agua.Length >= 3)
                    {
                        var agua = new GameObject("Barreira_Agua"); agua.transform.SetParent(setor.transform, false);
                        var col = agua.AddComponent<PolygonCollider2D>();
                        var pontos = new Vector2[dados.agua.Length];
                        for (int j = 0; j < pontos.Length; j++) pontos[j] = new Vector2(dados.agua[j].x, dados.agua[j].y);
                        col.SetPath(0, pontos);
                    }
                }
                var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
                camGo.transform.position = new Vector3(0f, 0f, -10f);
                Camera camera = camGo.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 9f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.045f, .07f, .085f);
                camGo.AddComponent<AudioListener>();
                GameObject jogador = CriarJogador(folha, mat);
                MTEXDemo demo = jogador.GetComponent<MTEXDemo>();
                demo.cameraDoMapa = camera;
                var ambiente = raiz.AddComponent<MTEXAmbiente>();
                ambiente.luzes = iluminacao;
                ambiente.mulherDoRelato = mulher; ambiente.centauraDaReferencia = centaura;
                var tingir = new List<SpriteRenderer>();
                foreach (GameObject root in cena.GetRootGameObjects())
                    foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                        if (!sr.transform.IsChildOf(iluminacao.transform)) tingir.Add(sr);
                ambiente.tingir = tingir.ToArray();
                demo.ambiente = ambiente;
                iluminacao.SetActive(false);
                if (mulher != null) mulher.SetActive(false);
                if (centaura != null) centaura.SetActive(false);
                string destino = AssetDatabase.GenerateUniqueAssetPath(Gerados + "/Cenas/Jaguaruana_Expansao_Visual.unity");
                if (!EditorSceneManager.SaveScene(cena, destino)) throw new IOException("Não foi possível salvar a demonstração.");
                Selection.activeGameObject = raiz;
                Debug.Log("Expansão criada em " + destino + ". Game 16:9 > Play. WASD/setas, 1/2/3 áreas, N noite, V variante, H oculta interface. A Missão 01 não foi alterada.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static Folha LerJogador()
        {
            const string path = "Assets/MemoriasTecidas/PracaV1/Data/praca_manifesto.json";
            if (!File.Exists(path)) throw new FileNotFoundException("A cena jogável usa o explorador do pacote Praça V1. Importe esse pacote primeiro. Os prefabs da expansão podem ser preparados sem ele.");
            Praca p = JsonUtility.FromJson<Praca>(File.ReadAllText(path));
            Folha f = p != null && p.assets != null ? Array.Find(p.assets, x => x.id == "jogador_folha") : null;
            if (f == null || f.frames == null || f.frames.Length != 12 || f.width <= 0 || f.height <= 0)
                throw new InvalidDataException("Manifesto da Praça V1 sem os 12 quadros do jogador.");
            if (!File.Exists("Assets/MemoriasTecidas/PracaV1/" + f.file)) throw new FileNotFoundException("Imagem do explorador ausente.");
            foreach (Quadro q in f.frames)
                if (q == null || q.rect == null || q.pivot == null || q.rect.width <= 0f || q.rect.height <= 0f
                    || q.rect.x < 0f || q.rect.y < 0f || q.rect.x + q.rect.width > f.width || q.rect.y + q.rect.height > f.height
                    || q.pixelsPorUnidade <= 0f)
                    throw new InvalidDataException("Recorte inválido no manifesto do explorador.");
            return f;
        }

        private static GameObject CriarJogador(Folha f, Material mat)
        {
            string path = "Assets/MemoriasTecidas/PracaV1/" + f.file;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) throw new FileNotFoundException("Textura do explorador indisponível.");
            var go = new GameObject("Explorador_Demonstracao"); go.transform.position = new Vector3(0f, -5.5f, 0f);
            go.AddComponent<SpriteRenderer>().sharedMaterial = mat;
            var corpo = go.AddComponent<Rigidbody2D>(); corpo.gravityScale = 0f;
            corpo.constraints = RigidbodyConstraints2D.FreezeRotation; corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<BoxCollider2D>(); col.size = new Vector2(.42f, .25f); col.offset = new Vector2(0f, .12f);
            string matPath = Gerados + "/Atrito_Zero.physicsMaterial2D";
            PhysicsMaterial2D atrito = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
            if (atrito == null)
            {
                atrito = new PhysicsMaterial2D("Atrito_Zero") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(atrito, matPath);
            }
            col.sharedMaterial = atrito;
            go.AddComponent<MTEXOrdemY>(); go.AddComponent<MTEXDemo>();
            var anim = go.AddComponent<MTEXAnimacaoJogador>(); anim.folha = tex;
            anim.tamanhoReferencia = new Vector2Int(f.width, f.height); anim.pixelsPorUnidade = f.ppu;
            anim.quadros = new MTEXAnimacaoJogador.Quadro[12];
            for (int i = 0; i < 12; i++)
            {
                Quadro q = f.frames[i];
                anim.quadros[i] = new MTEXAnimacaoJogador.Quadro
                {
                    rect = new Rect(q.rect.x, q.rect.y, q.rect.width, q.rect.height),
                    pivot = new Vector2(q.pivot.x, q.pivot.y), pixelsPorUnidade = q.pixelsPorUnidade
                };
            }
            anim.Recarregar();
            return go;
        }

        private static void Parede(Transform parent, string nome, Vector2 centro, Vector2 tamanho)
        {
            var obj = new GameObject(nome); obj.transform.SetParent(parent, false); obj.transform.localPosition = centro;
            obj.AddComponent<BoxCollider2D>().size = tamanho;
        }

        private static Sprite CriarMascaraDeLuz()
        {
            // Máscara geométrica de halo, não é uma textura de cenário nem altera os PNGs de arte.
            string path = Gerados + "/Halo_Suave.png";
            if (!File.Exists(path))
            {
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var cores = new Color[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), 2f);
                    cores[y * n + x] = new Color(1f, 1f, 1f, alpha);
                }
                tex.SetPixels(cores); tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex); AssetDatabase.ImportAsset(path);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 128f; imp.alphaIsTransparency = true; imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed; imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Halo(Transform pai, Vector3 lugar, Sprite sprite, Material material)
        {
            var go = new GameObject("Luz_Quente_Chao"); go.transform.SetParent(pai, false); go.transform.position = lugar;
            go.transform.localScale = new Vector3(6.8f, 4.4f, 1f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = material;
            sr.sortingOrder = -29000; sr.color = new Color(1f, .69f, .30f, .60f);
        }
    }
}

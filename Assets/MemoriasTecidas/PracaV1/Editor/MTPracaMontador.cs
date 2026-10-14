using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoriasTecidas.PracaV1.Editor
{
    public static class MTPracaMontador
    {
        private const string Raiz = "Assets/MemoriasTecidas/PracaV1";
        private const string Gerados = Raiz + "/Gerados";

        [Serializable] public sealed class Manifesto
        {
            public string version;
            public Arte[] assets;
            public Instancia[] instances;
        }
        [Serializable] public sealed class Arte
        {
            public string id, file;
            public int width, height;
            public float ppu, pivotX, pivotY;
            public bool ground, sheet;
            public float colliderWidth, colliderHeight, colliderOffsetY;
            public float shadowWidth, shadowHeight;
            public QuadroJson[] frames;
        }
        // O JSON usa campos x/y/width/height, não os campos internos de UnityEngine.Rect.
        [Serializable] public sealed class RetanguloJson { public float x, y, width, height; }
        [Serializable] public sealed class PontoJson { public float x, y; }
        [Serializable] public sealed class QuadroJson
        {
            public RetanguloJson rect;
            public PontoJson pivot;
            public float pixelsPorUnidade;
        }
        [Serializable] public sealed class Instancia
        {
            public string name, asset;
            public float x, y;
        }

        [MenuItem("Memorias Tecidas/Praca V1/1 - Preparar arte e prefabs")]
        public static void PrepararMenu()
        {
            try
            {
                Preparar(Ler());
                Debug.Log("Praça V1: prefabs e materiais preparados em " + Gerados);
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        [MenuItem("Memorias Tecidas/Praca V1/2 - Criar cena de teste")]
        public static void CriarCena()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de montar a praça."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Manifesto dados = Ler();
                Preparar(dados);
                var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var raiz = new GameObject("Praca_Visual_V1");
                var mapa = new GameObject("Cenario"); mapa.transform.SetParent(raiz.transform, false);
                var pessoas = new GameObject("Personagens"); pessoas.transform.SetParent(raiz.transform, false);
                var limites = new GameObject("Limites"); limites.transform.SetParent(raiz.transform, false);
                GameObject jogador = null, neto = null;
                foreach (Instancia item in dados.instances)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Gerados + "/Prefabs/" + item.asset + ".prefab");
                    if (prefab == null) throw new InvalidOperationException("Prefab ausente: " + item.asset);
                    var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cena);
                    obj.name = item.name;
                    obj.transform.SetParent(item.asset == "neto" || item.asset == "jogador_folha"
                        ? pessoas.transform : mapa.transform, false);
                    obj.transform.position = new Vector3(item.x, item.y, 0f);
                    if (item.asset == "jogador_folha") jogador = obj;
                    if (item.asset == "neto") neto = obj;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
                }
                if (jogador == null || neto == null) throw new InvalidOperationException("O manifesto precisa de jogador e Neto.");
                Parede(limites.transform, "Norte", new Vector2(0f, 11f), new Vector2(34f, 1f));
                Parede(limites.transform, "Sul", new Vector2(0f, -7f), new Vector2(34f, 1f));
                Parede(limites.transform, "Oeste", new Vector2(-16f, 2f), new Vector2(1f, 18f));
                Parede(limites.transform, "Leste", new Vector2(16f, 2f), new Vector2(1f, 18f));
                var cameraGO = new GameObject("Main Camera");
                cameraGO.tag = "MainCamera";
                cameraGO.transform.position = new Vector3(0f, 2f, -10f);
                Camera camera = cameraGO.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 9f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .14f, .13f);
                cameraGO.AddComponent<AudioListener>();
                cameraGO.AddComponent<MTPracaCamera>();
                var ui = new GameObject("Tutorial_Da_Praca");
                MTPracaDemo demo = ui.AddComponent<MTPracaDemo>();
                demo.jogador = jogador.transform; demo.neto = neto.transform;
                MTPracaJogador controle = jogador.GetComponent<MTPracaJogador>();
                controle.demonstracao = demo; controle.inicio = jogador.transform.position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(controle);
                Directory.CreateDirectory(Gerados + "/Cenas"); AssetDatabase.Refresh();
                string destino = AssetDatabase.GenerateUniqueAssetPath(Gerados + "/Cenas/Praca_Visual_V1.unity");
                if (!EditorSceneManager.SaveScene(cena, destino)) throw new IOException("Não foi possível salvar a cena.");
                Selection.activeGameObject = raiz;
                Debug.Log("Praça criada e salva em " + destino + ". Abra Game em 16:9 e pressione Play.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        internal static Manifesto Ler()
        {
            string arquivo = Raiz + "/Data/praca_manifesto.json";
            if (!File.Exists(arquivo)) throw new FileNotFoundException("Mantenha a pasta do pacote em " + Raiz, arquivo);
            Manifesto dados = JsonUtility.FromJson<Manifesto>(File.ReadAllText(arquivo));
            if (dados == null || dados.assets == null || dados.instances == null)
                throw new InvalidDataException("Manifesto da praça incompleto.");
            return dados;
        }

        internal static MTPracaAnimacao.Quadro[] ConverterQuadros(Arte a)
        {
            if (a.frames == null || a.frames.Length != 12 || a.width <= 0 || a.height <= 0)
                throw new InvalidDataException("O jogador precisa de 12 quadros e dimensões no manifesto.");
            var resultado = new MTPracaAnimacao.Quadro[12];
            for (int i = 0; i < resultado.Length; i++)
            {
                QuadroJson q = a.frames[i];
                if (q == null || q.rect == null || q.pivot == null)
                    throw new InvalidDataException("Quadro incompleto: " + (i + 1));
                Rect r = new Rect(q.rect.x, q.rect.y, q.rect.width, q.rect.height);
                if (r.width <= 0 || r.height <= 0 || r.xMin < 0 || r.yMin < 0 || r.xMax > a.width || r.yMax > a.height)
                    throw new InvalidDataException("Recorte fora da referência no manifesto: " + (i + 1) + " " + r);
                resultado[i] = new MTPracaAnimacao.Quadro {
                    rect = r, pivot = new Vector2(q.pivot.x, q.pivot.y), pixelsPorUnidade = q.pixelsPorUnidade
                };
            }
            return resultado;
        }

        private static void Preparar(Manifesto dados)
        {
            Directory.CreateDirectory(Gerados + "/Prefabs");
            Directory.CreateDirectory(Gerados + "/Materiais");
            AssetDatabase.Refresh();
            Material material = MaterialPrincipal();
            PhysicsMaterial2D atrito = AtritoZero();
            Sprite sombra = Sombra();
            foreach (Arte a in dados.assets)
            {
                string caminho = Raiz + "/" + a.file;
                var importer = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (importer == null) throw new FileNotFoundException("PNG não encontrado: " + caminho);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = a.ppu;
                importer.alphaIsTransparency = !a.ground;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.npotScale = TextureImporterNPOTScale.None;
                var ajustes = new TextureImporterSettings();
                importer.ReadTextureSettings(ajustes);
                ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
                ajustes.spritePivot = new Vector2(a.pivotX, a.pivotY);
                ajustes.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(ajustes); importer.SaveAndReimport();
                string caminhoPrefab = Gerados + "/Prefabs/" + a.id + ".prefab";
                // Um segundo preparo não apaga ajustes feitos pelo usuário no prefab.
                if (AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab) != null) continue;
                var previa = EditorSceneManager.NewPreviewScene();
                GameObject obj = null;
                try
                {
                    obj = new GameObject(a.id);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, previa);
                    SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
                    renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                    if (a.ground)
                    {
                        renderer.sortingOrder = -30000;
                        Vector2 tamanho = renderer.sprite.bounds.size;
                        obj.transform.localScale = new Vector3(32f / tamanho.x, 18f / tamanho.y, 1f);
                    }
                    else
                    {
                        obj.AddComponent<MTPracaOrdemY>();
                        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
                        collider.size = new Vector2(a.colliderWidth, a.colliderHeight);
                        collider.offset = new Vector2(0f, a.colliderOffsetY);
                        collider.sharedMaterial = atrito;
                        if (a.shadowWidth > 0f)
                        {
                            var go = new GameObject("Sombra_Contato"); go.transform.SetParent(obj.transform, false);
                            go.transform.localPosition = new Vector3(.10f, -.035f, 0f);
                            go.transform.localScale = new Vector3(a.shadowWidth, a.shadowHeight, 1f);
                            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sombra;
                            sr.sharedMaterial = material; sr.sortingOrder = -29000;
                            sr.color = new Color(.20f, .18f, .13f, .22f);
                        }
                    }
                    if (a.sheet)
                    {
                        renderer.sprite = null;
                        var corpo = obj.AddComponent<Rigidbody2D>();
                        corpo.bodyType = RigidbodyType2D.Dynamic;
                        corpo.gravityScale = 0f;
                        corpo.constraints = RigidbodyConstraints2D.FreezeRotation;
                        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
                        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                        corpo.linearDamping = 0f;
                        obj.AddComponent<MTPracaJogador>();
                        var animacao = obj.AddComponent<MTPracaAnimacao>();
                        animacao.folha = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
                        animacao.quadros = ConverterQuadros(a); animacao.pixelsPorUnidade = a.ppu;
                        animacao.tamanhoReferencia = new Vector2Int(a.width, a.height);
                    }
                    PrefabUtility.SaveAsPrefabAsset(obj, caminhoPrefab);
                }
                finally
                {
                    if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
                    EditorSceneManager.ClosePreviewScene(previa);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static Material MaterialPrincipal()
        {
            string caminho = Gerados + "/Materiais/Praca_Unlit.mat";
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (existente != null) return existente;
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") : Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Shader de sprite indisponível. Use projeto 2D Built-in ou URP 2D.");
            Material material = new Material(shader) { name = "Praca_Unlit" };
            AssetDatabase.CreateAsset(material, caminho); return material;
        }
        private static PhysicsMaterial2D AtritoZero()
        {
            string caminho = Gerados + "/Materiais/Praca_SemAtrito.physicsMaterial2D";
            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(caminho);
            if (material != null) return material;
            material = new PhysicsMaterial2D("Praca_SemAtrito") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, caminho); return material;
        }
        private static Sprite Sombra()
        {
            // Máscara geométrica de contato criada no Editor; não altera as imagens de arte.
            string arquivo = Gerados + "/Sombra_Contato.png";
            if (!File.Exists(arquivo))
            {
                const int n = 64;
                Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f - n * .5f) / (n * .5f);
                    float dy = (y + .5f - n * .5f) / (n * .5f);
                    float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - distancia) * 2.5f));
                }
                t.SetPixels(pixels); t.Apply(); File.WriteAllBytes(arquivo, t.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(t); AssetDatabase.ImportAsset(arquivo);
                TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(arquivo);
                imp.textureType = TextureImporterType.Sprite; imp.spritePixelsPerUnit = n;
                imp.alphaIsTransparency = true; imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.filterMode = FilterMode.Bilinear; imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(arquivo);
        }
        private static void Parede(Transform pai, string nome, Vector2 posicao, Vector2 tamanho)
        {
            var go = new GameObject(nome); go.transform.SetParent(pai, false);
            go.transform.position = posicao;
            go.AddComponent<BoxCollider2D>().size = tamanho;
        }
    }
}

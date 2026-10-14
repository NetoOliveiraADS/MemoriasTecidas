#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Independent of MTPlayer, inventory, dialogues and quests.
public static class MTPrepararArteReferencias
{
    private const string Root = "Assets/_Project/Art/MTReferencias";
    private const string Prefabs = Root + "/Prefabs";
    private const string ExamplePath = Prefabs + "/MT_Exemplo_Centro_Serra.prefab";

    [Serializable]
    private sealed class Manifesto { public string version; public Item[] assets; }

    [Serializable]
    private sealed class Item
    {
        public string id;
        public string label;
        public string relativePath;
        public bool ground;
        public bool flat;
        public float ppu;
        public float pivotX;
        public float pivotY;
        public float colliderWidth;
        public float colliderHeight;
        public float colliderOffsetX;
        public float colliderOffsetY;
    }

    [MenuItem("Memorias Tecidas/Arte/1 - Preparar sprites e prefabs")]
    public static void Preparar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Saia do modo Play antes de preparar a arte.");
            return;
        }

        try
        {
            Manifesto manifesto = LerManifesto();
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();

            // Validate every file before changing import settings.
            foreach (Item item in manifesto.assets)
            {
                string path = Root + "/" + item.relativePath;
                if (!File.Exists(path) || item.ppu <= 0f)
                    throw new InvalidOperationException("Arquivo ou escala invalida: " + path);
            }

            for (int i = 0; i < manifesto.assets.Length; i++)
            {
                Item item = manifesto.assets[i];
                EditorUtility.DisplayProgressBar("Memorias Tecidas", "Preparando " + item.label,
                    (float)i / manifesto.assets.Length);
                ConfigurarTextura(item);
            }

            Material material = ObterMaterial();
            Scene preview = EditorSceneManager.NewPreviewScene();
            int criados = 0;
            try
            {
                foreach (Item item in manifesto.assets)
                {
                    string path = Prefabs + "/" + item.id + ".prefab";
                    if (File.Exists(path)) continue; // Preserve edited prefabs.
                    GameObject obj = new GameObject(item.id);
                    SceneManager.MoveGameObjectToScene(obj, preview);
                    try
                    {
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + item.relativePath);
                        if (sprite == null) throw new InvalidOperationException("Sprite nao importado: " + item.id);
                        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
                        renderer.sprite = sprite;
                        renderer.sharedMaterial = material;
                        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                        if (item.ground)
                        {
                            renderer.sortingOrder = -30000;
                            renderer.drawMode = SpriteDrawMode.Tiled;
                            renderer.tileMode = SpriteTileMode.Continuous;
                            renderer.size = Vector2.one;
                        }
                        else if (item.flat)
                        {
                            renderer.sortingOrder = -29000;
                        }
                        else
                        {
                            obj.AddComponent<MTOrdemPorY>();
                            if (item.colliderWidth > 0f && item.colliderHeight > 0f)
                            {
                                BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
                                collider.size = new Vector2(item.colliderWidth, item.colliderHeight);
                                collider.offset = new Vector2(item.colliderOffsetX, item.colliderOffsetY);
                            }
                        }
                        if (PrefabUtility.SaveAsPrefabAsset(obj, path) == null)
                            throw new InvalidOperationException("Falha ao criar prefab: " + path);
                        criados++;
                    }
                    finally { UnityEngine.Object.DestroyImmediate(obj); }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Prefabs);
            Debug.Log("Arte preparada: " + manifesto.assets.Length + " sprites; " + criados
                + " prefabs novos. Prefabs existentes foram preservados. Pasta: " + Prefabs);
        }
        catch (Exception exception) { Debug.LogException(exception); }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private static Manifesto LerManifesto()
    {
        string path = Root + "/mt_assets.json";
        if (!File.Exists(path))
            throw new FileNotFoundException("Copie a pasta Assets do pacote para a raiz do seu projeto. Falta: " + path);
        Manifesto manifesto = JsonUtility.FromJson<Manifesto>(File.ReadAllText(path));
        if (manifesto == null || manifesto.assets == null || manifesto.assets.Length == 0)
            throw new InvalidOperationException("Manifesto de arte vazio ou invalido.");
        return manifesto;
    }

    private static void ConfigurarTextura(Item item)
    {
        string path = Root + "/" + item.relativePath;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("TextureImporter ausente: " + path);

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(item.pivotX, item.pivotY);
        settings.spritePixelsPerUnit = item.ppu;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);

        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = !item.ground;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = item.ground ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static Material ObterMaterial()
    {
        string path = Root + "/Materials/MT_SpriteUnlit.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) throw new InvalidOperationException("Shader de sprite nao encontrado no projeto.");
        material = new Material(shader) { name = "MT_SpriteUnlit" };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    [MenuItem("Memorias Tecidas/Arte/2 - Criar exemplo de centro e serra")]
    public static void CriarExemplo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Saia do modo Play antes de criar o exemplo.");
            return;
        }
        if (File.Exists(ExamplePath))
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ExamplePath);
            Debug.Log("O exemplo existente foi preservado: " + ExamplePath);
            return;
        }
        try
        {
            Manifesto manifesto = LerManifesto();
            foreach (Item item in manifesto.assets)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + item.id + ".prefab") == null)
                    throw new InvalidOperationException("Execute primeiro: Arte > 1 - Preparar sprites e prefabs.");

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = new GameObject("MT_Exemplo_Centro_Serra");
                SceneManager.MoveGameObjectToScene(root, preview);
                GameObject centro = new GameObject("Centro_Exemplo");
                SceneManager.MoveGameObjectToScene(centro, preview);
                centro.transform.SetParent(root.transform, false);
                GameObject serra = new GameObject("Serra_Exemplo");
                SceneManager.MoveGameObjectToScene(serra, preview);
                serra.transform.SetParent(root.transform, false);
                serra.transform.localPosition = new Vector3(16f, 0f, 0f);

                Piso("asfalto", centro.transform, 0, 1, 12, 18, -30000, preview);
                Piso("piso_claro", centro.transform, 0, 1, 9, 17, -29990, preview);
                Piso("piso_rosa", centro.transform, 0, 1, 8.4f, 16.4f, -29980, preview);
                Piso("piso_claro", centro.transform, 0, -3, 1.4f, 8, -29970, preview);
                Piso("grama", centro.transform, -3, -1, 1.8f, 2, -29960, preview);
                Piso("grama", centro.transform, 3, -1, 1.8f, 2, -29960, preview);
                Peca("igreja", centro.transform, 0, 3, preview);
                Peca("casario", centro.transform, -5.5f, 3.5f, preview);
                Peca("casario", centro.transform, 5.5f, 3.5f, preview);
                Peca("monumento", centro.transform, 0, -1, preview);
                Peca("banco", centro.transform, -2, -3, preview);
                Peca("banco", centro.transform, 2, -3, preview);
                Peca("quiosque", centro.transform, -3, -6, preview);
                Peca("poste", centro.transform, 3.5f, -5, preview);
                Peca("arvore_podada", centro.transform, -3, -1.7f, preview);
                Peca("arvore_podada", centro.transform, 3, -1.7f, preview);
                Peca("canteiro", centro.transform, 3, -1.7f, preview);

                Piso("terra", serra.transform, 0, 1, 12, 18, -30000, preview);
                Peca("serra", serra.transform, 0, 2.5f, preview);
                Peca("cruzeiro", serra.transform, 0, 3.7f, preview);
                Peca("palmeira", serra.transform, 3.5f, -3, preview);
                Peca("cactos", serra.transform, -2.5f, -2, preview);
                Peca("arbusto", serra.transform, 2.5f, 0, preview);
                Peca("arbusto", serra.transform, -3.5f, 1.5f, preview);
                Peca("arvore_seca", serra.transform, 4, 2, preview);
                Peca("rochas", serra.transform, -3.5f, -4, preview);
                Peca("rochas", serra.transform, 1, -4.5f, preview);
                Piso("terra", root.transform, 8, -3, 4, 2, -30000, preview);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ExamplePath);
                if (saved == null) throw new InvalidOperationException("Falha ao salvar o exemplo.");
                AssetDatabase.SaveAssets();
                Selection.activeObject = saved;
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Debug.Log("Exemplo criado. Arraste MT_Exemplo_Centro_Serra para uma copia da sua cena. Ele nao inclui Player, camera, missoes ou limites do mapa.");
        }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    private static GameObject Peca(string id, Transform parent, float x, float y, Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + id + ".prefab");
        if (prefab == null) throw new InvalidOperationException("Prefab ausente: " + id);
        GameObject obj = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (obj == null) throw new InvalidOperationException("Falha ao instanciar: " + id);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(x, y, 0);
        obj.transform.localScale = Vector3.one;
        return obj;
    }

    private static void Piso(string id, Transform parent, float x, float y, float w, float h, int order, Scene scene)
    {
        GameObject obj = Peca(id, parent, x, y, scene);
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        renderer.size = new Vector2(w, h);
        renderer.sortingOrder = order;
    }
}
#endif

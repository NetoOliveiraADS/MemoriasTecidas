using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMUrbanaArtesV24
    {
        public const string Raiz = "Assets/MemoriasTecidas/MundoV2/ExpansaoUrbanaV24";
        public static MTMPacoteArtesV23 Artes() => JsonUtility.FromJson<MTMPacoteArtesV23>(File.ReadAllText(Raiz + "/Data/artes.json"));
        public static MTMUrbanaPacoteV24 Locais() => JsonUtility.FromJson<MTMUrbanaPacoteV24>(File.ReadAllText(Raiz + "/Data/locais.json"));
        public static Material Tinta => AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Vida + "/Gerados/Pintura_V22.mat");

        [MenuItem("Memorias Tecidas/Expansao Urbana V2.4/1 - Preparar artes e prefabs")]
        public static void MenuPreparar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Saia do Play."); return; }
            try { Preparar(); Debug.Log("Artes V2.4 prontas. Execute o menu 2 para montar os quatro lugares na cena aberta."); }
            catch (Exception e) { Debug.LogException(e); }
            finally { EditorUtility.ClearProgressBar(); }
        }
        public static GameObject Prefab(string id)
        {
            var novo = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Gerados/Prefabs/" + id + ".prefab");
            return novo != null ? novo : MTMArtesLugaresV23.Prefab(id);
        }
        public static void Preparar()
        {
            var pacote = Locais(); var artes = Artes();
            if ((pacote.versao < 240 || pacote.versao > 243) || artes.artes == null) throw new InvalidDataException("Dados V2.4 incompletos.");
            var novos = artes.artes.Select(a => a.id).ToHashSet();
            var usados = pacote.locais.SelectMany(l => l.objetos).Select(o => o.id).Distinct().ToArray();
            if (Tinta == null || usados.Any(id => !novos.Contains(id) && MTMArtesLugaresV23.Prefab(id) == null))
                MTMArtesLugaresV23.Preparar();
            foreach (string id in usados)
                if (!novos.Contains(id) && MTMArtesLugaresV23.Prefab(id) == null) throw new FileNotFoundException("Prefab V2.3 ausente: " + id);
            Directory.CreateDirectory(Raiz + "/Gerados/Sprites"); Directory.CreateDirectory(Raiz + "/Gerados/Prefabs"); AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(Raiz + "/Arte", "*.png"))
            {
                var t = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
                if (t == null) throw new IOException("Imagem não importada: " + path);
                t.textureType = TextureImporterType.Default; t.alphaSource = TextureImporterAlphaSource.FromInput;
                t.alphaIsTransparency = true; t.sRGBTexture = true; t.npotScale = TextureImporterNPOTScale.None;
                t.maxTextureSize = 4096; t.textureCompression = TextureImporterCompression.Uncompressed;
                t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp; t.mipmapEnabled = false; t.SaveAndReimport();
            }
            var sombra = AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Vida + "/Gerados/Sombra_V22.mat");
            Scene previa = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var a in artes.artes)
                {
                    string caminhoSprite = Raiz + "/Gerados/Sprites/" + a.id + ".asset";
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminhoSprite);
                    if (sprite == null)
                    {
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz + "/Arte/" + a.arquivo);
                        if (tex == null || a.x < 0 || a.y < 0 || a.largura <= 0 || a.altura <= 0 || a.x + a.largura > tex.width || a.y + a.altura > tex.height || a.alturaMundo <= 0)
                            throw new InvalidDataException("Recorte inválido: " + a.id);
                        sprite = Sprite.Create(tex, new Rect(a.x, a.y, a.largura, a.altura), new Vector2(a.pivotX, a.pivotY), a.altura / a.alturaMundo, 0, SpriteMeshType.FullRect);
                        sprite.name = a.id; AssetDatabase.CreateAsset(sprite, caminhoSprite);
                    }
                    string caminhoPrefab = Raiz + "/Gerados/Prefabs/" + a.id + ".prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab) != null) continue;
                    var go = new GameObject(a.id); SceneManager.MoveGameObjectToScene(go, previa);
                    try
                    {
                        var d = Filho(go, "Desenho").AddComponent<SpriteRenderer>(); d.sprite = sprite; d.sharedMaterial = Tinta; d.spriteSortPoint = SpriteSortPoint.Pivot;
                        d.gameObject.AddComponent<MTMOrdemY>();
                        var marca = go.AddComponent<MTMElementoV23>(); marca.identificador = a.id;
                        if (a.colW > 0 && a.colH > 0) { var c = go.AddComponent<BoxCollider2D>(); c.size = new Vector2(a.colW, a.colH); c.offset = new Vector2(0, a.colY); }
                        // Os vãos das coberturas ficam livres; apenas pilares, bombas e loja têm colisão.
                        float w = sprite.bounds.size.x, h = sprite.bounds.size.y;
                        if (a.id == "urb_cobertura")
                        {
                            foreach (float x in new[] { -.46f, .46f }) Caixa(go, w * x, .10f, .24f, .24f);
                            foreach (float x in new[] { -.37f, .37f }) Caixa(go, w * x, h * .21f, .24f, .24f);
                        }
                        if (a.id == "urb_cobertura_praca_azul")
                        {
                            // Quatro pilares; o centro do abrigo permanece atravessável.
                            foreach (float x in new[] { -.448f, .448f }) Caixa(go, w * x, .06f, .22f, .22f);
                            foreach (float x in new[] { -.344f, .344f }) Caixa(go, w * x, h * .134f, .22f, .22f);
                        }
                        if (a.id == "urb_posto")
                        {
                            Caixa(go, w * -.30f, .4f, w * .16f, .8f);
                            Caixa(go, w * -.025f, .4f, w * .16f, .8f);
                            Caixa(go, w * .28f, h * .23f, w * .40f, 1.9f);
                        }
                        var s = Filho(go, "Sombra").AddComponent<SpriteRenderer>(); s.sprite = sprite; s.sharedMaterial = sombra;
                        s.sortingOrder = -22000; s.color = new Color(.1f, .13f, .16f, .18f); s.transform.localScale = new Vector3(1.02f, .2f, 1);
                        var v = go.AddComponent<MTMVisualV2>(); v.desenho = d; v.sombra = s; v.vento = 0; v.respirar = false;
                        if (a.alturaMundo > 3) { var o = go.AddComponent<MTMOclusaoV2>(); o.desenho = d; o.alturaDoJogador = 1.75f; }
                        if (a.tipo == "construcao")
                        {
                            var e = go.AddComponent<MTMEscalaConstrucaoV221>(); e.desenho = d; e.sombra = s;
                            e.largura = e.altura = e.profundidadeDaBase = 1; e.Inicializar();
                        }
                        if (a.tipo == "poste")
                        { var l = go.AddComponent<MTMLuzLocalV23>(); l.raio = 6; l.intensidade = .72f; }
                        if (a.id == MTMReparoMonumentoV242.Novo)
                        {
                            MTMReparoMonumentoV242.Configurar(go, d, s);
                            go.GetComponent<MTMMonumentoFixoV242>().PrepararParaSalvar();
                        }
                        if (a.id == MTMMonumentoEntradaV243.Novo)
                        {
                            MTMMonumentoEntradaV243.Configurar(go, d, s);
                            go.GetComponent<MTMMonumentoFixoV242>().PrepararParaSalvar();
                        }
                        PrefabUtility.SaveAsPrefabAsset(go, caminhoPrefab);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(go); }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(previa); }
            AssetDatabase.SaveAssets();
        }
        private static GameObject Filho(GameObject pai, string nome) { var go = new GameObject(nome); go.transform.SetParent(pai.transform, false); return go; }
        private static void Caixa(GameObject go, float x, float y, float w, float h) { var c = go.AddComponent<BoxCollider2D>(); c.offset = new Vector2(x, y); c.size = new Vector2(w, h); }
    }
}

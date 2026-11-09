#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.FerramentasEditor
{
    /// <summary>Ferramenta para inserir um baú de demonstração sem editar YAML da cena à mão.</summary>
    public static class MTMCriarBauTeste
    {
        private const string Pasta = "Assets/MemoriasTecidas/Prefabs/Segredos";
        private const string SpritePath = Pasta + "/bau_teste.png";
        private const string PrefabPath = Pasta + "/PF_BauTeste.prefab";

        [MenuItem("Memorias Tecidas/Exploracao/Criar bau de teste na Praca")]
        public static void Criar()
        {
            var regioes = Object.FindObjectsByType<MTMRegiao>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            MTMRegiao regiao = null;
            foreach (var r in regioes)
                if (r != null && r.dados != null && r.dados.id == "praca")
                {
                    regiao = r;
                    break;
                }
            if (regiao == null)
            {
                EditorUtility.DisplayDialog("Memórias Tecidas",
                    "Abra a cena Jaguaruana antes de usar este menu. A região 'praca' não foi encontrada.", "OK");
                return;
            }
            foreach (var bauExistente in regiao.GetComponentsInChildren<MTMBau>(true))
                if (bauExistente.idTesouro == "praca_01")
                {
                    Selection.activeGameObject = bauExistente.gameObject;
                    EditorUtility.DisplayDialog("Memórias Tecidas",
                        "O baú praca_01 já existe nesta região.", "OK");
                    return;
                }

            if (!AssetDatabase.IsValidFolder("Assets/MemoriasTecidas/Prefabs/Segredos"))
                AssetDatabase.CreateFolder("Assets/MemoriasTecidas/Prefabs", "Segredos");
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) == null)
                CriarSprite();

            Sprite arte = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (arte == null)
            {
                Debug.LogError("Não foi possível importar o sprite do baú.");
                return;
            }

            var objeto = new GameObject("Bau_Exploracao_Praca_01");
            var render = objeto.AddComponent<SpriteRenderer>();
            render.sprite = arte;
            objeto.AddComponent<MTMOrdemY>();
            var collider = objeto.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(.85f, .65f);
            collider.offset = new Vector2(0, -.12f);
            var ponto = objeto.AddComponent<MTMPonto>();
            ponto.dados = new MTMPontoDados
            {
                id = "exploracao_bau_praca_01",
                nome = "Baú esquecido",
                tipo = "tesouro",
                evento = "exploracao_bau_praca_01"
            };
            ponto.alcance = 1.45f;
            var bau = objeto.AddComponent<MTMBau>();
            bau.idTesouro = "praca_01";
            bau.idRecompensa = "cera";
            bau.quantidade = 1;
            bau.aparencia = render;
            bau.corFechado = Color.white;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(objeto, PrefabPath);
            Object.DestroyImmediate(objeto);
            if (prefab == null) return;

            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, regiao.transform);
            Undo.RegisterCreatedObjectUndo(instancia, "Criar baú na praça");
            Vector3 posicao = new Vector3(regiao.dados.seguroX + 2f, regiao.dados.seguroY, 0);
            instancia.transform.position = posicao;
            var pontoNovo = instancia.GetComponent<MTMPonto>();
            Undo.RecordObject(regiao, "Registrar ponto de baú");
            var anteriores = regiao.pontos ?? new MTMPonto[0];
            var atualizados = new MTMPonto[anteriores.Length + 1];
            System.Array.Copy(anteriores, atualizados, anteriores.Length);
            atualizados[anteriores.Length] = pontoNovo;
            regiao.pontos = atualizados;
            EditorUtility.SetDirty(regiao);
            EditorSceneManager.MarkSceneDirty(regiao.gameObject.scene);
            Selection.activeGameObject = instancia;
            EditorGUIUtility.PingObject(instancia);
            Debug.Log("Baú de teste criado na praça. Confira a posição na Scene, salve a cena e pressione Play.");
        }

        private static void CriarSprite()
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color[n * n];
            Color vazio = new Color(0, 0, 0, 0);
            Color escuro = new Color(.18f, .12f, .1f);
            Color madeira = new Color(.5f, .27f, .13f);
            Color claro = new Color(.73f, .41f, .18f);
            Color ouro = new Color(.9f, .71f, .29f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Color c = vazio;
                if (x >= 3 && x <= 28 && y >= 4 && y <= 24)
                {
                    c = escuro;
                    if (x >= 5 && x <= 26 && y >= 6 && y <= 22)
                        c = y >= 15 ? claro : madeira;
                    if ((x >= 8 && x <= 10 || x >= 21 && x <= 23) && y >= 6 && y <= 23)
                        c = ouro;
                    if (y >= 14 && y <= 16 && x >= 4 && x <= 27) c = ouro;
                    if (x >= 14 && x <= 17 && y >= 12 && y <= 18) c = ouro;
                    if (x >= 15 && x <= 16 && y >= 13 && y <= 15) c = escuro;
                }
                pixels[y * n + x] = c;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
#endif

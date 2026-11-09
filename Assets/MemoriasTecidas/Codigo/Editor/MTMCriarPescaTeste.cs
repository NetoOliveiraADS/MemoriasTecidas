#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.FerramentasEditor
{
    /// <summary>Ferramenta de teste para inserir um ponto de pesca reutilizável.</summary>
    public static class MTMCriarPescaTeste
    {
        private const string Pasta = "Assets/MemoriasTecidas/Prefabs/Minigames";
        private const string SpritePath = Pasta + "/ponto_pesca.png";
        private const string PrefabPath = Pasta + "/PF_PontoPesca.prefab";

        [MenuItem("Memorias Tecidas/Exploracao/Criar ponto de pesca no Rio Jaguaribe")]
        public static void Criar()
        {
            MTMRegiao rio = null;
            foreach (var regiao in UnityEngine.Object.FindObjectsByType<MTMRegiao>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (regiao != null && regiao.dados != null && regiao.dados.id == "rio")
                {
                    rio = regiao;
                    break;
                }
            }
            if (rio == null)
            {
                EditorUtility.DisplayDialog("Memórias Tecidas",
                    "Abra a cena Jaguaruana.unity. A região 'rio' não foi encontrada.", "OK");
                return;
            }

            foreach (var existente in rio.GetComponentsInChildren<MTMPontoPesca>(true))
            {
                if (existente.idPonto != "rio_01") continue;
                Selection.activeGameObject = existente.gameObject;
                EditorUtility.DisplayDialog("Memórias Tecidas",
                    "O ponto rio_01 já existe; ele foi selecionado.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(Pasta))
                AssetDatabase.CreateFolder("Assets/MemoriasTecidas/Prefabs", "Minigames");
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) == null)
                CriarArte();

            var arte = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (arte == null)
            {
                Debug.LogError("Falha ao criar a arte provisória do ponto de pesca.");
                return;
            }

            var objeto = new GameObject("PontoPesca_Rio_01");
            var visual = objeto.AddComponent<SpriteRenderer>();
            visual.sprite = arte;
            objeto.AddComponent<MTMOrdemY>();
            var ponto = objeto.AddComponent<MTMPonto>();
            ponto.dados = new MTMPontoDados
            {
                id = "exploracao_pesca_rio_01",
                nome = "Pescaria no Jaguaribe",
                tipo = "pesca",
                evento = "exploracao_pesca_rio_01"
            };
            ponto.alcance = 1.65f;
            var pesca = objeto.AddComponent<MTMPontoPesca>();
            pesca.idPonto = "rio_01";
            pesca.brilho = visual;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(objeto, PrefabPath);
            UnityEngine.Object.DestroyImmediate(objeto);
            if (prefab == null) return;

            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rio.transform);
            Undo.RegisterCreatedObjectUndo(instancia, "Criar ponto de pesca no rio");

            // Posição aproximada: ajustar na janela Scene até ficar junto a uma margem acessível.
            instancia.transform.position = new Vector3(
                rio.dados.seguroX + 1.75f, rio.dados.seguroY + .75f, 0f);

            var pontoNovo = instancia.GetComponent<MTMPonto>();
            Undo.RecordObject(rio, "Registrar ponto de pesca");
            var anteriores = rio.pontos ?? Array.Empty<MTMPonto>();
            var atualizados = new MTMPonto[anteriores.Length + 1];
            Array.Copy(anteriores, atualizados, anteriores.Length);
            atualizados[anteriores.Length] = pontoNovo;
            rio.pontos = atualizados;

            EditorUtility.SetDirty(rio);
            EditorSceneManager.MarkSceneDirty(rio.gameObject.scene);
            Selection.activeGameObject = instancia;
            EditorGUIUtility.PingObject(instancia);
            Debug.Log("Ponto de pesca criado no rio. Ajuste sua posição para a margem, salve a cena e teste com E.");
        }

        private static void CriarArte()
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color[n * n];
            var fundo = new Color(0f, 0f, 0f, 0f);
            var agua = new Color(.16f, .46f, .69f);
            var luz = new Color(.69f, .91f, .96f);
            var espuma = new Color(.95f, .96f, .89f);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - 15.5f, dy = y - 15.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color c = fundo;
                if (d < 13f && d > 10f) c = luz;
                else if (d <= 10f) c = agua;
                if (d < 5.2f && d > 3.8f) c = espuma;
                if (d < 3f) c = luz;
                pixels[y * n + x] = c;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
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
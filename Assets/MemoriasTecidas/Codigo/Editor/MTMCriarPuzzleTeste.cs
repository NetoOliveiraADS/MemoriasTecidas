#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.FerramentasEditor
{
    /// <summary>Cria um puzzle isolado na região da praça sem alterar o roteiro.</summary>
    public static class MTMCriarPuzzleTeste
    {
        private const string Pasta = "Assets/MemoriasTecidas/Prefabs/Segredos";
        private const string PrefabPuzzle = Pasta + "/PF_PuzzlePlacas.prefab";
        private const string SpritePlaca = Pasta + "/placa_puzzle.png";
        private const string SpriteSelo = Pasta + "/selo_puzzle.png";
        private const string PrefabBau = Pasta + "/PF_BauTeste.prefab";

        [MenuItem("Memorias Tecidas/Exploracao/Criar puzzle de placas na Praca")]
        public static void Criar()
        {
            var regioes = UnityEngine.Object.FindObjectsByType<MTMRegiao>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            MTMRegiao regiao = null;
            foreach (var item in regioes)
                if (item != null && item.dados != null && item.dados.id == "praca")
                {
                    regiao = item;
                    break;
                }

            if (regiao == null)
            {
                EditorUtility.DisplayDialog("Memórias Tecidas",
                    "Abra Jaguaruana.unity antes de criar o puzzle.", "OK");
                return;
            }

            foreach (var item in regiao.GetComponentsInChildren<MTMPuzzlePlacas>(true))
                if (item.idPuzzle == "praca_placas_01")
                {
                    Selection.activeGameObject = item.gameObject;
                    EditorUtility.DisplayDialog("Memórias Tecidas",
                        "Este puzzle já existe. O objeto foi selecionado.", "OK");
                    return;
                }

            GameObject bauFonte = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBau);
            if (bauFonte == null)
            {
                EditorUtility.DisplayDialog("Memórias Tecidas",
                    "Crie primeiro o baú usando o menu 'Criar bau de teste na Praca' para gerar o prefab base.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(Pasta))
                AssetDatabase.CreateFolder("Assets/MemoriasTecidas/Prefabs", "Segredos");
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SpritePlaca) == null)
                CriarSprite(SpritePlaca, false);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SpriteSelo) == null)
                CriarSprite(SpriteSelo, true);

            Sprite artePlaca = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePlaca);
            Sprite arteSelo = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteSelo);
            if (artePlaca == null || arteSelo == null)
            {
                Debug.LogError("Não foi possível importar as artes provisórias do puzzle.");
                return;
            }

            var raiz = new GameObject("Puzzle_Placas_Praca_01");
            raiz.AddComponent<MTMPuzzlePlacas>();
            var puzzle = raiz.GetComponent<MTMPuzzlePlacas>();
            puzzle.idPuzzle = "praca_placas_01";
            puzzle.idRegiao = "praca";
            puzzle.sequencia = new[] { 0, 1, 2 };
            puzzle.placas = new Transform[3];
            puzzle.visuais = new SpriteRenderer[3];
            var nomes = new[] { "Azul", "Ouro", "Verde" };
            var cores = new[]
            {
                new Color(.20f, .53f, .95f),
                new Color(.99f, .76f, .22f),
                new Color(.26f, .74f, .43f)
            };
            for (int i = 0; i < 3; i++)
            {
                var placa = new GameObject("Placa_" + (i + 1) + "_" + nomes[i]);
                placa.transform.SetParent(raiz.transform, false);
                placa.transform.localPosition = new Vector3((i - 1) * 1.6f, -.8f, 0f);
                var desenho = placa.AddComponent<SpriteRenderer>();
                desenho.sprite = artePlaca;
                desenho.color = cores[i];
                placa.AddComponent<MTMOrdemY>();
                puzzle.placas[i] = placa.transform;
                puzzle.visuais[i] = desenho;
            }

            var selo = new GameObject("Selo_Passagem");
            selo.transform.SetParent(raiz.transform, false);
            selo.transform.localPosition = new Vector3(0f, 1.25f, 0);
            selo.transform.localScale = new Vector3(1.6f, .65f, 1);
            var visual = selo.AddComponent<SpriteRenderer>();
            visual.sprite = arteSelo;
            visual.color = new Color(.85f, .49f, .83f);
            selo.AddComponent<MTMOrdemY>();
            var colisao = selo.AddComponent<BoxCollider2D>();
            colisao.size = Vector2.one;
            puzzle.visualPassagem = visual;
            puzzle.colisaoPassagem = colisao;

            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabPuzzle);
            UnityEngine.Object.DestroyImmediate(raiz);
            if (prefab == null)
            {
                Debug.LogError("Falha ao salvar o prefab do puzzle.");
                return;
            }

            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, regiao.transform);
            Undo.RegisterCreatedObjectUndo(instancia, "Criar puzzle de placas");
            Vector3 origem = new Vector3(regiao.dados.seguroX + 3.5f,
                regiao.dados.seguroY + 2f, 0f);
            instancia.transform.position = origem;

            // O baú é um objeto separado para manter seu MTMPonto na lista da região.
            var bauInstancia = (GameObject)PrefabUtility.InstantiatePrefab(bauFonte, regiao.transform);
            Undo.RegisterCreatedObjectUndo(bauInstancia, "Criar recompensa do puzzle");
            bauInstancia.name = "Bau_Recompensa_Puzzle_Praca";
            bauInstancia.transform.position = origem + new Vector3(0f, 2.15f, 0f);
            var bau = bauInstancia.GetComponent<MTMBau>();
            bau.idTesouro = "praca_puzzle_01";
            bau.idPuzzleNecessario = "praca_placas_01";
            bau.idRecompensa = "cera";
            bau.quantidade = 2;
            var ponto = bauInstancia.GetComponent<MTMPonto>();
            ponto.dados = new MTMPontoDados
            {
                id = "exploracao_bau_praca_puzzle_01",
                nome = "Tesouro das placas",
                tipo = "tesouro",
                evento = "exploracao_bau_praca_puzzle_01"
            };

            Undo.RecordObject(regiao, "Registrar recompensa do puzzle");
            var anteriores = regiao.pontos ?? Array.Empty<MTMPonto>();
            var novos = new MTMPonto[anteriores.Length + 1];
            Array.Copy(anteriores, novos, anteriores.Length);
            novos[anteriores.Length] = ponto;
            regiao.pontos = novos;
            EditorUtility.SetDirty(regiao);
            EditorSceneManager.MarkSceneDirty(regiao.gameObject.scene);
            Selection.activeGameObject = instancia;
            EditorGUIUtility.PingObject(instancia);
            Debug.Log("Puzzle criado: pise em Azul > Ouro > Verde. Posicione as peças em área livre, salve a cena e teste.");
        }

        private static void CriarSprite(string path, bool selo)
        {
            const int tamanho = 24;
            var tex = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
            var pixels = new Color[tamanho * tamanho];
            Color transparente = new Color(0, 0, 0, 0);
            Color borda = selo ? new Color(.26f, .16f, .32f) : new Color(.11f, .16f, .21f);
            Color miolo = selo ? new Color(.93f, .75f, .92f) : Color.white;

            for (int y = 0; y < tamanho; y++)
            for (int x = 0; x < tamanho; x++)
            {
                bool dentro = x >= 1 && x <= 22 && y >= 1 && y <= 22;
                bool contorno = x < 4 || x > 19 || y < 4 || y > 19;
                Color cor = dentro ? (contorno ? borda : miolo) : transparente;
                if (dentro && (x == y || x + y == 23))
                    cor = selo ? new Color(.5f, .22f, .52f) : new Color(.72f, .78f, .85f);
                pixels[y * tamanho + x] = cor;
            }
            tex.SetPixels(pixels);
            tex.Apply(false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 24f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
#endif

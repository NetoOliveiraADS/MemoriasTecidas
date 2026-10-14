using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.PracaV1.Editor
{
    public static class MTPracaRepararJogador
    {
        private const string Raiz = "Assets/MemoriasTecidas/PracaV1";

        [MenuItem("Memorias Tecidas/Praca V1/3 - Reparar jogador da cena aberta")]
        public static void Reparar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Saia do Play antes de reparar o jogador."); return;
            }
            try
            {
                MTPracaMontador.Manifesto dados = MTPracaMontador.Ler();
                MTPracaMontador.Arte arte = Array.Find(dados.assets, a => a.id == "jogador_folha");
                if (arte == null) throw new InvalidDataException("jogador_folha não encontrado no manifesto.");
                // Conversão numérica explícita: não desserializa JSON diretamente em Rect.
                MTPracaAnimacao.Quadro[] quadros = MTPracaMontador.ConverterQuadros(arte);
                string caminhoTextura = Raiz + "/" + arte.file;
                TextureImporter importer = AssetImporter.GetAtPath(caminhoTextura) as TextureImporter;
                if (importer == null) throw new FileNotFoundException("PNG do jogador não encontrado.", caminhoTextura);
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
                Texture2D folha = AssetDatabase.LoadAssetAtPath<Texture2D>(caminhoTextura);
                if (folha == null) throw new InvalidDataException("Não foi possível carregar a textura do jogador.");

                int reparados = 0;
                Undo.IncrementCurrentGroup();
                int grupoUndo = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Reparar animação do jogador da praça");
                var cena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                foreach (GameObject raiz in cena.GetRootGameObjects())
                {
                    foreach (MTPracaAnimacao animacao in raiz.GetComponentsInChildren<MTPracaAnimacao>(true))
                    {
                        bool pertenceAoJogador = animacao.GetComponentInParent<MTPracaJogador>() != null
                            || AssetDatabase.GetAssetPath(animacao.folha) == caminhoTextura;
                        if (!pertenceAoJogador) continue;
                        Undo.RecordObject(animacao, "Reparar quadros do jogador");
                        SpriteRenderer renderer = animacao.GetComponent<SpriteRenderer>();
                        if (renderer != null) Undo.RecordObject(renderer, "Mostrar jogador");
                        Configurar(animacao, arte, folha, quadros);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animacao);
                        if (renderer != null) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        EditorUtility.SetDirty(animacao);
                        reparados++;
                    }
                }
                Undo.CollapseUndoOperations(grupoUndo);
                if (reparados > 0) EditorSceneManager.MarkSceneDirty(cena);

                // Corrige também a origem de futuras instâncias, preservando o GUID do prefab.
                string caminhoPrefab = Raiz + "/Gerados/Prefabs/jogador_folha.prefab";
                bool prefabReparado = false;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab) != null)
                {
                    GameObject conteudo = PrefabUtility.LoadPrefabContents(caminhoPrefab);
                    try
                    {
                        MTPracaAnimacao animacao = conteudo.GetComponentInChildren<MTPracaAnimacao>(true);
                        if (animacao != null)
                        {
                            Configurar(animacao, arte, folha, quadros);
                            PrefabUtility.SaveAsPrefabAsset(conteudo, caminhoPrefab);
                            prefabReparado = true;
                        }
                    }
                    finally { PrefabUtility.UnloadPrefabContents(conteudo); }
                }
                AssetDatabase.SaveAssets();
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
                Debug.Log("Praça 1.0.1: " + reparados + " animação(ões) reparada(s) na cena '" + cena.name
                    + "'; prefab " + (prefabReparado ? "atualizado" : "não encontrado")
                    + ". 12 quadros lidos. Textura importada: " + folha.width + " x " + folha.height
                    + ". Salve a cena com Ctrl+S e teste no Play.");
                if (reparados == 0)
                    Debug.LogWarning("Abra a cena Praca_Visual_V1 com o Jogador_Praca e execute o reparo novamente.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static void Configurar(MTPracaAnimacao animacao, MTPracaMontador.Arte arte,
            Texture2D folha, MTPracaAnimacao.Quadro[] quadros)
        {
            animacao.folha = folha;
            animacao.quadros = (MTPracaAnimacao.Quadro[])quadros.Clone();
            animacao.tamanhoReferencia = new Vector2Int(arte.width, arte.height);
            animacao.pixelsPorUnidade = arte.ppu;
            animacao.enabled = true;
            SpriteRenderer renderer = animacao.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.enabled = true;
            animacao.Recarregar();
        }
    }
}

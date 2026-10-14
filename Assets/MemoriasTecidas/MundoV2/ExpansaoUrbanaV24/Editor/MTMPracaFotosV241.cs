using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMPracaFotosV241
    {
        [MenuItem("Memorias Tecidas/Expansao Urbana V2.4/6 - Atualizar praca pelas fotos (V2.4.1)")]
        public static void Atualizar()
        {
            string original = null, backup = null;
            bool iniciou = false;
            try
            {
                var jogo = MTMUrbanaMontagemV24.Jogo();
                var vida = jogo.GetComponent<MTMVidaV22>();
                if (vida == null || vida.versaoInstalada < 221) throw new InvalidOperationException("Instale Vida V2.2.1 primeiro.");
                if (jogo.regioes == null || jogo.regioes.Any(r => r == null || r.dados == null))
                    throw new InvalidOperationException("A lista de regiões está incompleta.");
                if (!MTMUrbanaMontagemV24.Alvos.All(id => jogo.regioes.Any(r => r.dados.id == id && r.GetComponent<MTMUrbanaV24>() != null)))
                    throw new InvalidOperationException("Execute primeiro o menu 2 - Montar lugares com backup. Na instalação nova, ele já inclui esta praça.");
                var praca = jogo.regioes.Single(r => r.dados.id == "prefeitura");
                var marca = praca.GetComponent<MTMUrbanaV24>();
                if (marca.versao >= 241)
                {
                    MTMUrbanaPainelV24.Mostrar("prefeitura");
                    Debug.Log("A praça V2.4.1 já está aplicada. Suas edições foram preservadas; nenhum objeto foi duplicado.");
                    return;
                }
                if (string.IsNullOrEmpty(jogo.gameObject.scene.path)) throw new InvalidOperationException("Salve a cena com Ctrl+S antes de atualizar.");
                if (praca.transform.position.sqrMagnitude > .0001f || Quaternion.Angle(praca.transform.rotation, Quaternion.identity) > .01f || (praca.transform.lossyScale - Vector3.one).sqrMagnitude > .0001f)
                    throw new InvalidOperationException("A raiz prefeitura deve ter posição zero, rotação zero e escala 1.");
                var dados = MTMUrbanaArtesV24.Locais().locais.Single(l => l.id == "prefeitura");
                MTMUrbanaArtesV24.Preparar();
                var cena = jogo.gameObject.scene;
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("A cena não pôde ser salva.");
                original = cena.path;
                Directory.CreateDirectory("Assets/MemoriasTecidas/Backups_V24"); AssetDatabase.Refresh();
                backup = "Assets/MemoriasTecidas/Backups_V24/" + Path.GetFileNameWithoutExtension(original) + "_antes_Praca241_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".unity";
                if (!EditorSceneManager.SaveScene(cena, backup, true)) throw new IOException("Falha ao criar o backup. A praça não foi alterada.");
                iniciou = true;
                vida.jogo = jogo;
                MTMUrbanaMontagemV24.MontarLocal(praca, dados, vida, false);
                MTMUrbanaValidacaoV24.Validar(jogo, true);
                foreach (var r in jogo.regioes) r.gameObject.SetActive(r == praca);
                EditorUtility.SetDirty(vida);
                EditorSceneManager.MarkSceneDirty(cena);
                foreach (var fixo in praca.GetComponentsInChildren<MTMMonumentoFixoV242>(true)) fixo.PrepararParaSalvar();
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("Não foi possível salvar a praça atualizada.");
                iniciou = false;
                MTMUrbanaPainelV24.Mostrar("prefeitura");
                Debug.Log("Praça V2.4.1 atualizada pelas fotos: torre com relógio, postes curvos, coberturas e novo piso. Backup: " + backup + ". Use o menu 3 para renovar a imagem no mapa; depois Ctrl+S.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (iniciou && !string.IsNullOrEmpty(original))
                {
                    EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
                    Debug.LogWarning("A cena anterior à atualização foi reaberta. Backup: " + backup);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
        }
    }
}

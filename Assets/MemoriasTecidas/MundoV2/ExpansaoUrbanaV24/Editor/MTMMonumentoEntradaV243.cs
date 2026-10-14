using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.Editor
{
    // Atualiza o objeto existente: nao recria a regiao nem seus pontos de jogo.
    public static class MTMMonumentoEntradaV243
    {
        public const string Antigo = "urb_escultura_verde";
        public const string Novo = "urb_monumento_entrada_duas_figuras";

        [MenuItem("Memorias Tecidas/Expansao Urbana V2.4/8 - Atualizar monumento da entrada (V2.4.3)")]
        public static void Atualizar()
        {
            string original = null, backup = null;
            bool iniciou = false;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Saia do Play antes de atualizar o monumento.");
                var jogo = MTMUrbanaMontagemV24.Jogo();
                var entrada = jogo.regioes == null ? null : Array.Find(jogo.regioes, r => r != null && r.dados != null && r.dados.id == "entrada");
                var tag = entrada != null ? entrada.GetComponent<MTMUrbanaV24>() : null;
                if (tag == null || tag.cenarioAtual == null)
                    throw new InvalidOperationException("A Entrada da V2.4 ainda nao esta montada. Abra a cena ampliada ou use o menu 2 para a primeira instalacao.");

                // Busca apenas na decoracao atual, nunca nos layouts antigos desativados.
                var alvos = tag.cenarioAtual.GetComponentsInChildren<MTMElementoV23>(true)
                    .Where(e => e.identificador == Antigo || e.identificador == Novo).ToArray();
                if (alvos.Length == 0)
                    throw new InvalidOperationException("Monumento da direita nao encontrado no Cenario_V24 da entrada. Se ele foi removido, prepare as artes pelo menu 1 e arraste o prefab urb_monumento_entrada_duas_figuras para Mobiliario da entrada.");
                var selecionado = Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponentInParent<MTMElementoV23>(true) : null;
                if (alvos.Length > 1 && !alvos.Contains(selecionado))
                    throw new InvalidOperationException("Ha mais de um monumento candidato. Selecione o da direita na Hierarchy e execute o menu 8 novamente.");
                var elemento = alvos.Length == 1 ? alvos[0] : selecionado;
                var go = elemento.gameObject;
                var visual = go.GetComponent<MTMVisualV2>();
                var desenho = visual != null ? visual.desenho : null;
                if (desenho == null)
                {
                    var t = go.transform.Find("Desenho");
                    desenho = t != null ? t.GetComponent<SpriteRenderer>() : null;
                }
                if (desenho == null)
                    throw new InvalidOperationException("O monumento precisa do filho Desenho com SpriteRenderer.");

                // Repetir o comando preserva as edicoes feitas depois da primeira troca.
                if (elemento.identificador == Novo && desenho.sprite != null && go.GetComponent<MTMMonumentoFixoV242>() != null)
                {
                    Mostrar(go);
                    Debug.Log("O monumento da entrada V2.4.3 ja esta aplicado. Suas edicoes foram preservadas. Salve com Ctrl+S.");
                    return;
                }
                var cena = jogo.gameObject.scene;
                if (string.IsNullOrEmpty(cena.path))
                    throw new InvalidOperationException("Salve a cena com Ctrl+S antes de atualizar.");
                if (go.transform.lossyScale.x <= 0 || go.transform.lossyScale.y <= 0)
                    throw new InvalidOperationException("Use escala positiva X e Y no monumento e nos seus pais antes de atualizar.");

                MTMUrbanaArtesV24.Preparar();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MTMUrbanaArtesV24.Raiz + "/Gerados/Sprites/" + Novo + ".asset");
                var materialSombra = AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Vida + "/Gerados/Sombra_V22.mat");
                if (sprite == null || MTMUrbanaArtesV24.Tinta == null || materialSombra == null)
                    throw new FileNotFoundException("Sprite ou materiais ausentes. Prepare as artes e os materiais da Vida V2.2.");

                PrepararSombrasParaSalvar(jogo);
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("Nao foi possivel salvar a cena atual.");
                original = cena.path;
                Directory.CreateDirectory("Assets/MemoriasTecidas/Backups_V24");
                AssetDatabase.Refresh();
                backup = "Assets/MemoriasTecidas/Backups_V24/" + Path.GetFileNameWithoutExtension(original)
                    + "_antes_Entrada243_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".unity";
                PrepararSombrasParaSalvar(jogo);
                if (!EditorSceneManager.SaveScene(cena, backup, true))
                    throw new IOException("Falha ao criar o backup. O monumento nao foi alterado.");

                iniciou = true;
                var sombraT = go.transform.Find("Sombra");
                var sombra = sombraT != null ? sombraT.GetComponent<SpriteRenderer>() : null;
                if (sombra == null)
                {
                    if (sombraT == null)
                    {
                        var filho = new GameObject("Sombra");
                        filho.transform.SetParent(go.transform, false);
                        sombraT = filho.transform;
                    }
                    sombra = sombraT.gameObject.AddComponent<SpriteRenderer>();
                }
                sombra.sharedMaterial = materialSombra;
                desenho.sprite = sprite;
                desenho.sharedMaterial = MTMUrbanaArtesV24.Tinta;
                desenho.spriteSortPoint = SpriteSortPoint.Pivot;
                // Pivo nos pes e corpo vertical; a sombra tem sua propria transformacao.
                go.transform.rotation = Quaternion.identity;
                desenho.transform.localPosition = Vector3.zero;
                desenho.transform.localRotation = Quaternion.identity;
                desenho.flipX = false;
                desenho.flipY = false;
                desenho.forceRenderingOff = false;
                foreach (var deformacao in desenho.GetComponents<MTMDeformacaoV22>()) deformacao.enabled = false;
                Configurar(go, desenho, sombra);
                elemento.identificador = Novo;
                // Nao muda nome, posicao, escala, colisores ou componentes de interacao.
                PrepararSombrasParaSalvar(jogo);
                foreach (var componente in go.GetComponentsInChildren<Component>(true))
                {
                    if (componente == null) continue;
                    EditorUtility.SetDirty(componente);
                    if (PrefabUtility.IsPartOfPrefabInstance(componente))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(componente);
                }
                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("Nao foi possivel salvar a atualizacao.");
                iniciou = false;
                Mostrar(go);
                Debug.Log("Monumento da entrada V2.4.3 atualizado: duas figuras e livro, pedestal e sombra curta independente. Backup: "
                    + backup + ". Confira no Play; para renovar o cartao da entrada no mapa, use o menu 3 e Ctrl+S.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (iniciou && !string.IsNullOrEmpty(original))
                {
                    EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
                    Debug.LogWarning("Cena anterior a troca reaberta. Backup: " + backup);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        internal static void Configurar(GameObject go, SpriteRenderer desenho, SpriteRenderer sombra)
        {
            MTMReparoMonumentoV242.Configurar(go, desenho, sombra);
            var fixo = go.GetComponent<MTMMonumentoFixoV242>();
            fixo.larguraBase = 1.42f;
            fixo.profundidadeContato = .55f;
            fixo.comprimentoSombra = .65f;
            fixo.opacidadeContato = .22f;
            fixo.opacidadeSombra = .10f;
            fixo.Aplicar();
        }

        internal static void PrepararSombrasParaSalvar(MTMJogo jogo)
        {
            foreach (var regiao in jogo.regioes ?? Array.Empty<MTMRegiao>())
            {
                if (regiao == null) continue;
                foreach (var fixo in regiao.GetComponentsInChildren<MTMMonumentoFixoV242>(true))
                    fixo.PrepararParaSalvar();
            }
        }

        private static void Mostrar(GameObject go)
        {
            MTMUrbanaPainelV24.Mostrar("entrada");
            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }
}

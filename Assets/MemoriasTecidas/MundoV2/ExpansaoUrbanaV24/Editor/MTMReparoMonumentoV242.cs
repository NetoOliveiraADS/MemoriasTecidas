using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMReparoMonumentoV242
    {
        public const string Antigo = "urb_torre_relogio_praca";
        public const string Novo = "urb_torre_relogio_praca_vertical";

        [MenuItem("Memorias Tecidas/Expansao Urbana V2.4/7 - Corrigir apoio do monumento (V2.4.2)")]
        public static void Corrigir()
        {
            string original = null, backup = null; bool iniciou = false;
            try
            {
                var jogo = MTMUrbanaMontagemV24.Jogo();
                var praca = jogo.regioes == null ? null : Array.Find(jogo.regioes, r => r != null && r.dados != null && r.dados.id == "prefeitura");
                var tag = praca != null ? praca.GetComponent<MTMUrbanaV24>() : null;
                if (tag == null || tag.cenarioAtual == null) throw new InvalidOperationException("Monte a Prefeitura pelo menu 2; se já existe, aplique os detalhes da praça pelo menu 6.");
                var alvos = tag.cenarioAtual.GetComponentsInChildren<MTMElementoV23>(true).Where(e => e.identificador == Antigo || e.identificador == Novo).ToArray();
                if (alvos.Length == 0) throw new InvalidOperationException("Monumento não encontrado no cenário atual. Aplique primeiro os detalhes da praça pelo menu 6.");
                var escolhido = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<MTMElementoV23>(true) : null;
                if (alvos.Length > 1 && !alvos.Contains(escolhido)) throw new InvalidOperationException("Existe mais de uma torre. Selecione a que deseja corrigir na Hierarchy e execute o menu 7 novamente.");
                var elemento = alvos.Length == 1 ? alvos[0] : escolhido;
                var go = elemento.gameObject;
                var visual = go.GetComponent<MTMVisualV2>();
                var sr = visual != null ? visual.desenho : null;
                if (sr == null) { var t = go.transform.Find("Desenho"); sr = t != null ? t.GetComponent<SpriteRenderer>() : null; }
                if (sr == null) throw new InvalidOperationException("O monumento precisa do filho Desenho com SpriteRenderer.");
                if (string.IsNullOrEmpty(jogo.gameObject.scene.path)) throw new InvalidOperationException("Salve a cena antes de corrigir.");
                if (go.transform.lossyScale.x <= 0 || go.transform.lossyScale.y <= 0) throw new InvalidOperationException("Use escala positiva X e Y na torre e nos seus pais antes de aplicar a correção.");
                MTMUrbanaArtesV24.Preparar();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MTMUrbanaArtesV24.Raiz + "/Gerados/Sprites/" + Novo + ".asset");
                if (sprite == null) throw new FileNotFoundException("Sprite novo do monumento não foi preparado.");
                var cena = jogo.gameObject.scene;
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("A cena não pôde ser salva.");
                original = cena.path;
                Directory.CreateDirectory("Assets/MemoriasTecidas/Backups_V24"); AssetDatabase.Refresh();
                backup = "Assets/MemoriasTecidas/Backups_V24/" + Path.GetFileNameWithoutExtension(original) + "_antes_Monumento242_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".unity";
                if (!EditorSceneManager.SaveScene(cena, backup, true)) throw new IOException("Falha ao criar backup; o monumento não foi alterado.");
                iniciou = true;
                var sombraT = go.transform.Find("Sombra");
                var sombra = sombraT != null ? sombraT.GetComponent<SpriteRenderer>() : null;
                if (sombra == null)
                {
                    var g = new GameObject("Sombra"); g.transform.SetParent(go.transform, false); sombra = g.AddComponent<SpriteRenderer>();
                }
                sombra.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Vida + "/Gerados/Sombra_V22.mat");
                sr.sprite = sprite; sr.sharedMaterial = MTMUrbanaArtesV24.Tinta;
                // Corrige inclinação da instância sem mexer no ponto do mapa ou na escala escolhida.
                go.transform.rotation = Quaternion.identity;
                sr.transform.localPosition = Vector3.zero; sr.transform.localRotation = Quaternion.identity;
                sr.flipX = false; sr.flipY = false; sr.forceRenderingOff = false;
                foreach (var d in sr.GetComponents<MTMDeformacaoV22>()) d.enabled = false;
                Configurar(go, sr, sombra);
                elemento.identificador = Novo;
                go.GetComponent<MTMMonumentoFixoV242>().PrepararParaSalvar();
                foreach (var c in go.GetComponentsInChildren<Component>(true))
                {
                    if (c == null) continue;
                    EditorUtility.SetDirty(c);
                    if (PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                }
                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("Não foi possível salvar o monumento corrigido.");
                iniciou = false;
                MTMUrbanaPainelV24.Mostrar("prefeitura"); Selection.activeGameObject = go;
                SceneView.lastActiveSceneView?.FrameSelected();
                Debug.Log("Monumento V2.4.2 corrigido: desenho vertical, base apoiada e sombra independente. Posição, escala, dimensões dos colisores e interações preservadas. Backup: " + backup + ". Confira no Play e atualize a imagem do mapa pelo menu 3.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (iniciou && !string.IsNullOrEmpty(original))
                { EditorSceneManager.OpenScene(original, OpenSceneMode.Single); Debug.LogWarning("Cena anterior à correção reaberta. Backup: " + backup); }
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        internal static void Configurar(GameObject go, SpriteRenderer desenho, SpriteRenderer sombra)
        {
            if (sombra == null || sombra.sharedMaterial == null) throw new FileNotFoundException("Material Sombra_V22 ausente. Prepare os materiais da Vida V2.2 antes de corrigir.");
            var visual = go.GetComponent<MTMVisualV2>();
            if (visual != null) { visual.vento = 0; visual.respirar = false; visual.enabled = false; }
            var escala = go.GetComponent<MTMEscalaConstrucaoV221>();
            if (escala != null) escala.sombra = null;
            var contato = go.transform.Find("Contato_Base_V242");
            if (contato == null) { var g = new GameObject("Contato_Base_V242"); g.transform.SetParent(go.transform, false); contato = g.transform; }
            var filtro = contato.GetComponent<MeshFilter>(); if (filtro == null) filtro = contato.gameObject.AddComponent<MeshFilter>();
            var renderer = contato.GetComponent<MeshRenderer>(); if (renderer == null) renderer = contato.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialContato();
            var fixo = go.GetComponent<MTMMonumentoFixoV242>(); if (fixo == null) fixo = go.AddComponent<MTMMonumentoFixoV242>();
            fixo.desenho = desenho; fixo.sombra = sombra; fixo.malhaContato = filtro; fixo.contato = renderer;
            fixo.manterDesenhoVertical = true; fixo.Aplicar();
        }
        private static Material MaterialContato()
        {
            string path = MTMUrbanaArtesV24.Raiz + "/Gerados/Contato_Monumento_V242.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            if (MTMUrbanaArtesV24.Tinta == null) throw new FileNotFoundException("Material Pintura_V22 ausente.");
            m = new Material(MTMUrbanaArtesV24.Tinta) { name = "Contato_Monumento_V242" };
            m.SetColor("_Color", Color.white); m.SetFloat("_Efeito", 1); m.SetFloat("_Sombra", 1); m.SetFloat("_Agua", 0);
            AssetDatabase.CreateAsset(m, path); return m;
        }
    }
}

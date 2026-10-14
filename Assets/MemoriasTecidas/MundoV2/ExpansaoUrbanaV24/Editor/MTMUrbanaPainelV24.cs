using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.Editor
{
    public sealed class MTMUrbanaPainelV24 : EditorWindow
    {
        [MenuItem("Memorias Tecidas/Expansao Urbana V2.4/4 - Abrir painel de edicao")]
        public static void Abrir() { GetWindow<MTMUrbanaPainelV24>("Cidade V2.4"); }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Escolha o lugar para editar", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Fora do Play, esta janela ativa uma região na Scene. Expanda Cenario_V24 para editar objetos e pisos. O Play continua usando o local salvo na sua jornada.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Entrada da cidade")) Mostrar("entrada");
                if (GUILayout.Button("Largo da Vila / Ponta da Rua")) Mostrar("largo");
                if (GUILayout.Button("Mercado Público")) Mostrar("mercado");
                if (GUILayout.Button("Prefeitura e praça")) Mostrar("prefeitura");
                if (GUILayout.Button("Aplicar detalhes das fotos da praça — V2.4.1")) MTMPracaFotosV241.Atualizar();
                if (GUILayout.Button("Corrigir apoio do monumento — V2.4.2")) MTMReparoMonumentoV242.Corrigir();
                if (GUILayout.Button("Atualizar monumento da entrada — V2.4.3")) MTMMonumentoEntradaV243.Atualizar();
                EditorGUILayout.Space();
                if (GUILayout.Button("Abrir novos prefabs"))
                { Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MTMUrbanaArtesV24.Raiz + "/Gerados/Prefabs"); EditorUtility.FocusProjectWindow(); }
                if (GUILayout.Button("Conferir caminhos e conexões")) MTMUrbanaValidacaoV24.MenuValidar();
                if (GUILayout.Button("Atualizar imagens do mapa")) MTMUrbanaCartoesV24.Capturar();
            }
            EditorGUILayout.HelpBox("W: mover. Prédios: MTMEscalaConstrucaoV221. Pisos: MTMSuperficieV23. Colisões: BoxCollider2D. Diálogos: MTMPonto > Dados > Falas. Salvar: Ctrl+S.", MessageType.None);
        }
        internal static void Mostrar(string id)
        {
            try
            {
                var jogo = MTMUrbanaMontagemV24.Jogo(); var alvo = Array.Find(jogo.regioes, r => r.dados.id == id);
                if (alvo == null) throw new InvalidOperationException("Monte a expansão pelo menu 2 primeiro.");
                foreach (var r in jogo.regioes) { Undo.RecordObject(r.gameObject, "Mostrar região"); r.gameObject.SetActive(r == alvo); }
                foreach (var piso in alvo.GetComponentsInChildren<MTMSuperficieV23>()) piso.Reconstruir();
                Selection.activeGameObject = alvo.gameObject;
                var vista = SceneView.lastActiveSceneView;
                if (vista != null) { vista.in2DMode = true; vista.Frame(new Bounds(Vector3.zero, new Vector3(alvo.dados.largura, alvo.dados.altura, 1)), false); }
                EditorSceneManager.MarkSceneDirty(jogo.gameObject.scene);
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}

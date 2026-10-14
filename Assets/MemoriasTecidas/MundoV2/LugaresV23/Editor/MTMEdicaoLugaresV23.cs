using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.Editor
{
    public sealed class MTMEdicaoLugaresV23:EditorWindow
    {
        [MenuItem("Memorias Tecidas/Lugares V2.3/5 - Abrir painel de edicao")]
        public static void Abrir(){GetWindow<MTMEdicaoLugaresV23>("Lugares V2.3");}
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Escolha o lugar para editar",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("A edição ocorre fora do Play. A janela ativa uma região por vez na mesma cena. Expanda Cenario_V23 na Hierarchy e selecione um objeto.",MessageType.Info);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if(GUILayout.Button("Praça Matriz"))Mostrar("praca");
                if(GUILayout.Button("Rua do Comércio"))Mostrar("rua");
                if(GUILayout.Button("Rio Serafim / Jaguaribe"))Mostrar("rio");
                if(GUILayout.Button("Beira-Rio · Polo de Lazer"))Mostrar("beira");
                EditorGUILayout.Space();
                if(GUILayout.Button("Abrir a pasta de prefabs"))
                {Selection.activeObject=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MTMArtesLugaresV23.Raiz+"/Gerados/Prefabs");EditorUtility.FocusProjectWindow();}
                if(GUILayout.Button("Revisar caminhos após minhas mudanças"))MTMCaminhosLugaresV23.MenuRevisar();
                if(GUILayout.Button("Atualizar imagens do mapa"))MTMCartoesLugaresV23.Capturar();
            }
            EditorGUILayout.HelpBox("Mover: W. Tamanho do prédio: MTMEscalaConstrucaoV221. Piso e água: MTMSuperficieV23. Margem: selecione a água e arraste os pontos na Scene. Diálogos: MTMPonto > Dados > Falas. Salvar: Ctrl+S.",MessageType.None);
        }
        private static void Mostrar(string id)
        {
            try
            {
                var jogo=MTMMontagemLugaresV23.JogoDaCena();var alvo=Array.Find(jogo.regioes,r=>r.dados.id==id);
                if(alvo==null)throw new InvalidOperationException("Região ausente: "+id);
                foreach(var r in jogo.regioes){Undo.RecordObject(r.gameObject,"Mostrar região para editar");r.gameObject.SetActive(r==alvo);}
                Selection.activeGameObject=alvo.gameObject;
                var vista=SceneView.lastActiveSceneView;if(vista!=null){vista.in2DMode=true;vista.Frame(new Bounds(Vector3.zero,new Vector3(alvo.dados.largura,alvo.dados.altura,1)),false);}
                EditorSceneManager.MarkSceneDirty(jogo.gameObject.scene);
            }
            catch(Exception e){Debug.LogException(e);}
        }
    }
    [CustomEditor(typeof(MTMSuperficieV23))]
    public sealed class MTMSuperficieV23Editor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Use Tamanho para pisos retangulares. Para margens, edite Contorno ou arraste os pontos na Scene. Água e colisão usam a mesma borda. Evite cruzar as linhas.",MessageType.Info);
            if(GUILayout.Button("Reconstruir superfície"))
            {try{((MTMSuperficieV23)target).Reconstruir();}catch(Exception e){Debug.LogException(e);}}
        }
        private void OnSceneGUI()
        {
            var s=(MTMSuperficieV23)target;if(s.contorno==null||s.contorno.Length<3)return;
            for(int i=0;i<s.contorno.Length;i++)
            {
                Vector3 p=s.transform.TransformPoint(s.contorno[i]);Handles.color=Color.cyan;
                EditorGUI.BeginChangeCheck();Vector3 nova=Handles.FreeMoveHandle(p,HandleUtility.GetHandleSize(p)*.055f,Vector3.one*.1f,Handles.DotHandleCap);
                Handles.Label(p," "+i);
                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(s,"Mover margem do rio");var col=s.GetComponent<PolygonCollider2D>();if(col!=null)Undo.RecordObject(col,"Mover colisão da margem");
                    Vector3 local=s.transform.InverseTransformPoint(nova);s.contorno[i]=new Vector2(local.x,local.y);
                    try{s.Reconstruir();}catch(Exception e){Debug.LogWarning(e.Message,s);}
                    EditorUtility.SetDirty(s);
                }
            }
        }
    }
}

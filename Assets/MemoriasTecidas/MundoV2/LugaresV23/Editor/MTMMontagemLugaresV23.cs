using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMMontagemLugaresV23
    {
        public static readonly string[] Alvos={"praca","rua","rio","beira"};
        public static T Copia<T>(T dado){return JsonUtility.FromJson<T>(JsonUtility.ToJson(dado));}

        public static MTMJogo JogoDaCena()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Saia do Play antes de editar a montagem.");
            var cena=SceneManager.GetActiveScene();
            var jogos=cena.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MTMJogo>(true)).ToArray();
            if(jogos.Length!=1)throw new InvalidOperationException("Abra a cena do Mundo V2 que contém um único Explorador_MundoV2.");
            return jogos[0];
        }

        [MenuItem("Memorias Tecidas/Lugares V2.3/2 - Montar quatro locais com backup")]
        public static void Montar()
        {
            string original=null,backup=null;bool iniciou=false;
            try
            {
                var jogo=JogoDaCena();var vida=jogo.GetComponent<MTMVidaV22>();
                if(vida==null||vida.versaoInstalada<221||jogo.GetComponent<MTMTravessiaV221>()==null)
                    throw new InvalidOperationException("Instale primeiro a Vida V2.2.1 e execute sua correção de movimento/passagens. O complemento V2.3 depende dela.");
                var pacote=MTMArtesLugaresV23.LerCenas();var todos=new Dictionary<string,MTMRegiao>();
                if(jogo.regioes==null)throw new InvalidOperationException("O jogo não tem regiões cadastradas.");
                foreach(var r in jogo.regioes)
                {
                    if(r==null||r.dados==null||todos.ContainsKey(r.dados.id))throw new InvalidOperationException("Região ausente ou ID duplicado.");
                    todos.Add(r.dados.id,r);
                }
                foreach(var d in pacote.locais)
                {
                    if(!todos.TryGetValue(d.id,out var r))throw new InvalidOperationException("Falta a região "+d.id+" na cena.");
                    if(r.GetComponent<MTMRegiaoVisualV23>()!=null)throw new InvalidOperationException("Já existe montagem V2.3 em "+d.id+". Edite a cena atual; para remontar do zero, abra a cena de backup anterior à V2.3.");
                    if(r.transform.position.sqrMagnitude>.0001f||Quaternion.Angle(r.transform.rotation,Quaternion.identity)>.01f||(r.transform.lossyScale-Vector3.one).sqrMagnitude>.0001f)
                        throw new InvalidOperationException("A região "+d.id+" precisa manter posição 0, rotação 0 e escala 1, como no Mundo V2.");
                    foreach(var p in d.pontos)if(r.pontos==null||!r.pontos.Any(x=>x!=null&&x.dados!=null&&x.dados.id==p.id))
                        throw new InvalidOperationException("Interação original ausente: "+p.id+". Confira se está usando a cena completa V2.2.1.");
                }
                foreach(string id in new[]{"canoa","tear","enfeite_palha"})
                    if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MemoriasTecidas/MundoV2/Gerados/Prefabs/"+id+".prefab")==null)
                        throw new FileNotFoundException("Prefab original necessário não encontrado: "+id);
                MTMArtesLugaresV23.Preparar();
                Scene cena=jogo.gameObject.scene;
                if(!EditorSceneManager.SaveScene(cena))return;
                original=cena.path;
                Directory.CreateDirectory("Assets/MemoriasTecidas/Backups_V23");AssetDatabase.Refresh();
                backup="Assets/MemoriasTecidas/Backups_V23/"+Path.GetFileNameWithoutExtension(original)+"_antes_V23_"+DateTime.Now.ToString("yyyyMMdd_HHmmssfff")+".unity";
                if(!EditorSceneManager.SaveScene(cena,backup,true))throw new IOException("Não foi possível salvar o backup. Nada foi montado.");
                iniciou=true;var log=new StringBuilder("MONTAGEM LUGARES V2.3\nBackup: "+backup+"\n");
                foreach(var d in pacote.locais){MontarRegiao(todos[d.id],d,vida);log.AppendLine("Montada: "+d.id+" — "+d.objetos.Length+" objetos novos.");}
                MTMCaminhosLugaresV23.Revisar(jogo,log);
                foreach(var r in todos.Values)
                    foreach(var p in r.pontos??Array.Empty<MTMPonto>())
                        if(p!=null&&p.dados!=null&&p.dados.tipo=="saida"&&p.dados.destino=="rio"&&p.dados.nome=="Rio Jaguaribe")
                        {p.dados.nome="Rio Serafim / Jaguaribe";SincronizarPontos(r);}
                foreach(var id in Alvos){var m=todos[id].GetComponent<MTMRegiaoVisualV23>();m.versao=230;m.InvalidarLuzes();m.AtualizarLuzes();}
                EditorSceneManager.MarkSceneDirty(cena);
                if(!EditorSceneManager.SaveScene(cena))throw new IOException("Falha ao salvar a cena montada.");
                iniciou=false;
                File.WriteAllText(MTMArtesLugaresV23.Raiz+"/Gerados/Relatorio_Montagem.txt",log.ToString());AssetDatabase.Refresh();
                Selection.activeObject=todos["praca"].gameObject;
                Debug.Log("Lugares V2.3 instalados. Backup: "+backup+". Use o menu 3 para capturar as novas imagens do mapa; menu Editar para abrir cada região.");
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                if(iniciou&&!string.IsNullOrEmpty(original))
                {
                    // Todas as mutações ficam na mesma cena; ela só é salva no fim.
                    EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
                    Debug.LogWarning("A montagem foi interrompida e a cena anterior foi reaberta. Backup adicional: "+backup);
                }
            }
            finally{EditorUtility.ClearProgressBar();}
        }

        private static void MontarRegiao(MTMRegiao r,MTMLocalV23 d,MTMVidaV22 vida)
        {
            var filhos=new List<Transform>();foreach(Transform t in r.transform)filhos.Add(t);
            var arquivo=Novo("Layout_Anterior_V23",r.transform);arquivo.SetActive(false);
            foreach(var t in filhos)
            {
                bool preservar=t.GetComponentInChildren<MTMPonto>(true)!=null||t.GetComponentInChildren<MTMAtorV22>(true)!=null;
                if(r.aparicoes!=null)foreach(var a in r.aparicoes)if(a!=null&&(a.transform==t||a.transform.IsChildOf(t)))preservar=true;
                if(!preservar)t.SetParent(arquivo.transform,true);
            }
            r.dados=Copia(r.dados);r.dados.nome=d.nome;r.dados.descricao=d.descricao;
            r.dados.largura=d.largura;r.dados.altura=d.altura;r.dados.seguroX=d.seguroX;r.dados.seguroY=d.seguroY;
            // A geometria editável passa a pertencer aos objetos da cena.
            r.dados.agua=Array.Empty<MTMVetor>();r.dados.pisos=Array.Empty<MTMPisoDados>();
            var cenario=Novo("Cenario_V23",r.transform);var pisos=Novo("Pisos_e_Agua",cenario.transform);
            var construcoes=Novo("Construcoes",cenario.transform);var vegetacao=Novo("Vegetacao",cenario.transform);var objetos=Novo("Mobiliario",cenario.transform);var luzes=Novo("Iluminacao",cenario.transform);
            var marcador=r.gameObject.AddComponent<MTMRegiaoVisualV23>();marcador.layoutAnterior=arquivo;marcador.cenarioAtual=cenario;marcador.referenciaVisual=d.descricao;
            var material=AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Raiz+"/Gerados/Superficie_V23.mat");
            foreach(var p in d.pisos)
            {
                var go=Novo(p.nome,pisos.transform);go.transform.localPosition=new Vector3(p.x,p.y,0);
                var s=go.AddComponent<MTMSuperficieV23>();s.material=material;
                s.textura=string.IsNullOrEmpty(p.textura)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(MTMArtesLugaresV23.Raiz+"/Arte/"+p.textura+".png");
                if(!string.IsNullOrEmpty(p.textura)&&s.textura==null)throw new FileNotFoundException("Textura ausente: "+p.textura);
                s.tamanho=new Vector2(p.w,p.h);s.unidadesPorRepeticao=p.repeticao;s.ordem=p.ordem;s.cor=new Color(p.r,p.g,p.b,p.a);s.agua=p.agua;s.bloquearPassagem=p.bloqueia;
                s.contorno=p.contorno==null?null:p.contorno.Select(x=>new Vector2(x.x,x.y)).ToArray();s.Reconstruir();
            }
            var artes=MTMArtesLugaresV23.LerArtes().artes.ToDictionary(a=>a.id);
            foreach(var o in d.objetos)
            {
                string tipo=artes[o.id].tipo;
                Transform pai=tipo=="construcao"?construcoes.transform:tipo=="arvore"||tipo=="palmeira"||tipo=="aquatica"?vegetacao.transform:tipo=="poste"?luzes.transform:objetos.transform;
                var prefab=MTMArtesLugaresV23.Prefab(o.id);if(prefab==null)throw new FileNotFoundException("Prefab ausente: "+o.id);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,pai);go.name=o.nome;go.transform.localPosition=new Vector3(o.x,o.y,0);go.transform.localScale=Vector3.one*o.escala;
                foreach(var sr in go.GetComponentsInChildren<SpriteRenderer>(true))sr.flipX=o.flipX;
                LigarVida(go,vida);
            }
            var limites=Novo("Limites",cenario.transform);
            Parede(limites.transform,"Norte",0,d.altura/2f-.2f,d.largura,.4f);
            Parede(limites.transform,"Sul",0,-d.altura/2f+.2f,d.largura,.4f);
            Parede(limites.transform,"Oeste",-d.largura/2f+.2f,0,.4f,d.altura);
            Parede(limites.transform,"Leste",d.largura/2f-.2f,0,.4f,d.altura);
            foreach(var alvo in d.pontos)
            {
                var p=Array.Find(r.pontos,x=>x.dados.id==alvo.id);p.dados=Copia(p.dados);
                Vector2 antes=p.transform.position,nova=new Vector2(alvo.x,alvo.y);
                MoverPonto(r,p,nova);
                if(p.dados.noturno&&r.aparicoes!=null)
                    foreach(var a in r.aparicoes)if(a!=null&&(Vector2.Distance(a.transform.position,antes)<4f))a.transform.position+=(Vector3)(nova-antes);
            }
            if(d.id=="praca")Reusar("enfeite_palha",objetos.transform,new Vector2(-5,-2.6f),vida);
            if(d.id=="rua")
            {
                Reusar("tear",objetos.transform,new Vector2(4,2),vida);
                Morador(r,vida,"feirante","Sueli · feirante (ficção)",new Vector2(-15,-1),new[]{"Bom dia! Sou Sueli, uma feirante criada para este jogo.","Olhe os caixotes, os toldos e as fachadas. A rua foi inspirada em fotografias de Jaguaruana.","Mais adiante há redes expostas. Converse com Zeca para conhecer os ofícios presentes nesta aventura."});
            }
            if(d.id=="beira")
            {
                Reusar("canoa",objetos.transform,new Vector2(-9,4.5f),vida);Reusar("canoa",objetos.transform,new Vector2(-13,4.5f),vida);
                Morador(r,vida,"barraqueiro","Tião · barraqueiro (ficção)",new Vector2(-15,-10),new[]{"Sou Tião, um barraqueiro fictício desta aventura. Seja bem-vindo à beira do rio!","Estas barracas e sombras de árvores foram inspiradas nas imagens do lugar. A disposição foi adaptada ao jogo.","Você pode seguir pelo calçamento para visitar o trecho do Rio Serafim / Jaguaribe. À noite, volte a conversar com André Lúcio na praça para ouvir seu relato."});
            }
            if(d.id=="rio")Reusar("canoa",objetos.transform,new Vector2(-10,-7),vida);
            // Rotas curtas em faixas abertas. A revisão verifica também os pontos de patrulha.
            int animal=0;
            foreach(var a in r.GetComponentsInChildren<MTMAtorV22>(true))
            {
                if(a.tipo!=MTMAtorV22.Tipo.Animal||a.transform.IsChildOf(arquivo.transform))continue;
                var raiz=a.GetComponentInParent<MTMVisualV2>();if(raiz==null)continue;
                Vector2 nova=d.id=="praca"?new Vector2(animal++==0?-3:6,-19):new Vector2(animal++==0?-22:22,-6);
                MoverRaiz(raiz.transform,nova);
            }
            SincronizarPontos(r);marcador.InvalidarLuzes();marcador.AtualizarLuzes();EditorUtility.SetDirty(r);
        }
        public static void LigarVida(GameObject go,MTMVidaV22 vida)
        {
            foreach(var d in go.GetComponentsInChildren<MTMDeformacaoV22>(true))d.vida=vida;
            foreach(var f in go.GetComponentsInChildren<MTMFolhasV22>(true))f.vida=vida;
            foreach(var o in go.GetComponentsInChildren<MTMOclusaoV2>(true))o.alvo=vida.jogo.transform;
        }
        private static void Reusar(string id,Transform pai,Vector2 pos,MTMVidaV22 vida)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MemoriasTecidas/MundoV2/Gerados/Prefabs/"+id+".prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,pai);go.name=id+"_interacao_preservada";go.transform.localPosition=pos;LigarVida(go,vida);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Vida+"/Gerados/Pintura_V22.mat");
            var visual=go.GetComponent<MTMVisualV2>();if(visual!=null&&visual.desenho!=null)visual.desenho.sharedMaterial=mat;
        }
        private static void Morador(MTMRegiao r,MTMVidaV22 vida,string arte,string nome,Vector2 pos,string[] falas)
        {
            var go=Novo(nome,r.transform);go.transform.localPosition=pos;
            var sr=Novo("Desenho",go.transform).AddComponent<SpriteRenderer>();sr.spriteSortPoint=SpriteSortPoint.Pivot;sr.gameObject.AddComponent<MTMOrdemY>();
            var tinta=AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Vida+"/Gerados/Pintura_V22.mat");sr.sharedMaterial=tinta;
            var quadros=new[]{MTMArtesLugaresV23.Sprite(arte+"_0"),MTMArtesLugaresV23.Sprite(arte+"_1"),MTMArtesLugaresV23.Sprite(arte+"_2")};sr.sprite=quadros[0];
            var a=sr.gameObject.AddComponent<MTMAtorV22>();a.tipo=MTMAtorV22.Tipo.Morador;a.quadros=quadros;a.vida=vida;a.jogo=vida.jogo;a.nomeDaConversa=nome;
            var d=sr.gameObject.AddComponent<MTMDeformacaoV22>();d.tipo=MTMDeformacaoV22.Tipo.Pessoa;d.material=tinta;d.vida=vida;d.escalaCabeca=.95f;d.alongamentoCorpo=1;d.fixarCorpo=true;d.amplitude=.008f;
            var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(.42f,.25f);c.offset=new Vector2(0,.12f);
            var ss=Novo("Sombra",go.transform).AddComponent<SpriteRenderer>();ss.sprite=sr.sprite;ss.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Vida+"/Gerados/Sombra_V22.mat");ss.sortingOrder=-22000;ss.color=new Color(.1f,.13f,.15f,.2f);ss.transform.localScale=new Vector3(1,.2f,1);
            var v=go.AddComponent<MTMVisualV2>();v.desenho=sr;v.sombra=ss;
            var p=Novo("Interacao_npc_v23_"+arte,go.transform).AddComponent<MTMPonto>();
            p.dados=new MTMPontoDados{id="npc_v23_"+arte,nome=nome,tipo="conversa",evento="v23_"+arte,x=pos.x,y=pos.y,falas=falas,fonte="Personagem e falas ficcionais criados para o jogo. Não representam depoimento de uma pessoa real."};
            p.retrato=quadros[0].texture;Rect rec=quadros[0].rect;
            p.recorteRosto=new Rect((rec.x+rec.width*.25f)/p.retrato.width,(rec.y+rec.height*.70f)/p.retrato.height,rec.width*.52f/p.retrato.width,rec.height*.28f/p.retrato.height);
            var lista=new List<MTMPonto>(r.pontos);lista.Add(p);r.pontos=lista.ToArray();
        }
        public static void MoverPonto(MTMRegiao r,MTMPonto p,Vector2 pos)
        {
            Vector2 delta=pos-(Vector2)p.transform.position;
            var visual=p.GetComponentInParent<MTMVisualV2>();
            if(visual!=null&&visual.transform.IsChildOf(r.transform))MoverRaiz(visual.transform,(Vector2)visual.transform.position+delta);
            else p.transform.position=new Vector3(pos.x,pos.y,p.transform.position.z);
            p.dados.x=p.transform.position.x;p.dados.y=p.transform.position.y;EditorUtility.SetDirty(p);
        }
        private static void MoverRaiz(Transform t,Vector2 pos)
        {
            var c=t.GetComponent<MTMCaminhanteV22>();Vector2 delta=t.parent==null?pos-(Vector2)t.position:(Vector2)t.parent.InverseTransformPoint(pos)-(Vector2)t.localPosition;
            t.position=new Vector3(pos.x,pos.y,t.position.z);
            if(c!=null&&c.pontos!=null)for(int i=0;i<c.pontos.Length;i++)c.pontos[i]+=delta;
        }
        public static void SincronizarPontos(MTMRegiao r)
        {r.dados.pontos=(r.pontos??Array.Empty<MTMPonto>()).Where(p=>p!=null&&p.dados!=null).Select(p=>Copia(p.dados)).ToArray();EditorUtility.SetDirty(r);}
        private static GameObject Novo(string nome,Transform pai){var go=new GameObject(nome);go.transform.SetParent(pai,false);return go;}
        private static void Parede(Transform pai,string nome,float x,float y,float w,float h)
        {var go=Novo(nome,pai);go.transform.localPosition=new Vector3(x,y,0);go.AddComponent<BoxCollider2D>().size=new Vector2(w,h);}
    }
}

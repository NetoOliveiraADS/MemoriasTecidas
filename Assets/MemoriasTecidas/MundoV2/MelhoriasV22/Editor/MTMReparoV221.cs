using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMReparoV221
    {
        private const string Raiz="Assets/MemoriasTecidas/MundoV2/MelhoriasV22";
        private const string Gerados=Raiz+"/Gerados";
        private static readonly HashSet<string> Predios=new HashSet<string>{"igreja","prefeitura","mercado","fabrica","casario","casa_azul","casa_ocre","sobrado_comercio","oficina_varanda","memorial","quiosque","coreto"};
        [Serializable] private sealed class Quadro{public string textura;public float x,y,largura,altura,pivotX,pivotY;}
        [Serializable] private sealed class Folha{public float alturaMundo;public Quadro[] quadros;}

        [MenuItem("Memorias Tecidas/Vida V2.2/5 - Corrigir pernas, saidas, vento e predios (V2.2.1)")]
        public static void Corrigir()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Saia do Play antes de aplicar a correção.");return;}
            string backup="";
            try
            {
                var jogo=EncontrarJogo();var vida=jogo.GetComponent<MTMVidaV22>();
                if(vida==null||vida.versaoInstalada<22)throw new InvalidOperationException("Instale primeiro a Vida V2.2 com os passos 1 e 2. Não execute o passo 3 para aumentar casas.");
                foreach(var r in jogo.regioes)
                {
                    if(r==null||r.dados==null||r.pontos==null)throw new InvalidOperationException("Região ou lista de pontos ausente; confira MTMJogo/Regioes.");
                    if(r.transform.position.sqrMagnitude>.001f||(r.transform.lossyScale-Vector3.one).sqrMagnitude>.001f||Quaternion.Angle(r.transform.rotation,Quaternion.identity)>.01f)
                        throw new InvalidOperationException("A revisão de caminhos requer regiões na origem e escala 1, como no Mundo V2.1. Nenhuma alteração foi iniciada.");
                }
                if(!EditorUtility.DisplayDialog("Correção V2.2.1","Cria backup, troca a animação do explorador, fixa o corpo contra o vento, aumenta a largura/altura das construções e reposiciona saídas/chegadas em caminhos livres. As dimensões das regiões serão mantidas. Aguarde a análise de colisões terminar.","Corrigir com backup","Cancelar"))return;
                backup=FazerBackup(jogo.gameObject.scene);
                var log=new StringBuilder("Memórias Tecidas — reparo V2.2.1\n");log.AppendLine("Backup: "+backup);
                Sprite[] frames=PrepararQuadros();
                var desenho=jogo.transform.Find("Desenho");if(desenho==null)throw new InvalidOperationException("Jogador sem filho Desenho.");
                foreach(var velho in jogo.GetComponentsInChildren<MTMAnimacao>(true))velho.enabled=false;
                var ator=desenho.GetComponent<MTMAtorV22>();if(ator==null)ator=desenho.gameObject.AddComponent<MTMAtorV22>();
                ator.enabled=true;ator.tipo=MTMAtorV22.Tipo.Jogador;ator.jogo=jogo;ator.vida=vida;ator.quadros=frames;ator.cicloCompleto=true;
                ator.fpsCaminhada=8;ator.fpsCorrida=12;
                Material tinta=MTMMaterialVidaV22.Preparar(Raiz,"Pintura_V22");
                var sr=desenho.GetComponent<SpriteRenderer>();sr.sprite=frames[0];sr.sharedMaterial=tinta;
                var corpo=jogo.GetComponent<MTMVisualV2>();if(corpo!=null){corpo.vento=0;corpo.respirar=false;}
                var deform=desenho.GetComponent<MTMDeformacaoV22>();if(deform!=null)Fixar(deform,true);
                var travessia=jogo.GetComponent<MTMTravessiaV221>();if(travessia==null)travessia=jogo.gameObject.AddComponent<MTMTravessiaV221>();travessia.jogo=jogo;
                int predios=0;
                foreach(var r in jogo.regioes)
                {
                    foreach(var d in r.GetComponentsInChildren<MTMDeformacaoV22>(true))if(d.tipo==MTMDeformacaoV22.Tipo.Pessoa)Fixar(d,false);
                    foreach(var v in r.GetComponentsInChildren<MTMVisualV2>(true))
                    {
                        if(v.desenho==null||v.desenho.sprite==null)continue;
                        string id=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(v.desenho.sprite.texture));
                        if(!Predios.Contains(id))continue;
                        var escala=v.GetComponent<MTMEscalaConstrucaoV221>();
                        if(escala==null)escala=v.gameObject.AddComponent<MTMEscalaConstrucaoV221>();
                        escala.desenho=v.desenho;escala.sombra=v.sombra;escala.Inicializar();escala.Aplicar();predios++;
                    }
                }
                log.AppendLine("Construções com controles próprios de escala: "+predios);
                RevisarCaminhos(jogo,vida,tinta,log);
                vida.versaoInstalada=221;
                Salvar(jogo.gameObject.scene);
                Directory.CreateDirectory("MemoriasTecidas_Backups/V221");
                string relatorio="MemoriasTecidas_Backups/V221/Reparo_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".txt";
                File.WriteAllText(relatorio,log.ToString());
                Selection.activeGameObject=jogo.gameObject;
                Debug.Log(log+"\nCorreção salva. Entre no círculo da saída ou aproxime-se e pressione E. Relatório: "+relatorio,jogo);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                if(backup.Length>0)Debug.LogWarning("A correção foi interrompida. Abra o backup antes de repetir: "+backup);
            }
            finally{EditorUtility.ClearProgressBar();}
        }
        private static void Fixar(MTMDeformacaoV22 d,bool jogador)
        {
            d.fixarCorpo=true;d.ventoNoCabelo=.004f;d.respiracao=jogador?0f:.001f;
            d.transform.localRotation=Quaternion.identity;
            var v=d.GetComponentInParent<MTMVisualV2>();if(v!=null){v.vento=0;v.respirar=false;}
        }
        private static MTMJogo EncontrarJogo()
        {
            var cena=SceneManager.GetActiveScene();MTMJogo jogo=null;
            foreach(var j in UnityEngine.Object.FindObjectsByType<MTMJogo>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(j.gameObject.scene==cena){if(jogo!=null)throw new InvalidOperationException("Há dois MTMJogo na cena. Remova a duplicação antes de aplicar.");jogo=j;}
            if(jogo==null||jogo.regioes==null||jogo.regioes.Length==0)throw new InvalidOperationException("Abra a cena jogável do Mundo V2.1/V2.2.");
            if(string.IsNullOrEmpty(cena.path))throw new InvalidOperationException("Salve a cena com Ctrl+S primeiro.");return jogo;
        }
        private static string FazerBackup(Scene cena)
        {
            const string dir="Assets/MemoriasTecidas/Backups_V221";Directory.CreateDirectory(dir);AssetDatabase.Refresh();
            string path=AssetDatabase.GenerateUniqueAssetPath(dir+"/"+Path.GetFileNameWithoutExtension(cena.path)+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");
            if(!EditorSceneManager.SaveScene(cena,path,true))throw new IOException("Backup não pôde ser salvo.");return path;
        }
        private static void Salvar(Scene cena)
        {
            foreach(var root in cena.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Component>(true))
            {
                if(c==null)continue;EditorUtility.SetDirty(c);
                if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(cena);
            if(!EditorSceneManager.SaveScene(cena))throw new IOException("Não foi possível salvar a cena; salve manualmente.");
        }
        private static Sprite[] PrepararQuadros()
        {
            string arquivo=Raiz+"/Arte/jogador_recortes_221.json";
            var dados=JsonUtility.FromJson<Folha>(File.ReadAllText(arquivo));
            if(dados==null||dados.quadros==null||dados.quadros.Length!=32)throw new IOException("Faltam os 32 quadros corrigidos.");
            Directory.CreateDirectory(Gerados+"/Sprites221");AssetDatabase.Refresh();
            var texturas=new Dictionary<string,Texture2D>();float maior=0;
            foreach(var q in dados.quadros)
            {
                maior=Mathf.Max(maior,q.altura);if(texturas.ContainsKey(q.textura))continue;
                string path=Raiz+"/Arte/"+q.textura;var imp=AssetImporter.GetAtPath(path) as TextureImporter;
                if(imp==null)throw new IOException("Imagem ausente: "+path);
                imp.textureType=TextureImporterType.Default;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;
                imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=4096;imp.filterMode=FilterMode.Bilinear;
                imp.textureCompression=TextureImporterCompression.Uncompressed;imp.wrapMode=TextureWrapMode.Clamp;imp.SaveAndReimport();
                texturas[q.textura]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            var sprites=new Sprite[32];
            for(int i=0;i<32;i++)
            {
                string path=Gerados+"/Sprites221/jogador_"+i.ToString("00")+".asset";
                var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(s==null)
                {
                    var q=dados.quadros[i];s=Sprite.Create(texturas[q.textura],new Rect(q.x,q.y,q.largura,q.altura),new Vector2(q.pivotX,q.pivotY),maior/dados.alturaMundo,0,SpriteMeshType.FullRect);
                    s.name="Jogador_221_"+i.ToString("00");AssetDatabase.CreateAsset(s,path);
                }
                sprites[i]=s;
            }
            return sprites;
        }

        private static void RevisarCaminhos(MTMJogo jogo,MTMVidaV22 vida,Material tinta,StringBuilder log)
        {
            var ativos=new bool[jogo.regioes.Length];var malhas=new Dictionary<string,Grade>();
            for(int i=0;i<ativos.Length;i++)ativos[i]=jogo.regioes[i].gameObject.activeSelf;
            Vector2 tamanho=new Vector2(.56f,.37f),offset=new Vector2(0,.12f);
            var col=jogo.GetComponent<Collider2D>();
            if(col!=null&&col.bounds.size.sqrMagnitude>.01f)
            {tamanho=(Vector2)col.bounds.size+new Vector2(.14f,.12f);offset=(Vector2)(col.bounds.center-jogo.transform.position);}
            try
            {
                for(int i=0;i<jogo.regioes.Length;i++)
                {
                    var r=jogo.regioes[i];EditorUtility.DisplayProgressBar("Vida V2.2.1","Conferindo caminhos: "+r.dados.nome,i/(float)jogo.regioes.Length);
                    foreach(var outra in jogo.regioes)outra.gameObject.SetActive(outra==r);Physics2D.SyncTransforms();
                    var grade=new Grade(r,jogo,tamanho,offset);malhas.Add(r.dados.id,grade);
                    // Dados de cena podem estar compartilhados com os pontos; separa antes de editar.
                    r.dados=JsonUtility.FromJson<MTMAreaDados>(JsonUtility.ToJson(r.dados));
                    var seguro=grade.Perto(new Vector2(r.dados.seguroX,r.dados.seguroY),null,0);
                    r.dados.seguroX=seguro.x;r.dados.seguroY=seguro.y;
                    var usados=new List<Vector2>();
                    foreach(var p in r.pontos)
                    {
                        if(p==null||p.dados==null||p.dados.tipo!="saida")continue;
                        p.dados=JsonUtility.FromJson<MTMPontoDados>(JsonUtility.ToJson(p.dados));
                        Vector2 antes=p.transform.position;var nova=grade.Perto(antes,usados,1.6f);usados.Add(nova);
                        p.transform.position=new Vector3(nova.x,nova.y,p.transform.position.z);
                        p.dados.x=nova.x;p.dados.y=nova.y;p.alcance=Mathf.Max(2.2f,p.alcance);
                        var marca=p.GetComponent<MTMMarcadorSaidaV221>();if(marca==null)marca=p.gameObject.AddComponent<MTMMarcadorSaidaV221>();marca.vida=vida;marca.material=tinta;
                        AtualizarPontoDados(r,p);
                        log.AppendLine("Saída "+p.dados.id+": "+antes+" -> "+nova+"; caminho livre desde o ponto seguro.");
                        if(Vector2.Distance(antes,nova)>4f)Debug.LogWarning("Saída deslocada mais de 4 unidades por obstáculos: "+p.dados.id+". Confira o layout.",p);
                    }
                    grade.saidas=usados;
                }
                foreach(var r in jogo.regioes)foreach(var p in r.pontos)
                {
                    if(p==null||p.dados==null||p.dados.tipo!="saida")continue;
                    Grade destino;if(!malhas.TryGetValue(p.dados.destino,out destino))throw new InvalidOperationException("Destino ausente: "+p.dados.id);
                    var chegada=destino.Perto(new Vector2(p.dados.chegadaX,p.dados.chegadaY),destino.saidas,1.4f);
                    p.dados.chegadaX=chegada.x;p.dados.chegadaY=chegada.y;AtualizarPontoDados(r,p);
                    log.AppendLine("Chegada "+p.dados.id+": "+chegada+"; fora dos obstáculos e afastada das saídas.");
                }
            }
            finally
            {for(int i=0;i<ativos.Length;i++)jogo.regioes[i].gameObject.SetActive(ativos[i]);Physics2D.SyncTransforms();}
        }
        private static void AtualizarPontoDados(MTMRegiao r,MTMPonto p)
        {
            if(r.dados.pontos==null)return;
            for(int i=0;i<r.dados.pontos.Length;i++)if(r.dados.pontos[i].id==p.dados.id)
                r.dados.pontos[i]=JsonUtility.FromJson<MTMPontoDados>(JsonUtility.ToJson(p.dados));
        }
        private sealed class Grade
        {
            private const float Passo=.5f;
            private readonly List<Vector2> acessiveis=new List<Vector2>();
            public List<Vector2> saidas;
            public Grade(MTMRegiao regiao,MTMJogo jogo,Vector2 tamanho,Vector2 offset)
            {
                float minX=-regiao.dados.largura*.5f+.65f,minY=-regiao.dados.altura*.5f+.65f;
                int nx=Mathf.FloorToInt((regiao.dados.largura-1.3f)/Passo)+1,ny=Mathf.FloorToInt((regiao.dados.altura-1.3f)/Passo)+1;
                if(nx<2||ny<2||(long)nx*ny>80000)throw new InvalidOperationException("Dimensões fora do limite da revisão: "+regiao.dados.id);
                var livre=new bool[nx*ny];var visto=new bool[nx*ny];var encontrados=new List<Collider2D>(16);
                var filtro=new ContactFilter2D();filtro.SetLayerMask(Physics2D.GetLayerCollisionMask(jogo.gameObject.layer));filtro.useTriggers=false;
                Vector2 seguro=new Vector2(regiao.dados.seguroX,regiao.dados.seguroY);int semente=-1;float melhor=float.PositiveInfinity;
                for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)
                {
                    int indice=y*nx+x;Vector2 ponto=new Vector2(minX+x*Passo,minY+y*Passo);bool bloqueado=false;
                    encontrados.Clear();Physics2D.OverlapBox(ponto+offset,tamanho,0f,filtro,encontrados);
                    foreach(var c in encontrados)
                    {
                        if(c==null||c.transform==jogo.transform||c.transform.IsChildOf(jogo.transform)||c.GetComponentInParent<MTMAtorV22>()!=null)continue;
                        // Os colliders dos moradores ficam no pai, enquanto o ator fica no filho Desenho.
                        if(c.GetComponent<MTMVisualV2>()!=null&&c.GetComponentInChildren<MTMAtorV22>(true)!=null)continue;
                        bloqueado=true;break;
                    }
                    livre[indice]=!bloqueado;
                    float d=(ponto-seguro).sqrMagnitude;if(!bloqueado&&d<melhor){melhor=d;semente=indice;}
                }
                if(semente<0)throw new InvalidOperationException("Não há chão livre na região "+regiao.dados.id+". Confira os colliders antes de aplicar.");
                var fila=new Queue<int>();fila.Enqueue(semente);visto[semente]=true;
                while(fila.Count>0)
                {
                    int n=fila.Dequeue(),x=n%nx,y=n/nx;acessiveis.Add(new Vector2(minX+x*Passo,minY+y*Passo));
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        if(dx==0&&dy==0)continue;int xx=x+dx,yy=y+dy;if(xx<0||xx>=nx||yy<0||yy>=ny)continue;
                        int v=yy*nx+xx;if(!livre[v]||visto[v])continue;
                        if(dx!=0&&dy!=0&&(!livre[y*nx+xx]||!livre[yy*nx+x]))continue;
                        visto[v]=true;fila.Enqueue(v);
                    }
                }
                if(acessiveis.Count<12)throw new InvalidOperationException("O ponto seguro de "+regiao.dados.id+" está cercado; reveja os colliders.");
            }
            public Vector2 Perto(Vector2 alvo,List<Vector2> evitar,float distancia)
            {
                float melhor=float.PositiveInfinity;Vector2 resultado=Vector2.zero;bool achou=false;
                foreach(var p in acessiveis)
                {
                    bool ocupado=false;if(evitar!=null)foreach(var e in evitar)if((p-e).sqrMagnitude<distancia*distancia){ocupado=true;break;}
                    if(ocupado)continue;float d=(p-alvo).sqrMagnitude;if(d<melhor){melhor=d;resultado=p;achou=true;}
                }
                if(!achou)throw new InvalidOperationException("Não há espaço suficiente para separar as saídas/chegadas nesta região.");return resultado;
            }
        }
    }
}

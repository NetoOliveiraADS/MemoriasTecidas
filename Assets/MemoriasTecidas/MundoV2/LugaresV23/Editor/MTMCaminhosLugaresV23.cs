using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMCaminhosLugaresV23
    {
        [MenuItem("Memorias Tecidas/Lugares V2.3/4 - Revisar caminhos apos editar")]
        public static void MenuRevisar()
        {
            string original=null;bool iniciou=false;
            try
            {
                var jogo=MTMMontagemLugaresV23.JogoDaCena();var cena=jogo.gameObject.scene;
                if(!jogo.regioes.Any(r=>r.GetComponent<MTMRegiaoVisualV23>()!=null))throw new InvalidOperationException("Monte os locais pelo menu 2 primeiro.");
                if(!EditorSceneManager.SaveScene(cena))return;
                original=cena.path;
                Directory.CreateDirectory("Assets/MemoriasTecidas/Backups_V23");AssetDatabase.Refresh();
                string backup="Assets/MemoriasTecidas/Backups_V23/Caminhos_"+DateTime.Now.ToString("yyyyMMdd_HHmmssfff")+".unity";
                if(!EditorSceneManager.SaveScene(cena,backup,true))throw new IOException("Falha no backup dos caminhos.");
                var log=new StringBuilder("REVISÃO DE CAMINHOS V2.3\nBackup: "+backup+"\n");iniciou=true;
                Revisar(jogo,log);EditorSceneManager.MarkSceneDirty(cena);
                if(!EditorSceneManager.SaveScene(cena))throw new IOException("Falha ao salvar a revisão.");
                iniciou=false;
                File.WriteAllText(MTMArtesLugaresV23.Raiz+"/Gerados/Relatorio_Caminhos.txt",log.ToString());AssetDatabase.Refresh();
                Debug.Log("Caminhos revisados. Leia Gerados/Relatorio_Caminhos.txt e teste as travessias no Play.");
            }
            catch(Exception e)
            {Debug.LogException(e);if(iniciou&&!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original,OpenSceneMode.Single);}
            finally{EditorUtility.ClearProgressBar();}
        }
        public static void Revisar(MTMJogo jogo,StringBuilder log)
        {
            var vida=jogo.GetComponent<MTMVidaV22>();if(vida==null)throw new InvalidOperationException("Vida V2.2.1 ausente.");
            var ativos=jogo.regioes.Select(r=>r.gameObject.activeSelf).ToArray();
            var grades=new Dictionary<string,Grade>();var regioes=jogo.regioes.ToDictionary(r=>r.dados.id);
            Vector2 tamanho=new Vector2(.56f,.39f),offset=new Vector2(0,.12f);
            var col=jogo.GetComponent<Collider2D>();Physics2D.SyncTransforms();
            if(col!=null&&col.bounds.size.sqrMagnitude>.01f){tamanho=(Vector2)col.bounds.size+new Vector2(.14f,.14f);offset=(Vector2)(col.bounds.center-jogo.transform.position);}
            var tinta=AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Vida+"/Gerados/Pintura_V22.mat");
            try
            {
                foreach(var r in jogo.regioes)
                {
                    if(r.GetComponent<MTMRegiaoVisualV23>()==null)continue;
                    EditorUtility.DisplayProgressBar("Caminhos V2.3","Calculando área acessível: "+r.dados.nome,grades.Count/4f);
                    foreach(var outra in jogo.regioes)outra.gameObject.SetActive(outra==r);
                    foreach(var s in r.GetComponentsInChildren<MTMSuperficieV23>())s.Reconstruir();
                    Physics2D.SyncTransforms();
                    var g=new Grade(r,jogo,tamanho,offset);grades.Add(r.dados.id,g);
                    Vector2 seguro=g.Perto(new Vector2(r.dados.seguroX,r.dados.seguroY));r.dados.seguroX=seguro.x;r.dados.seguroY=seguro.y;
                    var usados=new List<Vector2>();
                    foreach(var p in r.pontos??Array.Empty<MTMPonto>())
                    {
                        if(p==null||p.dados==null)continue;p.dados=MTMMontagemLugaresV23.Copia(p.dados);
                        Vector2 antes=p.transform.position;bool saida=p.dados.tipo=="saida";
                        Vector2 nova=g.Perto(antes,saida?usados:null,1.8f);
                        MTMMontagemLugaresV23.MoverPonto(r,p,nova);
                        if(saida)
                        {
                            usados.Add(nova);p.alcance=Mathf.Max(p.alcance,2.2f);
                            var marca=p.GetComponent<MTMMarcadorSaidaV221>();if(marca==null)marca=p.gameObject.AddComponent<MTMMarcadorSaidaV221>();marca.material=tinta;marca.vida=vida;
                        }
                        float dist=Vector2.Distance(antes,nova);
                        log.AppendLine(p.dados.id+": acessível; deslocamento "+dist.ToString("F2")+"; posição "+nova);
                        if(dist>2f)Debug.LogWarning("Interação "+p.dados.id+" deslocada "+dist.ToString("F1")+" unidades para chão acessível. Confira a posição visual.",p);
                    }
                    g.saidas=usados;
                    foreach(var c in r.GetComponentsInChildren<MTMCaminhanteV22>())
                    {
                        Vector2 centro=g.Perto(c.transform.position),esq=centro-Vector2.right*(c.animal?2.1f:1.1f),dir=centro+Vector2.right*(c.animal?2.1f:1.1f);
                        var rota=new List<Vector2>{centro};
                        if(g.SegmentoLivre(centro,dir))rota.Add(dir);
                        rota.Add(centro);if(g.SegmentoLivre(centro,esq))rota.Add(esq);
                        c.transform.position=new Vector3(centro.x,centro.y,c.transform.position.z);
                        c.pontos=rota.Select(p=>c.transform.parent!=null?(Vector2)c.transform.parent.InverseTransformPoint(p):p).ToArray();EditorUtility.SetDirty(c);
                        log.AppendLine("Rota de "+c.name+": "+(rota.Distinct().Count()>1?"segmentos livres":"sem espaço lateral; repouso até edição manual"));
                    }
                    // Pontos nos filhos dos NPCs acompanham o reposicionamento final das rotas.
                    foreach(var p in r.pontos)if(p!=null&&p.dados!=null){p.dados.x=p.transform.position.x;p.dados.y=p.transform.position.y;}
                    MTMMontagemLugaresV23.SincronizarPontos(r);
                    log.AppendLine("Células acessíveis em "+r.dados.id+": "+g.Contagem);
                }
                // Revisar também entradas que vêm de regiões não reconstruídas.
                foreach(var origem in jogo.regioes)
                {
                    foreach(var p in origem.pontos??Array.Empty<MTMPonto>())
                    {
                        if(p==null||p.dados==null||p.dados.tipo!="saida")continue;
                        if(!regioes.ContainsKey(p.dados.destino))throw new InvalidOperationException("Destino inexistente: "+p.dados.id);
                        if(!grades.TryGetValue(p.dados.destino,out var g))continue;
                        var destino=regioes[p.dados.destino];var volta=Array.Find(destino.pontos,x=>x!=null&&x.dados.tipo=="saida"&&x.dados.destino==origem.dados.id);
                        Vector2 seguro=new Vector2(destino.dados.seguroX,destino.dados.seguroY),alvo=seguro;
                        if(volta!=null)
                        {Vector2 v=volta.transform.position,direcao=(seguro-v).normalized;if(direcao.sqrMagnitude<.1f)direcao=Vector2.down;alvo=v+direcao*2.5f;}
                        Vector2 chegada=g.Perto(alvo,g.saidas,1.5f);
                        p.dados=MTMMontagemLugaresV23.Copia(p.dados);p.dados.chegadaX=chegada.x;p.dados.chegadaY=chegada.y;EditorUtility.SetDirty(p);
                        log.AppendLine("Chegada "+p.dados.id+" -> "+p.dados.destino+": "+chegada+"; livre e fora dos círculos de saída.");
                    }
                    MTMMontagemLugaresV23.SincronizarPontos(origem);
                }
            }
            finally
            {for(int i=0;i<ativos.Length;i++)jogo.regioes[i].gameObject.SetActive(ativos[i]);Physics2D.SyncTransforms();EditorUtility.ClearProgressBar();}
        }
        private sealed class Grade
        {
            private const float Passo=.5f;
            private readonly List<Vector2> acessiveis=new List<Vector2>();
            private readonly MTMJogo jogo;
            private readonly MTMRegiao regiao;
            private readonly Vector2 tamanho,offset;
            private readonly List<Collider2D> contatos=new List<Collider2D>(16);
            private ContactFilter2D filtro;
            public List<Vector2> saidas=new List<Vector2>();
            public int Contagem{get{return acessiveis.Count;}}
            public Grade(MTMRegiao r,MTMJogo j,Vector2 t,Vector2 o)
            {
                regiao=r;jogo=j;tamanho=t;offset=o;filtro.SetLayerMask(Physics2D.GetLayerCollisionMask(j.gameObject.layer));filtro.useTriggers=false;
                float minX=-r.dados.largura*.5f+.7f,minY=-r.dados.altura*.5f+.7f;
                int nx=Mathf.FloorToInt((r.dados.largura-1.4f)/Passo)+1,ny=Mathf.FloorToInt((r.dados.altura-1.4f)/Passo)+1;
                if(nx<2||ny<2||(long)nx*ny>80000)throw new InvalidOperationException("Dimensão fora do limite da revisão: "+r.dados.id);
                var livre=new bool[nx*ny];var visto=new bool[nx*ny];int semente=-1;float melhor=float.PositiveInfinity;
                Vector2 seguro=new Vector2(r.dados.seguroX,r.dados.seguroY);
                for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)
                {
                    int i=y*nx+x;Vector2 p=new Vector2(minX+x*Passo,minY+y*Passo);livre[i]=Livre(p);
                    float d=(p-seguro).sqrMagnitude;if(livre[i]&&d<melhor){melhor=d;semente=i;}
                }
                if(semente<0)throw new InvalidOperationException("Nenhum chão livre em "+r.dados.id);
                var fila=new Queue<int>();fila.Enqueue(semente);visto[semente]=true;
                while(fila.Count>0)
                {
                    int n=fila.Dequeue(),x=n%nx,y=n/nx;acessiveis.Add(new Vector2(minX+x*Passo,minY+y*Passo));
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        if(dx==0&&dy==0)continue;int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=nx||yy>=ny)continue;
                        int k=yy*nx+xx;if(!livre[k]||visto[k])continue;
                        if(dx!=0&&dy!=0&&(!livre[y*nx+xx]||!livre[yy*nx+x]))continue;
                        visto[k]=true;fila.Enqueue(k);
                    }
                }
                if(acessiveis.Count<12)throw new InvalidOperationException("Ponto seguro cercado em "+r.dados.id);
            }
            private bool Livre(Vector2 p)
            {
                if(Mathf.Abs(p.x)>regiao.dados.largura*.5f-.6f||Mathf.Abs(p.y)>regiao.dados.altura*.5f-.6f)return false;
                contatos.Clear();Physics2D.OverlapBox(p+offset,tamanho,0,filtro,contatos);
                foreach(var c in contatos)
                {
                    if(c==null||c.transform==jogo.transform||c.transform.IsChildOf(jogo.transform))continue;
                    if(c.GetComponentInParent<MTMAtorV22>()!=null||c.GetComponentInChildren<MTMAtorV22>(true)!=null)continue;
                    return false;
                }
                return true;
            }
            public bool SegmentoLivre(Vector2 a,Vector2 b)
            {int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.1f));for(int i=0;i<=n;i++)if(!Livre(Vector2.Lerp(a,b,i/(float)n)))return false;return true;}
            public Vector2 Perto(Vector2 alvo,List<Vector2> evitar=null,float distancia=0)
            {
                float menor=float.PositiveInfinity;Vector2 resultado=Vector2.zero;bool encontrou=false;
                foreach(var p in acessiveis)
                {
                    bool ocupado=false;if(evitar!=null)foreach(var e in evitar)if((p-e).sqrMagnitude<distancia*distancia){ocupado=true;break;}
                    if(ocupado)continue;float d=(p-alvo).sqrMagnitude;if(d<menor){menor=d;resultado=p;encontrou=true;}
                }
                if(!encontrou)throw new InvalidOperationException("Sem posição acessível separada das saídas em "+regiao.dados.id);return resultado;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMMontador
    {
        private const string Raiz = "Assets/MemoriasTecidas/MundoV2";
        private const string Gerados = Raiz + "/Gerados";
        private static readonly HashSet<string> VolumesAltos = new HashSet<string>
        {
            "igreja", "casario", "casa_azul", "casa_ocre", "sobrado_comercio",
            "oficina_varanda", "fabrica", "mercado", "prefeitura", "memorial",
            "coreto", "quiosque", "arvore", "carnauba_adulta", "letreiro"
        };

        [MenuItem("Memorias Tecidas/Mundo V2/1 - Preparar arte e prefabs")]
        public static void PrepararMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { Preparar(Ler()); Debug.Log("Mundo V2: arte e prefabs preparados em " + Gerados); }
            catch (Exception e) { Debug.LogException(e); }
        }

        [MenuItem("Memorias Tecidas/Mundo V2/2 - Criar mundo conectado")]
        public static void CriarMundo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Saia do Play para criar a cena."); return; }
            try
            {
                MTMDados dados = Ler(); Validar(dados);
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Preparar(dados);
                Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var raiz = new GameObject("Memorias_Tecidas_Mundo_Conectado");
                var regioes = new List<MTMRegiao>();
                foreach (MTMAreaDados area in dados.areas)
                {
                    var go = new GameObject(area.id + "_" + area.nome); go.transform.SetParent(raiz.transform, false);
                    var regiao = go.AddComponent<MTMRegiao>(); regiao.dados = area;
                    Piso(area.piso,go.transform,Vector2.zero,new Vector2(area.largura,area.altura),-30000,dados);
                    if(area.pisos!=null)foreach(MTMPisoDados p in area.pisos)
                        Piso(p.arte,go.transform,new Vector2(p.x,p.y),new Vector2(p.largura,p.altura),p.ordem,dados);
                    var aparicoes = new List<GameObject>();
                    foreach (MTMObjetoDados obj in area.objetos)
                    {
                        GameObject item = Instanciar(obj.arte, go.transform, new Vector2(obj.x,obj.y),obj.escala,obj.semColisao);
                        if (!string.IsNullOrEmpty(obj.nome)) item.name = obj.nome;
                        if (obj.noturno) { aparicoes.Add(item); item.SetActive(false); }
                    }
                    var pontos = new List<MTMPonto>();
                    foreach (MTMPontoDados p in area.pontos)
                    {
                        var nodo = new GameObject("Interacao_" + p.id);
                        Transform visual = p.id.StartsWith("npc_", StringComparison.Ordinal) ? go.transform.Find(p.nome) : null;
                        nodo.transform.SetParent(visual != null ? visual : go.transform,false);
                        nodo.transform.position = new Vector3(p.x,p.y,0f);
                        var componente = nodo.AddComponent<MTMPonto>(); componente.dados=p; pontos.Add(componente);
                        if(visual!=null)
                        {
                            var arte=Array.Find(dados.artes,a=>"npc_"+a.id==p.id);
                            if(arte!=null)
                            {
                                componente.retrato=AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz+"/"+arte.arquivo);
                                componente.recorteRosto=new Rect(arte.rostoX,arte.rostoY,arte.rostoW,arte.rostoH);
                            }
                        }
                    }
                    regiao.pontos=pontos.ToArray(); regiao.aparicoes=aparicoes.ToArray();
                    Parede(go.transform,"Limite_Norte",new Vector2(0,area.altura*.5f-.2f),new Vector2(area.largura,.4f));
                    Parede(go.transform,"Limite_Sul",new Vector2(0,-area.altura*.5f+.2f),new Vector2(area.largura,.4f));
                    Parede(go.transform,"Limite_Oeste",new Vector2(-area.largura*.5f+.2f,0),new Vector2(.4f,area.altura));
                    Parede(go.transform,"Limite_Leste",new Vector2(area.largura*.5f-.2f,0),new Vector2(.4f,area.altura));
                    if (area.interior) Parede(go.transform,"Parede_Do_Interior",new Vector2(0,7.4f),new Vector2(32,3.2f));
                    if (area.agua != null && area.agua.Length >= 3)
                    {
                        var agua=new GameObject("Margem_Intransponivel"); agua.transform.SetParent(go.transform,false);
                        var poly=agua.AddComponent<PolygonCollider2D>(); var path=new Vector2[area.agua.Length];
                        for(int i=0;i<path.Length;i++) path[i]=new Vector2(area.agua[i].x,area.agua[i].y);
                        poly.points=path;
                    }
                    CriarAmbiente(go.transform,area);
                    regioes.Add(regiao); go.SetActive(area.id=="praca");
                }
                var camGo=new GameObject("Main Camera"); camGo.tag="MainCamera";
                var cam=camGo.AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=8f;
                cam.transform.position=new Vector3(0,0,-10); cam.clearFlags=CameraClearFlags.SolidColor;
                cam.backgroundColor=new Color(.035f,.065f,.065f); cam.nearClipPlane=.1f; cam.farClipPlane=100f;
                camGo.AddComponent<AudioListener>();
                var jogador=CriarJogador(dados,MaterialPrincipal());
                foreach (var volume in raiz.GetComponentsInChildren<MTMOclusaoV2>(true))
                    volume.alvo = jogador.transform;
                var jogo=jogador.AddComponent<MTMJogo>(); jogo.cameraDoMapa=cam; jogo.regioes=regioes.ToArray();
                jogo.ambienteVisual=raiz.AddComponent<MTMAmbienteV2>();
                Shader.SetGlobalFloat("_MTV_Active",0f);Shader.SetGlobalInt("_MTV_Count",0);
                string destino=AssetDatabase.GenerateUniqueAssetPath(Gerados+"/Cenas/Jaguaruana_Mundo_Conectado.unity");
                if (!EditorSceneManager.SaveScene(cena,destino)) throw new IOException("Não foi possível salvar a cena.");
                Selection.activeGameObject=jogador;
                Debug.Log("Mundo criado em "+destino+". Abra Game em 16:9 e pressione Play. Converse com Neto usando E. M abre as rotas.");
            }
            catch(Exception e) { Debug.LogException(e); }
        }

        [MenuItem("Memorias Tecidas/Mundo V2/3 - Verificar dados e receita")]
        public static void VerificarMenu()
        {
            try
            {
                MTMDados d=Ler(); Validar(d); var e=new MTMEstado();
                Exigir(!e.Concluir("tecer"),"Não se deve produzir sem materiais.");
                for(int i=1;i<=6;i++) { Exigir(e.Concluir("algodao_"+i),"Coleta inicial"); Exigir(!e.Concluir("algodao_"+i),"Coleta repetida"); }
                Exigir(e.algodao==6,"Quantidade de algodão");
                Exigir(e.Concluir("fiar") && e.algodao==0 && e.fio==3,"Fiação");
                Exigir(!e.Concluir("mercado_entrega"),"Mercado sem encomendas");
                e.Concluir("pacote1"); e.Concluir("pacote2"); e.Concluir("pacote3");
                Exigir(e.Concluir("mercado_entrega"),"Entrega de punhos");
                e.Concluir("cruzeiro"); e.Concluir("enfeite");
                Exigir(e.PodeTecelagem && e.Concluir("tecer") && e.rede,"Receita completa");
                Exigir(!e.Concluir("tecer") && e.fio==0 && e.punho==0,"Sem consumo duplicado");
                Exigir(e.Concluir("entrega_final") && !e.Concluir("entrega_final"),"Entrega final única");
                var copia=JsonUtility.FromJson<MTMEstado>(JsonUtility.ToJson(e));
                Exigir(copia.rede && copia.Tem("entrega_final"),"Persistência de estado");
                Debug.Log("Mundo V2: grafo, assets, recortes e verificações de receita concluídos. Execute também os testes manuais do guia.");
            }
            catch(Exception e) { Debug.LogException(e); }
        }

        private static void Exigir(bool condicao,string mensagem) { if(!condicao) throw new InvalidDataException(mensagem); }
        private static MTMDados Ler()
        {
            string path=Raiz+"/Data/mundo.json";
            if(!File.Exists(path)) throw new FileNotFoundException("Mantenha a pasta MundoV2 dentro de Assets/MemoriasTecidas.",path);
            MTMDados d=JsonUtility.FromJson<MTMDados>(File.ReadAllText(path)); Validar(d); return d;
        }
        private static void Validar(MTMDados d)
        {
            Exigir(d!=null && d.artes!=null && d.areas!=null && d.quadros!=null && d.quadros.Length==16,"Dados incompletos.");
            var artes=new HashSet<string>(); var areas=new HashSet<string>(); var pontos=new HashSet<string>();
            foreach(MTMArte a in d.artes)
            {
                Exigir(!string.IsNullOrEmpty(a.id) && artes.Add(a.id),"ID de arte repetido.");
                Exigir(File.Exists(Raiz+"/"+a.arquivo),"Imagem ausente: "+a.arquivo);
                Exigir(a.ppu>0 && a.pivotX>=0 && a.pivotX<=1 && a.pivotY>=0 && a.pivotY<=1,"Escala/pivô inválido: "+a.id);
            }
            foreach(MTMAreaDados a in d.areas) Exigir(!string.IsNullOrEmpty(a.id) && areas.Add(a.id),"ID de região repetido.");
            Exigir(areas.Contains("praca"),"Falta a praça inicial.");
            foreach(MTMAreaDados a in d.areas)
            {
                Exigir(artes.Contains(a.piso) && a.objetos!=null && a.pontos!=null,"Região incompleta: "+a.id);
                Exigir(a.largura>=32f&&a.altura>=18f,"Região menor que a janela de exploração: "+a.id);
                if(a.pisos!=null)foreach(MTMPisoDados p in a.pisos)Exigir(artes.Contains(p.arte)&&p.largura>0&&p.altura>0,"Camada de piso inválida.");
                foreach(MTMObjetoDados o in a.objetos) Exigir(artes.Contains(o.arte) && o.escala>0,"Objeto inválido em "+a.id);
                foreach(MTMPontoDados p in a.pontos)
                {
                    Exigir(!string.IsNullOrEmpty(p.id) && pontos.Add(p.id),"ID de interação repetido.");
                    if(p.tipo=="saida") Exigir(areas.Contains(p.destino),"Destino ausente: "+p.id);
                    else if(p.tipo!="descanso") Exigir(p.falas!=null && p.falas.Length>0,"Falas ausentes: "+p.id);
                }
            }
            var visitadas=new HashSet<string>{"praca"}; var fila=new Queue<string>(); fila.Enqueue("praca");
            while(fila.Count>0)
            {
                string id=fila.Dequeue();
                var area=Array.Find(d.areas,a=>a.id==id);
                foreach(MTMPontoDados p in area.pontos) if(p.tipo=="saida" && visitadas.Add(p.destino)) fila.Enqueue(p.destino);
            }
            Exigir(visitadas.Count==areas.Count,"Existe região sem conexão com a praça.");
            foreach(MTMAreaDados a in d.areas) foreach(MTMPontoDados p in a.pontos)
                if(p.tipo=="saida") Exigir(Array.Exists(Array.Find(d.areas,x=>x.id==p.destino).pontos,q=>q.tipo=="saida" && q.destino==a.id),"Saída sem caminho de retorno: "+p.id);
            foreach(MTMQuadro q in d.quadros) Exigir(q.x>=0 && q.y>=0 && q.largura>0 && q.altura>0 && q.x+q.largura<=d.folhaLargura && q.y+q.altura<=d.folhaAltura && q.ppu>0,"Recorte inválido.");
        }

        private static Material MaterialPrincipal()
        {
            return MTMMateriais.Preparar(Raiz,"Pintado_V2");
        }
        private static void Preparar(MTMDados d)
        {
            Directory.CreateDirectory(Gerados+"/Prefabs"); Directory.CreateDirectory(Gerados+"/Cenas"); AssetDatabase.Refresh();
            Material mat=MaterialPrincipal();
            Material sombra=MTMMateriais.Preparar(Raiz,"Sombra_V2",0f,1f);
            MTMMateriais.Preparar(Raiz,"Particulas_V2",1f);
            MTMMateriais.Preparar(Raiz,"Agua_V2",0f,0f,1f);
            foreach(MTMArte a in d.artes)
            {
                string path=Raiz+"/"+a.arquivo;
                var imp=AssetImporter.GetAtPath(path) as TextureImporter; if(imp==null) throw new FileNotFoundException(path);
                imp.textureType=TextureImporterType.Sprite; imp.spriteImportMode=SpriteImportMode.Single; imp.spritePixelsPerUnit=a.ppu;
                imp.alphaIsTransparency=!a.piso; imp.mipmapEnabled=false; imp.filterMode=FilterMode.Bilinear;
                imp.wrapMode=TextureWrapMode.Clamp; imp.npotScale=TextureImporterNPOTScale.None;
                imp.maxTextureSize=4096; imp.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings(); imp.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom; settings.spritePivot=new Vector2(a.pivotX,a.pivotY);
                settings.spriteMeshType=SpriteMeshType.FullRect; imp.SetTextureSettings(settings); imp.SaveAndReimport();
                if(a.folha) continue;
                string prefab=Gerados+"/Prefabs/"+a.id+".prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(prefab)!=null) continue;
                Scene previa=EditorSceneManager.NewPreviewScene(); GameObject go=null;
                try
                {
                    go=new GameObject(a.id); SceneManager.MoveGameObjectToScene(go,previa);
                    GameObject desenho=go;
                    if(!a.piso){desenho=new GameObject("Desenho");desenho.transform.SetParent(go.transform,false);}
                    var sr=desenho.AddComponent<SpriteRenderer>(); sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    sr.sharedMaterial=mat; sr.spriteSortPoint=SpriteSortPoint.Pivot;
                    if(sr.sprite==null) throw new InvalidOperationException("Sprite não carregado: "+a.id);
                    if(a.piso)
                    {
                        sr.sortingOrder=-30000;
                        if(a.repetivel){sr.drawMode=SpriteDrawMode.Tiled;sr.size=new Vector2(32,18);}
                        else go.transform.localScale=new Vector3(32f/sr.sprite.bounds.size.x,18f/sr.sprite.bounds.size.y,1f);
                    }
                    else
                    {
                        desenho.AddComponent<MTMOrdemY>();
                        var sombraGo=new GameObject("Sombra");sombraGo.transform.SetParent(go.transform,false);
                        var srSombra=sombraGo.AddComponent<SpriteRenderer>();srSombra.sprite=sr.sprite;srSombra.sharedMaterial=sombra;
                        srSombra.sortingOrder=-22000;srSombra.color=new Color(.1f,.13f,.15f,.22f);
                        sombraGo.transform.localScale=new Vector3(1.02f,.2f,1f);
                        var visual=go.AddComponent<MTMVisualV2>();visual.desenho=sr;visual.sombra=srSombra;visual.vento=a.vento;visual.respirar=a.personagem;
                    }
                    if(a.colW>0 && a.colH>0) { var c=go.AddComponent<BoxCollider2D>(); c.size=new Vector2(a.colW,a.colH); c.offset=new Vector2(0,a.colY); }
                    PrefabUtility.SaveAsPrefabAsset(go,prefab);
                }
                finally { if(go!=null) UnityEngine.Object.DestroyImmediate(go); EditorSceneManager.ClosePreviewScene(previa); }
            }
            AssetDatabase.SaveAssets();
        }
        private static GameObject Instanciar(string arte,Transform pai,Vector2 pos,float escala,bool semColisao)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Gerados+"/Prefabs/"+arte+".prefab");
            if(prefab==null) throw new FileNotFoundException("Prefab ausente: "+arte);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,pai);
            go.transform.localPosition=new Vector3(pos.x,pos.y,0f); go.transform.localScale*=escala;
            if(semColisao) foreach(Collider2D c in go.GetComponents<Collider2D>()) c.enabled=false;
            if(VolumesAltos.Contains(arte))
            {
                var visual=go.GetComponent<MTMVisualV2>();
                if(visual!=null && visual.desenho!=null)
                {
                    var oclusao=go.GetComponent<MTMOclusaoV2>();
                    if(oclusao==null)oclusao=go.AddComponent<MTMOclusaoV2>();
                    oclusao.desenho=visual.desenho;
                }
            }
            return go;
        }
        private static GameObject CriarJogador(MTMDados d,Material mat)
        {
            var go=new GameObject("Explorador_MundoV2"); go.transform.position=new Vector3(0,-6,0);
            var desenho=new GameObject("Desenho");desenho.transform.SetParent(go.transform,false);
            var sr=desenho.AddComponent<SpriteRenderer>();sr.sharedMaterial=mat;sr.spriteSortPoint=SpriteSortPoint.Pivot;
            var rb=go.AddComponent<Rigidbody2D>(); rb.gravityScale=0; rb.constraints=RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation=RigidbodyInterpolation2D.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var col=go.AddComponent<BoxCollider2D>(); col.size=new Vector2(.42f,.25f); col.offset=new Vector2(0,.12f);
            string matPath=Gerados+"/Sem_Atrito.physicsMaterial2D";
            var atrito=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
            if(atrito==null) { atrito=new PhysicsMaterial2D("Sem_Atrito"){friction=0,bounciness=0}; AssetDatabase.CreateAsset(atrito,matPath); }
            col.sharedMaterial=atrito; desenho.AddComponent<MTMOrdemY>();
            var anim=desenho.AddComponent<MTMAnimacao>(); anim.folha=AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz+"/Arte/jogador_folha.png");
            anim.tamanhoReferencia=new Vector2Int(d.folhaLargura,d.folhaAltura); anim.quadros=new MTMAnimacao.Quadro[d.quadros.Length];
            for(int i=0;i<d.quadros.Length;i++)
            {
                MTMQuadro q=d.quadros[i]; anim.quadros[i]=new MTMAnimacao.Quadro{rect=new Rect(q.x,q.y,q.largura,q.altura),pivot=new Vector2(q.pivotX,q.pivotY),pixelsPorUnidade=q.ppu};
            }
            var sombraGo=new GameObject("Sombra");sombraGo.transform.SetParent(go.transform,false);
            var ss=sombraGo.AddComponent<SpriteRenderer>();ss.sharedMaterial=MTMMateriais.Preparar(Raiz,"Sombra_V2",0f,1f);ss.sortingOrder=-22000;
            var visual=go.AddComponent<MTMVisualV2>();visual.desenho=sr;visual.sombra=ss;visual.jogador=true;
            var passos=go.AddComponent<MTMPassosV2>();passos.animacao=anim;
            passos.poeira=CriarParticulas(go.transform,"Poeira_Dos_Passos",new Vector2(.1f,.1f),false,0f);
            var pm=passos.poeira.main;pm.startLifetime=.45f;pm.startSize=new ParticleSystem.MinMaxCurve(.09f,.2f);pm.maxParticles=22;
            anim.Recarregar(); return go;
        }
        private static void Piso(string id,Transform pai,Vector2 pos,Vector2 tamanho,int ordem,MTMDados dados)
        {
            var go=Instanciar(id,pai,pos,1f,true);var sr=go.GetComponent<SpriteRenderer>();
            var arte=Array.Find(dados.artes,a=>a.id==id);sr.sortingOrder=ordem;
            go.transform.localScale=Vector3.one;
            if(arte.repetivel){sr.drawMode=SpriteDrawMode.Tiled;sr.size=tamanho;}
            else go.transform.localScale=new Vector3(tamanho.x/sr.sprite.bounds.size.x,tamanho.y/sr.sprite.bounds.size.y,1f);
            if(id=="rio_margem")sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Gerados+"/Agua_V2.mat");
        }
        private static void CriarAmbiente(Transform pai,MTMAreaDados area)
        {
            var efeitos=new GameObject("Atmosfera_V2");efeitos.transform.SetParent(pai,false);
            var c=efeitos.AddComponent<MTMParticulasV2>();c.interior=area.interior;
            c.natural=area.bioma=="rural"||area.bioma=="rio";
            c.poeira=CriarParticulas(efeitos.transform,"Po_no_ar",new Vector2(area.largura-2,area.altura-2),false,4f);
            if(c.natural)c.vagalumes=CriarParticulas(efeitos.transform,"Vagalumes",new Vector2(area.largura-3,area.altura-3),true,0f);
        }
        private static ParticleSystem CriarParticulas(Transform pai,string nome,Vector2 tamanho,bool brilho,float taxa)
        {
            var go=new GameObject(nome);go.transform.SetParent(pai,false);var ps=go.AddComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=ps.main;m.loop=true;m.playOnAwake=true;m.duration=10f;m.startLifetime=brilho?7f:9f;
            m.startSpeed=0f;m.startSize=new ParticleSystem.MinMaxCurve(brilho?.05f:.025f,brilho?.10f:.075f);
            m.startColor=brilho?new Color(1f,.89f,.38f,.6f):new Color(.94f,.81f,.62f,.24f);
            m.simulationSpace=ParticleSystemSimulationSpace.World;m.maxParticles=100;
            var e=ps.emission;e.rateOverTime=taxa;
            var s=ps.shape;s.enabled=true;s.shapeType=ParticleSystemShapeType.Box;s.scale=new Vector3(tamanho.x,tamanho.y,.01f);
            var vel=ps.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.World;
            vel.x=new ParticleSystem.MinMaxCurve(brilho?-.10f:.09f,brilho?.10f:.16f);vel.y=new ParticleSystem.MinMaxCurve(-.02f,.07f);
            // MTM_REPARO_PARTICULAS_V21
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var cor=ps.colorOverLifetime;cor.enabled=true;
            var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(.7f,.7f),new GradientAlphaKey(0,1)});cor.color=grad;
            var sr=ps.GetComponent<ParticleSystemRenderer>();sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Gerados+"/Particulas_V2.mat");sr.sortingOrder=22000;
            return ps;
        }
        private static void Parede(Transform pai,string nome,Vector2 pos,Vector2 tamanho)
        {
            var go=new GameObject(nome); go.transform.SetParent(pai,false); go.transform.localPosition=pos;
            go.AddComponent<BoxCollider2D>().size=tamanho;
        }
    }
}

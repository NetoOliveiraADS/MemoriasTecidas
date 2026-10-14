using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMInstaladorVidaV22
    {
        private const string Base = "Assets/MemoriasTecidas/MundoV2";
        private const string Raiz = Base + "/MelhoriasV22";
        private const string Gerados = Raiz + "/Gerados";
        private const string Interface = Base + "/Runtime/MTMJogoInterface.cs";
        [Serializable] private sealed class Recorte { public float x,y,largura,altura,pivotX,pivotY; }
        [Serializable] private sealed class Entrada { public string id,textura; public float alturaMundo; public Recorte[] quadros; }
        [Serializable] private sealed class Atlas { public Entrada[] entradas; }
        private static readonly HashSet<string> Predios = new HashSet<string> {"igreja","prefeitura","mercado","fabrica","casario","casa_azul","casa_ocre","sobrado_comercio","oficina_varanda","memorial","quiosque","coreto"};
        private static readonly HashSet<string> Arvores = new HashSet<string> {"arvore","carnauba_adulta","carnauba_jovem","arbusto_florido","algodoeiro"};
        private static readonly HashSet<string> Andantes = new HashSet<string> {"zeca","cida","feirante"};

        [MenuItem("Memorias Tecidas/Vida V2.2/1 - Conectar interface (aguarde compilar)")]
        public static void Conectar()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try
            {
                if(!File.Exists(Interface))throw new IOException("Não encontrei MTMJogoInterface.cs do Mundo V2.1. Não importe esta atualização em um projeto vazio.");
                string texto=File.ReadAllText(Interface);
                if(texto.Contains("if (DesenharVidaV22()) return;")){Debug.Log("Vida V2.2: interface já conectada. Use o passo 2 com sua cena aberta.");return;}
                var expressao=new Regex(@"private\s+void\s+OnGUI\s*\(\s*\)\s*\{");
                if(expressao.Matches(texto).Count!=1)throw new IOException("OnGUI diferente do esperado. Nada foi alterado; consulte o guia para conectar manualmente.");
                Directory.CreateDirectory("MemoriasTecidas_Backups/VidaV22");
                string copia="MemoriasTecidas_Backups/VidaV22/MTMJogoInterface_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".cs.txt";
                File.Copy(Interface,copia,false);
                texto=expressao.Replace(texto,m=>m.Value+"\n            // MTM_VIDA_V22: antes do teste de F1 da versão antiga.\n            if (DesenharVidaV22()) return;",1);
                File.WriteAllText(Interface,texto);
                AssetDatabase.ImportAsset(Interface);
                Debug.Log("Vida V2.2: conexão inserida; nenhum código de F1 foi apagado. Aguarde a compilação e use o passo 2. Backup: "+copia);
            }
            catch(Exception e){Debug.LogException(e);}
        }

        [MenuItem("Memorias Tecidas/Vida V2.2/2 - Aplicar melhorias na cena aberta")]
        public static void Aplicar()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try
            {
                if(!File.Exists(Interface)||!File.ReadAllText(Interface).Contains("if (DesenharVidaV22()) return;"))
                    throw new InvalidOperationException("Execute primeiro o passo 1 e aguarde a compilação.");
                MTMJogo jogo=JogoDaCena();var existente=jogo.GetComponent<MTMVidaV22>();
                if(existente!=null&&existente.versaoInstalada>=22){Debug.Log("Esta cena já recebeu a Vida V2.2. Seus ajustes foram mantidos; não foram duplicados NPCs ou escalas.");Selection.activeGameObject=jogo.gameObject;return;}
                if(existente!=null)throw new InvalidOperationException("Há uma instalação incompleta. Restaure o backup da cena antes de repetir, conforme o guia.");
                if(!EditorUtility.DisplayDialog("Aplicar Vida V2.2?","Será salva uma cópia da cena antes das alterações. O instalador troca as artes dos personagens, ajusta volumes e acrescenta efeitos, moradores e animais. Não recria o mundo nem apaga diálogos ou progresso.","Aplicar com backup","Cancelar"))return;
                string backup=Backup(jogo.gameObject.scene);
                var atlas=JsonUtility.FromJson<Atlas>(File.ReadAllText(Raiz+"/Arte/recortes.json"));
                if(atlas==null||atlas.entradas==null)throw new IOException("Recortes das artes ausentes.");
                Directory.CreateDirectory(Gerados+"/Sprites");AssetDatabase.Refresh();
                foreach(string caminho in Directory.GetFiles(Raiz+"/Arte","*.png"))ImportarTextura(caminho.Replace('\\','/'));
                Material pintura=MTMMaterialVidaV22.Preparar(Raiz,"Pintura_V22");
                Material sombra=MTMMaterialVidaV22.Preparar(Raiz,"Sombra_V22",0,1);
                Material particulas=MTMMaterialVidaV22.Preparar(Raiz,"Particulas_V22",1);
                Material agua=MTMMaterialVidaV22.Preparar(Raiz,"Agua_V22",0,0,1);
                var sprites=new Dictionary<string,Sprite[]>();
                foreach(var entrada in atlas.entradas)sprites.Add(entrada.id,Recortar(entrada));
                var vida=jogo.gameObject.AddComponent<MTMVidaV22>();vida.jogo=jogo;
                vida.painelTecido=AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz+"/Arte/painel_tecido.png");
                vida.lugares=Cartoes(jogo);
                var personagem=jogo.transform.Find("Desenho");
                if(personagem==null)throw new InvalidOperationException("Jogador sem filho Desenho. Restaure a cópia de segurança e adapte a referência no instalador.");
                var antigo=personagem.GetComponent<MTMAnimacao>();if(antigo!=null)antigo.enabled=false;
                Ator(personagem.GetComponent<SpriteRenderer>(),sprites["jogador"],MTMAtorV22.Tipo.Jogador,vida,pintura,"");
                var visualJogador=jogo.GetComponent<MTMVisualV2>();if(visualJogador!=null){visualJogador.vento=0;visualJogador.respirar=false;if(visualJogador.sombra!=null)visualJogador.sombra.sharedMaterial=sombra;}
                int moradores=0,arvores=0,redes=0;
                foreach(var regiao in jogo.regioes)
                {
                    if(regiao==null)continue;
                    foreach(var visual in regiao.GetComponentsInChildren<MTMVisualV2>(true))
                    {
                        if(visual.desenho==null||visual.desenho.sprite==null)continue;
                        var sr=visual.desenho;
                        string id=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sr.sprite.texture));
                        if(visual.sombra!=null)visual.sombra.sharedMaterial=sombra;
                        if(Predios.Contains(id))
                        {
                            visual.transform.localScale*=1.10f;
                            sr.transform.localScale=Vector3.Scale(sr.transform.localScale,new Vector3(1,1.12f,1));
                        }
                        if(Arvores.Contains(id))
                        {
                            visual.vento=0;
                            var deformacao=Deformar(sr,vida,pintura,MTMDeformacaoV22.Tipo.Copa);
                            deformacao.amplitude=id.StartsWith("carnauba")?.095f:.055f;
                            deformacao.inicioDoMovimento=id.StartsWith("carnauba")?.75f:.50f;
                            if(id=="arvore")
                            {var f=visual.gameObject.AddComponent<MTMFolhasV22>();f.copa=sr;f.vida=vida;f.material=particulas;}
                            arvores++;
                        }
                        if(id=="rede_pronta"||id=="varal_redes")
                        {visual.vento=0;Deformar(sr,vida,pintura,MTMDeformacaoV22.Tipo.Rede).amplitude=.085f;redes++;}
                    }
                    foreach(var ponto in regiao.pontos)
                    {
                        if(ponto==null||ponto.dados==null||!ponto.dados.id.StartsWith("npc_",StringComparison.Ordinal))continue;
                        string id=ponto.dados.id.Substring(4);if(!sprites.ContainsKey(id))continue;
                        Transform raiz=ponto.transform.parent;
                        if(raiz==regiao.transform)continue;
                        var visual=raiz.GetComponent<MTMVisualV2>();if(visual==null||visual.desenho==null)continue;
                        visual.vento=0;visual.respirar=false;
                        Ator(visual.desenho,sprites[id],MTMAtorV22.Tipo.Morador,vida,pintura,ponto.dados.nome);
                        if(Andantes.Contains(id))Andar(raiz,vida,false,.95f);
                        moradores++;
                    }
                    foreach(var o in regiao.GetComponentsInChildren<MTMOclusaoV2>(true))o.alturaDoJogador=1.95f;
                    foreach(var sr in regiao.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if(sr.sprite==null)continue;
                        string id=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sr.sprite.texture));
                        if(id=="rio_margem")
                        {
                            var nova=SpriteRio();Vector3 dimensao=sr.sprite.bounds.size;
                            sr.sprite=nova;var escala=sr.transform.localScale;
                            sr.transform.localScale=new Vector3(escala.x*dimensao.x/nova.bounds.size.x,escala.y*dimensao.y/nova.bounds.size.y,escala.z);
                            sr.sharedMaterial=agua;
                        }
                        else if(sr.gameObject.name!="Sombra"&&(sr.sharedMaterial==null||sr.sharedMaterial.shader.name.StartsWith("MemoriasTecidas/",StringComparison.Ordinal)||sr.sharedMaterial.shader.name=="Sprites/Default"))sr.sharedMaterial=pintura;
                    }
                    foreach(var ps in regiao.GetComponentsInChildren<ParticleSystem>(true))
                    {NormalizarParticulas(ps);ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=particulas;}
                    if(regiao.dados.id=="praca"||regiao.dados.id=="rua"||regiao.dados.id=="vila")
                    {
                        Animal(regiao,"Cachorro_Caramelo_V22",sprites["cachorro"],vida,pintura,sombra,new Vector2(-3,-8),1.8f);
                        Animal(regiao,"Gato_Rajado_V22",sprites["gato"],vida,pintura,sombra,new Vector2(4,-10),1.4f);
                    }
                }
                foreach(var ps in jogo.GetComponentsInChildren<ParticleSystem>(true))
                {NormalizarParticulas(ps);ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=particulas;}
                vida.versaoInstalada=22;
                SalvarCena(jogo);
                Selection.activeGameObject=jogo.gameObject;
                Debug.Log("Vida V2.2 aplicada: "+moradores+" moradores, "+arvores+" vegetações, "+redes+" redes/varais. Backup: "+backup+". Execute agora o passo 5 para aplicar as correções V2.2.1. Para aumentar casas e prédios, use o passo 5; o passo 3 amplia apenas a distribuição do mapa.");
            }
            catch(Exception e){Debug.LogException(e);Debug.LogWarning("Se a aplicação foi interrompida após iniciar, restaure a cópia de cena em Assets/MemoriasTecidas/Backups_V22. Não repita sobre uma instalação parcial.");}
        }

        private static void ImportarTextura(string caminho)
        {
            var i=AssetImporter.GetAtPath(caminho) as TextureImporter;if(i==null)throw new IOException("Imagem não importada: "+caminho);
            i.textureType=TextureImporterType.Default;i.alphaIsTransparency=!caminho.EndsWith("rio_agua_doce.png",StringComparison.Ordinal);
            i.mipmapEnabled=false;i.npotScale=TextureImporterNPOTScale.None;i.maxTextureSize=4096;
            i.filterMode=FilterMode.Bilinear;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.SaveAndReimport();
        }
        private static Sprite[] Recortar(Entrada entrada)
        {
            Texture2D t=AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz+"/Arte/"+entrada.textura);
            if(t==null)throw new IOException("Textura ausente: "+entrada.textura);
            float maior=0;foreach(var q in entrada.quadros)maior=Mathf.Max(maior,q.altura);
            var resultado=new Sprite[entrada.quadros.Length];
            for(int n=0;n<resultado.Length;n++)
            {
                string path=Gerados+"/Sprites/"+entrada.id+"_"+n.ToString("00")+".asset";
                Sprite s=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(s==null)
                {
                    var q=entrada.quadros[n];s=Sprite.Create(t,new Rect(q.x,q.y,q.largura,q.altura),new Vector2(q.pivotX,q.pivotY),maior/entrada.alturaMundo,0,SpriteMeshType.FullRect);
                    s.name=entrada.id+"_"+n.ToString("00");AssetDatabase.CreateAsset(s,path);
                }
                resultado[n]=s;
            }
            return resultado;
        }
        private static Sprite SpriteRio()
        {
            string path=Gerados+"/Sprites/rio_agua_doce.asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(s!=null)return s;
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz+"/Arte/rio_agua_doce.png");
            s=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),t.width/32f,0,SpriteMeshType.FullRect);s.name="Rio_Agua_Doce";AssetDatabase.CreateAsset(s,path);return s;
        }
        private static MTMDeformacaoV22 Deformar(SpriteRenderer sr,MTMVidaV22 vida,Material m,MTMDeformacaoV22.Tipo tipo)
        {
            var d=sr.GetComponent<MTMDeformacaoV22>();if(d==null)d=sr.gameObject.AddComponent<MTMDeformacaoV22>();
            d.tipo=tipo;d.vida=vida;d.material=m;sr.sharedMaterial=m;return d;
        }
        private static void Ator(SpriteRenderer sr,Sprite[] sprites,MTMAtorV22.Tipo tipo,MTMVidaV22 vida,Material m,string nome)
        {
            if(sr==null)throw new InvalidOperationException("Personagem sem SpriteRenderer.");
            sr.sprite=sprites[0];sr.sharedMaterial=m;
            var ator=sr.gameObject.AddComponent<MTMAtorV22>();ator.quadros=sprites;ator.tipo=tipo;ator.jogo=vida.jogo;ator.vida=vida;ator.nomeDaConversa=nome;
            if(tipo!=MTMAtorV22.Tipo.Animal){var d=Deformar(sr,vida,m,MTMDeformacaoV22.Tipo.Pessoa);d.amplitude=.012f;}
        }
        private static void Andar(Transform raiz,MTMVidaV22 vida,bool animal,float distancia)
        {
            var c=raiz.gameObject.AddComponent<MTMCaminhanteV22>();c.vida=vida;c.animal=animal;
            Vector2 p=raiz.localPosition;c.pontos=new[]{p,p+Vector2.right*distancia,p,p-Vector2.right*distancia};
            var rb=raiz.GetComponent<Rigidbody2D>();if(rb==null)rb=raiz.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType=RigidbodyType2D.Kinematic;rb.gravityScale=0;rb.constraints=RigidbodyConstraints2D.FreezeRotation;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
        }
        private static void Animal(MTMRegiao r,string nome,Sprite[] sprites,MTMVidaV22 vida,Material m,Material sombra,Vector2 pos,float distancia)
        {
            var go=new GameObject(nome);go.transform.SetParent(r.transform,false);go.transform.localPosition=pos;
            var desenho=new GameObject("Desenho");desenho.transform.SetParent(go.transform,false);
            var sr=desenho.AddComponent<SpriteRenderer>();sr.spriteSortPoint=SpriteSortPoint.Pivot;desenho.AddComponent<MTMOrdemY>();
            Ator(sr,sprites,MTMAtorV22.Tipo.Animal,vida,m,"");
            var col=go.AddComponent<CircleCollider2D>();col.radius=.17f;col.offset=new Vector2(0,.09f);
            var s=new GameObject("Sombra");s.transform.SetParent(go.transform,false);var ss=s.AddComponent<SpriteRenderer>();ss.sharedMaterial=sombra;
            var vis=go.AddComponent<MTMVisualV2>();vis.desenho=sr;vis.sombra=ss;
            Andar(go.transform,vida,true,distancia);
        }
        private static void NormalizarParticulas(ParticleSystem ps)
        {
            var v=ps.velocityOverLifetime;if(!v.enabled)return;
            bool simples=(v.x.mode==ParticleSystemCurveMode.Constant||v.x.mode==ParticleSystemCurveMode.TwoConstants)
                &&(v.y.mode==ParticleSystemCurveMode.Constant||v.y.mode==ParticleSystemCurveMode.TwoConstants)
                &&(v.z.mode==ParticleSystemCurveMode.Constant||v.z.mode==ParticleSystemCurveMode.TwoConstants);
            if(!simples)return;
            v.x=Dois(v.x);v.y=Dois(v.y);v.z=Dois(v.z);
        }
        private static ParticleSystem.MinMaxCurve Dois(ParticleSystem.MinMaxCurve c)
        {return c.mode==ParticleSystemCurveMode.Constant?new ParticleSystem.MinMaxCurve(c.constant,c.constant):c;}
        private static MTMVidaV22.Cartao[] Cartoes(MTMJogo jogo)
        {
            var ids=new Dictionary<string,string>{{"entrada","letreiro"},{"praca","igreja"},{"mercado","mercado"},{"fabrica","fabrica"},{"oficina","tear"},{"carnaubal","carnauba_adulta"},{"museu","memorial"},{"sitio","algodoeiro"},{"serra","piso_serra"},{"memorias","casario"},{"rua","sobrado_comercio"},{"vila","casa_azul"},{"largo","coreto"}};
            var resultado=new List<MTMVidaV22.Cartao>();
            foreach(var r in jogo.regioes)
            {
                if(r==null)continue;string id=r.dados.id;Texture2D imagem;
                if(id=="rio"||id=="beira")imagem=AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz+"/Arte/rio_agua_doce.png");
                else{string arte;imagem=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"/Arte/"+(ids.TryGetValue(id,out arte)?arte:r.dados.piso)+".png");}
                resultado.Add(new MTMVidaV22.Cartao{regiao=id,imagem=imagem});
            }
            return resultado.ToArray();
        }
        private static MTMJogo JogoDaCena()
        {
            var cena=SceneManager.GetActiveScene();MTMJogo jogo=null;
            foreach(var j in UnityEngine.Object.FindObjectsByType<MTMJogo>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(j.gameObject.scene==cena){if(jogo!=null)throw new InvalidOperationException("Mais de um MTMJogo na cena; resolva a duplicação antes de aplicar.");jogo=j;}
            if(jogo==null||jogo.regioes==null||jogo.regioes.Length==0)throw new InvalidOperationException("Abra a cena jogável do Mundo V2.1, com Explorador_MundoV2/MTMJogo e suas regiões.");
            if(string.IsNullOrEmpty(cena.path))throw new InvalidOperationException("Salve a cena com Ctrl+S antes de continuar.");
            return jogo;
        }
        private static string Backup(Scene cena)
        {
            const string dir="Assets/MemoriasTecidas/Backups_V22";Directory.CreateDirectory(dir);AssetDatabase.Refresh();
            string path=AssetDatabase.GenerateUniqueAssetPath(dir+"/"+Path.GetFileNameWithoutExtension(cena.path)+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");
            if(!EditorSceneManager.SaveScene(cena,path,true))throw new IOException("Não foi possível salvar o backup. Nenhuma alteração de cena foi iniciada.");return path;
        }
        private static void SalvarCena(MTMJogo jogo)
        {
            // Inclui overrides dos prefabs, para que sobrevivam ao fechar/reabrir a cena.
            foreach(var raiz in jogo.gameObject.scene.GetRootGameObjects())foreach(var c in raiz.GetComponentsInChildren<Component>(true))
            {
                if(c==null)continue;EditorUtility.SetDirty(c);
                if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
            EditorSceneManager.MarkSceneDirty(jogo.gameObject.scene);AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(jogo.gameObject.scene))throw new IOException("Não foi possível salvar a cena modificada; salve manualmente.");
        }

        [MenuItem("Memorias Tecidas/Vida V2.2/3 - Ampliar espaços em 20 por cento (uma vez)")]
        public static void Ampliar()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try
            {
                var jogo=JogoDaCena();var vida=jogo.GetComponent<MTMVidaV22>();
                if(vida==null||vida.versaoInstalada<22)throw new InvalidOperationException("Aplique primeiro o passo 2.");
                if(vida.ampliacaoAplicada>1.01f){Debug.Log("A ampliação já foi aplicada. Nada foi multiplicado novamente.");return;}
                foreach(var r in jogo.regioes)if(r!=null&&((r.transform.position-Vector3.zero).sqrMagnitude>.001f||(r.transform.lossyScale-Vector3.one).sqrMagnitude>.001f||Quaternion.Angle(r.transform.rotation,Quaternion.identity)>.01f))
                    throw new InvalidOperationException("Ampliação automática requer regiões na origem, sem rotação e com escala 1, como no Mundo V2.1. A cena personalizada não foi alterada.");
                if(!EditorUtility.DisplayDialog("Ampliar regiões em 20%?","Aumenta o espaço entre os objetos, pisos, limites, rotas, posições de chegada e luzes. Mantém o tamanho dos moradores. Cria outro backup antes. Teste entradas e margens após aplicar.","Ampliar com backup","Cancelar"))return;
                string backup=Backup(jogo.gameObject.scene);const float f=1.2f;
                foreach(var r in jogo.regioes)
                {
                    if(r==null)continue;
                    r.dados=JsonUtility.FromJson<MTMAreaDados>(JsonUtility.ToJson(r.dados));
                    var a=r.dados;a.largura*=f;a.altura*=f;a.seguroX*=f;a.seguroY*=f;
                    if(a.luzes!=null)foreach(var l in a.luzes){l.x*=f;l.y*=f;l.raio*=f;}
                    if(a.objetos!=null)foreach(var o in a.objetos){o.x*=f;o.y*=f;}
                    if(a.pisos!=null)foreach(var p in a.pisos){p.x*=f;p.y*=f;p.largura*=f;p.altura*=f;}
                    if(a.agua!=null)foreach(var p in a.agua){p.x*=f;p.y*=f;}
                    if(a.pontos!=null)foreach(var p in a.pontos){p.x*=f;p.y*=f;p.chegadaX*=f;p.chegadaY*=f;}
                    foreach(Transform filho in r.transform)
                    {
                        filho.localPosition=new Vector3(filho.localPosition.x*f,filho.localPosition.y*f,filho.localPosition.z);
                        var sr=filho.GetComponent<SpriteRenderer>();
                        if(sr!=null&&sr.sortingOrder<=-25000)
                        {if(sr.drawMode==SpriteDrawMode.Tiled)sr.size*=f;else filho.localScale=Vector3.Scale(filho.localScale,new Vector3(f,f,1));}
                        if(filho.name.StartsWith("Limite_",StringComparison.Ordinal)||filho.name=="Parede_Do_Interior")
                        {var box=filho.GetComponent<BoxCollider2D>();if(box!=null){box.size*=f;box.offset*=f;}}
                        if(filho.name=="Margem_Intransponivel")
                        {var pol=filho.GetComponent<PolygonCollider2D>();if(pol!=null)for(int i=0;i<pol.pathCount;i++){var pts=pol.GetPath(i);for(int j=0;j<pts.Length;j++)pts[j]*=f;pol.SetPath(i,pts);}}
                    }
                    foreach(var p in r.pontos)
                    {
                        if(p==null||p.dados==null)continue;
                        p.dados=JsonUtility.FromJson<MTMPontoDados>(JsonUtility.ToJson(p.dados));
                        p.dados.x*=f;p.dados.y*=f;p.dados.chegadaX*=f;p.dados.chegadaY*=f;
                    }
                    foreach(var c in r.GetComponentsInChildren<MTMCaminhanteV22>(true))for(int i=0;i<c.pontos.Length;i++)c.pontos[i]*=f;
                    foreach(var ps in r.GetComponentsInChildren<ParticleSystem>(true))
                    {var shape=ps.shape;if(shape.enabled)shape.scale=Vector3.Scale(shape.scale,new Vector3(f,f,1));}
                }
                jogo.transform.position=new Vector3(jogo.transform.position.x*f,jogo.transform.position.y*f,jogo.transform.position.z);
                vida.ampliacaoAplicada=f;SalvarCena(jogo);
                Debug.Log("Regiões ampliadas em 20% por eixo (44% de área). Backup: "+backup+". O arquivo mundo.json não foi regravado; edite a cena atual sem usar Criar mundo conectado.");
            }
            catch(Exception e){Debug.LogException(e);}
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMArtesLugaresV23
    {
        public const string Raiz="Assets/MemoriasTecidas/MundoV2/LugaresV23";
        public const string Vida="Assets/MemoriasTecidas/MundoV2/MelhoriasV22";
        public static MTMPacoteArtesV23 LerArtes(){return JsonUtility.FromJson<MTMPacoteArtesV23>(File.ReadAllText(Raiz+"/Data/artes.json"));}
        public static MTMPacoteCenasV23 LerCenas(){return JsonUtility.FromJson<MTMPacoteCenasV23>(File.ReadAllText(Raiz+"/Data/locais.json"));}

        [MenuItem("Memorias Tecidas/Lugares V2.3/1 - Preparar artes e prefabs")]
        public static void MenuPreparar()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Saia do Play antes de preparar as artes.");return;}
            try{Preparar();Debug.Log("V2.3: artes e prefabs preparados em "+Raiz+"/Gerados. Próximo: menu 2 - Montar quatro locais.");}
            catch(Exception e){Debug.LogException(e);}finally{EditorUtility.ClearProgressBar();}
        }
        public static void Preparar()
        {
            Directory.CreateDirectory(Raiz+"/Gerados/Sprites");Directory.CreateDirectory(Raiz+"/Gerados/Prefabs");
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Raiz+"/Arte","*.png"))
            {
                string p=path.Replace('\\','/');var t=AssetImporter.GetAtPath(p) as TextureImporter;
                if(t==null)throw new IOException("Não foi possível importar "+p);
                bool piso=Path.GetFileNameWithoutExtension(p)=="mosaico_praca"||Path.GetFileNameWithoutExtension(p)=="pedra_clara"||Path.GetFileNameWithoutExtension(p)=="asfalto"||Path.GetFileNameWithoutExtension(p)=="terra"||Path.GetFileNameWithoutExtension(p)=="grama"||Path.GetFileNameWithoutExtension(p)=="agua_rio";
                t.textureType=TextureImporterType.Default;t.alphaIsTransparency=!piso;t.sRGBTexture=true;
                t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;
                t.filterMode=FilterMode.Bilinear;t.wrapMode=piso?TextureWrapMode.Mirror:TextureWrapMode.Clamp;
                t.mipmapEnabled=piso;t.isReadable=false;t.SaveAndReimport();
            }
            Material tinta=MaterialExistente("Pintura_V22",0,0);
            Material sombra=MaterialExistente("Sombra_V22",0,1);
            Material particulas=MaterialExistente("Particulas_V22",1,0);
            MTMMateriaisLugaresV23.Preparar();
            var dados=LerArtes();
            foreach(var a in dados.artes)CriarSprite(a);
            Scene previa=EditorSceneManager.NewPreviewScene();
            try
            {
                for(int i=0;i<dados.artes.Length;i++)
                {
                    var a=dados.artes[i];if(a.tipo=="quadro")continue;
                    EditorUtility.DisplayProgressBar("Lugares V2.3","Preparando "+a.id,i/(float)dados.artes.Length);
                    string path=Raiz+"/Gerados/Prefabs/"+a.id+".prefab";
                    // Preserva alterações manuais dos prefabs já existentes.
                    if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)continue;
                    var go=new GameObject(a.id);SceneManager.MoveGameObjectToScene(go,previa);
                    try
                    {
                        var sr=Filho(go,"Desenho").AddComponent<SpriteRenderer>();sr.sprite=Sprite(a.id);sr.sharedMaterial=tinta;sr.spriteSortPoint=SpriteSortPoint.Pivot;
                        sr.gameObject.AddComponent<MTMOrdemY>();
                        var marker=go.AddComponent<MTMElementoV23>();marker.identificador=a.id;
                        if(a.colW>0&&a.colH>0){var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(a.colW,a.colH);c.offset=new Vector2(0,a.colY);}
                        if(a.sombra)
                        {
                            var ss=Filho(go,"Sombra").AddComponent<SpriteRenderer>();ss.sprite=sr.sprite;ss.sharedMaterial=sombra;
                            ss.color=new Color(.11f,.15f,.18f,.19f);ss.sortingOrder=-22000;ss.transform.localScale=new Vector3(1.02f,.2f,1);
                            var v=go.AddComponent<MTMVisualV2>();v.desenho=sr;v.sombra=ss;
                        }
                        if(a.alturaMundo>3&&a.tipo!="aquatica")
                        {var o=go.AddComponent<MTMOclusaoV2>();o.desenho=sr;o.alturaDoJogador=1.95f;}
                        if(a.tipo=="construcao")
                        {var e=go.AddComponent<MTMEscalaConstrucaoV221>();e.desenho=sr;var v=go.GetComponent<MTMVisualV2>();e.sombra=v!=null?v.sombra:null;e.largura=e.altura=e.profundidadeDaBase=1;e.Inicializar();}
                        if(a.tipo=="arvore"||a.tipo=="palmeira"||a.tipo=="rede")
                        {
                            var d=sr.gameObject.AddComponent<MTMDeformacaoV22>();d.material=tinta;
                            d.tipo=a.tipo=="rede"?MTMDeformacaoV22.Tipo.Rede:MTMDeformacaoV22.Tipo.Copa;
                            d.amplitude=a.tipo=="rede"?.035f:.025f;d.inicioDoMovimento=a.tipo=="palmeira"?.72f:.52f;
                            if(a.tipo=="arvore"){var f=go.AddComponent<MTMFolhasV22>();f.copa=sr;f.material=particulas;}
                        }
                        if(a.tipo=="poste"){var l=go.AddComponent<MTMLuzLocalV23>();l.raio=6;l.intensidade=.72f;}
                        if(a.tipo=="aquatica")
                        {UnityEngine.Object.DestroyImmediate(sr.GetComponent<MTMOrdemY>());sr.sortingOrder=-27000;}
                        PrefabUtility.SaveAsPrefabAsset(go,path);
                    }
                    finally{UnityEngine.Object.DestroyImmediate(go);}
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(previa);EditorUtility.ClearProgressBar();}
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        private static Material MaterialExistente(string nome,float efeito,float sombra)
        {var m=AssetDatabase.LoadAssetAtPath<Material>(Vida+"/Gerados/"+nome+".mat");return m!=null?m:MTMMaterialVidaV22.Preparar(Vida,nome,efeito,sombra);}
        private static GameObject Filho(GameObject pai,string nome){var f=new GameObject(nome);f.transform.SetParent(pai.transform,false);return f;}
        private static void CriarSprite(MTMArteV23 a)
        {
            string path=Raiz+"/Gerados/Sprites/"+a.id+".asset";if(AssetDatabase.LoadAssetAtPath<Sprite>(path)!=null)return;
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz+"/Arte/"+a.arquivo);
            if(t==null||a.x<0||a.y<0||a.x+a.largura>t.width||a.y+a.altura>t.height||a.alturaMundo<=0)throw new InvalidDataException("Recorte inválido: "+a.id);
            var s=UnityEngine.Sprite.Create(t,new Rect(a.x,a.y,a.largura,a.altura),new Vector2(a.pivotX,a.pivotY),a.altura/a.alturaMundo,0,SpriteMeshType.FullRect);
            s.name=a.id;AssetDatabase.CreateAsset(s,path);
        }
        public static Sprite Sprite(string id){return AssetDatabase.LoadAssetAtPath<Sprite>(Raiz+"/Gerados/Sprites/"+id+".asset");}
        public static GameObject Prefab(string id){return AssetDatabase.LoadAssetAtPath<GameObject>(Raiz+"/Gerados/Prefabs/"+id+".prefab");}
    }
}

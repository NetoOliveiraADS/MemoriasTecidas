using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMUrbanaCartoesV24
    {
        [MenuItem("Memorias Tecidas/Expansao Urbana V2.4/3 - Capturar imagens dos lugares no mapa")]
        public static void Capturar()
        {
            MTMJogo jogo=null;bool[] ativos=null;GameObject go=null;RenderTexture rt=null;Texture2D leitura=null;
            var anteriorRT=RenderTexture.active;float ativo=Shader.GetGlobalFloat("_MTV_Active");int quantidade=Shader.GetGlobalInt("_MTV_Count");
            var rendererEstado=new Dictionary<Renderer,bool>();
            try
            {
                jogo=MTMMontagemLugaresV23.JogoDaCena();var vida=jogo.GetComponent<MTMVidaV22>();
                if(vida==null)throw new InvalidOperationException("Vida V2.2.1 ausente.");
                var regioes=jogo.regioes.Where(r=>MTMUrbanaMontagemV24.Alvos.Contains(r.dados.id)&&r.GetComponent<MTMUrbanaV24>()!=null).ToArray();
                if(regioes.Length!=4)throw new InvalidOperationException("Monte os quatro locais primeiro, pelo menu 2.");
                ativos=jogo.regioes.Select(r=>r.gameObject.activeSelf).ToArray();
                go=new GameObject("Camera_Temporaria_Cartoes_V24"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.enabled=false;
                cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.20f,.23f,.20f,1);
                cam.nearClipPlane=.1f;cam.farClipPlane=100;cam.allowHDR=false;cam.allowMSAA=false;
                cam.transparencySortMode=TransparencySortMode.Orthographic;go.transform.position=new Vector3(0,0,-20);
                rt=new RenderTexture(1024,768,24,RenderTextureFormat.ARGB32){antiAliasing=1};rt.Create();cam.targetTexture=rt;
                leitura=new Texture2D(1024,768,TextureFormat.RGBA32,false);
                string dir=MTMUrbanaArtesV24.Raiz+"/Gerados/Cartoes";Directory.CreateDirectory(dir);AssetDatabase.Refresh();
                var lista=vida.lugares==null?new List<MTMVidaV22.Cartao>():new List<MTMVidaV22.Cartao>(vida.lugares);
                foreach(var ren in jogo.GetComponentsInChildren<Renderer>(true)){rendererEstado[ren]=ren.forceRenderingOff;ren.forceRenderingOff=true;}
                Shader.SetGlobalFloat("_MTV_Active",0);Shader.SetGlobalInt("_MTV_Count",0);
                foreach(var r in regioes)
                {
                    foreach(var outra in jogo.regioes)outra.gameObject.SetActive(outra==r);
                    foreach(var s in r.GetComponentsInChildren<MTMSuperficieV23>())s.Reconstruir();
                    foreach(var sr in r.GetComponentsInChildren<SpriteRenderer>())
                        if(sr.GetComponent<MTMOrdemY>()!=null)sr.sortingOrder=Mathf.Clamp(Mathf.RoundToInt(-sr.transform.position.y*100),-28000,28000);
                    cam.aspect=4f/3f;cam.orthographicSize=Mathf.Max(r.dados.altura/2f,r.dados.largura/(2f*cam.aspect))+1f;
                    if(GraphicsSettings.currentRenderPipeline==null)cam.Render();
                    else
                    {
                        var pedido=new RenderPipeline.StandardRequest{destination=rt};
                        if(!RenderPipeline.SupportsRenderRequest(cam,pedido))throw new InvalidOperationException("O pipeline não aceitou a captura automática. Abra a Scene/Game uma vez e tente novamente; as imagens anteriores do mapa foram mantidas.");
                        RenderPipeline.SubmitRenderRequest(cam,pedido);
                    }
                    RenderTexture.active=rt;leitura.ReadPixels(new Rect(0,0,1024,768),0,0);leitura.Apply();
                    string path=dir+"/"+r.dados.id+".png";File.WriteAllBytes(path,leitura.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    var imp=AssetImporter.GetAtPath(path) as TextureImporter;imp.textureType=TextureImporterType.Default;imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;imp.maxTextureSize=1024;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
                    var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);lista.RemoveAll(c=>c!=null&&c.regiao==r.dados.id);lista.Add(new MTMVidaV22.Cartao{regiao=r.dados.id,imagem=t});
                }
                vida.lugares=lista.ToArray();EditorUtility.SetDirty(vida);EditorSceneManager.MarkSceneDirty(jogo.gameObject.scene);
                AssetDatabase.SaveAssets();Debug.Log("Quatro cartões capturados a partir dos cenários editados. Salve a cena com Ctrl+S e confira o mapa no Play.");
            }
            catch(Exception e){Debug.LogException(e);}
            finally
            {
                foreach(var par in rendererEstado)if(par.Key!=null)par.Key.forceRenderingOff=par.Value;
                if(jogo!=null&&ativos!=null)for(int i=0;i<ativos.Length;i++)jogo.regioes[i].gameObject.SetActive(ativos[i]);
                Shader.SetGlobalFloat("_MTV_Active",ativo);Shader.SetGlobalInt("_MTV_Count",quantidade);RenderTexture.active=anteriorRT;
                if(go!=null)UnityEngine.Object.DestroyImmediate(go);
                if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(leitura!=null)UnityEngine.Object.DestroyImmediate(leitura);
            }
        }
    }
}

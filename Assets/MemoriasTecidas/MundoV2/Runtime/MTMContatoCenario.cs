using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [DisallowMultipleComponent]
    public sealed class MTMContatoCenario:MonoBehaviour
    {
        private GameObject objeto;
        private Mesh malha;
        private Material material;
        private MeshRenderer desenho;
        public static void Preparar(MTMRegiao regiao)
        {
            if(regiao==null)return;
            var efeito=regiao.GetComponent<MTMContatoCenario>();
            if(efeito==null){efeito=regiao.gameObject.AddComponent<MTMContatoCenario>();efeito.hideFlags=HideFlags.DontSave;}
        }
        private void OnEnable(){if(Application.isPlaying)Reconstruir();}
        private void Update(){if(desenho!=null)desenho.enabled=MTMAmbienteV2.Qualidade>0&&MTMConfiguracaoVisual.Atual.sombrasContato>.001f;}
        [ContextMenu("Reconstruir contatos do cenário")]
        public void Reconstruir()
        {
            if(!Application.isPlaying)return;
            Liberar();Shader shader=Resources.Load<Shader>("MemoriasTecidas/ContatoSuave");
            if(shader==null){Debug.LogWarning("Shader ContatoSuave ausente; sombras originais mantidas.",this);return;}
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            var vistos=new HashSet<SpriteRenderer>();MaterialPropertyBlock bloco=null;
            foreach(var visual in GetComponentsInChildren<MTMVisualV2>(false))
            {
                var sr=visual.desenho;
                if(sr==null||sr.sprite==null||visual.jogador||!vistos.Add(sr)||visual.GetComponentInChildren<MTMAtorV22>()!=null||visual.GetComponentInParent<MTMCaminhanteV22>()!=null)continue;
                Bounds b=sr.bounds;if(b.size.x<.5f||b.size.y<.8f)continue;
                var volume=sr.GetComponentInParent<MTMVolumeCenario>();
                if(volume!=null&&!volume.enabled)volume=null;
                MTMVolumeCenario.Aplicar(sr,volume!=null?volume.relevo:.24f,volume!=null?volume.desgasteBase:.18f,ref bloco);
                if(volume!=null&&!volume.sombraContato)continue;
                Vector2 raio=new Vector2(Mathf.Clamp(b.size.x*.24f,.2f,3.5f),Mathf.Clamp(b.size.x*.065f,.1f,.48f));
                Vector2 centro=new Vector2(b.center.x,b.min.y+.04f);
                if(volume!=null){centro+=volume.deslocamentoContato;if(volume.raioContato.x>0)raio.x=volume.raioContato.x;if(volume.raioContato.y>0)raio.y=volume.raioContato.y;}
                int k=vertices.Count;
                vertices.Add(transform.InverseTransformPoint(new Vector3(centro.x-raio.x,centro.y-raio.y,0f)));
                vertices.Add(transform.InverseTransformPoint(new Vector3(centro.x+raio.x,centro.y-raio.y,0f)));
                vertices.Add(transform.InverseTransformPoint(new Vector3(centro.x+raio.x,centro.y+raio.y,0f)));
                vertices.Add(transform.InverseTransformPoint(new Vector3(centro.x-raio.x,centro.y+raio.y,0f)));
                uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.one);uv.Add(Vector2.up);
                indices.Add(k);indices.Add(k+1);indices.Add(k+2);indices.Add(k);indices.Add(k+2);indices.Add(k+3);
                if(vertices.Count>=60000)break;
            }
            if(vertices.Count==0)return;
            malha=new Mesh{name="Contatos agrupados",hideFlags=HideFlags.DontSave};malha.SetVertices(vertices);malha.SetUVs(0,uv);malha.SetTriangles(indices,0);malha.RecalculateBounds();
            material=new Material(shader){hideFlags=HideFlags.DontSave};
            objeto=new GameObject("Acabamento · sombras de contato"){hideFlags=HideFlags.DontSave,layer=gameObject.layer};objeto.transform.SetParent(transform,false);
            objeto.AddComponent<MeshFilter>().sharedMesh=malha;desenho=objeto.AddComponent<MeshRenderer>();desenho.sharedMaterial=material;desenho.sortingOrder=-21980;
        }
        private static void Descartar(UnityEngine.Object o){if(o==null)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
        private void OnDisable(){Liberar();}
        private void Liberar()
        {
            if(objeto!=null)objeto.SetActive(false);Descartar(objeto);Descartar(malha);Descartar(material);
            objeto=null;malha=null;material=null;desenho=null;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Piso/água editável. A mesma borda gera a imagem e a colisão da água.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class MTMSuperficieV23 : MonoBehaviour
    {
        public Texture2D textura;
        public Material material;
        public Color cor=Color.white;
        public Vector2 tamanho=new Vector2(8,8);
        [Min(.1f)] public float unidadesPorRepeticao=6f;
        [Tooltip("Vazio = retângulo. Com 3 ou mais pontos = contorno em coordenadas locais, sem repetir o primeiro ponto.")]
        public Vector2[] contorno;
        public int ordem=-30000;
        public bool agua, bloquearPassagem;
        [Range(0f,.04f)] public float ondulacao=.009f;
        [Range(0f,2f)] public float velocidadeAgua=.65f;
        private Mesh malha;
        private MaterialPropertyBlock bloco;
        private bool reconstruir=true;
        private void OnEnable(){reconstruir=true;}
        private void OnValidate(){reconstruir=true;}
        private void Update()
        {
            if(!reconstruir)return;reconstruir=false;
            try{Reconstruir();}catch(Exception e){Debug.LogError("Superfície V2.3: "+e.Message,this);}
        }
        public Vector2[] Pontos()
        {
            if(contorno!=null&&contorno.Length>=3)return (Vector2[])contorno.Clone();
            Vector2 m=new Vector2(Mathf.Max(.1f,tamanho.x),Mathf.Max(.1f,tamanho.y))*.5f;
            return new[]{new Vector2(-m.x,-m.y),new Vector2(m.x,-m.y),new Vector2(m.x,m.y),new Vector2(-m.x,m.y)};
        }
        [ContextMenu("Reconstruir superficie e colisao")]
        public void Reconstruir()
        {
            Vector2[] pontos=Pontos();int[] indices=MTMTriangulacaoV23.Triangular(pontos);
            if(malha==null){malha=new Mesh{name="Superficie_V23",hideFlags=HideFlags.DontSave};}
            var vertices=new Vector3[pontos.Length];var uv=new Vector2[pontos.Length];
            for(int i=0;i<pontos.Length;i++){vertices[i]=pontos[i];uv[i]=pontos[i]/Mathf.Max(.1f,unidadesPorRepeticao);}
            malha.Clear();malha.vertices=vertices;malha.uv=uv;malha.triangles=indices;malha.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh=malha;
            var r=GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=ordem;
            if(bloco==null)bloco=new MaterialPropertyBlock();
            bloco.SetTexture("_MainTex",textura!=null?textura:Texture2D.whiteTexture);
            bloco.SetColor("_Color",cor);bloco.SetFloat("_Agua",agua?1f:0f);
            bloco.SetFloat("_Ondulacao",ondulacao);bloco.SetFloat("_Velocidade",velocidadeAgua);r.SetPropertyBlock(bloco);
            var c=GetComponent<PolygonCollider2D>();
            if(bloquearPassagem)
            {
                if(c==null)c=gameObject.AddComponent<PolygonCollider2D>();
                c.enabled=true;c.isTrigger=false;c.pathCount=1;c.SetPath(0,pontos);
            }
            else if(c!=null)c.enabled=false;
            reconstruir=false;
        }
        private void OnDisable(){LiberarMalha();}
        private void OnDestroy(){LiberarMalha();}
        private void LiberarMalha()
        {
            if(malha==null)return;
            var filtro=GetComponent<MeshFilter>();if(filtro!=null&&filtro.sharedMesh==malha)filtro.sharedMesh=null;
            if(Application.isPlaying)Destroy(malha);else DestroyImmediate(malha);
            malha=null;reconstruir=true;
        }
        private void OnDrawGizmosSelected()
        {
            var p=Pontos();Gizmos.color=agua?Color.cyan:new Color(1f,.75f,.2f);
            for(int i=0;i<p.Length;i++)Gizmos.DrawLine(transform.TransformPoint(p[i]),transform.TransformPoint(p[(i+1)%p.Length]));
        }
    }
    public static class MTMTriangulacaoV23
    {
        public static int[] Triangular(Vector2[] p)
        {
            if(p==null||p.Length<3||p.Length>256)throw new ArgumentException("Use de 3 a 256 vértices no contorno.");
            float area=0;for(int i=0;i<p.Length;i++)area+=Cruz(p[i],p[(i+1)%p.Length]);
            if(Mathf.Abs(area)<.00001f)throw new ArgumentException("Contorno sem área.");
            var vivos=new List<int>();for(int i=0;i<p.Length;i++)vivos.Add(area>0?i:p.Length-1-i);
            var tr=new List<int>();
            while(vivos.Count>3)
            {
                bool achou=false;
                for(int i=0;i<vivos.Count;i++)
                {
                    int a=vivos[(i+vivos.Count-1)%vivos.Count],b=vivos[i],c=vivos[(i+1)%vivos.Count];
                    if(Cruz(p[b]-p[a],p[c]-p[b])<=.000001f)continue;
                    bool ocupado=false;
                    foreach(int k in vivos)if(k!=a&&k!=b&&k!=c&&Dentro(p[k],p[a],p[b],p[c])){ocupado=true;break;}
                    if(ocupado)continue;tr.Add(a);tr.Add(b);tr.Add(c);vivos.RemoveAt(i);achou=true;break;
                }
                if(!achou)throw new ArgumentException("Contorno inválido: retire cruzamentos, vértices repetidos ou alinhados desnecessários.");
            }
            tr.AddRange(vivos);return tr.ToArray();
        }
        private static float Cruz(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
        private static bool Dentro(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {return Cruz(b-a,p-a)>=-.000001f&&Cruz(c-b,p-b)>=-.000001f&&Cruz(a-c,p-c)>=-.000001f;}
    }
}

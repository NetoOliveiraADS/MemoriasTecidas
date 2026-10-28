using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Deforma uma malha subdividida; não gira o tronco/suportes junto com as folhas/rede.
    [DefaultExecutionOrder(200), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTMDeformacaoV22 : MonoBehaviour
    {
        public enum Tipo { Pessoa, Copa, Rede }
        public Tipo tipo;
        public MTMVidaV22 vida;
        public Material material;
        [Range(0f,.3f)] public float amplitude = .035f;
        [Range(0f,.95f)] public float inicioDoMovimento = .55f;
        [Tooltip("Diminui a cabeça sem encolher o corpo. 1 = desenho original.")]
        [Range(.6f,1f)] public float escalaCabeca = .84f;
        [Range(1f,1.5f)] public float alongamentoCorpo = 1.12f;
        [Header("Personagens — V2.2.1")]
        public bool fixarCorpo = true;
        [Range(0f,.012f)] public float ventoNoCabelo = .004f;
        [Range(0f,.004f)] public float respiracao = 0f;
        private SpriteRenderer fonte;
        private Mesh malha;
        private GameObject filho;
        private MeshRenderer renderizador;
        private Sprite anterior;
        private Vector3[] baseVertices, vertices;
        private Vector2[] uvLocal;
        private Vector2[] uvs;
        private Color[] cores;
        private int[] indices;
        private MaterialPropertyBlock propriedades;
        private float fase;
        private MTMCaminhanteV22 caminhante;
        private MTMVisualV2 visualAntigo;
        private Quaternion rotacaoBase;
        private Vector3 escalaBase;
        private Bounds limitesRepouso;
        private const int Colunas = 12, Linhas = 20;

        private void OnEnable()
        {
            fonte = GetComponent<SpriteRenderer>(); caminhante = GetComponentInParent<MTMCaminhanteV22>();
            visualAntigo=GetComponentInParent<MTMVisualV2>();
            rotacaoBase=transform.localRotation;escalaBase=transform.localScale;
            fase = transform.position.x * .73f + transform.position.y;
            filho = new GameObject("Malha_Viva_V22"); filho.transform.SetParent(transform, false);
            filho.layer=gameObject.layer;
            filho.hideFlags = HideFlags.DontSave;
            malha = new Mesh { name = "Deformacao_V22" }; malha.MarkDynamic();
            filho.AddComponent<MeshFilter>().sharedMesh = malha;
            renderizador = filho.AddComponent<MeshRenderer>(); renderizador.sharedMaterial = material;
            propriedades = new MaterialPropertyBlock(); anterior = null;
        }
        private void LateUpdate()
        {
            if(tipo==Tipo.Pessoa && fixarCorpo)
            {
                if(visualAntigo!=null){visualAntigo.vento=0;visualAntigo.respirar=false;}
                transform.localRotation=rotacaoBase;transform.localScale=escalaBase;
            }
            if (fonte == null || fonte.sprite == null || material == null || filho == null)
            { if (fonte != null) fonte.forceRenderingOff = false; return; }
            if (anterior != fonte.sprite) Construir();
            fonte.forceRenderingOff = true;
            renderizador.enabled = fonte.enabled;
            renderizador.sortingLayerID = fonte.sortingLayerID; renderizador.sortingOrder = fonte.sortingOrder;
            fonte.GetPropertyBlock(propriedades);
            propriedades.SetTexture("_MainTex", fonte.sprite.texture);
            propriedades.SetColor("_Color", fonte.color);
            filho.transform.localScale = new Vector3(fonte.flipX ? -1f : 1f, fonte.flipY ? -1f : 1f, 1f);
            float vento = vida != null ? vida.intensidadeVento : 1f;
            if (MTMAmbienteV2.Qualidade == 0||MTMConfiguracaoVisual.Atual.reduzirMovimento) vento = 0f;
            if(tipo!=Tipo.Pessoa)
            {
                propriedades.SetVector("_MTM_Deform",new Vector4(tipo==Tipo.Copa?1f:2f,amplitude*vento,inicioDoMovimento,fase));
                renderizador.SetPropertyBlock(propriedades);
                Bounds limite=limitesRepouso;limite.Expand(new Vector3(amplitude*vento*MTMConfiguracaoVisual.Atual.vento*3f,amplitude*vento*MTMConfiguracaoVisual.Atual.vento*.8f+.05f,0f));malha.bounds=limite;
                return; // Copas e redes são deformadas no shader; a malha base permanece imutável.
            }
            propriedades.SetVector("_MTM_Deform",Vector4.zero);renderizador.SetPropertyBlock(propriedades);
            vento*=MTMConfiguracaoVisual.Atual.vento;
            float andar = caminhante != null ? Mathf.Clamp01(caminhante.Velocidade.magnitude * 2f) : 0f;
            float altura = fonte.sprite.rect.height / fonte.sprite.pixelsPerUnit;
            float largura = fonte.sprite.rect.width / fonte.sprite.pixelsPerUnit;
            float baixo = -fonte.sprite.pivot.y / fonte.sprite.pixelsPerUnit;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = baseVertices[i]; Vector2 uv = uvLocal[i];
                {
                    const float pescoco = .70f;
                    float pesoCabeca = Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(pescoco,.83f,uv.y));
                    v.x *= Mathf.Lerp(1f,escalaCabeca,pesoCabeca);
                    float y = uv.y <= pescoco ? uv.y*alongamentoCorpo : pescoco*alongamentoCorpo+(uv.y-pescoco)*escalaCabeca;
                    v.y = baixo + y*altura;
                    float cabelo = Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.92f,1f,uv.y));
                    v.x += Mathf.Sin(Time.time*2.5f+fase+uv.x*2f)*ventoNoCabelo*cabelo*vento;
                    // Micromovimento respiratório e passos procedurais dos moradores.
                    v.y += Mathf.Sin(Time.time*1.6f+fase)*respiracao*altura*uv.y;
                    float pernas = 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.18f,.45f,uv.y));
                    v.x += Mathf.Sin(Time.time*9f+uv.x*Mathf.PI)*.024f*largura*pernas*andar;
                    v.y += Mathf.Abs(Mathf.Sin(Time.time*9f))*.012f*andar*uv.y;
                }
                vertices[i] = v;
            }
            malha.vertices = vertices; malha.RecalculateBounds();
        }
        private void Construir()
        {
            anterior = fonte.sprite; var s = anterior; var r = s.rect;
            int n = (Colunas+1)*(Linhas+1);
            if (vertices == null || vertices.Length != n) { vertices=new Vector3[n]; baseVertices=new Vector3[n]; uvLocal=new Vector2[n]; }
            if(uvs==null||uvs.Length!=n){uvs=new Vector2[n];cores=new Color[n];indices=new int[Colunas*Linhas*6];}
            int k=0;
            for (int y=0;y<=Linhas;y++) for(int x=0;x<=Colunas;x++)
            {
                int i=y*(Colunas+1)+x; float u=x/(float)Colunas,v=y/(float)Linhas;
                uvLocal[i]=new Vector2(u,v);
                vertices[i]=baseVertices[i]=new Vector3((u*r.width-s.pivot.x)/s.pixelsPerUnit,(v*r.height-s.pivot.y)/s.pixelsPerUnit,0);
                uvs[i]=new Vector2((r.x+u*r.width)/s.texture.width,(r.y+v*r.height)/s.texture.height); cores[i]=Color.white;
                if(x<Colunas&&y<Linhas){int a=i,b=i+1,c=i+Colunas+1,d=c+1;indices[k++]=a;indices[k++]=c;indices[k++]=b;indices[k++]=b;indices[k++]=c;indices[k++]=d;}
            }
            malha.Clear(); malha.vertices=vertices;malha.uv=uvs;malha.colors=cores;malha.triangles=indices;malha.uv2=uvLocal;malha.RecalculateBounds();limitesRepouso=malha.bounds;
        }
        private void OnDisable()
        {
            if(fonte!=null)fonte.forceRenderingOff=false;
            if(filho!=null){if(Application.isPlaying)Destroy(filho);else DestroyImmediate(filho);}
            if(malha!=null){if(Application.isPlaying)Destroy(malha);else DestroyImmediate(malha);}
        }
    }
}

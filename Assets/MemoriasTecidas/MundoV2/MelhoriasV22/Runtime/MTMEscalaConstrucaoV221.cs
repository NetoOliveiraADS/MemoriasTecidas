using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [DefaultExecutionOrder(260),DisallowMultipleComponent]
    public sealed class MTMEscalaConstrucaoV221:MonoBehaviour
    {
        public SpriteRenderer desenho,sombra;
        [Range(1f,1.8f)] public float largura=1.20f;
        [Range(1f,2f)] public float altura=1.35f;
        [Range(1f,1.5f)] public float profundidadeDaBase=1.08f;
        [SerializeField,HideInInspector] private bool inicializado;
        [SerializeField,HideInInspector] private Vector3 escalaOriginal;
        [SerializeField,HideInInspector] private BoxCollider2D[] caixas;
        [SerializeField,HideInInspector] private Vector2[] tamanhos,deslocamentos;
        public void Inicializar()
        {
            if(inicializado||desenho==null)return;
            escalaOriginal=desenho.transform.localScale;
            caixas=GetComponents<BoxCollider2D>();tamanhos=new Vector2[caixas.Length];deslocamentos=new Vector2[caixas.Length];
            for(int i=0;i<caixas.Length;i++){tamanhos[i]=caixas[i].size;deslocamentos[i]=caixas[i].offset;}
            inicializado=true;
        }
        public void Aplicar()
        {
            if(!inicializado||desenho==null)return;
            desenho.transform.localScale=Vector3.Scale(escalaOriginal,new Vector3(largura,altura,1));
            if(caixas!=null)for(int i=0;i<caixas.Length;i++)if(caixas[i]!=null)
            {caixas[i].size=Vector2.Scale(tamanhos[i],new Vector2(largura,profundidadeDaBase));caixas[i].offset=Vector2.Scale(deslocamentos[i],new Vector2(largura,profundidadeDaBase));}
        }
        private void OnValidate(){Aplicar();}
        private void Start(){Aplicar();}
        private void LateUpdate()
        {
            if(desenho==null||sombra==null)return;
            var s=sombra.transform.localScale;s.x=desenho.transform.localScale.x*1.02f;
            s.y=Mathf.Lerp(.16f,.29f,1f-MTMAmbienteV2.Sol)*desenho.transform.localScale.y;
            sombra.transform.localScale=s;
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMVisualV2:MonoBehaviour
    {
        public SpriteRenderer desenho,sombra;
        public float vento;
        public bool respirar,jogador;
        private Vector3 escala;
        private Quaternion rotacao;
        private float fase;
        private MTMDeformacaoV22 deformacao;
        private void Awake()
        {
            if(desenho!=null){escala=desenho.transform.localScale;rotacao=desenho.transform.localRotation;deformacao=desenho.GetComponent<MTMDeformacaoV22>();}
            fase=transform.position.x*1.37f+transform.position.y*.91f;
        }
        private void LateUpdate()
        {
            if(desenho==null)return;
            var arte=MTMConfiguracaoVisual.Atual;
            float onda=Mathf.Sin(Time.time*1.2f+fase);
            float vivo=MTMAmbienteV2.Qualidade>0&&!arte.reduzirMovimento?arte.vento:0f;
            bool malhaViva=deformacao!=null&&deformacao.isActiveAndEnabled;
            // O balanço da malha não gira a base da árvore, a construção ou os suportes da rede.
            desenho.transform.localRotation=rotacao*Quaternion.Euler(0,0,malhaViva?0f:vento*onda*vivo);
            if(respirar&&!jogador&&!malhaViva)desenho.transform.localScale=new Vector3(escala.x,escala.y*(1f+.002f*onda*vivo),escala.z);
            if(sombra==null)return;
            sombra.enabled=(MTMAmbienteV2.Qualidade>0||jogador)&&desenho.enabled;
            sombra.sprite=desenho.sprite;
            float direcao=Mathf.Sin((MTMAmbienteV2.Fase-.25f)*Mathf.PI*2f);
            float baixoSol=1f-MTMAmbienteV2.Sol;
            sombra.transform.localPosition=new Vector3(direcao*.19f,-.035f,0f);
            sombra.transform.localRotation=Quaternion.Euler(0,0,-direcao*32f);
            sombra.transform.localScale=new Vector3(1.015f,Mathf.Lerp(.16f,.30f,baixoSol),1f);
            sombra.color=new Color(.10f,.14f,.15f,Mathf.Lerp(.075f,.23f,MTMAmbienteV2.Sol));
            sombra.sortingOrder=-22000;
        }
    }
}

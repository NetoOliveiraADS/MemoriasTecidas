using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMVisualV2 : MonoBehaviour
    {
        public SpriteRenderer desenho, sombra;
        public float vento;
        public bool respirar, jogador;
        private Vector3 escala;
        private float fase;
        private void Awake()
        {
            if(desenho!=null) escala=desenho.transform.localScale;
            fase=transform.position.x*1.37f+transform.position.y*.91f;
        }
        private void LateUpdate()
        {
            if(desenho==null)return;
            float onda=Mathf.Sin(Time.time*1.2f+fase);
            float vivo=MTMAmbienteV2.Qualidade>0?1f:0f;
            desenho.transform.localRotation=Quaternion.Euler(0,0,vento*onda*vivo);
            if(respirar && !jogador) desenho.transform.localScale=new Vector3(escala.x,escala.y*(1f+.0035f*onda*vivo),escala.z);
            if(sombra==null)return;
            sombra.enabled=(MTMAmbienteV2.Qualidade>0||jogador) && desenho.enabled;
            sombra.sprite=desenho.sprite;
            float direcao=Mathf.Sin((MTMAmbienteV2.Fase-.25f)*Mathf.PI*2f);
            sombra.transform.localPosition=new Vector3(direcao*.14f,-.025f,0f);
            sombra.transform.localRotation=Quaternion.Euler(0,0,-direcao*38f);
            sombra.transform.localScale=new Vector3(1.02f,Mathf.Lerp(.16f,.29f,1f-MTMAmbienteV2.Sol),1f);
            sombra.color=new Color(.11f,.15f,.18f,Mathf.Lerp(.11f,.24f,MTMAmbienteV2.Sol));
            sombra.sortingOrder=-22000;
        }
    }
}

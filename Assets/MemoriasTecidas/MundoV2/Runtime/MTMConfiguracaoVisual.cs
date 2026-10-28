using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [Serializable]
    public sealed class MTMConfiguracaoVisual
    {
        [Header("Movimento")]
        [Range(8f,80f)] public float aceleracao=36f;
        [Range(8f,100f)] public float frenagem=48f;
        [Header("Câmera")]
        [Range(.04f,.5f)] public float suavidadeCamera=.16f;
        [Range(0f,.5f)] public float antecipacao=.22f;
        [Range(0f,1f)] public float aberturaCorrida=.45f;
        public Vector2 zonaMorta=new Vector2(.25f,.16f);
        public bool aproximarConversas=true;
        public bool reduzirMovimento;
        [Header("Pintura e luz")]
        [Range(.7f,1.4f)] public float exposicao=1.06f;
        [Range(.7f,1.3f)] public float saturacao=.96f;
        [Range(0f,1f)] public float detalhe=.55f;
        [Range(0f,1f)] public float sombrasContato=.65f;
        [Range(0f,1.5f)] public float vento=.8f;
        public Color luzDia=new Color(1.02f,1f,.94f);
        public Color luzEntardecer=new Color(1.09f,.79f,.57f);
        public Color luzNoite=new Color(.34f,.43f,.58f);
        public Color luzInterior=new Color(.81f,.77f,.69f);

        public static MTMConfiguracaoVisual Atual { get; private set; }=new MTMConfiguracaoVisual();
        public static void Usar(MTMConfiguracaoVisual valor){Atual=valor??new MTMConfiguracaoVisual();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar(){Atual=new MTMConfiguracaoVisual();MTMAmbienteV2.Qualidade=1;}
    }

    public static class MTMCalculosVisuais
    {
        public static Vector2 AvancarVelocidade(Vector2 atual,Vector2 entrada,float rapidez,float acelerar,float frear,float dt)
        {
            // Soltar a tecla encerra o comando sem uma cauda de deslizamento.
            if(entrada.sqrMagnitude<.0001f||rapidez<=0f)return Vector2.zero;
            Vector2 alvo=Vector2.ClampMagnitude(entrada,1f)*Mathf.Max(0f,rapidez);
            bool freando=alvo.sqrMagnitude<atual.sqrMagnitude||Vector2.Dot(atual,alvo)<0f;
            return Vector2.MoveTowards(atual,alvo,Mathf.Max(1f,freando?frear:acelerar)*Mathf.Max(0f,dt));
        }
        public static Vector3 ConterCamera(Vector3 alvo,Vector2 centro,Vector2 dimensoes,float tamanho,float aspecto)
        {
            float x=Mathf.Max(0f,Mathf.Abs(dimensoes.x)*.5f-tamanho*Mathf.Max(.1f,aspecto));
            float y=Mathf.Max(0f,Mathf.Abs(dimensoes.y)*.5f-tamanho);
            return new Vector3(Mathf.Clamp(alvo.x,centro.x-x,centro.x+x),Mathf.Clamp(alvo.y,centro.y-y,centro.y+y),alvo.z);
        }
        public static float Sol(float fase)
        {
            float h=Mathf.Repeat(fase,1f)*24f;
            return Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(5.5f,8f,h))*(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(17f,19.5f,h)));
        }
    }
}

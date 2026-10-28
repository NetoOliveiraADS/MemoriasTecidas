Shader "MemoriasTecidas/LugaresV23/Superficie"
{
    Properties
    {
        [PerRendererData] _MainTex("Textura",2D)="white"{}
        _Color("Cor",Color)=(1,1,1,1)
        _Agua("Água",Float)=0
        _Ondulacao("Ondulação",Float)=.009
        _Velocidade("Velocidade",Float)=.65
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Assets/MemoriasTecidas/MundoV2/Qualidade/Shaders/MTMIluminacao.cginc"
            #include "Assets/MemoriasTecidas/MundoV2/Qualidade/Shaders/MTMDetalheOriginal.cginc"
            sampler2D _MainTex;float4 _MainTex_TexelSize,_Color;
            float _Agua,_Ondulacao,_Velocidade;
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;};
            V vert(A a){V o;o.pos=UnityObjectToClipPos(a.pos);o.uv=a.uv;o.world=mul(unity_ObjectToWorld,a.pos).xy;return o;}
            float4 frag(V i):SV_Target
            {
                float movimento=step(.5,_MTV_Quality)*_MTV_Motion;
                float t=_MTV_Time*_Velocidade*movimento;
                float2 onda=float2(sin(i.world.y*1.7+t)+.35*sin(i.world.x*2.9-t*.7),sin(i.world.x*1.2-t*.8));
                float2 uv=i.uv+onda*_Ondulacao*_Agua*movimento;
                float4 amostra=tex2D(_MainTex,uv);
                amostra=MTMDetalharOriginal(_MainTex,uv,amostra,_MainTex_TexelSize,float4(0,0,1,1),1,_Agua);
                float4 c=amostra*_Color;
                if(_Agua>.5)
                {
                    float reflexo=pow(saturate(sin(i.world.y*4.8+t*.8+sin(i.world.x*.6))),12);
                    c.rgb+=reflexo*.014*movimento*_MTV_Sun.y;
                }
                c.rgb=MTMPintura(c.rgb,i.world);return c;
            }
            ENDCG
        }
    }
}

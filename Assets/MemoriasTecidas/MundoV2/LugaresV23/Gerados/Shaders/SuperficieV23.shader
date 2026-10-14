Shader "MemoriasTecidas/LugaresV23/Superficie"
{
    Properties
    {
        [PerRendererData] _MainTex("Textura",2D)="white"{}
        _Color("Cor",Color)=(1,1,1,1)
        _Agua("Agua",Float)=0
        _Ondulacao("Ondulacao",Float)=.009
        _Velocidade("Velocidade",Float)=.65
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent"  }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Always" }

            CGPROGRAM
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            #define SAMPLE_TEX(uv) tex2D(_MainTex,uv)
            #define TO_CLIP(pos) UnityObjectToClipPos(pos)
            #define TO_WORLD(pos) mul(unity_ObjectToWorld,pos).xyz
            float4 _Color; float _Agua,_Ondulacao,_Velocidade;

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; };
            float4 _MTV_Ambient,_MTV_Lights[16],_MTV_Colors[16];
            float _MTV_Active,_MTV_Time,_MTV_Quality; int _MTV_Count;
            V vert(A a){V o;o.pos=TO_CLIP(a.pos);o.uv=a.uv;o.world=TO_WORLD(a.pos).xy;return o;}
            float4 frag(V i):SV_Target
            {
                float w=_Agua*step(.5,_MTV_Quality),t=_MTV_Time*_Velocidade;
                float2 uv=i.uv;
                uv+=float2(sin(i.world.y*1.9+t),sin(i.world.x*1.3-t*.8))*_Ondulacao*w;
                float4 c=SAMPLE_TEX(uv)*_Color;
                float3 luz=lerp(float3(1,1,1),_MTV_Ambient.rgb,saturate(_MTV_Active));
                [loop] for(int n=0;n<16;n++)
                {
                    if(n>=_MTV_Count)break;
                    float2 d=i.world-_MTV_Lights[n].xy;
                    float f=saturate(1-dot(d,d)/max(.01,_MTV_Lights[n].z*_MTV_Lights[n].z));
                    luz+=_MTV_Colors[n].rgb*f*f*_MTV_Lights[n].w;
                }
                c.rgb*=min(luz,float3(1.45,1.45,1.45));
                c.rgb+=.009*sin(i.world.x*5+t)*sin(i.world.y*3-t)*w;
                return c;
            }
ENDCG
        }
    }
}
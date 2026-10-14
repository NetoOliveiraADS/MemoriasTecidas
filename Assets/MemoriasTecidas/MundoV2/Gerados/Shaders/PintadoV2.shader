Shader "MemoriasTecidas/MundoV2/Pintado"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Cor",Color)=(1,1,1,1)
        _Efeito("Particula",Float)=0
        _Sombra("Sombra",Float)=0
        _Agua("Ondulacao",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"  }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Always" }

            CGPROGRAM
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            #define SAMPLE_TEX(uv) tex2D(_MainTex, uv)
            #define TO_CLIP(pos) UnityObjectToClipPos(pos)
            #define TO_WORLD(pos) mul(unity_ObjectToWorld,pos).xyz
            float4 _Color; float _Efeito, _Sombra, _Agua;

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; float4 color:COLOR; };
            float4 _MTV_Ambient, _MTV_Lights[16], _MTV_Colors[16];
            float _MTV_Active, _MTV_Time, _MTV_Quality;
            int _MTV_Count;
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS=TO_CLIP(v.positionOS); o.world=TO_WORLD(v.positionOS).xy;
                o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            float4 frag(Varyings i):SV_Target
            {
                if (_Efeito>.5)
                {
                    float2 q=i.uv*2-1;
                    float a=saturate(1-dot(q,q));
                    return float4(i.color.rgb,i.color.a*a*a);
                }
                float2 uv=i.uv;
                float water=smoothstep(.53,.62,uv.y)*_Agua*step(.5,_MTV_Quality);
                uv.x+=sin(i.world.y*2.4+_MTV_Time*.65)*.0009*water;
                uv.y+=sin(i.world.x*1.8+_MTV_Time*.5)*.0006*water;
                float4 c=SAMPLE_TEX(uv)*i.color;
                if (_Sombra>.5) return c;
                float3 light=lerp(float3(1,1,1),_MTV_Ambient.rgb,saturate(_MTV_Active));
                [loop] for(int n=0;n<16;n++)
                {
                    if(n>=_MTV_Count) break;
                    float2 delta=i.world-_MTV_Lights[n].xy;
                    float fall=saturate(1-dot(delta,delta)/max(.01,_MTV_Lights[n].z*_MTV_Lights[n].z));
                    light+=_MTV_Colors[n].rgb*fall*fall*_MTV_Lights[n].w;
                }
                float shimmer=.012*sin(i.world.x*9+_MTV_Time*1.4)*sin(i.world.y*6-_MTV_Time)*water;
                c.rgb=c.rgb*min(light,float3(1.45,1.45,1.45))+shimmer;
                return c;
            }
ENDCG
        }
    }
}

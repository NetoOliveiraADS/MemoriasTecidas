Shader "MemoriasTecidas/VidaV22/Pintado"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Cor",Color)=(1,1,1,1)
        _CorteAlfa("Corte de alfa",Range(0,.1))=.02
        _Efeito("Partícula",Float)=0
        _Sombra("Sombra",Float)=0
        _Agua("Ondulação",Float)=0
        _MTM_Deform("Deformação local",Vector)=(0,0,0,0)
        _MTM_Base("Base do cenário",Vector)=(0,1,0,0)
        _MTM_Relevo("Relevo pintado",Range(0,1))=0
        _MTM_ArteOriginal("Acabamento original",Float)=0
        _MTM_RecorteOriginal("Limites da arte",Vector)=(0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Always" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Assets/MemoriasTecidas/Shaders/Includes/MTMIluminacao.cginc"
            #include "Assets/MemoriasTecidas/Shaders/Includes/MTMDetalheOriginal.cginc"
            sampler2D _MainTex;float4 _MainTex_TexelSize,_Color,_MTM_Base;
            float _Efeito,_Sombra,_Agua,_CorteAlfa,_MTM_Relevo,_MTM_ArteOriginal;
            float4 _MTM_RecorteOriginal;
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;float4 color:COLOR;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;float4 color:COLOR;};
            V vert(A a)
            {
                V o;a.pos.xyz=MTMDeformar(a.pos.xyz,a.local);o.pos=UnityObjectToClipPos(a.pos);
                o.world=mul(unity_ObjectToWorld,a.pos).xy;o.uv=a.uv;o.color=a.color*_Color;return o;
            }
            float4 frag(V i):SV_Target
            {
                if(_Efeito>.5){float2 q=i.uv*2-1;float a=saturate(1-dot(q,q));return float4(i.color.rgb,i.color.a*a*a);}
                float water=smoothstep(.53,.62,i.uv.y)*_Agua*step(.5,_MTV_Quality)*_MTV_Motion;
                float2 uv=i.uv+float2(sin(i.world.y*2.4+_MTV_Time*.65)*.0009,sin(i.world.x*1.8+_MTV_Time*.5)*.0006)*water;
                float4 amostra=tex2D(_MainTex,uv);
                if(_MTM_ArteOriginal>.5&&_Sombra<.5)
                    amostra=MTMDetalharOriginal(_MainTex,uv,amostra,_MainTex_TexelSize,_MTM_RecorteOriginal,0,_Agua);
                float4 c=amostra*i.color;
                clip(c.a-_CorteAlfa);
                if(_Sombra>.5)return float4(i.color.rgb,c.a);
                if(_MTM_ArteOriginal<.5&&_MTM_Relevo>.001&&_MTV_Quality>1.5)
                {
                    float3 vizinho=tex2D(_MainTex,uv+_MainTex_TexelSize.xy*1.5).rgb*i.color.rgb;
                    float relevo=clamp(dot(c.rgb-vizinho,float3(.299,.587,.114)),-.12,.12);
                    c.rgb*=1+relevo*_MTM_Relevo*_MTV_Sun.y;
                }
                float baseSuja=1-smoothstep(_MTM_Base.x,_MTM_Base.x+max(.1,_MTM_Base.y)*.24,i.world.y);
                c.rgb*=1-baseSuja*saturate(_MTM_Base.z)*.13;
                c.rgb=MTMPintura(c.rgb,i.world);
                c.rgb+=.006*sin(i.world.x*9+_MTV_Time)*sin(i.world.y*6-_MTV_Time)*water*_MTV_Sun.y;
                return c;
            }
            ENDCG
        }
    }
}

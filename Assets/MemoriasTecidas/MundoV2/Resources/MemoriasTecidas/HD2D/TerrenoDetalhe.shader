Shader "MemoriasTecidas/HD2D/TerrenoDetalhe"
{
    Properties
    {
        [PerRendererData] _MainTex("Piso original", 2D) = "white" {}
        [PerRendererData] _DetailTex("Acabamento", 2D) = "white" {}
        [PerRendererData] _Mistura("Intensidade", Range(0, .5)) = .28
        [PerRendererData] _Repeticao("Metros por repetição", Float) = 6
        _Color("Cor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Assets/MemoriasTecidas/MundoV2/Qualidade/Shaders/MTMIluminacao.cginc"
            sampler2D _MainTex, _DetailTex;
            float4 _Color;
            float _Mistura, _Repeticao;
            struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; float4 color:COLOR; };
            V vert(A a)
            {
                V o; o.pos=UnityObjectToClipPos(a.pos); o.uv=a.uv;
                o.world=mul(unity_ObjectToWorld,a.pos).xy; o.color=a.color*_Color; return o;
            }
            float4 frag(V i):SV_Target
            {
                float4 baseColor=tex2D(_MainTex,i.uv);
                float3 detalhe=tex2D(_DetailTex,i.world/max(.1,_Repeticao)).rgb;
                // Conserva as ruas, cruzamentos e jardins desenhados na textura existente.
                // O acabamento não muda transparência, geometria, colisores nem o mapa.
                float3 cor=lerp(baseColor.rgb,baseColor.rgb*(.65+detalhe*.7),saturate(_Mistura));
                cor*=i.color.rgb;
                return float4(MTMPintura(cor,i.world),baseColor.a*i.color.a);
            }
            ENDCG
        }
    }
}

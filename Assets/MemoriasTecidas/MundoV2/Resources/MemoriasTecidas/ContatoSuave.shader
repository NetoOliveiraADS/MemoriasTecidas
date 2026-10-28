Shader "MemoriasTecidas/Acabamento/ContatoSuave"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _MTV_Sun;
            float4 _MTM_RefinoOriginal;
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A a){V o;o.pos=UnityObjectToClipPos(a.pos);o.uv=a.uv;return o;}
            float4 frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;float borda=1-smoothstep(.04,1,dot(p,p));
                if(_MTM_RefinoOriginal.x>.5)
                {
                    float nucleo=1-smoothstep(0,.30,dot(p,p));
                    float alpha=(borda*.15+nucleo*.11)*_MTV_Sun.w*lerp(.5,1,_MTV_Sun.y);
                    return float4(.16,.13,.10,alpha);
                }
                return float4(.12,.16,.16,borda*borda*.28*_MTV_Sun.w*lerp(.5,1,_MTV_Sun.y));
            }
            ENDCG
        }
    }
}

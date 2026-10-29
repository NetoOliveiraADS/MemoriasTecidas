Shader "MemoriasTecidas/AmostraTecida"
{
    Properties { }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Assets/MemoriasTecidas/Shaders/Includes/MTMIluminacao.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 world:TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.world=mul(unity_ObjectToWorld,v.vertex).xy; return o; }
            fixed4 frag(v2f i):SV_Target { return fixed4(MTMPintura(i.color.rgb,i.world),i.color.a); }
            ENDCG
        }
    }
}

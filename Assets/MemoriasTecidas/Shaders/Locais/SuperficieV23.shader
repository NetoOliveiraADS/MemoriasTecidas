Shader "MemoriasTecidas/LugaresV23/Superficie"
{
    Properties
    {
        [PerRendererData] _MainTex("Textura",2D)="white"{}
        _Color("Cor",Color)=(1,1,1,1)
        [PerRendererData] _MTM_SuavizarSolo("Leitura do piso",Range(0,1))=0
        _Agua("Água",Float)=0
        _Ondulacao("Ondulação",Float)=.009
        _Velocidade("Velocidade",Float)=.65
        [PerRendererData] _MTM_DistanciaBorda("Distância da margem",2D)="white"{}
        _MTM_ContornoAtivo("Transição natural",Float)=0
        _MTM_LarguraBorda("Largura da transição",Float)=.12
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
            #include "Assets/MemoriasTecidas/Shaders/Includes/MTMIluminacao.cginc"
            #include "Assets/MemoriasTecidas/Shaders/Includes/MTMDetalheOriginal.cginc"
            sampler2D _MainTex,_MTM_DistanciaBorda;float4 _MainTex_TexelSize,_Color;
            float _MTM_SuavizarSolo;
            float _Agua,_Ondulacao,_Velocidade,_MTM_ContornoAtivo,_MTM_LarguraBorda;
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;float2 borda:TEXCOORD1;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;float2 borda:TEXCOORD2;};
            V vert(A a){V o;o.pos=UnityObjectToClipPos(a.pos);o.uv=a.uv;o.borda=a.borda;o.world=mul(unity_ObjectToWorld,a.pos).xy;return o;}
            float4 RepeticaoSuave(float2 uv)
            {
                if(_MTV_Quality<.5)return tex2D(_MainTex,uv);
                // Mistura apenas uma faixa estreita da emenda; derivadas estáveis evitam salto de mip.
                float2 u=frac(uv),dx=ddx(uv),dy=ddy(uv);
                float2 peso=smoothstep(.9,1,abs(u-.5)*2)*.5;
                float4 a=tex2Dgrad(_MainTex,u,dx,dy);
                if(max(peso.x,peso.y)<.001)return a;
                float4 b=tex2Dgrad(_MainTex,float2(1-u.x,u.y),dx*float2(-1,1),dy*float2(-1,1));
                float4 c=tex2Dgrad(_MainTex,float2(u.x,1-u.y),dx*float2(1,-1),dy*float2(1,-1));
                float4 d=tex2Dgrad(_MainTex,1-u,-dx,-dy);
                return lerp(lerp(a,b,peso.x),lerp(c,d,peso.x),peso.y);
            }
            float4 frag(V i):SV_Target
            {
                float movimento=step(.5,_MTV_Quality)*_MTV_Motion;
                float t=_MTV_Time*_Velocidade*movimento;
                float2 onda=float2(sin(i.world.y*1.7+t)+.35*sin(i.world.x*2.9-t*.7),sin(i.world.x*1.2-t*.8));
                float2 uv=i.uv+onda*_Ondulacao*_Agua*movimento;
                float4 amostra=RepeticaoSuave(uv);
                float4 detalhe=MTMDetalharOriginal(_MainTex,uv,amostra,_MainTex_TexelSize,float4(0,0,1,1),1,_Agua);
                float emenda=max(abs(frac(uv.x)-.5),abs(frac(uv.y)-.5))*2;
                amostra=lerp(detalhe,amostra,smoothstep(.9,1,emenda));
                if(_Agua<.5 && _MTM_SuavizarSolo>.001 && _MTV_Quality>.5 &&
                    _MTM_RefinoOriginal.x>.5 && _MTM_DetalheOriginal.z>.5)
                {
                    float2 passo=_MainTex_TexelSize.xy*2;
                    float3 media=(tex2D(_MainTex,uv+float2(passo.x,0)).rgb+
                        tex2D(_MainTex,uv-float2(passo.x,0)).rgb+
                        tex2D(_MainTex,uv+float2(0,passo.y)).rgb+
                        tex2D(_MainTex,uv-float2(0,passo.y)).rgb)*.25;
                    amostra.rgb=lerp(amostra.rgb,media,saturate(_MTM_SuavizarSolo));
                }
                float4 c=amostra*_Color;
                float distancia=tex2D(_MTM_DistanciaBorda,i.borda).r*.6;
                if(_MTM_ContornoAtivo>.5)
                {
                    float irregularidade=lerp(.8,1.2,MTMRuido(i.world*5.2));
                    c.a*=smoothstep(0,max(.01,_MTM_LarguraBorda)*irregularidade,distancia);
                }
                if(_Agua>.5)
                {
                    // Margem rasa no mesmo contorno da água; nenhum objeto ou colisor adicional.
                    float rasa=(1-smoothstep(.06,.42,distancia))*saturate(_MTM_ContornoAtivo);
                    c.rgb=lerp(c.rgb,c.rgb*float3(1.13,1.06,.88)+float3(.022,.018,.008),rasa*.55);
                    float fase1=i.world.x*.82+i.world.y*1.41+t;
                    float fase2=i.world.x*1.9-i.world.y*.67-t*.7;
                    float2 gradiente=float2(.82,1.41)*cos(fase1)+float2(1.9,-.67)*cos(fase2)*.35;
                    float3 normal=normalize(float3(gradiente*min(_Ondulacao*5,.15),1));
                    float3 meio=normalize(float3(-_MTV_Sun.x*.45,.35,1.5));
                    float reflexo=pow(saturate(dot(normal,meio)),28);
                    float brilho=reflexo*.024*saturate(_MTV_Sun.y)*(1-rasa*.75)*step(.5,_MTV_Quality);
                    c.rgb+=brilho*float3(.86,.94,1);
                }
                c.rgb=MTMPintura(c.rgb,i.world);return c;
            }
            ENDCG
        }
    }
}

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
        [PerRendererData] _MTM_Fluxo("Correnteza (xy no mundo, z = 1/unidades por repetição)",Vector)=(0,0,.1667,0)
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
            float4 _MTM_Fluxo;
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
            float AcasoCelula(float2 c){return frac(sin(dot(c,float2(127.1,311.7)))*43758.5453);}
            // Um brilho possível por célula (~.45 x .18 unidades): um tracinho deitado que pisca no
            // próprio ritmo; só aparecem em grupos (manchas de ~3 unidades que andam devagar).
            float BrilhosAgua(float2 w,float tempo)
            {
                float2 g=w*float2(2.2,5.5);
                float2 c=floor(g),f=frac(g);
                float h1=AcasoCelula(c),h2=AcasoCelula(c+17.13),h3=AcasoCelula(c+41.7);
                float2 e=(f-float2(.2+.6*h1,.25+.5*h2))/float2(.16,.11);
                float forma=saturate(1-dot(e,e));
                float pisca=saturate(sin(tempo*(1.7+2.6*h3)+h1*6.2832)*1.8-.8);
                float grupo=smoothstep(.45,.80,MTMRuido(w*.32+float2(tempo*.04,0)));
                return forma*forma*pisca*step(.45,h3)*grupo;
            }
            float4 frag(V i):SV_Target
            {
                float movimento=step(.5,_MTV_Quality)*_MTV_Motion;
                float t=_MTV_Time*_Velocidade*movimento;
                float2 onda=float2(sin(i.world.y*1.7+t)+.35*sin(i.world.x*2.9-t*.7),sin(i.world.x*1.2-t*.8));
                float2 uv=i.uv+onda*_Ondulacao*_Agua*movimento;
                // Correnteza: a textura da água desce o rio devagar (o ondulado acima fica parado no lugar).
                float tempo=_MTV_Time*movimento;
                uv-=_MTM_Fluxo.xy*_MTM_Fluxo.z*tempo*step(.5,_Agua);
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
                    float sol=saturate(_MTV_Sun.y);
                    float2 w=i.world;
                    float2 arrasto=w-_MTM_Fluxo.xy*tempo*1.3;
                    // Manchas de luz e sombra largas que descem com a correnteza.
                    float manchas=MTMRuido(arrasto*float2(.42,.75))*.6+MTMRuido(arrasto*float2(1.1,1.9)+t*.12)*.4;
                    c.rgb*=lerp(1,.94+.12*manchas,movimento);
                    // Brilhos do sol: tracinhos curtos e deitados que piscam em grupos e descem com a água.
                    // À noite viram pontos fracos de luar.
                    float faisca=BrilhosAgua(w-_MTM_Fluxo.xy*tempo*1.6,tempo)*movimento;
                    c.rgb+=faisca*lerp(float3(.12,.15,.22),float3(.62,.58,.48),sol)*(1-rasa*.4);
                    // Espuma: ondinhas chegam à margem e voltam; na beira fica uma espuma em pedaços.
                    float quebra=MTMRuido(w*2.3+float2(tempo*.21,0));
                    float quebraFina=MTMRuido(w*5.1-float2(0,tempo*.3));
                    float chegada=sin(distancia*24+tempo*1.6+quebra*4);
                    float costa=1-smoothstep(.03,.30,distancia);
                    float espuma=smoothstep(.55,.95,chegada)*costa*(.35+.65*quebraFina)*movimento;
                    espuma+=(1-smoothstep(0,.06,distancia))*smoothstep(.25,.75,quebraFina+quebra*.4);
                    c.rgb=lerp(c.rgb,float3(.90,.92,.88)*(.72+.28*sol),saturate(espuma)*.5*saturate(_MTM_ContornoAtivo));
                    float fase1=w.x*.82+w.y*1.41+t;
                    float fase2=w.x*1.9-w.y*.67-t*.7;
                    float2 gradiente=float2(.82,1.41)*cos(fase1)+float2(1.9,-.67)*cos(fase2)*.35;
                    float3 normal=normalize(float3(gradiente*min(_Ondulacao*5,.15),1));
                    float3 meio=normalize(float3(-_MTV_Sun.x*.45,.35,1.5));
                    float reflexo=pow(saturate(dot(normal,meio)),28);
                    float brilho=reflexo*.05*sol*(1-rasa*.75)*step(.5,_MTV_Quality);
                    c.rgb+=brilho*float3(.86,.94,1);
                }
                c.rgb=MTMPintura(c.rgb,i.world);return c;
            }
            ENDCG
        }
    }
}

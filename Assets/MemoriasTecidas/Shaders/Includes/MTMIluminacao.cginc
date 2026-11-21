#ifndef MTM_ILUMINACAO_INCLUIDA
#define MTM_ILUMINACAO_INCLUIDA
float4 _MTV_Ambient,_MTV_Lights[16],_MTV_Colors[16],_MTV_Art,_MTV_Sun;
float _MTV_Active,_MTV_Time,_MTV_Quality,_MTV_Motion;
int _MTV_Count;
float4 _MTM_Deform;
float MTMRuido(float2 p)
{
    float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
    float4 n=frac(sin(float4(dot(a,float2(127.1,311.7)),dot(a+float2(1,0),float2(127.1,311.7)),dot(a+float2(0,1),float2(127.1,311.7)),dot(a+1,float2(127.1,311.7))))*43758.5453);
    return lerp(lerp(n.x,n.y,f.x),lerp(n.z,n.w,f.x),f.y);
}
float3 MTMLuz(float2 pos)
{
    float3 luz=lerp(float3(1,1,1),_MTV_Ambient.rgb,saturate(_MTV_Active));
    [loop]for(int n=0;n<16;n++)
    {
        if(n>=_MTV_Count)break;
        float2 delta=pos-_MTV_Lights[n].xy;
        float queda=saturate(1-dot(delta,delta)/max(.01,_MTV_Lights[n].z*_MTV_Lights[n].z));
        queda=queda*queda*(3-2*queda);
        luz+=_MTV_Colors[n].rgb*queda*_MTV_Lights[n].w*.86;
    }
    return min(luz,float3(1.4,1.4,1.4));
}
// Sombras de nuvens que atravessam devagar os lugares abertos: manchas de ~20 unidades, no máximo 8%
// mais escuras. Não aparecem em interiores, à noite, na qualidade leve ou com "reduzir movimento".
float MTMNuvens(float2 pos)
{
    float forca=.08*saturate(_MTV_Sun.y)*saturate(_MTV_Active)*_MTV_Motion*step(.5,_MTV_Quality)*(1-step(.5,_MTV_Sun.z));
    if(forca<=0)return 1;
    float2 q=pos*.045+float2(.021,.007)*_MTV_Time;
    float n=MTMRuido(q)*.62+MTMRuido(q*2.07+float2(5.3,1.7))*.38;
    return 1-smoothstep(.50,.80,n)*forca;
}
float3 MTMPintura(float3 cor,float2 pos)
{
    float3 iluminada=cor*MTMLuz(pos)*MTMNuvens(pos);
    float luminancia=dot(iluminada,float3(.2126,.7152,.0722));
    float3 graduada=lerp(float3(luminancia,luminancia,luminancia),iluminada,clamp(_MTV_Art.y,.7,1.3));
    graduada*=clamp(_MTV_Art.x,.7,1.4);
    return lerp(iluminada,graduada,saturate(_MTV_Active));
}
// Rajadas de vento que atravessam o lugar de oeste para leste: chegam antes às árvores mais a oeste.
// Vai de .55 (calmaria) a 1 (rajada). A mesma conta está em MTMAmbienteV2.Rajada (C#).
float MTMRajada(float x,float fase,float t)
{
    float frente=.5+.5*sin(x*.12-t*.45);
    float pulso=.5+.5*sin(t*.23+fase*.7);
    return .55+.45*frente*(.55+.45*pulso);
}
float3 MTMDeformar(float3 p,float2 uv)
{
    if(_MTM_Deform.x<.5)return p;
    float intensidade=_MTM_Deform.y*_MTV_Art.w*_MTV_Motion*step(.5,_MTV_Quality);
    float fase=_MTM_Deform.w,t=_MTV_Time;
    float rajada=MTMRajada(unity_ObjectToWorld._m03,fase,t);
    float peso;
    if(_MTM_Deform.x<1.5)
    {
        // Copa: a ponta curva um pouco mais que o meio; a rajada empurra a árvore para leste e ela volta.
        // Nas palmeiras (início alto, só a coroa se mexe) o movimento começa um pouco abaixo da coroa.
        float inicio=_MTM_Deform.z-.08*step(.65,_MTM_Deform.z);
        peso=smoothstep(inicio,1,uv.y);
        float curva=peso*(.6+.4*peso);
        float balanco=sin(t*1.15+fase)*.75+sin(t*2.35+fase*1.7)*.25;
        float empurrao=(rajada-.55)*1.6;
        p.x+=(balanco*rajada*3.2+empurrao*1.8)*intensidade*curva;
        // Folhas: tremor curto que muda de lugar pela copa.
        float tremor=sin(t*7.1+uv.x*11+uv.y*7+fase*2.1)*sin(t*5.3+uv.y*13-uv.x*5+fase);
        p.x+=tremor*intensidade*.6*peso*rajada;
        p.y+=(sin(t*1.6+uv.x*5+fase)*.16+tremor*.22)*intensidade*peso*rajada;
    }
    else
    {
        // Rede: balança no meio e sente as rajadas.
        peso=pow(sin(uv.x*3.14159265),2)*(1-smoothstep(.74,.98,uv.y));
        float vai=sin(t*1.1+fase)+.18*sin(t*2.3+fase*1.4);
        p.x+=vai*intensidade*peso*(.75+.45*rajada);
        p.y+=sin(t*1.1+fase+.4)*intensidade*.3*peso;
    }
    return p;
}
#endif

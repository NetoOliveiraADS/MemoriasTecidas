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
float3 MTMPintura(float3 cor,float2 pos)
{
    float3 iluminada=cor*MTMLuz(pos);
    float luminancia=dot(iluminada,float3(.2126,.7152,.0722));
    float3 graduada=lerp(float3(luminancia,luminancia,luminancia),iluminada,clamp(_MTV_Art.y,.7,1.3));
    graduada*=clamp(_MTV_Art.x,.7,1.4);
    return lerp(iluminada,graduada,saturate(_MTV_Active));
}
float3 MTMDeformar(float3 p,float2 uv)
{
    if(_MTM_Deform.x<.5)return p;
    float intensidade=_MTM_Deform.y*_MTV_Art.w*_MTV_Motion*step(.5,_MTV_Quality);
    float fase=_MTM_Deform.w,t=_MTV_Time;
    float rajada=.72+.28*sin(t*.37+fase*.3);
    float peso;
    if(_MTM_Deform.x<1.5)
    {
        peso=smoothstep(_MTM_Deform.z,1,uv.y);
        p.x+=(sin(t*1.25+fase)+.22*sin(t*2.7+uv.x*7+fase))*intensidade*peso*rajada;
        p.y+=sin(t*1.6+uv.x*5+fase)*intensidade*.16*peso;
    }
    else
    {
        peso=pow(sin(uv.x*3.14159265),2)*(1-smoothstep(.74,.98,uv.y));
        p.x+=sin(t*1.1+fase)*intensidade*peso;
        p.y+=sin(t*1.1+fase+.4)*intensidade*.3*peso;
    }
    return p;
}
#endif

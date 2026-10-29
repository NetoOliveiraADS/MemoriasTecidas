#ifndef MTM_DETALHE_ORIGINAL_INCLUIDO
#define MTM_DETALHE_ORIGINAL_INCLUIDO
// x: nitidez, y: relevo, z: ativo. Configurado uma vez ao abrir a jornada.
float4 _MTM_DetalheOriginal;
// Refinamento opcional da arte existente.
float4 _MTM_RefinoOriginal;
float2 MTMLimitarAmostra(float2 uv, float4 recorte, float2 texel, float repetida)
{
    if(repetida>.5) return uv; // Mantém Repeat/Mirror do material original.
    float2 margem=min(texel*.5,max(float2(0,0),(recorte.zw-recorte.xy)*.49));
    return clamp(uv,recorte.xy+margem,recorte.zw-margem);
}
float4 MTMDetalharOriginal(sampler2D textura,float2 uv,float4 centro,float4 texel,float4 recorte,float repetida,float agua)
{
    if(_MTM_DetalheOriginal.z<.5 || _MTV_Quality<.5) return centro;
    // Suprime o realce ao reduzir a imagem, para evitar cintilação de telhas e folhas.
    float escala=max(length(ddx(uv)*texel.zw),length(ddy(uv)*texel.zw));
    float peso=1-smoothstep(1.25,2.25,escala);
    if(peso<.001 || centro.a<.98) return centro;
    float2 dx=float2(texel.x,0),dy=float2(0,texel.y);
    float4 l=tex2D(textura,MTMLimitarAmostra(uv-dx,recorte,texel.xy,repetida));
    float4 r=tex2D(textura,MTMLimitarAmostra(uv+dx,recorte,texel.xy,repetida));
    float4 b=tex2D(textura,MTMLimitarAmostra(uv-dy,recorte,texel.xy,repetida));
    float4 t=tex2D(textura,MTMLimitarAmostra(uv+dy,recorte,texel.xy,repetida));
    // Não acentua bordas transparentes nem amostra sprites vizinhos do atlas.
    peso*=step(.98,min(min(l.a,r.a),min(b.a,t.a)));
    float3 lum=float3(.2126,.7152,.0722);
    float c=dot(centro.rgb,lum);
    float4 viz=float4(dot(l.rgb,lum),dot(r.rgb,lum),dot(b.rgb,lum),dot(t.rgb,lum));
    float detalhe=clamp(c-dot(viz,float4(.25,.25,.25,.25)),-.08,.08);
    float nitidez=detalhe/max(.12,c)*_MTM_DetalheOriginal.x;
    float gradiente=clamp((viz.x-viz.y)*-_MTV_Sun.x*.6+(viz.z-viz.w)*.8,-.1,.1);
    float solExterior=saturate(_MTV_Sun.y)*(1-saturate(_MTV_Sun.z));
    float relevo=gradiente*_MTM_DetalheOriginal.y*step(1.5,_MTV_Quality)*solExterior*(1-saturate(agua));
    float ganho=clamp((nitidez+relevo)*peso*saturate(_MTV_Art.z),-.06,.06);
    if(_MTM_RefinoOriginal.x>.5)
    {
        // Relevo suave inferido da luminância, sem substituir a arte nem alterar o alfa.
        float minimo=min(c,min(min(viz.x,viz.y),min(viz.z,viz.w)));
        float maximo=max(c,max(max(viz.x,viz.y),max(viz.z,viz.w)));
        float bordaForte=smoothstep(.08,.28,maximo-minimo);
        float2 inclinacao=clamp(float2(viz.x-viz.y,viz.z-viz.w),-.15,.15);
        float3 normal=normalize(float3(inclinacao*1.6,1));
        float3 luz=normalize(float3(-_MTV_Sun.x,.65,1.2));
        float resposta=dot(normal,luz)-luz.z;
        float nivel=lerp(.45,1,step(1.5,_MTV_Quality));
        float luzLocal=resposta*_MTM_RefinoOriginal.y*nivel*solExterior*(1-saturate(agua));
        float cavidade=min(0,detalhe)*.12*_MTM_RefinoOriginal.y*nivel*(1-saturate(agua));
        ganho=clamp((nitidez*(1-bordaForte*.7)+luzLocal+cavidade)*peso*saturate(_MTV_Art.z),-.045,.045);
    }
    // Um único ganho por pixel conserva a proporção entre as cores e o alfa original.
    centro.rgb*=1+ganho;
    return centro;
}
#endif

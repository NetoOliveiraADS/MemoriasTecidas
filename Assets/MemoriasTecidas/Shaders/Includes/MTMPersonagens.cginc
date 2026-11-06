#ifndef MTM_PERSONAGENS_INCLUIDO
#define MTM_PERSONAGENS_INCLUIDO
// x: ativo; y: saturação; z: suavização; w: preenchimento comum.
float4 _MTM_PerfilPersonagens;
float _MTM_Pessoa;
float4 _MTM_RecortePessoa;

float4 MTMPrepararPessoa(sampler2D textura, float2 uv, float4 fonte, float4 texel)
{
    if(_MTM_Pessoa<.5 || _MTM_PerfilPersonagens.x<.5) return fonte;
    float3 cor=fonte.rgb;
    // Não mistura transparência ou quadros vizinhos, nem suaviza olhos/contornos escuros.
    if(_MTV_Quality>.5 && _MTM_PerfilPersonagens.z>.001 && fonte.a>.98)
    {
        float2 margem=min(texel.xy*.5,max(float2(0,0),(_MTM_RecortePessoa.zw-_MTM_RecortePessoa.xy)*.49));
        float2 baixo=_MTM_RecortePessoa.xy+margem,alto=_MTM_RecortePessoa.zw-margem;
        float4 l=tex2D(textura,clamp(uv-float2(texel.x,0),baixo,alto));
        float4 r=tex2D(textura,clamp(uv+float2(texel.x,0),baixo,alto));
        float4 b=tex2D(textura,clamp(uv-float2(0,texel.y),baixo,alto));
        float4 t=tex2D(textura,clamp(uv+float2(0,texel.y),baixo,alto));
        float opaco=step(.98,min(min(l.a,r.a),min(b.a,t.a)));
        float detalhe=smoothstep(.04,.18,dot(cor,float3(.2126,.7152,.0722)));
        cor=lerp(cor,(l.rgb+r.rgb+b.rgb+t.rgb)*.25,_MTM_PerfilPersonagens.z*opaco*detalhe);
    }
    float lum=dot(cor,float3(.2126,.7152,.0722));
    fonte.rgb=lerp(float3(lum,lum,lum),cor,saturate(_MTM_PerfilPersonagens.y));
    return fonte; // Alfa original, silhueta e recorte intactos.
}

float3 MTMPreencherPessoa(float3 cor, float ganhoLegado)
{
    float ativo=step(.5,_MTM_Pessoa)*step(.5,_MTM_PerfilPersonagens.x);
    float ganho=lerp(ganhoLegado,clamp(_MTM_PerfilPersonagens.w,0,.18),ativo);
    if(ganho<=0) return cor;
    float pico=max(cor.r,max(cor.g,cor.b));
    return cor*min(1+ganho,1/max(.001,pico));
}
#endif

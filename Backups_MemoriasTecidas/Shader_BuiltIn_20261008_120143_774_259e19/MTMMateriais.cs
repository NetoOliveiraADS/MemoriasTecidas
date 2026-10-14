using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoriasTecidas.MundoV2.Editor
{
    // Gera apenas a variante do pipeline presente; não instala nem altera o pipeline.
    public static class MTMMateriais
    {
        public static Material Preparar(string raiz, string nome, float efeito = 0f, float sombra = 0f, float agua = 0f)
        {
            string dir = raiz + "/Gerados";
            Directory.CreateDirectory(dir + "/Shaders");
            bool urp = GraphicsSettings.currentRenderPipeline != null &&
                GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal");
            string codigo = Codigo(urp);
            string shaderPath = dir + "/Shaders/PintadoV2.shader";
            if (!File.Exists(shaderPath) || File.ReadAllText(shaderPath) != codigo)
            {
                File.WriteAllText(shaderPath, codigo);
                AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport);
            }
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null) throw new System.IO.IOException("Shader V2 não importado. Confira a Console.");
            string path = dir + "/" + nome + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader; m.SetColor("_Color", Color.white);
            m.SetFloat("_Efeito", efeito); m.SetFloat("_Sombra", sombra); m.SetFloat("_Agua", agua);
            EditorUtility.SetDirty(m); return m;
        }

        private static string Codigo(bool urp)
        {
            string inicio = urp ? @"
            HLSLPROGRAM
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            #define SAMPLE_TEX(uv) SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv)
            #define TO_CLIP(pos) TransformObjectToHClip(pos.xyz)
            #define TO_WORLD(pos) TransformObjectToWorld(pos.xyz)
            CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _Efeito, _Sombra, _Agua;
            CBUFFER_END
" : @"
            CGPROGRAM
            #include ""UnityCG.cginc""
            sampler2D _MainTex;
            #define SAMPLE_TEX(uv) tex2D(_MainTex, uv)
            #define TO_CLIP(pos) UnityObjectToClipPos(pos)
            #define TO_WORLD(pos) mul(unity_ObjectToWorld,pos).xyz
            float4 _Color; float _Efeito, _Sombra, _Agua;
";
            string fim = urp ? "ENDHLSL" : "ENDCG";
            string tag = urp ? "\"RenderPipeline\"=\"UniversalPipeline\"" : "";
            return @"Shader ""MemoriasTecidas/MundoV2/Pintado""
{
    Properties
    {
        [PerRendererData] _MainTex(""Sprite"",2D)=""white""{}
        _Color(""Cor"",Color)=(1,1,1,1)
        _Efeito(""Particula"",Float)=0
        _Sombra(""Sombra"",Float)=0
        _Agua(""Ondulacao"",Float)=0
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" ""CanUseSpriteAtlas""=""True"" " + tag + @" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { ""LightMode""=""SRPDefaultUnlit"" }
" + inicio + @"
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; float4 color:COLOR; };
            float4 _MTV_Ambient, _MTV_Lights[16], _MTV_Colors[16];
            float _MTV_Active, _MTV_Time, _MTV_Quality;
            int _MTV_Count;
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS=TO_CLIP(v.positionOS); o.world=TO_WORLD(v.positionOS).xy;
                o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            float4 frag(Varyings i):SV_Target
            {
                if (_Efeito>.5)
                {
                    float2 q=i.uv*2-1;
                    float a=saturate(1-dot(q,q));
                    return float4(i.color.rgb,i.color.a*a*a);
                }
                float2 uv=i.uv;
                float water=smoothstep(.53,.62,uv.y)*_Agua*step(.5,_MTV_Quality);
                uv.x+=sin(i.world.y*2.4+_MTV_Time*.65)*.0009*water;
                uv.y+=sin(i.world.x*1.8+_MTV_Time*.5)*.0006*water;
                float4 c=SAMPLE_TEX(uv)*i.color;
                if (_Sombra>.5) return c;
                float3 light=lerp(float3(1,1,1),_MTV_Ambient.rgb,saturate(_MTV_Active));
                [loop] for(int n=0;n<16;n++)
                {
                    if(n>=_MTV_Count) break;
                    float2 delta=i.world-_MTV_Lights[n].xy;
                    float fall=saturate(1-dot(delta,delta)/max(.01,_MTV_Lights[n].z*_MTV_Lights[n].z));
                    light+=_MTV_Colors[n].rgb*fall*fall*_MTV_Lights[n].w;
                }
                float shimmer=.012*sin(i.world.x*9+_MTV_Time*1.4)*sin(i.world.y*6-_MTV_Time)*water;
                c.rgb=c.rgb*min(light,float3(1.45,1.45,1.45))+shimmer;
                return c;
            }
" + fim + @"
        }
    }
}
";
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMMateriaisLugaresV23
    {
        public static Material Preparar()
        {
            string dir=MTMArtesLugaresV23.Raiz+"/Gerados";
            Directory.CreateDirectory(dir+"/Shaders");
            bool urp=GraphicsSettings.currentRenderPipeline!=null;
            if(urp&&!GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal"))
                throw new System.InvalidOperationException("Este complemento atende Built-in e URP. HDRP não foi preparado.");
            string path=dir+"/Shaders/SuperficieV23.shader",codigo=Codigo(urp);
            if(!File.Exists(path)||File.ReadAllText(path)!=codigo){File.WriteAllText(path,codigo);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);}
            Shader shader=AssetDatabase.LoadAssetAtPath<Shader>(path);
            if(shader==null)throw new IOException("Shader da superfície não importado; confira a Console.");
            string mat=dir+"/Superficie_V23.mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(mat);
            if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,mat);}
            m.shader=shader;EditorUtility.SetDirty(m);return m;
        }
        private static string Codigo(bool urp)
        {
            string inicio=urp?@"
            HLSLPROGRAM
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            #define SAMPLE_TEX(uv) SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv)
            #define TO_CLIP(pos) TransformObjectToHClip(pos.xyz)
            #define TO_WORLD(pos) TransformObjectToWorld(pos.xyz)
            CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _Agua,_Ondulacao,_Velocidade;
            CBUFFER_END
":@"
            CGPROGRAM
            #include ""UnityCG.cginc""
            sampler2D _MainTex;
            #define SAMPLE_TEX(uv) tex2D(_MainTex,uv)
            #define TO_CLIP(pos) UnityObjectToClipPos(pos)
            #define TO_WORLD(pos) mul(unity_ObjectToWorld,pos).xyz
            float4 _Color; float _Agua,_Ondulacao,_Velocidade;
";
            return @"Shader ""MemoriasTecidas/LugaresV23/Superficie""
{
    Properties
    {
        [PerRendererData] _MainTex(""Textura"",2D)=""white""{}
        _Color(""Cor"",Color)=(1,1,1,1)
        _Agua(""Agua"",Float)=0
        _Ondulacao(""Ondulacao"",Float)=.009
        _Velocidade(""Velocidade"",Float)=.65
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" "+(urp?"\"RenderPipeline\"=\"UniversalPipeline\"":"")+@" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { ""LightMode""="""+(urp?"SRPDefaultUnlit":"Always")+@""" }
"+inicio+@"
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            struct A { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; };
            float4 _MTV_Ambient,_MTV_Lights[16],_MTV_Colors[16];
            float _MTV_Active,_MTV_Time,_MTV_Quality; int _MTV_Count;
            V vert(A a){V o;o.pos=TO_CLIP(a.pos);o.uv=a.uv;o.world=TO_WORLD(a.pos).xy;return o;}
            float4 frag(V i):SV_Target
            {
                float w=_Agua*step(.5,_MTV_Quality),t=_MTV_Time*_Velocidade;
                float2 uv=i.uv;
                uv+=float2(sin(i.world.y*1.9+t),sin(i.world.x*1.3-t*.8))*_Ondulacao*w;
                float4 c=SAMPLE_TEX(uv)*_Color;
                float3 luz=lerp(float3(1,1,1),_MTV_Ambient.rgb,saturate(_MTV_Active));
                [loop] for(int n=0;n<16;n++)
                {
                    if(n>=_MTV_Count)break;
                    float2 d=i.world-_MTV_Lights[n].xy;
                    float f=saturate(1-dot(d,d)/max(.01,_MTV_Lights[n].z*_MTV_Lights[n].z));
                    luz+=_MTV_Colors[n].rgb*f*f*_MTV_Lights[n].w;
                }
                c.rgb*=min(luz,float3(1.45,1.45,1.45));
                c.rgb+=.009*sin(i.world.x*5+t)*sin(i.world.y*3-t)*w;
                return c;
            }
"+(urp?"ENDHLSL":"ENDCG")+@"
        }
    }
}";
        }
    }
}

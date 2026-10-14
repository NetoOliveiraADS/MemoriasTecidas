using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMAmbienteV2 : MonoBehaviour
    {
        public static int Qualidade = 1;
        public static float Sol = 1f, Fase = .33f;
        public static readonly string[] Nomes = { "Leve", "Equilibrada", "Alta" };
        private readonly Vector4[] posicoes = new Vector4[16];
        private readonly Vector4[] cores = new Vector4[16];
        private bool[] usadas;
        private Camera cameraPrincipal;

        public void Atualizar(MTMAreaDados area, float relogio)
        {
            Fase = relogio;
            float h = relogio*24f;
            float amanhecer = Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(5.5f,8f,h));
            float anoitecer = 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(17f,19.5f,h));
            Sol = amanhecer*anoitecer;
            Color dia = Color.Lerp(new Color(1.0f,.81f,.64f),Color.white,Mathf.Clamp01(Sol*1.5f));
            Color ambiente = area.interior ? new Color(.76f,.72f,.63f) : Color.Lerp(new Color(.25f,.34f,.53f),dia,Sol);
            Shader.SetGlobalFloat("_MTV_Active",1f);
            Shader.SetGlobalFloat("_MTV_Time",Time.time);
            Shader.SetGlobalFloat("_MTV_Quality",Qualidade);
            Shader.SetGlobalColor("_MTV_Ambient",ambiente);
            int limite=Qualidade==0?6:Qualidade==1?10:16;
            int count=0;
            if(cameraPrincipal==null)cameraPrincipal=Camera.main;
            Vector2 centro=cameraPrincipal!=null?(Vector2)cameraPrincipal.transform.position:Vector2.zero;
            if(area.luzes!=null)
            {
                if(usadas==null||usadas.Length!=area.luzes.Length)usadas=new bool[area.luzes.Length];
                System.Array.Clear(usadas,0,usadas.Length);
                for(int passo=0;passo<limite;passo++)
                {
                int melhor=-1;float distancia=float.PositiveInfinity;
                for(int i=0;i<area.luzes.Length;i++)
                {
                    if(usadas[i])continue;
                    MTMLuzDados candidata=area.luzes[i];
                    if(!candidata.sempre&&Sol>.995f)continue;
                    float dist=(new Vector2(candidata.x,candidata.y)-centro).sqrMagnitude;
                    if(dist<distancia){distancia=dist;melhor=i;}
                }
                if(melhor<0)break;
                usadas[melhor]=true;MTMLuzDados l=area.luzes[melhor];
                float forca=l.sempre?l.intensidade:l.intensidade*(1f-Sol);
                if(forca<.005f)continue;
                float variacao=1f+Mathf.Sin(Time.time*2.2f+count*1.73f)*.025f;
                posicoes[count]=new Vector4(l.x,l.y,l.raio,forca*variacao);
                cores[count]=new Vector4(l.r,l.g,l.b,1f); count++;
                }
            }
            Shader.SetGlobalVectorArray("_MTV_Lights",posicoes);
            Shader.SetGlobalVectorArray("_MTV_Colors",cores);
            Shader.SetGlobalInt("_MTV_Count",count);
        }
        private void OnDisable()
        {
            Shader.SetGlobalFloat("_MTV_Active",0f); Shader.SetGlobalInt("_MTV_Count",0);
            Shader.SetGlobalFloat("_MTV_Time",0f);
        }
    }
}

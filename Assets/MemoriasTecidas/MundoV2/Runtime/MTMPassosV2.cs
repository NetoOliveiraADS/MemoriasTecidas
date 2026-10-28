using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMPassosV2:MonoBehaviour
    {
        public ParticleSystem poeira;
        public MTMAnimacao animacao;
        private Vector3 anterior;
        private float distancia;
        private bool primeira=true;
        private void OnEnable(){primeira=true;distancia=0f;}
        private void Update()
        {
            Vector3 pos=transform.position;
            if(primeira){anterior=pos;primeira=false;return;}
            Vector2 delta=pos-anterior;anterior=pos;
            float percurso=delta.magnitude;
            if(percurso>1f){distancia=0f;return;}
            float rapidez=Time.deltaTime>.00001f?percurso/Time.deltaTime:0f;
            if(animacao!=null)animacao.quadrosPorSegundo=Mathf.Lerp(6f,10f,Mathf.InverseLerp(1f,5.4f,rapidez));
            if(poeira==null||MTMAmbienteV2.Qualidade==0||MTMConfiguracaoVisual.Atual.reduzirMovimento||rapidez<.15f){distancia=0f;return;}
            distancia+=percurso;
            if(distancia<(rapidez>4f?.68f:.78f))return;
            distancia=0f;var p=new ParticleSystem.EmitParams();
            Vector3 mundo=pos+Vector3.up*.02f;
            var main=poeira.main;
            Transform referencial=main.simulationSpace==ParticleSystemSimulationSpace.Custom?main.customSimulationSpace:poeira.transform;
            p.position=main.simulationSpace==ParticleSystemSimulationSpace.World?mundo:referencial!=null?referencial.InverseTransformPoint(mundo):mundo;
            Vector3 velocidade=new Vector3(-delta.x/Mathf.Max(.001f,Time.deltaTime)*.025f,.07f,0f);
            p.velocity=main.simulationSpace==ParticleSystemSimulationSpace.World?velocidade:referencial!=null?referencial.InverseTransformDirection(velocidade):velocidade;
            poeira.Emit(p,1);
        }
    }
}

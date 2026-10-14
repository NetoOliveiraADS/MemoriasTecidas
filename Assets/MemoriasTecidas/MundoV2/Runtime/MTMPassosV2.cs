using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMPassosV2 : MonoBehaviour
    {
        public ParticleSystem poeira;
        public MTMAnimacao animacao;
        private Rigidbody2D corpo;
        private float proximo;
        private void Awake(){corpo=GetComponent<Rigidbody2D>();}
        private void Update()
        {
            if(corpo==null)return;float rapidez=corpo.linearVelocity.magnitude;
            if(animacao!=null)animacao.quadrosPorSegundo=rapidez>4f?10f:7f;
            if(poeira!=null && rapidez>.15f && MTMAmbienteV2.Qualidade>0 && Time.time>=proximo)
            {
                proximo=Time.time+(rapidez>4f?.14f:.24f);
                var p=new ParticleSystem.EmitParams();
                p.position=transform.position+Vector3.up*.02f;
                p.velocity=new Vector3(-corpo.linearVelocity.x*.035f,.12f,0f);
                poeira.Emit(p,1);
            }
        }
    }
}

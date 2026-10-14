using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMParticulasV2 : MonoBehaviour
    {
        public ParticleSystem poeira, vagalumes;
        public bool interior, natural;
        private int ultimaQualidade=-1;
        private float ultimaNoite=-1f;
        private void Update()
        {
            int qualidade=MTMAmbienteV2.Qualidade;
            float noite=MTMAmbienteV2.Sol<.25f?1f:0f;
            if(qualidade==ultimaQualidade && noite==ultimaNoite)return;
            ultimaQualidade=qualidade;ultimaNoite=noite;
            Configurar(poeira,qualidade==0?0:interior?1f:qualidade==1?4f:8f,qualidade==2?120:65);
            Configurar(vagalumes,natural&&noite>.5f&&qualidade>0?(qualidade==2?3f:1.5f):0f,36);
        }
        private static void Configurar(ParticleSystem ps,float taxa,int maximo)
        {
            if(ps==null)return;var emissao=ps.emission;emissao.rateOverTime=taxa;
            var main=ps.main;main.maxParticles=maximo;
            if(taxa<=0f)ps.Clear();
        }
    }
}

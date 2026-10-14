using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [DisallowMultipleComponent]
    public sealed class MTMFolhasV22 : MonoBehaviour
    {
        public SpriteRenderer copa;
        public Material material;
        public MTMVidaV22 vida;
        private ParticleSystem folhas;
        private void Start()
        {
            if(copa==null||copa.sprite==null||material==null)return;
            var go=new GameObject("Folhas_Ocasionais_V22");go.transform.SetParent(transform,false);
            go.transform.position=new Vector3(copa.bounds.center.x,copa.bounds.min.y+copa.bounds.size.y*.75f,0);
            folhas=go.AddComponent<ParticleSystem>();folhas.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=folhas.main;m.loop=true;m.startLifetime=new ParticleSystem.MinMaxCurve(3f,5f);m.startSpeed=0f;
            m.startSize3D=true;m.startSizeX=new ParticleSystem.MinMaxCurve(.035f,.06f);m.startSizeY=new ParticleSystem.MinMaxCurve(.07f,.13f);m.startSizeZ=new ParticleSystem.MinMaxCurve(1f,1f);
            m.startColor=new ParticleSystem.MinMaxGradient(new Color(.47f,.43f,.17f,.8f),new Color(.72f,.46f,.20f,.7f));
            m.maxParticles=8;m.simulationSpace=ParticleSystemSimulationSpace.World;m.gravityModifier=0f;
            var shape=folhas.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(copa.bounds.size.x*.6f,.1f,.01f);
            var v=folhas.velocityOverLifetime;v.enabled=true;v.space=ParticleSystemSimulationSpace.World;
            v.x=new ParticleSystem.MinMaxCurve(.08f,.22f);v.y=new ParticleSystem.MinMaxCurve(-.65f,-.35f);v.z=new ParticleSystem.MinMaxCurve(0f,0f);
            var rot=folhas.rotationOverLifetime;rot.enabled=true;rot.z=new ParticleSystem.MinMaxCurve(-1.5f,1.5f);
            var cor=folhas.colorOverLifetime;cor.enabled=true;
            var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});cor.color=g;
            var rend=folhas.GetComponent<ParticleSystemRenderer>();rend.sharedMaterial=material;rend.sortingLayerID=copa.sortingLayerID;rend.sortingOrder=30000;
            var em=folhas.emission;em.rateOverTime=.10f;folhas.Play();
        }
        private void Update()
        {
            if(folhas==null)return;var em=folhas.emission;
            em.rateOverTime=MTMAmbienteV2.Qualidade==0||vida==null||vida.intensidadeVento<.01f?0f:MTMAmbienteV2.Qualidade==2?.16f:.07f;
        }
        private void OnDisable(){if(folhas!=null)folhas.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
        private void OnEnable(){if(folhas!=null)folhas.Play();}
    }
}

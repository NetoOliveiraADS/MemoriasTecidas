using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MTMRegiaoVisualV23:MonoBehaviour
    {
        [HideInInspector] public int versao;
        public GameObject layoutAnterior,cenarioAtual;
        [TextArea] public string referenciaVisual;
        private MTMRegiao regiao;
        private MTMLuzLocalV23[] luzes;
        public void InvalidarLuzes(){luzes=null;}
        private void OnEnable(){regiao=GetComponent<MTMRegiao>();luzes=null;}
        private void LateUpdate(){AtualizarLuzes();}
        public void AtualizarLuzes()
        {
            if(regiao==null)regiao=GetComponent<MTMRegiao>();if(regiao==null||regiao.dados==null)return;
            if(luzes==null)luzes=GetComponentsInChildren<MTMLuzLocalV23>(false);
            if(regiao.dados.luzes==null||regiao.dados.luzes.Length!=luzes.Length)regiao.dados.luzes=new MTMLuzDados[luzes.Length];
            for(int i=0;i<luzes.Length;i++)
            {
                if(regiao.dados.luzes[i]==null)regiao.dados.luzes[i]=new MTMLuzDados();
                var d=regiao.dados.luzes[i];var l=luzes[i];
                if(l==null||!l.isActiveAndEnabled){d.intensidade=0;continue;}
                d.x=l.transform.position.x;d.y=l.transform.position.y;
                d.raio=l.raio;d.intensidade=l.intensidade;d.r=l.cor.r;d.g=l.cor.g;d.b=l.cor.b;d.sempre=l.sempre;
            }
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Escreve depois dos Updates, antes da malha (ordem 200).
    [DefaultExecutionOrder(150), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTMAtorV22 : MonoBehaviour
    {
        public enum Tipo { Jogador, Morador, Animal }
        public Tipo tipo;
        public Sprite[] quadros;
        public MTMJogo jogo;
        public MTMVidaV22 vida;
        public string nomeDaConversa;
        [Range(2f,14f)] public float fpsCaminhada=7f, fpsCorrida=10f;
        [Tooltip("V2.2.1: inclui apoio, passada e joelho levantado no ciclo.")]
        public bool cicloCompleto=true;
        [SerializeField] private int direcaoAtual, quadroAtual;
        private SpriteRenderer desenho;
        private MTMCaminhanteV22 caminhante;
        private Transform raizMovimento;
        private Vector3 posicaoAnterior;
        private float tempo,fase;
        private bool primeiraLeitura=true,andava;

        private void Awake()
        {
            desenho=GetComponent<SpriteRenderer>();
            caminhante=GetComponentInParent<MTMCaminhanteV22>();
            if(jogo==null&&tipo==Tipo.Jogador)jogo=GetComponentInParent<MTMJogo>();
            raizMovimento=tipo==Tipo.Jogador&&jogo!=null?jogo.transform:transform.parent!=null?transform.parent:transform;
            fase=Mathf.Abs(raizMovimento.position.x*.831f+raizMovimento.position.y*.53f);
            posicaoAnterior=raizMovimento.position;
        }
        private void OnEnable(){primeiraLeitura=true;andava=false;tempo=0;}

        // Um novo movimento já exibe a passada 1; o quadro 0 é o repouso/apoio.
        // Método puro compartilhado com a verificação no Editor.
        public static int AvancarPasso(bool andando,bool anterior,bool completo,float dt,float fps,float proporcao,ref float fasePasso)
        {
            if(!andando){fasePasso=0f;return 0;}
            if(!anterior){fasePasso=completo?1f:0f;return 1;}
            int quantidade=completo?4:3;
            fasePasso=Mathf.Repeat(fasePasso+Mathf.Max(0f,dt)*Mathf.Max(0f,fps)*Mathf.Clamp(proporcao,.1f,1f),quantidade);
            return (completo?0:1)+Mathf.FloorToInt(fasePasso);
        }
        private void LateUpdate()
        {
            if(desenho==null||quadros==null||quadros.Length==0||raizMovimento==null)return;
            Vector3 pos=raizMovimento.position;
            Vector2 delta=primeiraLeitura?Vector2.zero:(Vector2)(pos-posicaoAnterior);
            primeiraLeitura=false;posicaoAnterior=pos;
            if(delta.sqrMagnitude>1f){delta=Vector2.zero;tempo=0;andava=false;}
            Vector2 medida=Time.deltaTime>.00001f?delta/Time.deltaTime:Vector2.zero;
            // O Rigidbody já interpola o deslocamento visível. Outra suavização atrasava
            // a primeira passada e mantinha a animação depois de parar.
            Vector2 velocidade=tipo==Tipo.Jogador?medida:caminhante!=null?caminhante.Velocidade:Vector2.zero;
            if(tipo==Tipo.Jogador)
            {
                // A passada segue o deslocamento efetivo. Teclas contra uma parede não simulam caminhada.
                if(jogo!=null&&jogo.V22Bloqueado)velocidade=Vector2.zero;
            }
            bool andando=velocidade.sqrMagnitude>(tipo==Tipo.Jogador ? .0001f : .01f);
            quadroAtual=0;
            if(tipo==Tipo.Jogador&&quadros.Length>=32)
            {
                float fps=fpsCaminhada,proporcao=0f;
                if(andando)
                {
                    float angulo=Mathf.Atan2(-velocidade.x,-velocidade.y)*Mathf.Rad2Deg;
                    direcaoAtual=(Mathf.RoundToInt(angulo/45f)+8)%8;
                    float baseAndar=jogo!=null?Mathf.Max(.1f,jogo.velocidade):3.5f;
                    float rapidez=velocidade.magnitude;
                    fps=Mathf.Lerp(fpsCaminhada,fpsCorrida,Mathf.InverseLerp(baseAndar,baseAndar*1.55f,rapidez));
                    proporcao=rapidez/baseAndar;
                }
                int passo=AvancarPasso(andando,andava,cicloCompleto,Time.deltaTime,fps,proporcao,ref tempo);
                quadroAtual=direcaoAtual*4+passo;
            }
            else if(tipo==Tipo.Morador)
            {
                bool fala=jogo!=null&&jogo.V22FalandoCom(nomeDaConversa);
                if(fala)quadroAtual=Mathf.FloorToInt(Time.unscaledTime*3.2f)%2==0?2:0;
                else if(!andando&&Mathf.Repeat(Time.time+fase,5.7f)<.18f)quadroAtual=1;
            }
            else if(tipo==Tipo.Animal)
            {
                tempo=andando?tempo+Time.deltaTime*7f:0;
                quadroAtual=andando?Mathf.FloorToInt(tempo)%quadros.Length:0;
                if(Mathf.Abs(velocidade.x)>.02f)desenho.flipX=velocidade.x<0;
            }
            andava=andando;
            desenho.sprite=quadros[Mathf.Clamp(quadroAtual,0,quadros.Length-1)];
        }
    }
}

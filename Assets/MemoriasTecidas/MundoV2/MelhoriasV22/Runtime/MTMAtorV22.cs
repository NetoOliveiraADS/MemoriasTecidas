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
        private Rigidbody2D corpo;
        private MTMCaminhanteV22 caminhante;
        private Transform raizMovimento;
        private Vector3 posicaoAnterior;
        private Vector2 deslocamentoSuave;
        private float tempo,fase;
        private bool primeiraLeitura=true;

        private void Awake()
        {
            desenho=GetComponent<SpriteRenderer>();corpo=GetComponentInParent<Rigidbody2D>();
            caminhante=GetComponentInParent<MTMCaminhanteV22>();
            if(jogo==null&&tipo==Tipo.Jogador)jogo=GetComponentInParent<MTMJogo>();
            raizMovimento=tipo==Tipo.Jogador&&jogo!=null?jogo.transform:transform.parent!=null?transform.parent:transform;
            fase=Mathf.Abs(raizMovimento.position.x*.831f+raizMovimento.position.y*.53f);
            posicaoAnterior=raizMovimento.position;
        }
        private void OnEnable(){primeiraLeitura=true;tempo=0;deslocamentoSuave=Vector2.zero;}
        private void LateUpdate()
        {
            if(desenho==null||quadros==null||quadros.Length==0||raizMovimento==null)return;
            Vector3 pos=raizMovimento.position;
            Vector2 delta=primeiraLeitura?Vector2.zero:(Vector2)(pos-posicaoAnterior);
            primeiraLeitura=false;posicaoAnterior=pos;
            if(delta.sqrMagnitude>1f){delta=Vector2.zero;deslocamentoSuave=Vector2.zero;tempo=0;}
            Vector2 medida=Time.deltaTime>.00001f?delta/Time.deltaTime:Vector2.zero;
            deslocamentoSuave=Vector2.Lerp(deslocamentoSuave,medida,1f-Mathf.Exp(-18f*Time.deltaTime));
            Vector2 velocidade=tipo==Tipo.Jogador?deslocamentoSuave:caminhante!=null?caminhante.Velocidade:Vector2.zero;
            if(tipo==Tipo.Jogador)
            {
                // Aceita tanto Rigidbody2D quanto deslocamento pelo Transform.
                if(corpo!=null&&corpo.linearVelocity.sqrMagnitude>velocidade.sqrMagnitude)velocidade=corpo.linearVelocity;
                Vector2 entrada=MTMEntrada.Movimento();
                if(entrada.sqrMagnitude>.01f&&velocidade.sqrMagnitude<.01f)velocidade=entrada*(jogo!=null?jogo.velocidade:3.5f);
                if(jogo!=null&&jogo.V22Bloqueado)velocidade=Vector2.zero;
            }
            bool andando=velocidade.sqrMagnitude>.01f;
            quadroAtual=0;
            if(tipo==Tipo.Jogador&&quadros.Length>=32)
            {
                if(andando)
                {
                    float angulo=Mathf.Atan2(-velocidade.x,-velocidade.y)*Mathf.Rad2Deg;
                    direcaoAtual=(Mathf.RoundToInt(angulo/45f)+8)%8;
                    tempo+=Time.deltaTime*(MTMEntrada.Correndo()?fpsCorrida:fpsCaminhada);
                }
                else tempo=0;
                int passo=andando?(cicloCompleto?Mathf.FloorToInt(tempo)%4:1+Mathf.FloorToInt(tempo)%3):0;
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
            desenho.sprite=quadros[Mathf.Clamp(quadroAtual,0,quadros.Length-1)];
        }
    }
}

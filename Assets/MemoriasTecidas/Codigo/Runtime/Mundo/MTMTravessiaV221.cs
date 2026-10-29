using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [DefaultExecutionOrder(110), DisallowMultipleComponent]
    public sealed class MTMTravessiaV221 : MonoBehaviour
    {
        public MTMJogo jogo;
        [Tooltip("Entrar no círculo troca a região. E continua funcionando próximo da saída.")]
        public bool automatico = true;
        [Range(.3f, 1f)]
        public float raio = .60f;
        private MTMRegiao anterior;
        private Vector3 ultimaPosicao;
        private bool liberada;
        private float esperaAte;
        private void OnEnable()
        {
            anterior = null;
            liberada = false;
            ultimaPosicao = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 pos = transform.position;
            bool moveu = (pos - ultimaPosicao).sqrMagnitude > .000001f;
            ultimaPosicao = pos;
            if (jogo == null)
                return;
            var regiao = jogo.V221RegiaoAtual;
            if (regiao != anterior)
            {
                anterior = regiao;
                liberada = false;
                esperaAte = Time.time + 1f;
                return;
            }

            if (!automatico || regiao == null || regiao.pontos == null || jogo.V22Bloqueado)
                return;
            MTMPonto perto = null;
            float menor = float.PositiveInfinity;
            foreach (var p in regiao.pontos)
            {
                if (p == null || p.dados == null || p.dados.tipo != "saida" || !p.gameObject.activeInHierarchy)
                    continue;
                float d = Vector2.Distance(pos, p.transform.position);
                if (d < menor)
                {
                    menor = d;
                    perto = p;
                }
            }

            if (menor > Mathf.Max(1.15f, raio + .25f))
                liberada = true;
            if (liberada && moveu && Time.time >= esperaAte && menor <= raio && jogo.V221Atravessar(perto))
            {
                liberada = false;
                esperaAte = Time.time + 1f;
            }
        }
    }

    public sealed partial class MTMJogo
    {
        public MTMRegiao V221RegiaoAtual
        {
            get
            {
                return atual;
            }
        }

        public bool V221Atravessar(MTMPonto ponto)
        {
            if (!pronto || Bloqueado || ponto == null || ponto.dados == null || ponto.dados.tipo != "saida" || !Disponivel(ponto))
                return false;
            quadroDaAcao = Time.frameCount;
            return IrPara(ponto.dados.destino, new Vector2(ponto.dados.chegadaX, ponto.dados.chegadaY));
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Faz o desenho boiar: sobe e desce devagar e inclina um pouco, como canoas e aguapés no rio.
    // Roda depois de MTMVisualV2 (que também mexe no desenho) e respeita "reduzir movimento".
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class MTMFlutuanteV25 : MonoBehaviour
    {
        [Tooltip("Filho que boia. Vazio = o filho chamado \"Desenho\".")]
        public Transform desenho;
        [Range(0f, .1f)]
        public float altura = .03f;
        [Range(0f, 4f)]
        public float inclinacao = 1f;
        [Range(.2f, 3f)]
        public float ritmo = 1f;
        private Vector3 posicao;
        private Quaternion rotacao;
        private float fase;
        private bool pronto;

        private void Awake()
        {
            if (desenho == null)
                desenho = transform.Find("Desenho");
            if (desenho == null)
                return;
            posicao = desenho.localPosition;
            rotacao = desenho.localRotation;
            fase = transform.position.x * 1.13f + transform.position.y * .57f;
            pronto = true;
        }

        private void LateUpdate()
        {
            if (!pronto)
                return;
            bool vivo = MTMAmbienteV2.Qualidade > 0 && !MTMConfiguracaoVisual.Atual.reduzirMovimento;
            float t = MTMAmbienteV2.Tempo * ritmo;
            float sobe = vivo ? (Mathf.Sin(t * 1.3f + fase) * .7f + Mathf.Sin(t * 2.1f + fase * 1.6f) * .3f) * altura : 0f;
            float inclina = vivo ? Mathf.Sin(t * .9f + fase + .8f) * inclinacao : 0f;
            desenho.localPosition = posicao + new Vector3(0f, sobe, 0f);
            // Substitui a rotação do desenho: canoas e aguapés não usam o vento de MTMVisualV2.
            desenho.localRotation = rotacao * Quaternion.Euler(0f, 0f, inclina);
        }

        private void OnDisable()
        {
            if (!pronto)
                return;
            desenho.localPosition = posicao;
            desenho.localRotation = rotacao;
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMVisualV2 : MonoBehaviour
    {
        public SpriteRenderer desenho, sombra;
        public float vento;
        public bool respirar, jogador;
        private Vector3 escala;
        private Quaternion rotacao;
        private float fase;
        private MTMDeformacaoV22 deformacao;
        private void Awake()
        {
            if (desenho != null)
            {
                escala = desenho.transform.localScale;
                rotacao = desenho.transform.localRotation;
                deformacao = desenho.GetComponent<MTMDeformacaoV22>();
            }

            fase = transform.position.x * 1.37f + transform.position.y * .91f;
        }

        private void LateUpdate()
        {
            if (desenho == null)
                return;
            var arte = MTMConfiguracaoVisual.Atual;
            float onda = Mathf.Sin(Time.time * 1.2f + fase);
            float vivo = MTMAmbienteV2.Qualidade > 0 && !arte.reduzirMovimento ? arte.vento : 0f;
            bool malhaViva = deformacao != null && deformacao.isActiveAndEnabled;
            // O balanço da malha não gira a base da árvore, a construção ou os suportes da rede.
            desenho.transform.localRotation = rotacao * Quaternion.Euler(0, 0, malhaViva ? 0f : vento * onda * vivo);
            if (respirar && !jogador && !malhaViva)
                desenho.transform.localScale = new Vector3(escala.x, escala.y * (1f + .002f * onda * vivo), escala.z);
            if (sombra == null)
                return;
            sombra.enabled = (MTMAmbienteV2.Qualidade > 0 || jogador) && desenho.enabled;
            sombra.sprite = desenho.sprite;
            if (desenho.sprite == null)
                return;
            // O protagonista pode viver fora da hierarquia da região: usa o ambiente ativo.
            bool interior = MTMAmbienteV2.Interior;
            float direcao = interior ? 0f : Mathf.Sin((MTMAmbienteV2.Fase - .5f) * Mathf.PI * 2f);
            float alturaSol = Mathf.Clamp01(Mathf.Sin((MTMAmbienteV2.Fase - .25f) * Mathf.PI * 2f));
            float comprimento = interior ? .12f : Mathf.Lerp(.13f, .34f, 1f - alturaSol);
            Vector3 escalaMundo = desenho.transform.lossyScale;
            Vector3 escalaPai = sombra.transform.parent != null ? sombra.transform.parent.lossyScale : Vector3.one;
            sombra.transform.localScale = new Vector3(escalaMundo.x / Mathf.Max(.001f, Mathf.Abs(escalaPai.x)) * 1.01f, escalaMundo.y / Mathf.Max(.001f, Mathf.Abs(escalaPai.y)) * comprimento, 1f);
            sombra.transform.rotation = Quaternion.Euler(0, 0, -direcao * 32f);
            // Ancora a projeção no pé do sprite, mesmo quando o pivô não está na base.
            Vector3 pe = new Vector3(0, desenho.sprite.bounds.min.y, 0);
            sombra.transform.position = desenho.transform.TransformPoint(pe) - sombra.transform.TransformVector(pe);
            sombra.flipX = desenho.flipX;
            sombra.flipY = desenho.flipY;
            float opacidade = interior ? .08f : Mathf.Lerp(.035f, .19f, MTMAmbienteV2.Sol);
            sombra.color = new Color(.13f, .12f, .105f, opacidade);
            sombra.sortingOrder = -22000;
        }
    }
}

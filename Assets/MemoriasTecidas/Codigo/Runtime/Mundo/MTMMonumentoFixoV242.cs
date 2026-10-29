using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // A torre é rígida. A projeção da sombra jamais altera o corpo do monumento.
    [ExecuteAlways, DefaultExecutionOrder(400), DisallowMultipleComponent]
    public sealed class MTMMonumentoFixoV242 : MonoBehaviour
    {
        public SpriteRenderer desenho, sombra;
        public MeshFilter malhaContato;
        public MeshRenderer contato;
        [Min(.1f)]
        public float larguraBase = 3.78f;
        [Range(.1f, 2f)]
        public float profundidadeContato = .85f;
        [Range(0f, .5f)]
        public float opacidadeContato = .24f;
        [Range(.2f, 3f)]
        public float comprimentoSombra = 1.2f;
        [Range(0f, .3f)]
        public float opacidadeSombra = .10f;
        public bool manterDesenhoVertical = true;
        private Mesh malha;
        private MaterialPropertyBlock propriedades;
        private void OnEnable()
        {
            Aplicar();
        }

        private void LateUpdate()
        {
            Aplicar();
        }

        public void Aplicar()
        {
            if (desenho == null || desenho.sprite == null)
                return;
            var visual = GetComponent<MTMVisualV2>();
            if (visual != null)
            {
                visual.vento = 0;
                visual.respirar = false;
                visual.enabled = false;
            }

            if (manterDesenhoVertical)
                desenho.transform.localRotation = Quaternion.identity;
            var escala = GetComponent<MTMEscalaConstrucaoV221>();
            float largura = larguraBase * (escala != null ? escala.largura : 1f);
            float profundidade = profundidadeContato * (escala != null ? escala.profundidadeDaBase : 1f);
            bool visivel = desenho.enabled && !desenho.forceRenderingOff && MTMAmbienteV2.Qualidade > 0;
            float sol = Mathf.Clamp01(MTMAmbienteV2.Sol);
            if (sombra != null)
            {
                // Sombra curta, ligada ao mesmo apoio, sem deslocar o pé da torre.
                sombra.enabled = visivel;
                sombra.sprite = desenho.sprite;
                sombra.flipX = desenho.flipX;
                sombra.flipY = false;
                float direcao = Mathf.Sin((MTMAmbienteV2.Fase - .25f) * Mathf.PI * 2f);
                float altura = Mathf.Max(.1f, desenho.sprite.bounds.size.y);
                float encurtamento = Mathf.Min(.13f, comprimentoSombra / altura);
                sombra.transform.localPosition = new Vector3(0, .035f, 0);
                sombra.transform.localRotation = Quaternion.Euler(0, 0, -direcao * 12f);
                sombra.transform.localScale = new Vector3(Mathf.Abs(desenho.transform.localScale.x), encurtamento * Mathf.Abs(desenho.transform.localScale.y), 1);
                sombra.color = new Color(0, 0, 0, opacidadeSombra * Mathf.Lerp(.55f, 1f, sol));
                sombra.sortingOrder = -22002;
            }

            if (contato == null || malhaContato == null)
                return;
            if (malha == null)
            {
                malha = new Mesh
                {
                    name = "ContatoMonumentoV242",
                    hideFlags = HideFlags.DontSave
                };
                malha.vertices = new[]
                {
                    new Vector3(-.5f, -.5f, 0),
                    new Vector3(.5f, -.5f, 0),
                    new Vector3(.5f, .5f, 0),
                    new Vector3(-.5f, .5f, 0)
                };
                malha.uv = new[]
                {
                    Vector2.zero,
                    Vector2.right,
                    Vector2.one,
                    Vector2.up
                };
                malha.colors = new[]
                {
                    Color.white,
                    Color.white,
                    Color.white,
                    Color.white
                };
                malha.triangles = new[]
                {
                    0,
                    1,
                    2,
                    0,
                    2,
                    3
                };
                malha.RecalculateBounds();
            }

            malhaContato.sharedMesh = malha;
            contato.enabled = visivel;
            contato.sortingOrder = -22001;
            contato.transform.localPosition = new Vector3(0, .10f, 0);
            contato.transform.localRotation = Quaternion.identity;
            contato.transform.localScale = new Vector3(largura + .30f, profundidade, 1);
            if (propriedades == null)
                propriedades = new MaterialPropertyBlock();
            propriedades.SetColor("_Color", new Color(0, 0, 0, opacidadeContato * Mathf.Lerp(.65f, 1f, sol)));
            contato.SetPropertyBlock(propriedades);
        }

        private void OnDisable()
        {
            if (sombra != null)
                sombra.enabled = false;
            if (contato != null)
                contato.enabled = false;
            LiberarMalha();
        }

        private void OnDestroy()
        {
            LiberarMalha();
        }

        // A malha é reconstruída em edição e no Play; não é uma referência persistente do prefab.
        public void PrepararParaSalvar()
        {
            LiberarMalha();
        }

        private void LiberarMalha()
        {
            if (malha == null)
                return;
            if (malhaContato != null && malhaContato.sharedMesh == malha)
                malhaContato.sharedMesh = null;
            if (Application.isPlaying)
                Destroy(malha);
            else
                DestroyImmediate(malha);
            malha = null;
        }
    }
}

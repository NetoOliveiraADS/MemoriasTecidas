using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Renderiza depois do MTMAtorV22 (150). Substitui a deformacao generica apenas em Neto.
    [DefaultExecutionOrder(220), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTMNetoVisual : MonoBehaviour
    {
        public MTMJogo jogo;
        public MTMVidaV22 vida;
        public Material material;
        [Header("Tamanho — sem esticar rosto ou corpo")]
        [Range(.75f, 1.25f)]
        public float escalaVisual = 1f;
        [Header("Acabamento do Neto — rosto original preservado")]
        [Range(0f, .035f)]
        public float alongamentoPescoco = .018f;
        [Range(0f, .18f)]
        public float luzPreenchimento = .10f;
        [Header("Postura da corrida")]
        [Range(0f, 14f)]
        public float inclinacaoCorridaGraus = 8f;
        [Range(0f, .6f)]
        public float projecaoFrenteCostas = .35f;
        [Range(.04f, .4f)]
        public float tempoEntrar = .10f, tempoVoltar = .14f;
        [Header("Vento somente no cabelo")]
        [Range(0f, .008f)]
        public float ventoNoCabelo = .0025f;
        private const int Colunas = 12, Linhas = 24;
        private SpriteRenderer fonte, fonteSombra;
        private MTMVisualV2 visualAntigo;
        private Mesh malha;
        private MeshRenderer pintura, sombra;
        private GameObject objetoPintura, objetoSombra;
        private MaterialPropertyBlock propriedades, propriedadesSombra;
        private Vector3[] baseVertices, vertices;
        private Vector2[] uvLocal;
        private Vector2[] uvs;
        private Color[] cores;
        private int[] indices;
        private Sprite spriteAnterior;
        private Vector3 posicaoAnterior;
        private Vector2 velocidadeSuave, inclinacao;
        private bool primeiraLeitura;
        private void OnEnable()
        {
            primeiraLeitura = true;
            velocidadeSuave = Vector2.zero;
            inclinacao = Vector2.zero;
            if (Application.isPlaying)
                Inicializar();
        }

        private void Inicializar()
        {
            if (objetoPintura != null)
                return;
            fonte = GetComponent<SpriteRenderer>();
            if (jogo == null)
                jogo = GetComponentInParent<MTMJogo>();
            if (vida == null)
                vida = GetComponentInParent<MTMVidaV22>();
            visualAntigo = GetComponentInParent<MTMVisualV2>();
            var legado = GetComponent<MTMDeformacaoV22>();
            if (legado != null && legado.enabled)
                legado.enabled = false;
            if (visualAntigo != null)
            {
                visualAntigo.vento = 0f;
                visualAntigo.respirar = false;
            }

            malha = new Mesh
            {
                name = "Neto_Visual_110"
            };
            malha.MarkDynamic();
            objetoPintura = CriarObjeto("Neto_Malha", transform, out pintura);
            propriedades = new MaterialPropertyBlock();
            propriedadesSombra = new MaterialPropertyBlock();
            fonteSombra = visualAntigo != null ? visualAntigo.sombra : null;
            if (fonteSombra != null)
                objetoSombra = CriarObjeto("Neto_Sombra", fonteSombra.transform, out sombra);
            spriteAnterior = null;
            posicaoAnterior = jogo != null ? jogo.transform.position : transform.position;
            primeiraLeitura = true;
        }

        private GameObject CriarObjeto(string nome, Transform pai, out MeshRenderer renderer)
        {
            var go = new GameObject(nome)
            {
                hideFlags = HideFlags.DontSave,
                layer = pai.gameObject.layer
            };
            go.transform.SetParent(pai, false);
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            renderer = go.AddComponent<MeshRenderer>();
            return go;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;
            if (objetoPintura == null)
                Inicializar();
            if (fonte == null || fonte.sprite == null || material == null || jogo == null)
            {
                if (fonte != null)
                    fonte.forceRenderingOff = false;
                if (fonteSombra != null)
                    fonteSombra.forceRenderingOff = false;
                if (pintura != null)
                    pintura.enabled = false;
                if (sombra != null)
                    sombra.enabled = false;
                return;
            }

            if (spriteAnterior != fonte.sprite)
                Construir();
            AtualizarInclinacao();
            float altura = fonte.sprite.rect.height / fonte.sprite.pixelsPerUnit * escalaVisual;
            float baixo = -fonte.sprite.pivot.y / fonte.sprite.pixelsPerUnit * escalaVisual;
            float quadril = baixo + altura * .40f;
            float radianos = -inclinacao.x * inclinacaoCorridaGraus * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radianos), sin = Mathf.Sin(radianos);
            float frenteCostas = inclinacao.y * Mathf.Sin(inclinacaoCorridaGraus * Mathf.Deg2Rad) * altura * projecaoFrenteCostas;
            float vento = vida != null ? vida.intensidadeVento : 0f;
            if (MTMAmbienteV2.Qualidade == 0 || MTMConfiguracaoVisual.Atual.reduzirMovimento)
                vento = 0f;
            vento *= MTMConfiguracaoVisual.Atual.vento;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = baseVertices[i] * escalaVisual;
                Vector2 uv = uvLocal[i];
                // Translada a cabeça inteira; só a faixa entre gola e barba alonga.
                // Os pés e o pivô continuam no mesmo lugar, sem acumular deformação.
                v.y += altura * alongamentoPescoco * PesoPescoco(uv.y);
                // Pes e parte inferior das pernas ficam fixos. Torso e cabeca giram juntos.
                float peso = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.16f, .48f, uv.y));
                float dx = v.x, dy = v.y - quadril;
                float rx = dx * cos - dy * sin;
                float ry = dx * sin + dy * cos;
                v.x += (rx - dx) * peso;
                v.y += (ry - dy + frenteCostas) * peso;
                float cabelo = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.93f, 1f, uv.y));
                v.x += Mathf.Sin(Time.time * 2.5f + uv.x * 2f) * ventoNoCabelo * cabelo * vento;
                vertices[i] = v;
            }

            // Parte sempre da geometria base: a inclinacao nunca se acumula entre frames.
            malha.vertices = vertices;
            malha.RecalculateBounds();
            fonte.forceRenderingOff = true;
            pintura.sharedMaterial = material;
            pintura.enabled = fonte.enabled;
            pintura.sortingLayerID = fonte.sortingLayerID;
            pintura.sortingOrder = fonte.sortingOrder;
            propriedades.SetTexture("_MainTex", fonte.sprite.texture);
            propriedades.SetColor("_Color", fonte.color);
            propriedades.SetFloat("_MTM_LuzNeto", Mathf.Clamp(luzPreenchimento, 0f, .18f));
            pintura.SetPropertyBlock(propriedades);
            var espelho = new Vector3(fonte.flipX ? -1f : 1f, fonte.flipY ? -1f : 1f, 1f);
            objetoPintura.transform.localScale = espelho;
            if (fonteSombra != null && sombra != null)
            {
                fonteSombra.sprite = fonte.sprite;
                fonteSombra.forceRenderingOff = true;
                sombra.enabled = fonte.enabled && fonteSombra.enabled;
                sombra.sharedMaterial = fonteSombra.sharedMaterial;
                sombra.sortingLayerID = fonteSombra.sortingLayerID;
                sombra.sortingOrder = fonteSombra.sortingOrder;
                propriedadesSombra.SetTexture("_MainTex", fonte.sprite.texture);
                propriedadesSombra.SetColor("_Color", fonteSombra.color);
                sombra.SetPropertyBlock(propriedadesSombra);
                objetoSombra.transform.localScale = espelho;
            }
        }

        public static float PesoPescoco(float alturaNormalizada)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.58f, .66f, alturaNormalizada));
        }

        private void AtualizarInclinacao()
        {
            Vector3 posicao = jogo.transform.position;
            Vector2 delta = primeiraLeitura ? Vector2.zero : (Vector2)(posicao - posicaoAnterior);
            primeiraLeitura = false;
            posicaoAnterior = posicao;
            bool salto = delta.sqrMagnitude > 1f;
            if (salto)
            {
                delta = Vector2.zero;
                velocidadeSuave = Vector2.zero;
                inclinacao = Vector2.zero;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, .10f);
            Vector2 medida = Time.deltaTime > .00001f ? delta / Time.deltaTime : Vector2.zero;
            velocidadeSuave = Vector2.Lerp(velocidadeSuave, medida, 1f - Mathf.Exp(-22f * dt));
            bool bloqueado = jogo.V22Bloqueado || Time.timeScale <= 0f;
            bool correndo = !bloqueado && !salto && MTMEntrada.Correndo() && MTMEntrada.Movimento().sqrMagnitude > .01f && velocidadeSuave.sqrMagnitude > .04f;
            Vector2 alvo = correndo ? velocidadeSuave.normalized : Vector2.zero;
            float tempo = correndo ? tempoEntrar : tempoVoltar;
            inclinacao = Vector2.Lerp(inclinacao, alvo, 1f - Mathf.Exp(-dt / Mathf.Max(.01f, tempo)));
            if (!correndo && inclinacao.sqrMagnitude < .000001f)
                inclinacao = Vector2.zero;
        }

        private void Construir()
        {
            spriteAnterior = fonte.sprite;
            Sprite s = fonte.sprite;
            Rect r = s.rect;
            int n = (Colunas + 1) * (Linhas + 1);
            if (vertices == null || vertices.Length != n)
            {
                vertices = new Vector3[n];
                baseVertices = new Vector3[n];
                uvLocal = new Vector2[n];
            }

            if (uvs == null || uvs.Length != n)
            {
                uvs = new Vector2[n];
                cores = new Color[n];
                indices = new int[Colunas * Linhas * 6];
            }

            int k = 0;
            for (int y = 0; y <= Linhas; y++)
                for (int x = 0; x <= Colunas; x++)
                {
                    int i = y * (Colunas + 1) + x;
                    float u = x / (float)Colunas, v = y / (float)Linhas;
                    uvLocal[i] = new Vector2(u, v);
                    vertices[i] = baseVertices[i] = new Vector3((u * r.width - s.pivot.x) / s.pixelsPerUnit, (v * r.height - s.pivot.y) / s.pixelsPerUnit, 0f);
                    uvs[i] = new Vector2((r.x + u * r.width) / s.texture.width, (r.y + v * r.height) / s.texture.height);
                    cores[i] = Color.white;
                    if (x < Colunas && y < Linhas)
                    {
                        int a = i, b = i + 1, c = i + Colunas + 1, d = c + 1;
                        indices[k++] = a;
                        indices[k++] = c;
                        indices[k++] = b;
                        indices[k++] = b;
                        indices[k++] = c;
                        indices[k++] = d;
                    }
                }

            malha.Clear();
            malha.vertices = vertices;
            malha.uv = uvs;
            malha.colors = cores;
            malha.triangles = indices;
        }

        private void OnDisable()
        {
            if (fonte != null)
                fonte.forceRenderingOff = false;
            if (fonteSombra != null)
                fonteSombra.forceRenderingOff = false;
            Liberar(objetoPintura);
            Liberar(objetoSombra);
            Liberar(malha);
            objetoPintura = null;
            objetoSombra = null;
            malha = null;
            inclinacao = Vector2.zero;
            velocidadeSuave = Vector2.zero;
        }

        private static void Liberar(UnityEngine.Object objeto)
        {
            if (objeto == null)
                return;
            if (Application.isPlaying)
                Destroy(objeto);
            else
                DestroyImmediate(objeto);
        }
    }
}

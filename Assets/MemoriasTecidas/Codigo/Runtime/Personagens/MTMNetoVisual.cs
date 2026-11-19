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
        [Tooltip("Quanto o corpo sobe a cada passada da corrida, em fração da altura do Neto.")]
        [Range(0f, .06f)]
        public float puloCorrida = .035f;
        [Header("Vento somente no cabelo")]
        [Range(0f, .008f)]
        public float ventoNoCabelo = .0025f;
        private const int Colunas = 12, Linhas = 24;
        private SpriteRenderer fonte, fonteSombra;
        private MTMAtorV22 ator;
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
        // Altura do pulo da corrida neste quadro (a arma solta da luta acompanha).
        public float PuloAtual { get; private set; }
        // Fração da opacidade no piscar depois de um golpe (a arma da luta pisca junto).
        public float AlfaDano { get; private set; } = 1f;
        // Golpe recebido: tranco do tronco para o lado do empurrão, pulinho, clarão e piscar.
        private const float InclinacaoDanoGraus = 15f;
        private float danoInicio = -10f, danoPiscar;
        private Vector2 danoEmpurrao = Vector2.down;

        // Chamado quando uma lenda acerta Neto. "empurrao" é para onde ele é jogado;
        // "piscar" é quanto tempo ele fica protegido (pisca nesse intervalo).
        public void Atingir(Vector2 empurrao, float piscar)
        {
            danoInicio = Time.time;
            danoEmpurrao = empurrao.sqrMagnitude > .0001f ? empurrao.normalized : Vector2.down;
            danoPiscar = Mathf.Max(0f, piscar);
        }

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
            ator = GetComponent<MTMAtorV22>();
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
            // Com as poses próprias da corrida, o desenho já se inclina e já tira os pés do chão.
            bool posesCorrida = ator != null && ator.CorrendoComQuadros;
            float graus = inclinacaoCorridaGraus * (posesCorrida ? .35f : 1f);
            bool reduzirDano = MTMConfiguracaoVisual.Atual.reduzirMovimento;
            float desdeDano = Time.time - danoInicio;
            float tranco = 0f, amassaDano = 0f, puloDano = 0f;
            if (desdeDano >= 0f && desdeDano < .5f && !reduzirDano)
            {
                // Tranco rápido para o lado do empurrão e volta devagar; amassa no impacto e sai do chão.
                tranco = desdeDano < .06f ? desdeDano / .06f : 1f - Mathf.SmoothStep(0f, 1f, (desdeDano - .06f) / .44f);
                amassaDano = Mathf.Sin(Mathf.PI * Mathf.Clamp01(desdeDano / .18f)) * .09f;
                puloDano = Mathf.Sin(Mathf.PI * Mathf.Clamp01((desdeDano - .06f) / .3f)) * altura * .07f;
            }

            float radianos = -inclinacao.x * graus * Mathf.Deg2Rad - danoEmpurrao.x * InclinacaoDanoGraus * Mathf.Deg2Rad * tranco;
            float cos = Mathf.Cos(radianos), sin = Mathf.Sin(radianos);
            float frenteCostas = inclinacao.y * Mathf.Sin(graus * Mathf.Deg2Rad) * altura * projecaoFrenteCostas
                + danoEmpurrao.y * Mathf.Sin(InclinacaoDanoGraus * Mathf.Deg2Rad) * altura * projecaoFrenteCostas * tranco;
            // Pulo da corrida: o corpo sobe no meio das poses 1 e 3 (pernas abertas) e desce
            // no apoio (poses 0 e 2). Só o desenho sobe; a sombra continua no chão.
            // As poses próprias da corrida já desenham o voo: aí o pulo extra não entra.
            float pulo = 0f;
            if (ator != null && !posesCorrida && !MTMConfiguracaoVisual.Atual.reduzirMovimento)
                pulo = Mathf.Abs(Mathf.Sin((ator.FasePasso - .5f) * Mathf.PI * .5f)) * altura * puloCorrida *
                    Mathf.Clamp01(inclinacao.magnitude);
            pulo += puloDano;
            // O alongamento do pescoço foi pensado para as poses em pé; na corrida, curvada, ele não entra.
            float pescoco = posesCorrida ? 0f : alongamentoPescoco;
            float vento = vida != null ? vida.intensidadeVento : 0f;
            if (MTMAmbienteV2.Qualidade == 0 || MTMConfiguracaoVisual.Atual.reduzirMovimento)
                vento = 0f;
            vento *= MTMConfiguracaoVisual.Atual.vento;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = baseVertices[i] * escalaVisual;
                // Amassa a partir dos pés no impacto: eles continuam no chão.
                v.x *= 1f + amassaDano * .7f;
                v.y *= 1f - amassaDano;
                Vector2 uv = uvLocal[i];
                // Translada a cabeça inteira; só a faixa entre gola e barba alonga.
                // Os pés e o pivô continuam no mesmo lugar, sem acumular deformação.
                v.y += altura * pescoco * PesoPescoco(uv.y);
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
            Color cor = fonte.color, clarao = new Color(0f, 0f, 0f, 0f);
            if (desdeDano >= 0f)
            {
                if (reduzirDano)
                {
                    // Sem clarão nem piscar: só um tom avermelhado que some devagar.
                    if (desdeDano < .6f)
                        clarao = new Color(.86f, .2f, .14f, .35f * (1f - desdeDano / .6f));
                }
                else
                {
                    // Clarão claro no impacto, depois vermelho que se apaga.
                    if (desdeDano < .07f)
                        clarao = new Color(1f, .96f, .88f, .85f);
                    else if (desdeDano < .4f)
                        clarao = new Color(.9f, .16f, .1f, .55f * (1f - (desdeDano - .07f) / .33f));
                    // Protegido depois do golpe: pisca até poder ser atingido de novo.
                    if (desdeDano >= .4f && desdeDano < danoPiscar && Mathf.Repeat(desdeDano * 12f, 1f) < .5f)
                        cor.a *= .35f;
                }
            }

            AlfaDano = fonte.color.a > .001f ? cor.a / fonte.color.a : 1f;
            propriedades.SetColor("_Color", cor);
            propriedades.SetColor("_MTM_Clarao", clarao);
            propriedades.SetFloat("_MTM_LuzNeto", Mathf.Clamp(luzPreenchimento, 0f, .18f));
            pintura.SetPropertyBlock(propriedades);
            var espelho = new Vector3(fonte.flipX ? -1f : 1f, fonte.flipY ? -1f : 1f, 1f);
            objetoPintura.transform.localScale = espelho;
            objetoPintura.transform.localPosition = new Vector3(0f, pulo, 0f);
            PuloAtual = pulo;
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
            bool bloqueado = jogo.V22Bloqueado || jogo.NetoAtordoado || Time.timeScale <= 0f;
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

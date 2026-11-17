using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // A arma de Neto nas lutas (o pau de jucá que Simão empresta para enfrentar a Mulher-Cavalo).
    // Com "netoArmado", Neto anda com a arte que já segura o pau; no golpe volta por um instante
    // para a arte de mãos livres e o pau solto varre um arco com rastro.
    // A arte e o nome vêm de "arma", "nomeArma" e "netoArmado" em confrontos.json.
    public sealed partial class MTMJogo
    {
        private const float DuracaoGolpeArma = .2f, VoltaGolpeArma = .14f, DuracaoRastroArma = .24f;
        // Comprimento da arma em relação à altura do quadro de Neto.
        private const float ProporcaoArma = .49f;
        // Ponto de pega na arte da arma (fração da altura, de baixo para cima).
        private const float PegaArma = .11f;
        // Pose da arma em cada direção do sprite (0 baixo, 1 baixo-esquerda, 2 esquerda, 3 cima-esquerda,
        // 4 cima, 5 cima-direita, 6 direita, 7 baixo-direita): x = 1 se fica atrás do corpo,
        // y = ângulo de repouso (graus, 0 = ponta para cima, positivo = gira para a esquerda).
        private static readonly Vector2[] PoseArmaNeto =
        {
            new Vector2(0f, 25f), new Vector2(0f, 20f), new Vector2(0f, 58f), new Vector2(1f, 22f),
            new Vector2(1f, 25f), new Vector2(1f, -28f), new Vector2(0f, -58f), new Vector2(0f, 22f)
        };

        // Punho que segura a arma em cada quadro da arte de Neto (PersonagensRevisados/NetoCardinais
        // e NetoDiagonais), em pixels da folha a partir dos pés; 4 passos por direção, na ordem acima.
        // Medido nos próprios quadros (centro do punho, sempre a mesma mão em cada direção).
        private static readonly Vector2[] MaoArmaNeto =
        {
            new Vector2(-46f, 113f), new Vector2(-46f, 117f), new Vector2(-47f, 116f), new Vector2(-41f, 119f),
            new Vector2(-38f, 115f), new Vector2(-33f, 126f), new Vector2(-38f, 121f), new Vector2(-29f, 133f),
            new Vector2(5f, 102f), new Vector2(55f, 110f), new Vector2(-2f, 102f), new Vector2(32f, 105f),
            new Vector2(-37f, 104f), new Vector2(-59f, 136f), new Vector2(-36f, 103f), new Vector2(-76f, 121f),
            new Vector2(-53f, 114f), new Vector2(-71f, 113f), new Vector2(-57f, 123f), new Vector2(-60f, 113f),
            new Vector2(31f, 104f), new Vector2(60f, 131f), new Vector2(36f, 106f), new Vector2(58f, 118f),
            new Vector2(-6f, 100f), new Vector2(-13f, 99f), new Vector2(11f, 102f), new Vector2(-21f, 102f),
            new Vector2(-39f, 102f), new Vector2(-93f, 112f), new Vector2(-36f, 105f), new Vector2(-23f, 107f)
        };

        // Mesma medida na folha HD anterior (1448 × 1086), usada só se o kit revisado não carregar.
        private static readonly Vector2[] MaoArmaNetoAnterior =
        {
            new Vector2(-49f, 98f), new Vector2(-50f, 107f), new Vector2(-53f, 104f), new Vector2(-50f, 106f),
            new Vector2(-49f, 109f), new Vector2(-63f, 109f), new Vector2(-60f, 108f), new Vector2(-42f, 113f),
            new Vector2(13f, 91f), new Vector2(35f, 94f), new Vector2(29f, 94f), new Vector2(-32f, 95f),
            new Vector2(76f, 100f), new Vector2(77f, 121f), new Vector2(78f, 106f), new Vector2(78f, 105f),
            new Vector2(58f, 103f), new Vector2(54f, 105f), new Vector2(54f, 106f), new Vector2(51f, 104f),
            new Vector2(24f, 92f), new Vector2(30f, 110f), new Vector2(30f, 103f), new Vector2(29f, 112f),
            new Vector2(-14f, 91f), new Vector2(1f, 90f), new Vector2(12f, 97f), new Vector2(14f, 94f),
            new Vector2(-36f, 91f), new Vector2(-41f, 94f), new Vector2(-43f, 95f), new Vector2(-13f, 86f)
        };

        private Sprite[] quadrosNetoArmado, quadrosNetoOriginais, quadrosCorridaSemArma;
        private GameObject objetoArma;
        private SpriteRenderer desenhoArma, rastroArma;
        private MTMAtorV22 atorNeto;
        private MTMNetoVisual visualNeto;
        private SpriteRenderer quadroNeto;
        private Sprite spriteRastroArma;
        private float inicioGolpeArma = -10f, anguloArma, alturaQuadroNeto;
        private int resultadoGolpeArma;
        private Vector2 frenteGolpeArma = Vector2.down;

        private bool NetoComArma
        {
            get
            {
                return EmConfronto && confrontoAtual != null && confrontoAtual.tipo == "luta" && !string.IsNullOrEmpty(confrontoAtual.arma);
            }
        }

        private void MontarArma(MTMConfrontoDados c)
        {
            LimparArma();
            if (c == null || c.tipo != "luta" || string.IsNullOrEmpty(c.arma))
                return;
            if (atorNeto == null)
                foreach (var ator in GetComponentsInChildren<MTMAtorV22>(true))
                    if (ator.tipo == MTMAtorV22.Tipo.Jogador)
                    {
                        atorNeto = ator;
                        break;
                    }

            if (atorNeto == null)
                return;
            // Na luta, a corrida usa as poses da caminhada: a arma é posicionada a partir delas.
            quadrosCorridaSemArma = atorNeto.quadrosCorrida;
            atorNeto.quadrosCorrida = null;
            quadroNeto = atorNeto.GetComponent<SpriteRenderer>();
            visualNeto = atorNeto.GetComponent<MTMNetoVisual>();
            if (quadroNeto == null || quadroNeto.sprite == null)
                return;

            var textura = Resources.Load<Texture2D>(c.arma);
            if (textura == null)
            {
                var inteira = Resources.Load<Sprite>(c.arma);
                textura = inteira == null ? null : inteira.texture;
            }

            if (textura == null)
            {
                Debug.LogWarning("Arma: a arte '" + c.arma + "' não está em Resources; Neto luta de mãos vazias.", this);
                return;
            }

            // Altura de referência: o quadro parado de frente, em unidades locais do desenho de Neto.
            alturaQuadroNeto = quadroNeto.sprite.rect.height / quadroNeto.sprite.pixelsPerUnit;
            float comprimento = alturaQuadroNeto * ProporcaoArma * (visualNeto != null ? visualNeto.escalaVisual : 1f);
            var sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(.5f, PegaArma),
                textura.height / Mathf.Max(.1f, comprimento), 0, SpriteMeshType.FullRect);
            sprite.name = "Arma · " + c.id;
            sprite.hideFlags = HideFlags.DontSave;

            objetoArma = new GameObject("Arma de Neto") { hideFlags = HideFlags.DontSave, layer = atorNeto.gameObject.layer };
            objetoArma.transform.SetParent(atorNeto.transform, false);
            desenhoArma = objetoArma.AddComponent<SpriteRenderer>();
            desenhoArma.sprite = sprite;
            desenhoArma.sharedMaterial = quadroNeto.sharedMaterial;

            var rastro = new GameObject("Rastro do golpe") { hideFlags = HideFlags.DontSave, layer = atorNeto.gameObject.layer };
            rastro.transform.SetParent(atorNeto.transform, false);
            rastroArma = rastro.AddComponent<SpriteRenderer>();
            rastroArma.sprite = SpriteRastroArma();
            rastroArma.enabled = false;

            inicioGolpeArma = -10f;
            anguloArma = PoseArmaNeto[0].y;
            MontarNetoArmado(c);
            AtualizarArma();
        }

        // Folha de Neto segurando a arma (mesmas 32 poses, mesma ordem), recortada pelo JSON indicado.
        private void MontarNetoArmado(MTMConfrontoDados c)
        {
            quadrosNetoArmado = quadrosNetoOriginais = null;
            if (string.IsNullOrEmpty(c.netoArmado) || atorNeto.quadros == null)
                return;
            try
            {
                var arquivo = Resources.Load<TextAsset>(c.netoArmado);
                if (arquivo == null)
                    throw new System.InvalidOperationException("JSON ausente em Resources: " + c.netoArmado);
                var dados = JsonUtility.FromJson<MTMNetoArmadoDados>(arquivo.text);
                if (dados == null || dados.quadros == null || dados.quadros.Length != atorNeto.quadros.Length)
                    throw new System.InvalidOperationException("a folha precisa das mesmas " + atorNeto.quadros.Length + " poses de Neto.");
                var textura = Resources.Load<Texture2D>(dados.textura);
                if (textura == null)
                    throw new System.InvalidOperationException("imagem ausente em Resources: " + dados.textura);
                var quadros = new Sprite[dados.quadros.Length];
                for (int i = 0; i < quadros.Length; i++)
                {
                    var q = dados.quadros[i];
                    quadros[i] = Sprite.Create(textura, new Rect(q.x, q.y, q.largura, q.altura), new Vector2(q.pivotX, q.pivotY), Mathf.Max(1f, q.ppu), 0, SpriteMeshType.FullRect);
                    quadros[i].name = q.id + "_armado";
                    quadros[i].hideFlags = HideFlags.DontSave;
                }

                quadrosNetoOriginais = atorNeto.quadros;
                quadrosNetoArmado = quadros;
            }
            catch (System.Exception erro)
            {
                quadrosNetoArmado = quadrosNetoOriginais = null;
                Debug.LogWarning("Neto armado: " + erro.Message + " Usando o pau solto na mão.", this);
            }
        }

        private void LimparArma()
        {
            if (quadrosNetoOriginais != null && atorNeto != null)
                atorNeto.quadros = quadrosNetoOriginais;
            if (quadrosCorridaSemArma != null && atorNeto != null)
                atorNeto.quadrosCorrida = quadrosCorridaSemArma;
            quadrosNetoArmado = quadrosNetoOriginais = quadrosCorridaSemArma = null;
            if (objetoArma != null)
                Destroy(objetoArma);
            if (rastroArma != null)
                Destroy(rastroArma.gameObject);
            objetoArma = null;
            desenhoArma = rastroArma = null;
        }

        // Chamado quando Neto golpeia: 1 acertou, 0 errou, -1 aparado.
        private void GolpearComArma(Vector2 frente, int resultado)
        {
            if (desenhoArma == null)
                return;
            inicioGolpeArma = Time.unscaledTime;
            frenteGolpeArma = frente.sqrMagnitude > .01f ? frente.normalized : Vector2.down;
            resultadoGolpeArma = resultado;
        }

        // Posição, ângulo e ordem de desenho da arma neste quadro.
        private void AtualizarArma()
        {
            if (desenhoArma == null || quadroNeto == null)
                return;
            bool visivel = NetoComArma && quadroNeto.enabled;
            float decorrido = Time.unscaledTime - inicioGolpeArma;
            bool golpeando = decorrido < DuracaoGolpeArma + VoltaGolpeArma;
            // Com a arte armada, o pau solto só aparece durante o golpe.
            if (quadrosNetoArmado != null)
            {
                var desejado = visivel && !golpeando ? quadrosNetoArmado : quadrosNetoOriginais;
                if (atorNeto.quadros != desejado)
                    atorNeto.quadros = desejado;
            }

            desenhoArma.enabled = visivel && (quadrosNetoArmado == null || golpeando);
            if (!visivel)
            {
                rastroArma.enabled = false;
                return;
            }

            int direcao = Mathf.Clamp(atorNeto.Direcao, 0, 7);
            Vector2 pose = PoseArmaNeto[direcao];
            int indice = Mathf.Clamp(atorNeto.Quadro, 0, MaoArmaNeto.Length - 1);
            // A mão é medida na arte de mãos livres, mesmo enquanto a armada está na tela.
            var quadro = quadrosNetoOriginais != null && indice < quadrosNetoOriginais.Length && quadrosNetoOriginais[indice] != null
                ? quadrosNetoOriginais[indice] : quadroNeto.sprite;
            // O punho foi medido quadro a quadro; a folha anterior tem outra altura (1086 px).
            var tabela = quadro.texture != null && quadro.texture.height == 1086 ? MaoArmaNetoAnterior : MaoArmaNeto;
            Vector2 mao = tabela[indice];
            float escala = (visualNeto != null ? visualNeto.escalaVisual : 1f) / Mathf.Max(1f, quadro.pixelsPerUnit);
            Vector3 local = new Vector3(mao.x * escala, mao.y * escala + (visualNeto != null ? visualNeto.PuloAtual : 0f), 0f);

            float alvo = pose.y;
            if (golpeando)
            {
                // Arco que passa pela direção do golpe: começa erguido atrás e termina à frente.
                float frente = Mathf.Atan2(frenteGolpeArma.y, frenteGolpeArma.x) * Mathf.Rad2Deg - 90f;
                bool horario = frenteGolpeArma.x >= 0f;
                float inicio = frente + (horario ? 125f : -125f), fim = frente + (horario ? -75f : 75f);
                if (decorrido < DuracaoGolpeArma)
                {
                    float t = decorrido / DuracaoGolpeArma;
                    // Ergue rápido e desce com força (ease-out).
                    alvo = Mathf.Lerp(inicio, fim, 1f - (1f - t) * (1f - t) * (1f - t));
                }
                else
                    alvo = Mathf.LerpAngle(fim, pose.y, (decorrido - DuracaoGolpeArma) / VoltaGolpeArma);
                anguloArma = alvo;
            }
            else
                anguloArma = Mathf.LerpAngle(anguloArma, alvo, 1f - Mathf.Exp(-Time.deltaTime * 18f));

            objetoArma.transform.localPosition = local;
            objetoArma.transform.localRotation = Quaternion.Euler(0f, 0f, anguloArma);
            desenhoArma.sortingLayerID = quadroNeto.sortingLayerID;
            // No golpe a arma vem para a frente, a não ser quando Neto está de costas.
            bool atras = pose.x > .5f && !(golpeando && frenteGolpeArma.y < .35f);
            desenhoArma.sortingOrder = quadroNeto.sortingOrder + (atras ? -1 : 1);
            desenhoArma.color = quadroNeto.color;

            // Rastro em meia-lua, do tamanho do alcance da arma.
            float rastroIdade = decorrido;
            rastroArma.enabled = rastroIdade < DuracaoRastroArma;
            if (rastroArma.enabled)
            {
                float frente = Mathf.Atan2(frenteGolpeArma.y, frenteGolpeArma.x) * Mathf.Rad2Deg;
                bool horario = frenteGolpeArma.x >= 0f;
                float raio = alturaQuadroNeto * ProporcaoArma * 1.18f * (visualNeto != null ? visualNeto.escalaVisual : 1f);
                rastroArma.transform.localPosition = local;
                rastroArma.transform.localRotation = Quaternion.Euler(0f, 0f, frente + (horario ? 25f : -25f));
                rastroArma.transform.localScale = new Vector3(raio, horario ? raio : -raio, 1f);
                float a = 1f - rastroIdade / DuracaoRastroArma;
                Color cor = resultadoGolpeArma > 0 ? new Color(1f, .86f, .5f) : resultadoGolpeArma < 0 ? new Color(.75f, .78f, .85f) : new Color(1f, .97f, .9f);
                rastroArma.color = new Color(cor.r, cor.g, cor.b, a * (resultadoGolpeArma > 0 ? .95f : .7f));
                rastroArma.sortingLayerID = quadroNeto.sortingLayerID;
                rastroArma.sortingOrder = quadroNeto.sortingOrder + 2;
            }
        }

        // Meia-lua de 200°, centrada no eixo +x, mais forte na ponta que vem por último (o sentido horário).
        private Sprite SpriteRastroArma()
        {
            if (spriteRastroArma != null)
                return spriteRastroArma;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Rastro_golpe", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            var p = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    float faixa = Mathf.Clamp01(1f - Mathf.Abs(r - .82f) / .16f);
                    // De +100° (começo, apagado) a -100° (fim, forte).
                    float ao = Mathf.InverseLerp(100f, -100f, ang);
                    float borda = Mathf.Clamp01((100f - Mathf.Abs(ang)) / 18f);
                    float alfa = faixa * faixa * Mathf.Pow(ao, 1.6f) * borda * (r < 1f ? 1f : 0f);
                    p[y * n + x] = new Color(1f, 1f, 1f, alfa);
                }

            tex.SetPixels(p);
            tex.Apply(false, true);
            spriteRastroArma = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n * .5f, 0, SpriteMeshType.FullRect);
            spriteRastroArma.hideFlags = HideFlags.DontSave;
            return spriteRastroArma;
        }
    }

    [System.Serializable]
    public sealed class MTMNetoArmadoDados
    {
        public int versao;
        public string textura;
        public MTMQuadroNetoArmado[] quadros;
    }

    [System.Serializable]
    public sealed class MTMQuadroNetoArmado
    {
        public string id;
        public int x, y, largura, altura;
        public float pivotX, pivotY, ppu;
    }
}

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

        // Mão de Neto nas 32 poses da corrida (PersonagensRevisados/NetoCorrida...), em pixels da folha a partir
        // dos pés, na mesma ordem: a mesma mão que segura o pau na corrida armada.
        private static readonly Vector2[] MaoCorridaNeto =
        {
            new Vector2(-69f, 151f), new Vector2(-69f, 157f), new Vector2(-56f, 162f), new Vector2(-35f, 162f),
            new Vector2(-61f, 174f), new Vector2(-50f, 172f), new Vector2(-14f, 158f), new Vector2(-51f, 169f),
            new Vector2(30f, 118f), new Vector2(-37f, 172f), new Vector2(-68f, 176f), new Vector2(22f, 126f),
            new Vector2(-57f, 184f), new Vector2(-53f, 177f), new Vector2(-60f, 189f), new Vector2(-50f, 179f),
            new Vector2(59f, 130f), new Vector2(59f, 137f), new Vector2(59f, 158f), new Vector2(60f, 142f),
            new Vector2(50f, 184f), new Vector2(49f, 180f), new Vector2(49f, 188f), new Vector2(45f, 182f),
            new Vector2(-35f, 118f), new Vector2(33f, 173f), new Vector2(62f, 175f), new Vector2(-28f, 125f),
            new Vector2(-57f, 148f), new Vector2(-57f, 151f), new Vector2(13f, 156f), new Vector2(-10f, 153f)
        };

        // Direções da corrida em que essa mão fica atrás do corpo (costas e costas-direita).
        private static readonly bool[] MaoCorridaAtras = { false, false, false, false, true, true, false, false };

        private Sprite[] quadrosNetoArmado, quadrosNetoOriginais, quadrosCorridaSemArma, quadrosCorridaArmado;
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
            // A luta guarda as poses "sem arma": precisam ser as normais, não as da lanterna.
            RestaurarPosesSemLanterna();
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
            // Na luta, a corrida usa a folha da corrida armada (netoArmadoCorrida); sem ela, ou durante
            // o golpe, usa as poses da caminhada, em que a mão do pau solto foi medida.
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
            MontarCorridaArmada(c);
            AtualizarArma();
        }

        // Corrida segurando a arma: as mesmas 32 poses da corrida, na mesma ordem, recortadas pelo JSON indicado.
        // ppu 0 no JSON = o mesmo da pose de corrida sem arma, para Neto não mudar de tamanho ao correr.
        private void MontarCorridaArmada(MTMConfrontoDados c)
        {
            quadrosCorridaArmado = null;
            // Só com a caminhada armada: sem ela o pau solto já acompanha a mão e ficaria em dobro.
            if (string.IsNullOrEmpty(c.netoArmadoCorrida) || quadrosNetoArmado == null || quadrosCorridaSemArma == null)
                return;
            try
            {
                var arquivo = Resources.Load<TextAsset>(c.netoArmadoCorrida);
                if (arquivo == null)
                    throw new System.InvalidOperationException("JSON ausente em Resources: " + c.netoArmadoCorrida);
                var dados = JsonUtility.FromJson<MTMNetoArmadoDados>(arquivo.text);
                if (dados == null || dados.quadros == null || dados.quadros.Length != quadrosCorridaSemArma.Length)
                    throw new System.InvalidOperationException("a folha precisa das mesmas " + quadrosCorridaSemArma.Length + " poses da corrida.");
                var textura = Resources.Load<Texture2D>(dados.textura);
                if (textura == null)
                    throw new System.InvalidOperationException("imagem ausente em Resources: " + dados.textura);
                var quadros = new Sprite[dados.quadros.Length];
                for (int i = 0; i < quadros.Length; i++)
                {
                    var semArma = quadrosCorridaSemArma[i];
                    // Direção sem pose de corrida continua sem: aí Neto corre com a caminhada armada.
                    if (semArma == null)
                        continue;
                    var q = dados.quadros[i];
                    float ppu = q.ppu > 0f ? q.ppu : semArma.pixelsPerUnit;
                    quadros[i] = Sprite.Create(textura, new Rect(q.x, q.y, q.largura, q.altura), new Vector2(q.pivotX, q.pivotY), Mathf.Max(1f, ppu), 0, SpriteMeshType.FullRect);
                    quadros[i].name = q.id;
                    quadros[i].hideFlags = HideFlags.DontSave;
                }

                quadrosCorridaArmado = quadros;
            }
            catch (System.Exception erro)
            {
                quadrosCorridaArmado = null;
                Debug.LogWarning("Corrida armada: " + erro.Message + " Na luta, Neto corre com as poses da caminhada.", this);
            }
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
            quadrosNetoArmado = quadrosNetoOriginais = quadrosCorridaSemArma = quadrosCorridaArmado = null;
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
                // Correndo, as poses da corrida armada já seguram o pau; no golpe, volta para a caminhada.
                var corrida = visivel && !golpeando ? quadrosCorridaArmado : null;
                if (atorNeto.quadrosCorrida != corrida)
                    atorNeto.quadrosCorrida = corrida;
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
            // Pisca junto com Neto depois de um golpe recebido.
            desenhoArma.color = visualNeto == null ? quadroNeto.color
                : new Color(quadroNeto.color.r, quadroNeto.color.g, quadroNeto.color.b, quadroNeto.color.a * visualNeto.AlfaDano);

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

        // ---------- lanterna ----------
        // À noite Neto pode acender a lanterna (F ou RB): ela fica na mão, apontada para onde ele olha, com o
        // facho no chão à frente, e a luz da noite passa a sair dela. Apagada, só um pouco de luar fica em volta
        // dele. Na luta a mão está no pau de jucá: a lanterna some e a luz volta a ficar em volta de Neto.
        private const string ArteLanterna = "MemoriasTecidas/Itens/lanterna";
        private const string ChaveLanterna = "MemoriasTecidas.Interface.Lanterna";
        // Comprimento no mundo e ponto de pega (fração a partir da ponta de trás).
        private const float ComprimentoLanterna = .34f, PegaLanterna = .36f;
        private const float AlcanceFacho = 4.2f, LarguraFacho = 2.6f, OrdemFacho = -21950f;
        private GameObject objetoLanterna, objetoFacho;
        private SpriteRenderer desenhoLanterna, desenhoFacho, brilhoLanterna;
        private Sprite spriteFacho;
        private bool lanternaLigada = true, lanternaSemArte, avisoLanternaDado;
        private float alfaLanterna, anguloLanterna;
        private Vector2 pontaLanterna, direcaoLanterna = Vector2.down;

        // A lanterna está na mão e acesa agora: a luz da noite sai do facho.
        private bool LanternaAcesa
        {
            get { return objetoLanterna != null && objetoLanterna.activeSelf && alfaLanterna > .05f; }
        }

        private void AlternarLanterna()
        {
            lanternaLigada = !lanternaLigada;
            PlayerPrefs.SetInt(ChaveLanterna, lanternaLigada ? 1 : 0);
            TocarSom("ui_clique", .75f, .05f);
            if (Escuridao < .05f)
                Avisar(lanternaLigada ? "Lanterna pronta: ela acende quando escurecer." : "Lanterna desligada.");
        }

        // Poses do Neto segurando a lanterna (andando e correndo, as mesmas 32 de cada), com a lente medida
        // em cada quadro. Acesa, a lanterna usa essas poses; o facho e o brilho saem da lente desenhada.
        private const string PosesLanternaAndar = "MemoriasTecidas/HD2D/PersonagensRevisados/neto_lanterna";
        private const string PosesLanternaCorrida = "MemoriasTecidas/HD2D/PersonagensRevisados/neto_corrida_lanterna";
        private Sprite[] quadrosLanternaAndar, quadrosLanternaCorrida, quadrosBaseAndar, quadrosBaseCorrida;
        private Vector2[] lenteAndar, lenteCorrida;
        private bool posesLanternaTentadas, posesLanternaAplicadas;

        private void MontarPosesLanterna()
        {
            posesLanternaTentadas = true;
            if (atorNeto == null || atorNeto.quadros == null)
                return;
            quadrosBaseAndar = atorNeto.quadros;
            quadrosBaseCorrida = atorNeto.quadrosCorrida;
            quadrosLanternaAndar = CarregarPosesLanterna(PosesLanternaAndar, quadrosBaseAndar, out lenteAndar);
            quadrosLanternaCorrida = quadrosBaseCorrida == null ? null : CarregarPosesLanterna(PosesLanternaCorrida, quadrosBaseCorrida, out lenteCorrida);
        }

        // Mesmas poses, mesma ordem; ppu 0 = o da pose original, para o Neto não mudar de tamanho.
        private Sprite[] CarregarPosesLanterna(string caminho, Sprite[] originais, out Vector2[] lentes)
        {
            lentes = null;
            try
            {
                var arquivo = Resources.Load<TextAsset>(caminho);
                if (arquivo == null)
                    return null;
                var dados = JsonUtility.FromJson<MTMNetoArmadoDados>(arquivo.text);
                if (dados == null || dados.quadros == null || dados.quadros.Length != originais.Length)
                    throw new System.InvalidOperationException("a folha precisa das mesmas " + originais.Length + " poses.");
                var textura = Resources.Load<Texture2D>(dados.textura);
                if (textura == null)
                    throw new System.InvalidOperationException("imagem ausente em Resources: " + dados.textura);
                var quadros = new Sprite[originais.Length];
                lentes = new Vector2[originais.Length];
                for (int i = 0; i < quadros.Length; i++)
                {
                    var original = originais[i];
                    if (original == null)
                        continue;
                    var q = dados.quadros[i];
                    float ppu = q.ppu > 0f ? q.ppu : original.pixelsPerUnit;
                    quadros[i] = Sprite.Create(textura, new Rect(q.x, q.y, q.largura, q.altura), new Vector2(q.pivotX, q.pivotY), Mathf.Max(1f, ppu), 0, SpriteMeshType.FullRect);
                    quadros[i].name = q.id;
                    quadros[i].hideFlags = HideFlags.DontSave;
                    lentes[i] = new Vector2(q.lenteX, q.lenteY);
                }

                return quadros;
            }
            catch (System.Exception erro)
            {
                lentes = null;
                Debug.LogWarning("Poses com a lanterna (" + caminho + "): " + erro.Message + " Usando a lanterna solta na mão.", this);
                return null;
            }
        }

        // Troca as poses só na mudança (acende/apaga), para não brigar com as poses da luta.
        private void AplicarPosesLanterna(bool usar)
        {
            usar = usar && quadrosLanternaAndar != null && atorNeto != null;
            if (usar == posesLanternaAplicadas || atorNeto == null)
                return;
            atorNeto.quadros = usar ? quadrosLanternaAndar : quadrosBaseAndar;
            atorNeto.quadrosCorrida = usar ? quadrosLanternaCorrida ?? quadrosBaseCorrida : quadrosBaseCorrida;
            posesLanternaAplicadas = usar;
        }

        private void RestaurarPosesSemLanterna()
        {
            AplicarPosesLanterna(false);
        }

        // Lente desenhada no quadro atual (pixels da folha a partir dos pés), se as poses com a lanterna estão na tela.
        private bool LenteNaArte(bool corrida, int indice, out Vector2 lente)
        {
            lente = Vector2.zero;
            if (!posesLanternaAplicadas || quadroNeto == null)
                return false;
            var poses = corrida ? quadrosLanternaCorrida : quadrosLanternaAndar;
            var lentes = corrida ? lenteCorrida : lenteAndar;
            if (poses == null || lentes == null || indice < 0 || indice >= lentes.Length || quadroNeto.sprite != poses[indice])
                return false;
            lente = lentes[indice];
            return lente.sqrMagnitude > .01f;
        }

        // 0 baixo, 1 baixo-esquerda, 2 esquerda, 3 cima-esquerda, 4 cima, 5 cima-direita, 6 direita, 7 baixo-direita.
        private static float AnguloDaDirecao(int direcao)
        {
            return 270f - direcao * 45f;
        }

        private void MontarLanterna()
        {
            if (atorNeto == null)
                VisualDoNeto();
            if (atorNeto == null)
                return;
            if (quadroNeto == null)
                quadroNeto = atorNeto.GetComponent<SpriteRenderer>();
            var textura = Resources.Load<Texture2D>(ArteLanterna);
            if (textura == null)
            {
                var inteira = Resources.Load<Sprite>(ArteLanterna);
                textura = inteira == null ? null : inteira.texture;
            }

            if (textura == null)
            {
                lanternaSemArte = true;
                Debug.LogWarning("Lanterna: a arte '" + ArteLanterna + "' não está em Resources.", this);
                return;
            }

            var sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(PegaLanterna, .5f),
                textura.width / ComprimentoLanterna, 0, SpriteMeshType.FullRect);
            sprite.name = "Lanterna";
            sprite.hideFlags = HideFlags.DontSave;
            int camada = atorNeto.gameObject.layer;
            objetoLanterna = new GameObject("Lanterna de Neto") { hideFlags = HideFlags.DontSave, layer = camada };
            objetoLanterna.transform.SetParent(atorNeto.transform, false);
            desenhoLanterna = objetoLanterna.AddComponent<SpriteRenderer>();
            desenhoLanterna.sprite = sprite;
            if (quadroNeto != null)
                desenhoLanterna.sharedMaterial = quadroNeto.sharedMaterial;

            var lente = new GameObject("Brilho da lente") { hideFlags = HideFlags.DontSave, layer = camada };
            lente.transform.SetParent(objetoLanterna.transform, false);
            lente.transform.localPosition = new Vector3(ComprimentoLanterna * (1f - PegaLanterna), 0f, 0f);
            lente.transform.localScale = Vector3.one * .36f;
            brilhoLanterna = lente.AddComponent<SpriteRenderer>();
            brilhoLanterna.sprite = SpriteBrilho();

            // O facho fica no chão: abaixo de tudo o que está em pé, acima do piso e das sombras.
            objetoFacho = new GameObject("Facho da lanterna") { hideFlags = HideFlags.DontSave, layer = camada };
            objetoFacho.transform.SetParent(transform, false);
            desenhoFacho = objetoFacho.AddComponent<SpriteRenderer>();
            desenhoFacho.sprite = SpriteFacho();
            desenhoFacho.sortingOrder = (int)OrdemFacho;
            anguloLanterna = AnguloDaDirecao(Mathf.Clamp(atorNeto.Direcao, 0, 7));
        }

        // Chamado depois da câmera, a cada quadro (também com diálogo aberto: a lanterna continua na mão).
        private void AtualizarLanterna()
        {
            if (!pronto || corpo == null)
                return;
            float noite = Escuridao;
            if (!avisoLanternaDado && noite > .5f && jornadaIniciadaUI && !inicioAbertoUI && falas == null)
            {
                avisoLanternaDado = true;
                Avisar("Anoiteceu. " + (MTMEntrada.UsandoControle ? "RB" : "F") + " liga e desliga a lanterna.");
            }

            bool quer = lanternaLigada && noite > .05f && !EmConfronto;
            if (objetoLanterna == null)
            {
                alfaLanterna = 0f;
                if (!quer || lanternaSemArte)
                    return;
                MontarLanterna();
                if (objetoLanterna == null)
                    return;
            }

            bool pode = quer && atorNeto != null && quadroNeto != null && quadroNeto.enabled && quadroNeto.sprite != null;
            alfaLanterna = Mathf.MoveTowards(alfaLanterna, pode ? 1f : 0f, Time.deltaTime * 5f);
            bool visivel = alfaLanterna > .01f && atorNeto != null && quadroNeto != null && quadroNeto.sprite != null;
            // As poses com a lanterna na mão entram quando ela acende de verdade (e nunca na luta).
            if (visivel && !posesLanternaTentadas)
                MontarPosesLanterna();
            AplicarPosesLanterna(visivel && alfaLanterna > .5f && !EmConfronto);
            if (objetoLanterna.activeSelf != visivel)
                objetoLanterna.SetActive(visivel);
            if (objetoFacho.activeSelf != visivel)
                objetoFacho.SetActive(visivel);
            if (!visivel)
                return;

            // Na mão: a mesma mão do pau de jucá, medida quadro a quadro na caminhada e na corrida.
            // Com as poses da lanterna, o ponto é a lente desenhada no quadro.
            int direcao = Mathf.Clamp(atorNeto.Direcao, 0, 7);
            int indice = Mathf.Clamp(atorNeto.Quadro, 0, MaoArmaNeto.Length - 1);
            var quadro = quadroNeto.sprite;
            bool corrida = atorNeto.CorrendoComQuadros;
            Vector2 lente;
            bool naArte = LenteNaArte(corrida, indice, out lente);
            Vector2 mao = naArte ? lente : corrida ? MaoCorridaNeto[indice] : (quadro.texture != null && quadro.texture.height == 1086 ? MaoArmaNetoAnterior : MaoArmaNeto)[indice];
            float escala = (visualNeto != null ? visualNeto.escalaVisual : 1f) / Mathf.Max(1f, quadro.pixelsPerUnit);
            objetoLanterna.transform.localPosition = new Vector3(mao.x * escala, mao.y * escala + (visualNeto != null ? visualNeto.PuloAtual : 0f), 0f);

            // Aponta para onde Neto olha, virando suave entre as 8 direções.
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            float alvo = AnguloDaDirecao(direcao);
            anguloLanterna = reduzir ? alvo : Mathf.LerpAngle(anguloLanterna, alvo, 1f - Mathf.Exp(-14f * Time.deltaTime));
            float rad = anguloLanterna * Mathf.Deg2Rad;
            direcaoLanterna = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            objetoLanterna.transform.localRotation = Quaternion.Euler(0f, 0f, anguloLanterna);
            // Apontando para a esquerda a arte vira de cabeça para baixo: espelha para o botão ficar em cima.
            desenhoLanterna.flipY = direcaoLanterna.x < -.01f;
            // Com a lanterna já desenhada na mão, a solta some: ficam o brilho na lente e o facho saindo dela.
            desenhoLanterna.enabled = !naArte;
            brilhoLanterna.transform.localPosition = naArte ? Vector3.zero : new Vector3(ComprimentoLanterna * (1f - PegaLanterna), 0f, 0f);
            pontaLanterna = naArte ? (Vector2)objetoLanterna.transform.position
                : (Vector2)objetoLanterna.transform.position + direcaoLanterna * (ComprimentoLanterna * (1f - PegaLanterna));

            bool atras = corrida ? MaoCorridaAtras[direcao] : PoseArmaNeto[direcao].x > .5f;
            desenhoLanterna.sortingLayerID = brilhoLanterna.sortingLayerID = quadroNeto.sortingLayerID;
            desenhoLanterna.sortingOrder = quadroNeto.sortingOrder + (atras ? -1 : 2);
            brilhoLanterna.sortingOrder = naArte ? quadroNeto.sortingOrder + (atras ? -1 : 3) : desenhoLanterna.sortingOrder + 1;
            desenhoLanterna.color = new Color(1f, 1f, 1f, alfaLanterna);

            float forca = Mathf.Clamp01(noite * 1.2f) * alfaLanterna;
            brilhoLanterna.color = new Color(1f, .93f, .76f, .85f * forca);
            Vector3 s = transform.lossyScale;
            objetoFacho.transform.position = new Vector3(pontaLanterna.x, pontaLanterna.y, 0f);
            objetoFacho.transform.rotation = Quaternion.Euler(0f, 0f, anguloLanterna);
            objetoFacho.transform.localScale = new Vector3(AlcanceFacho / Mathf.Max(.001f, Mathf.Abs(s.x)), LarguraFacho / Mathf.Max(.001f, Mathf.Abs(s.y)), 1f);
            desenhoFacho.color = new Color(1f, .95f, .8f, .34f * forca);
        }

        // Cone de luz de 1 x 1 unidade, saindo do meio da borda esquerda: forte perto da lente, some no fim.
        private Sprite SpriteFacho()
        {
            if (spriteFacho != null)
                return spriteFacho;
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Facho_lanterna", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var p = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + .5f) / n, v = Mathf.Abs((y + .5f) / n * 2f - 1f);
                    float largura = .1f + .9f * u;
                    float dentro = v / largura;
                    float a = 0f;
                    if (dentro < 1f)
                    {
                        float borda = 1f - Mathf.SmoothStep(.6f, 1f, dentro);
                        float ao = Mathf.Pow(1f - u, 1.4f) * Mathf.Clamp01(u / .04f);
                        a = ao * borda * (1f - .35f * dentro * dentro);
                    }

                    p[y * n + x] = new Color(1f, 1f, 1f, a);
                }

            tex.SetPixels(p);
            tex.Apply(false, true);
            spriteFacho = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0f, .5f), n, 0, SpriteMeshType.FullRect);
            spriteFacho.hideFlags = HideFlags.DontSave;
            return spriteFacho;
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
        // Só nas poses com a lanterna: a lente, em pixels da folha a partir dos pés (y para cima). 0, 0 = sem medida.
        public float lenteX, lenteY;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Monta o acervo da Casa das Memórias: importa a arte de Arte/Objetos/Memorias, cria os prefabs em
    // Prefabs/Memorias e coloca na região "memorias" a galeria de retratos, o canto de cada personagem,
    // o tapete com tamboretes, a tradição da rede e duas leituras (galeria e moldura vazia).
    // Pode rodar de novo: refaz o grupo "Acervo" e as leituras, sem duplicar nada.
    // Linha de comando (Unity fechado):
    //   unity run <projeto> -- -executeMethod MemoriasTecidas.MundoV2.MTMMontagemMemorias.Montar
    public static class MTMMontagemMemorias
    {
        private const string Cena = "Assets/MemoriasTecidas/Cenas/Jaguaruana.unity";
        private const string PastaArte = "Assets/MemoriasTecidas/Arte/Objetos/Memorias";
        private const string PastaPrefabs = "Assets/MemoriasTecidas/Prefabs/Memorias";
        private const string MaterialDesenho = "Assets/MemoriasTecidas/Materiais/Mundo/Pintado_V2.mat";
        private const string MaterialSombra = "Assets/MemoriasTecidas/Materiais/Mundo/Sombra_V2.mat";
        // Acima do piso de tijolos (-29000 na cena) e abaixo das sombras (-22000) e de tudo que usa MTMOrdemY (-28000 em diante).
        private const int OrdemTapete = -28500;

        private enum Tipo
        {
            Parede, // pendurado: sem sombra no chão e sem colisão
            Piso,   // deitado no chão: ordem fixa
            Movel   // em pé: sombra, ordem pela altura e colisão na base
        }

        private sealed class Peca
        {
            public readonly string arte;
            public readonly float ppu, pivo;
            public readonly Tipo tipo;
            public readonly Vector2 colisor;

            public Peca(string arte, float ppu, float pivo, Tipo tipo, float largura = 0f, float altura = 0f)
            {
                this.arte = arte;
                this.ppu = ppu;
                this.pivo = pivo;
                this.tipo = tipo;
                colisor = new Vector2(largura, altura);
            }
        }

        // PPU e pivô (fração da altura, de baixo para cima) de cada desenho.
        private static readonly Peca[] Pecas =
        {
            new Peca("galeria_raimundo", 220f, .04857f, Tipo.Parede),
            new Peca("galeria_rua", 220f, .06489f, Tipo.Parede),
            new Peca("galeria_praca", 220f, .06489f, Tipo.Parede),
            new Peca("galeria_matriz", 220f, .05787f, Tipo.Parede),
            new Peca("galeria_bandeira", 220f, .07160f, Tipo.Parede),
            new Peca("galeria_duceu", 220f, .06199f, Tipo.Parede),
            new Peca("galeria_ivonete", 220f, .06199f, Tipo.Parede),
            new Peca("tapete_varandas", 100f, .50052f, Tipo.Piso),
            new Peca("lousa_mestre", 200f, .02909f, Tipo.Movel, 1.7f, .34f),
            new Peca("carteira_escolar", 200f, .04000f, Tipo.Movel, 1.3f, .42f),
            new Peca("mesa_redacao", 200f, .02791f, Tipo.Movel, 2.5f, .55f),
            new Peca("estante_livros", 200f, .02000f, Tipo.Movel, 1.95f, .45f),
            new Peca("moldura_vazia", 200f, .02692f, Tipo.Movel, 1.2f, .32f),
            new Peca("tamborete_couro", 200f, .06410f, Tipo.Movel, .56f, .22f),
            new Peca("expositor_galeria", 200f, .03448f, Tipo.Movel, .62f, .28f),
        };

        [MenuItem("Memorias Tecidas/Montar acervo da Casa das Memórias")]
        public static void Montar()
        {
            try
            {
                var prefabs = PrepararArteEPrefabs();
                var cena = EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
                var regiao = UnityEngine.Object.FindObjectsByType<MTMRegiao>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(r => r.dados != null && r.dados.id == "memorias");
                if (regiao == null)
                    throw new InvalidOperationException("região \"memorias\" não encontrada na cena.");

                var cenario = Filho(regiao.transform, "05 · Cenário");
                ArrumarMobiliario(Filho(cenario, "Mobiliario"));
                MontarAcervo(cenario, prefabs);
                MontarLeituras(Filho(regiao.transform, "07 · Placas"));

                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena))
                    throw new InvalidOperationException("não foi possível salvar a cena.");
                Debug.Log("Casa das Memórias: acervo montado e cena salva.");
            }
            catch (Exception erro)
            {
                Debug.LogError("Casa das Memórias: " + erro.Message + "\n" + erro.StackTrace);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        // Monta e já fotografa a sala (aceita os mesmos argumentos de MTMCapturaRegiao: -saida, -zoom, -foco).
        public static void MontarECapturar()
        {
            Montar();
            MTMCapturaRegiao.Capturar();
        }

        // ------------------------------------------------------------ arte e prefabs
        private static Dictionary<string, GameObject> PrepararArteEPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(PastaArte))
                throw new InvalidOperationException("pasta de arte ausente: " + PastaArte);
            if (!AssetDatabase.IsValidFolder(PastaPrefabs))
                AssetDatabase.CreateFolder("Assets/MemoriasTecidas/Prefabs", "Memorias");

            ConfigurarTextura(PastaArte + "/retrato_raimundo.png", null);
            foreach (var peca in Pecas)
                ConfigurarTextura(PastaArte + "/" + peca.arte + ".png", peca);

            var desenho = AssetDatabase.LoadAssetAtPath<Material>(MaterialDesenho);
            var sombra = AssetDatabase.LoadAssetAtPath<Material>(MaterialSombra);
            if (desenho == null || sombra == null)
                throw new InvalidOperationException("materiais Pintado_V2/Sombra_V2 não encontrados.");

            var prefabs = new Dictionary<string, GameObject>();
            foreach (var peca in Pecas)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PastaArte + "/" + peca.arte + ".png");
                if (sprite == null)
                    throw new InvalidOperationException("sprite não importado: " + peca.arte);
                prefabs[peca.arte] = CriarPrefab(peca, sprite, desenho, sombra);
            }

            AssetDatabase.SaveAssets();
            return prefabs;
        }

        private static void ConfigurarTextura(string caminho, Peca peca)
        {
            var importador = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (importador == null)
                throw new InvalidOperationException("textura não encontrada: " + caminho);
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.spritePixelsPerUnit = peca != null ? peca.ppu : 100f;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;
            importador.filterMode = FilterMode.Bilinear;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.maxTextureSize = 2048;
            importador.textureCompression = TextureImporterCompression.CompressedHQ;
            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
            ajustes.spritePivot = new Vector2(.5f, peca != null ? peca.pivo : .5f);
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.spriteGenerateFallbackPhysicsShape = false;
            importador.SetTextureSettings(ajustes);
            importador.SaveAndReimport();
        }

        // Mesma estrutura dos prefabs do mundo (ex.: banco): raiz com MTMVisualV2 e colisão,
        // "Desenho" com MTMOrdemY e "Sombra" achatada no chão.
        private static GameObject CriarPrefab(Peca peca, Sprite sprite, Material desenho, Material sombra)
        {
            var raiz = new GameObject(peca.arte);
            try
            {
                var arte = new GameObject("Desenho");
                arte.transform.SetParent(raiz.transform, false);
                var sr = arte.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sharedMaterial = desenho;
                sr.spriteSortPoint = SpriteSortPoint.Pivot;
                if (peca.tipo == Tipo.Piso)
                    sr.sortingOrder = OrdemTapete;
                else
                    arte.AddComponent<MTMOrdemY>();

                if (peca.tipo == Tipo.Movel)
                {
                    var go = new GameObject("Sombra");
                    go.transform.SetParent(raiz.transform, false);
                    go.transform.localScale = new Vector3(1.02f, .2f, 1f);
                    var ss = go.AddComponent<SpriteRenderer>();
                    ss.sprite = sprite;
                    ss.sharedMaterial = sombra;
                    ss.sortingOrder = -22000;
                    ss.color = new Color(.1f, .13f, .15f, .22f);
                    var visual = raiz.AddComponent<MTMVisualV2>();
                    visual.desenho = sr;
                    visual.sombra = ss;
                    var colisor = raiz.AddComponent<BoxCollider2D>();
                    colisor.size = peca.colisor;
                    colisor.offset = new Vector2(0f, peca.colisor.y * .5f);
                }

                return PrefabUtility.SaveAsPrefabAsset(raiz, PastaPrefabs + "/" + peca.arte + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        // ------------------------------------------------------------ cena
        private static Transform Filho(Transform pai, string nome)
        {
            var t = pai.Find(nome);
            if (t == null)
                throw new InvalidOperationException("\"" + nome + "\" não encontrado em " + pai.name);
            return t;
        }

        // Tira as bancas e caixotes da feira; os bancos vão para junto das paredes laterais.
        private static void ArrumarMobiliario(Transform mobiliario)
        {
            var bancos = new List<Transform>();
            foreach (Transform t in mobiliario.Cast<Transform>().ToArray())
            {
                if (t.name.StartsWith("banca", StringComparison.Ordinal) || t.name.StartsWith("caixotes", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                else if (t.name.StartsWith("banco", StringComparison.Ordinal))
                    bancos.Add(t);
            }

            bancos.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));
            for (int i = 0; i < bancos.Count; i++)
            {
                if (i == 0)
                    bancos[i].localPosition = new Vector3(-15.8f, .2f, 0f);
                else if (i == bancos.Count - 1)
                    bancos[i].localPosition = new Vector3(15.8f, .2f, 0f);
                else
                    UnityEngine.Object.DestroyImmediate(bancos[i].gameObject);
            }
        }

        private static void MontarAcervo(Transform cenario, Dictionary<string, GameObject> prefabs)
        {
            var antigo = cenario.Find("Acervo");
            if (antigo != null)
                UnityEngine.Object.DestroyImmediate(antigo.gameObject);
            var acervo = Grupo(cenario, "Acervo");

            // Galeria no painel claro da parede do fundo (x de -6,8 a 6,9): os três retratos no centro,
            // a cidade nas pontas, todos centrados na altura 8,72 (o pivô é a base da moldura).
            var galeria = Grupo(acervo, "Galeria");
            Por(galeria, prefabs["galeria_rua"], "Quadro · Rua antiga", -5.34f, 8.08f, .85f);
            Por(galeria, prefabs["galeria_matriz"], "Quadro · Matriz de Sant'Ana", -3.33f, 7.93f, .9f);
            Por(galeria, prefabs["galeria_duceu"], "Quadro · Padre Ducéu", -1.73f, 7.92f);
            Por(galeria, prefabs["galeria_raimundo"], "Quadro · Raimundo Jaguaribe", 0f, 7.72f, .9f);
            Por(galeria, prefabs["galeria_ivonete"], "Quadro · Ivonete Maia", 1.73f, 7.92f);
            Por(galeria, prefabs["galeria_bandeira"], "Quadro · Bandeira de Jaguaruana", 3.43f, 8.19f, .82f);
            Por(galeria, prefabs["galeria_praca"], "Quadro · Praça antiga", 5.53f, 8.08f, .85f);
            Por(galeria, prefabs["expositor_galeria"], "Expositor da galeria", -3.3f, 3.5f);

            // Canto de cada personagem, atrás de onde ele fica.
            var gerardinho = Grupo(acervo, "Canto · Mestre Gerardinho");
            Por(gerardinho, prefabs["lousa_mestre"], "Lousa do Mestre", -11.2f, 2.4f);
            Por(gerardinho, prefabs["carteira_escolar"], "Carteira escolar", -7.9f, 2.0f);
            Por(gerardinho, prefabs["carteira_escolar"], "Carteira escolar (2)", -7.9f, 3.45f);

            var ivonete = Grupo(acervo, "Canto · Ivonete Maia");
            Por(ivonete, prefabs["mesa_redacao"], "Mesa de redação", 0f, 2.5f);

            var emilia = Grupo(acervo, "Canto · Emília Freitas");
            Por(emilia, prefabs["moldura_vazia"], "Moldura vazia", 7.9f, 2.4f);
            Por(emilia, prefabs["estante_livros"], "Estante de livros", 11.4f, 2.7f);

            // Roda de escuta: tapete de varandas e tamboretes, com passagem livre no meio.
            var roda = Grupo(acervo, "Roda de escuta");
            Por(roda, prefabs["tapete_varandas"], "Tapete de varandas", 0f, -2.55f);
            Por(roda, prefabs["tamborete_couro"], "Tamborete", -4.5f, -3.5f);
            Por(roda, prefabs["tamborete_couro"], "Tamborete (2)", -2.3f, -4.0f);
            Por(roda, prefabs["tamborete_couro"], "Tamborete (3)", 2.3f, -4.0f);
            Por(roda, prefabs["tamborete_couro"], "Tamborete (4)", 4.5f, -3.5f);
            // Dois tamboretes diante do Mestre Gerardinho e dois diante de Emília Freitas.
            Por(roda, prefabs["tamborete_couro"], "Tamborete (5)", -10.7f, -1.5f);
            Por(roda, prefabs["tamborete_couro"], "Tamborete (6)", -8.5f, -1.8f);
            Por(roda, prefabs["tamborete_couro"], "Tamborete (7)", 8.5f, -1.8f);
            Por(roda, prefabs["tamborete_couro"], "Tamborete (8)", 10.7f, -1.5f);

            // A tradição da rede, nos cantos de baixo, e potes de barro.
            var rede = Grupo(acervo, "Tradição da rede");
            Por(rede, Existente("Mundo/tear"), "Tear", -14.4f, -7.4f);
            Por(rede, Existente("Locais/sacos_algodao"), "Sacos de algodão", -11.3f, -8.1f);
            Por(rede, Existente("Locais/rede_tecida"), "Rede tecida", 14.2f, -7.3f);
            Por(rede, Existente("Mundo/vasos"), "Potes de barro", 11.2f, -8.1f);
            Por(rede, Existente("Mundo/vasos"), "Potes de barro (2)", 3.4f, 4.8f);
            Por(rede, Existente("Mundo/vasos"), "Potes de barro na porta", -2.1f, -9.4f);
            Por(rede, Existente("Mundo/vasos"), "Potes de barro na porta (2)", 2.1f, -9.4f);
        }

        private static Transform Grupo(Transform pai, string nome)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            return go.transform;
        }

        private static GameObject Existente(string caminho)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MemoriasTecidas/Prefabs/" + caminho + ".prefab");
            if (prefab == null)
                throw new InvalidOperationException("prefab não encontrado: " + caminho);
            return prefab;
        }

        private static void Por(Transform pai, GameObject prefab, string nome, float x, float y, float escala = 1f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
            go.name = nome;
            go.transform.localPosition = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(escala, escala, 1f);
            // A mesma ordem que MTMOrdemY calcula em jogo, para a cena já abrir (e ser fotografada) certa.
            foreach (var ordem in go.GetComponentsInChildren<MTMOrdemY>(true))
                ordem.GetComponent<SpriteRenderer>().sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-ordem.transform.position.y * 100f), -28000, 28000);
        }

        // Duas leituras: a galeria (com o retrato do Raimundo na conversa) e a moldura vazia de Emília.
        private static void MontarLeituras(Transform placas)
        {
            foreach (Transform t in placas.Cast<Transform>().ToArray())
                if (t.name.StartsWith("Leitura · ", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(t.gameObject);

            Leitura(placas, "Leitura · Galeria", new Vector2(-3.3f, 3.5f), 1.6f, new MTMPontoDados
            {
                id = "memorias_galeria",
                nome = "Galeria dos ilustres",
                tipo = "leitura",
                fonte = "Memórias de Jaguaruana (blog, 2017): fotos, texto de Vicência Jaguaribe e entrevista com o prof. José Wilson Freitas. Histórico da EEF Gerardo Correia Lima (Prefeitura). Livro de João Luiz de Oliveira. Vermelho (14/02/2012).",
                falas = new[]
                {
                    "Nesta parede estão alguns jaguaruanenses ilustres. No centro, Raimundo Francisco Freitas Jaguaribe (1952–2017). Como prefeito, criou em 1978 a escola Gerardo Correia Lima, inaugurada com a bênção do Padre Ducéu.",
                    "A irmã dele, Vicência Jaguaribe, lembra um leitor de Santo Agostinho e de Clarice Lispector, bom contador de histórias, que acreditava na educação: escola de música, banda e biblioteca.",
                    "À esquerda está o Padre Ducéu, vigário da paróquia a partir de 1969. À direita, a jornalista Ivonete Maia, nascida aqui: foi da primeira turma de Jornalismo da UFC e a primeira mulher a presidir o Sindicato dos Jornalistas do Ceará.",
                    "Nas pontas estão a rua antiga, a Matriz de Sant'Ana, a bandeira e a praça: a cidade que essas pessoas ajudaram a construir.",
                    "Repare: numa entrevista, o prof. José Wilson conta que foi o prefeito Manoel Barbosa Rodrigues quem primeiro deu à escola o nome do Mestre. Quando dois registros não batem, anote os dois e compare."
                }
            }, AssetDatabase.LoadAssetAtPath<Texture2D>(PastaArte + "/retrato_raimundo.png"), new Rect(0f, 0f, 1f, 1f));

            Leitura(placas, "Leitura · Moldura vazia", new Vector2(7.9f, 2.4f), 1.4f, new MTMPontoDados
            {
                id = "memorias_moldura",
                nome = "Moldura de Emília Freitas",
                tipo = "leitura",
                fonte = "O Povo: \"De renegada a rainha\" (2021) e reportagem sobre o filme (2026).",
                falas = new[]
                {
                    "Esta moldura continua vazia. Não se conhece nenhum retrato de Emília Freitas; a imagem que costuma circular como dela é de outra mulher, também chamada Emília de Freitas.",
                    "Por isso, a Emília desta casa é uma representação artística. Um filme em produção, A Paladina da Ilha do Nevoeiro, parte justamente dessa busca pelo rosto perdido da escritora."
                }
            }, AssetDatabase.LoadAssetAtPath<Texture2D>(PastaArte + "/moldura_vazia.png"), new Rect(.05f, .36f, .9f, .62f));
        }

        private static void Leitura(Transform pai, string nome, Vector2 posicao, float alcance, MTMPontoDados dados, Texture2D retrato, Rect recorte)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localPosition = new Vector3(posicao.x, posicao.y, 0f);
            dados.x = posicao.x;
            dados.y = posicao.y;
            dados.destino = dados.evento = dados.condicao = "";
            var ponto = go.AddComponent<MTMPonto>();
            ponto.dados = dados;
            ponto.retrato = retrato;
            ponto.recorteRosto = recorte;
            ponto.alcance = alcance;
        }
    }
}

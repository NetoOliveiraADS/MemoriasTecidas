using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MemoriasTecidas.PracaV1;
using MemoriasTecidas.PracaV1.Editor;

namespace MemoriasTecidas.Missao01.Editor
{
    public static class MTMemoriaInstalador
    {
        private const string Raiz = "Assets/MemoriasTecidas/Missao01";
        private const string Gerados = Raiz + "/Gerados";
        private static readonly Color Fundo = new Color(.055f, .12f, .12f, .97f);
        private static readonly Color Ouro = new Color(.97f, .78f, .43f);
        private static readonly Color Claro = new Color(.98f, .96f, .88f);
        private static readonly Color Suave = new Color(.72f, .85f, .81f);

        [MenuItem("Memorias Tecidas/Missao 01/1 - Criar copia da praca com a missao")]
        public static void Instalar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("Saia do Play antes de instalar a Missão 01."); return; }
            try
            {
                Scene cena = SceneManager.GetActiveScene();
                if (SceneManager.sceneCount != 1)
                    throw new InvalidOperationException("Abra somente a cena da praça antes de instalar.");
                var existente = NaCena<MTMemoriaMissao>(cena);
                if (existente.Count > 0)
                {
                    Selection.activeGameObject = existente[0].gameObject;
                    Debug.Log("A cena já tem a Missão 01. Nada foi duplicado. Use o menu 2 para validar.");
                    return;
                }
                LocalizarBase(cena, out MTPracaJogador jogador, out Transform neto, out Camera camera);
                if (!File.Exists("Assets/MemoriasTecidas/PracaV1/Data/praca_manifesto.json"))
                    throw new FileNotFoundException("Importe primeiro o pacote Praça V1 na pasta original.");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Directory.CreateDirectory(Gerados + "/Cenas");
                Directory.CreateDirectory(Gerados + "/Dialogos");
                AssetDatabase.Refresh();
                Sprite sprite = PrepararAndre();
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Não foi possível carregar a fonte da Unity.");
                VerificarRegras();

                // Salva sob um caminho novo antes de alterar a cena. O arquivo original permanece.
                string destino = AssetDatabase.GenerateUniqueAssetPath(Gerados + "/Cenas/Jaguaruana_Praca_Missao01.unity");
                if (!EditorSceneManager.SaveScene(cena, destino)) throw new IOException("A cópia da cena não foi salva.");
                CorrigirAnimacao(jogador);
                foreach (MTPracaDemo demo in NaCena<MTPracaDemo>(cena))
                {
                    demo.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(demo);
                }
                jogador.demonstracao = null;
                PrefabUtility.RecordPrefabInstancePropertyModifications(jogador);
                GameObject raiz = new GameObject("Missao_01_Primeiros_Fios");
                MTMemoriaMissao missao = raiz.AddComponent<MTMemoriaMissao>();
                missao.jogador = jogador;
                MTMemoriaNPC npcNeto = neto.GetComponent<MTMemoriaNPC>();
                if (npcNeto == null) npcNeto = neto.gameObject.AddComponent<MTMemoriaNPC>();
                npcNeto.nome = "Neto"; npcNeto.papel = MTMemoriaPapel.Neto;
                PrefabUtility.RecordPrefabInstancePropertyModifications(npcNeto);
                missao.neto = npcNeto;
                missao.andre = CriarAndre(neto, sprite);
                missao.personagens = new[] { npcNeto, missao.andre };
                missao.painel = CriarInterface(camera, font);
                CriarDialogos(missao);
                missao.painel.MostrarObjetivo("Procure Neto e converse com ele.", "1 / 2 • Comece pela praça", npcNeto);
                EditorUtility.SetDirty(missao);
                EditorSceneManager.MarkSceneDirty(cena);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("Não foi possível salvar a missão.");
                Selection.activeGameObject = raiz;
                Debug.Log("Missão 01 instalada em " + destino
                    + ". Game em 16:9 > Play > clique no Game. WASD/setas; E/Enter conversa; Esc sai."
                    + " O progresso recomeça em cada sessão; ainda não há salvamento.");
                Validar();
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static void LocalizarBase(Scene cena, out MTPracaJogador jogador, out Transform neto, out Camera camera)
        {
            var jogadores = NaCena<MTPracaJogador>(cena);
            if (jogadores.Count != 1) throw new InvalidOperationException("A cena precisa de exatamente um MTPracaJogador. Abra a praça jogável.");
            jogador = jogadores[0];
            neto = null;
            foreach (MTPracaDemo demo in NaCena<MTPracaDemo>(cena))
                if (demo.neto != null) { neto = demo.neto; break; }
            if (neto == null)
                foreach (Transform t in NaCena<Transform>(cena))
                    if (t.name == "Neto" && t.GetComponent<SpriteRenderer>() != null) { neto = t; break; }
            if (neto == null) throw new InvalidOperationException("Neto não encontrado. Mantenha a referência no Tutorial_Da_Praca ou o nome Neto.");
            camera = null;
            foreach (Camera c in NaCena<Camera>(cena))
                if (c.CompareTag("MainCamera") && c.enabled) { camera = c; break; }
            if (camera == null || !camera.orthographic) throw new InvalidOperationException("A praça precisa de uma Main Camera ortográfica ativa.");
        }

        private static List<T> NaCena<T>(Scene cena) where T : Component
        {
            var lista = new List<T>();
            foreach (GameObject root in cena.GetRootGameObjects()) lista.AddRange(root.GetComponentsInChildren<T>(true));
            return lista;
        }

        private static void CorrigirAnimacao(MTPracaJogador jogador)
        {
            var dados = MTPracaMontador.Ler();
            var arte = Array.Find(dados.assets, a => a.id == "jogador_folha");
            if (arte == null) throw new InvalidDataException("Manifesto sem jogador_folha.");
            var anim = jogador.GetComponentInChildren<MTPracaAnimacao>(true);
            if (anim == null) throw new InvalidOperationException("MTPracaAnimacao ausente no jogador.");
            string png = "Assets/MemoriasTecidas/PracaV1/" + arte.file;
            var importer = AssetImporter.GetAtPath(png) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Folha do jogador ausente: " + png);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            anim.folha = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            anim.quadros = MTPracaMontador.ConverterQuadros(arte);
            anim.tamanhoReferencia = new Vector2Int(arte.width, arte.height);
            anim.pixelsPorUnidade = arte.ppu;
            anim.enabled = true;
            var sr = anim.GetComponent<SpriteRenderer>();
            sr.enabled = true;
            anim.Recarregar();
            PrefabUtility.RecordPrefabInstancePropertyModifications(anim);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
        }

        private static Sprite PrepararAndre()
        {
            string png = Raiz + "/Arte/andre_lucio.png";
            var imp = AssetImporter.GetAtPath(png) as TextureImporter;
            if (imp == null) throw new FileNotFoundException("Sprite ausente: " + png);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 1042.22f;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.npotScale = TextureImporterNPOTScale.None;
            var cfg = new TextureImporterSettings();
            imp.ReadTextureSettings(cfg);
            cfg.spriteAlignment = (int)SpriteAlignment.Custom;
            cfg.spritePivot = new Vector2(.5f, .04883f);
            cfg.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(cfg);
            imp.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
            if (sprite == null) throw new InvalidDataException("André não foi importado como Sprite.");
            return sprite;
        }

        private static MTMemoriaNPC CriarAndre(Transform neto, Sprite sprite)
        {
            var go = new GameObject("Andre_Lucio");
            go.transform.SetParent(neto.parent, false);
            go.transform.position = new Vector3(3.2f, .2f, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            SpriteRenderer modelo = neto.GetComponent<SpriteRenderer>();
            if (modelo != null) { sr.sharedMaterial = modelo.sharedMaterial; sr.sortingLayerID = modelo.sortingLayerID; }
            go.AddComponent<MTPracaOrdemY>();
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(.42f, .25f);
            col.offset = new Vector2(0f, .12f);
            BoxCollider2D colNeto = neto.GetComponent<BoxCollider2D>();
            if (colNeto != null) col.sharedMaterial = colNeto.sharedMaterial;
            Transform sombra = neto.Find("Sombra_Contato");
            if (sombra != null)
            {
                var copia = UnityEngine.Object.Instantiate(sombra.gameObject, go.transform, false);
                copia.name = "Sombra_Contato";
            }
            var npc = go.AddComponent<MTMemoriaNPC>();
            npc.nome = "André Lúcio"; npc.papel = MTMemoriaPapel.AndreLucio;
            return npc;
        }

        private static MTMemoriaPainel CriarInterface(Camera camera, Font font)
        {
            var canvasGO = new GameObject("Canvas_Memorias", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            // Canvas Overlay no nível principal da Hierarchy.
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            var ui = canvasGO.AddComponent<MTMemoriaPainel>();
            ui.cameraDoMapa = camera;
            ui.areaDaCamera = Rect("AreaDaCamera", canvasGO.transform, 0, 0, 1280, 720);
            ui.areaDaCamera.anchorMin = Vector2.zero; ui.areaDaCamera.anchorMax = Vector2.one;
            ui.areaDaCamera.offsetMin = ui.areaDaCamera.offsetMax = Vector2.zero;
            ui.conteudo = Rect("Conteudo_1280x720", ui.areaDaCamera, 0, 0, 1280, 720);
            ui.conteudo.anchorMin = ui.conteudo.anchorMax = ui.conteudo.pivot = new Vector2(.5f, .5f);
            ui.conteudo.anchoredPosition = Vector2.zero;

            RectTransform hud = Caixa("Objetivo", ui.conteudo, 28, 24, 604, 134);
            Texto("TituloMissao", hud, font, "PRIMEIROS FIOS DE MEMÓRIA", 22, Ouro, 20, 12, 564, 30, true);
            ui.objetivo = Texto("ObjetivoAtual", hud, font, "Procure Neto e converse com ele.", 23, Claro, 20, 50, 564, 36);
            ui.etapa = Texto("Etapa", hud, font, "1 / 2 • Comece pela praça", 18, Suave, 20, 96, 564, 26);
            RectTransform ajuda = Caixa("Controles", ui.conteudo, 894, 24, 358, 88);
            Texto("ControlesTexto", ajuda, font, "WASD / setas: caminhar\nE / Enter: conversar   •   R: reposicionar", 18, Claro, 16, 14, 326, 60);

            var marker = Caixa("Marcador_Objetivo", ui.conteudo, 0, 0, 290, 36);
            marker.anchorMin = marker.anchorMax = new Vector2(.5f, .5f);
            marker.pivot = new Vector2(.5f, 0f);
            ui.marcador = marker;
            ui.nomeMarcador = Texto("Nome", marker, font, "Neto | Objetivo", 17, Ouro, 8, 6, 274, 26, true);
            ui.nomeMarcador.alignment = TextAnchor.UpperCenter;

            var prompt = Caixa("Dica_Interacao", ui.conteudo, 340, 624, 600, 54);
            ui.caixaDica = prompt.gameObject;
            ui.dica = Texto("Texto", prompt, font, "", 23, Claro, 12, 12, 576, 34);
            ui.dica.alignment = TextAnchor.UpperCenter;
            var conversa = Caixa("Painel_Conversa", ui.conteudo, 28, 406, 1224, 290);
            ui.caixaConversa = conversa.gameObject;
            var linha = Rect("Linha_Ouro", conversa, 0, 0, 1224, 4);
            linha.gameObject.AddComponent<Image>().color = Ouro;
            ui.nome = Texto("Nome", conversa, font, "André Lúcio", 28, Ouro, 24, 18, 1176, 38, true);
            ui.contexto = Texto("Contexto", conversa, font, "Lenda • Adaptação para o jogo", 18, Suave, 24, 64, 1176, 28);
            ui.fala = Texto("Fala", conversa, font, "", 27, Claro, 24, 102, 1176, 116);
            ui.fala.resizeTextForBestFit = true; ui.fala.resizeTextMinSize = 22; ui.fala.resizeTextMaxSize = 27;
            ui.fonte = Texto("Fonte", conversa, font, "", 16, Suave, 24, 224, 1176, 26);
            ui.contador = Texto("Contador_e_Teclas", conversa, font, "", 18, Ouro, 24, 258, 1176, 26);
            var aviso = Caixa("Aviso_Progresso", ui.conteudo, 300, 176, 680, 56);
            ui.caixaAviso = aviso.gameObject;
            ui.aviso = Texto("Texto", aviso, font, "", 22, Ouro, 16, 14, 648, 34, true);
            ui.aviso.alignment = TextAnchor.UpperCenter;
            ui.ReiniciarVisual();
            return ui;
        }

        private static RectTransform Rect(string nome, Transform pai, float x, float y, float w, float h)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(pai, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static RectTransform Caixa(string nome, Transform pai, float x, float y, float w, float h)
        {
            RectTransform rt = Rect(nome, pai, x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>(); img.color = Fundo; img.raycastTarget = false;
            return rt;
        }

        private static Text Texto(string nome, Transform pai, Font font, string conteudo, int tamanho, Color cor,
            float x, float y, float w, float h, bool negrito = false)
        {
            Text t = Rect(nome, pai, x, y, w, h).gameObject.AddComponent<Text>();
            t.font = font; t.text = conteudo; t.fontSize = tamanho; t.color = cor;
            t.fontStyle = negrito ? FontStyle.Bold : FontStyle.Normal;
            t.alignment = TextAnchor.UpperLeft; t.supportRichText = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        private static MTMemoriaDialogo Dialogo(string arquivo, string nome, string contexto, string fonte, params string[] falas)
        {
            string caminho = Gerados + "/Dialogos/" + arquivo + ".asset";
            var existente = AssetDatabase.LoadAssetAtPath<MTMemoriaDialogo>(caminho);
            if (existente != null) return existente; // Preserva o texto que a equipe editar.
            if (File.Exists(caminho)) throw new InvalidDataException("Há um arquivo incompatível em " + caminho);
            var d = ScriptableObject.CreateInstance<MTMemoriaDialogo>();
            d.id = arquivo; d.personagem = nome; d.contexto = contexto; d.fonte = fonte; d.falas = falas;
            AssetDatabase.CreateAsset(d, caminho);
            return d;
        }

        private static void CriarDialogos(MTMemoriaMissao m)
        {
            const string ficcao = "Conversa de abertura • Roteiro ficcional do jogo";
            const string fonteFiccao = "Roteiro original de Memórias Tecidas.";
            m.netoInicio = Dialogo("N01_Neto_Inicio", "Neto", ficcao, fonteFiccao,
                "Bem-vindo! Sou Neto. Vamos conhecer a cidade por suas histórias, seus lugares e pelo trabalho de quem vive aqui.",
                "Cada pessoa guarda um fio dessa memória. Para começar, procure André Lúcio aqui na praça. Ele vai apresentar um causo: a lenda da Mulher-Cavalo.",
                "Ouça até o fim e repare em como uma história muda quando passa de uma pessoa para outra. Sua primeira missão é encontrar André e conhecer essa lenda.");
            m.netoLembrete = Dialogo("N02_Neto_Lembrete", "Neto", ficcao, fonteFiccao,
                "André Lúcio está aqui na praça, indicado pelo marcador. Aproxime-se dele e pressione E ou Enter para conversar.");
            m.netoFim = Dialogo("N03_Neto_Fim", "Neto", ficcao, fonteFiccao,
                "Você conheceu o primeiro causo da nossa jornada. As memórias também vivem nas vozes de quem conta e de quem escuta. Pode explorar a praça e ouvir a história outra vez.");
            m.andreAntes = Dialogo("A00_Andre_Antes", "André Lúcio", "Conversa de abertura • Fala ficcional para o jogo", fonteFiccao,
                "Chegue mais! Tenho uma história para contar. Antes, converse com Neto: ele vai apresentar o começo da sua jornada pela praça.");
            m.andreLenda = Dialogo("A01_Andre_Mulher_Cavalo", "André Lúcio", "Lenda • Adaptação para o jogo; não é uma transcrição de André Lúcio.",
                "Base: Pesquisas e ideia de jogo.pdf, páginas 5 e 8. Relato de tradição oral.",
                "Você já ouviu falar da Mulher-Cavalo? Numa versão desse causo, ela é descrita com corpo de gente e cabeça de cavalo.",
                "Uma das histórias fala de um jovem que teria sido perseguido depois de sair de um circo. Imagine o susto de atravessar o caminho pensando ter visto aquela figura!",
                "Outro relato menciona uma aparição perto da barragem do rio do Guilherme. Esse causo também chegou ao rádio, no programa Acordando com os Passarinhos.",
                "Esses causos circulam de boca em boca e chegam também ao rádio. Quem escuta pode lembrar de outros detalhes: uma mesma lenda pode ter várias versões.",
                "Guarde esta história como uma lenda da tradição oral. Conhecer quem conta, onde ouviu e qual versão conhece também faz parte de cuidar da memória da cidade.");
        }

        [MenuItem("Memorias Tecidas/Missao 01/2 - Validar cena aberta")]
        public static void Validar()
        {
            try
            {
                VerificarRegras();
                Scene cena = SceneManager.GetActiveScene();
                var missoes = NaCena<MTMemoriaMissao>(cena);
                if (missoes.Count != 1) throw new InvalidOperationException("Esperada uma Missão 01 na cena; encontradas " + missoes.Count + ".");
                MTMemoriaMissao m = missoes[0];
                if (m.jogador == null || m.painel == null || m.neto == null || m.andre == null)
                    throw new InvalidOperationException("Missão com referências vazias no Inspector.");
                if (m.personagens == null || Array.IndexOf(m.personagens, m.neto) < 0 || Array.IndexOf(m.personagens, m.andre) < 0)
                    throw new InvalidOperationException("A lista Personagens precisa conter Neto e André.");
                if (NaCena<MTPracaJogador>(cena).Count != 1) throw new InvalidOperationException("Existe mais de um controlador da praça.");
                foreach (MTPracaDemo d in NaCena<MTPracaDemo>(cena))
                    if (d.enabled) throw new InvalidOperationException("Desative MTPracaDemo na cena da missão.");
                if (m.painel.cameraDoMapa == null || m.painel.fala == null || m.painel.objetivo == null
                    || m.painel.caixaConversa == null || m.painel.marcador == null)
                    throw new InvalidOperationException("Interface com referências ausentes.");
                foreach (var d in new[] { m.netoInicio, m.netoLembrete, m.netoFim, m.andreAntes, m.andreLenda })
                {
                    if (d == null || !d.Valido) throw new InvalidOperationException("Diálogo ausente ou sem falas.");
                    foreach (string f in d.falas)
                    {
                        if (string.IsNullOrWhiteSpace(f)) throw new InvalidOperationException("Fala vazia em " + d.name);
                        if (f.Length > 260) Debug.LogWarning("Divida a fala longa em páginas menores: " + d.name, d);
                    }
                }
                foreach (var n in new[] { m.neto, m.andre })
                {
                    var sr = n.GetComponent<SpriteRenderer>();
                    if (sr == null || sr.sprite == null || !sr.enabled) throw new InvalidOperationException("Sprite ausente em " + n.nome);
                    if (!n.gameObject.activeInHierarchy) throw new InvalidOperationException("NPC inativo: " + n.nome);
                }
                Debug.Log("Missão 01: referências e regras de sequência verificadas. Faça o teste de interação no Play conforme o guia.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static void VerificarRegras()
        {
            var p = new MTMemoriaProgresso();
            Exigir(p.Etapa == MTMemoriaEtapa.FalarComNeto, "início");
            Exigir(!p.Concluir(MTMemoriaEncontro.RelatoAndre), "André antes de Neto");
            Exigir(!p.Concluir(MTMemoriaEncontro.SemAvanco), "conversa livre");
            Exigir(p.Concluir(MTMemoriaEncontro.ApresentacaoNeto) && p.Etapa == MTMemoriaEtapa.OuvirAndre, "fim de Neto");
            Exigir(!p.Concluir(MTMemoriaEncontro.ApresentacaoNeto), "repetição de Neto");
            Exigir(p.Concluir(MTMemoriaEncontro.RelatoAndre) && p.Etapa == MTMemoriaEtapa.Concluida, "fim de André");
            Exigir(!p.Concluir(MTMemoriaEncontro.RelatoAndre), "repetição de André");
            Exigir(!p.Concluir(MTMemoriaEncontro.ApresentacaoNeto) && p.Etapa == MTMemoriaEtapa.Concluida, "sem regressão");
        }

        private static void Exigir(bool condicao, string caso)
        {
            if (!condicao) throw new InvalidOperationException("Falha na regra de missão: " + caso);
        }
    }
}

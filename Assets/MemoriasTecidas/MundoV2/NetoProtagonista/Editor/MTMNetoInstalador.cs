using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    // Complemento para Mundo V2.1 + Vida V2.2.1, inclusive com Lugares V2.3.
    // Atua somente no jogador da cena ativa. Nao remonta regioes nem altera missoes.
    public static class MTMNetoInstalador
    {
        internal const string Raiz = "Assets/MemoriasTecidas/MundoV2/NetoProtagonista";
        private static readonly string[] Direcoes = { "S", "SW", "W", "NW", "N", "NE", "E", "SE" };

        [Serializable] private sealed class Quadro
        {
            public string nome, textura, direcao;
            public int passo;
            public float x, y, largura, altura, pivotX, pivotY, pixelsPorUnidade;
        }
        [Serializable] private sealed class Folha
        {
            public int versao;
            public float alturaBase, escalaCabeca, alongamentoCorpo;
            public Quadro[] quadros;
        }
        private sealed class Contexto
        {
            public MTMJogo jogo;
            public MTMVidaV22 vida;
            public Transform desenho;
            public SpriteRenderer renderer;
            public MTMAtorV22 ator;
            public MTMDeformacaoV22 deformacao;
            public MTMVisualV2 visual;
            public MTMNetoVisual neto;
        }

        [MenuItem("Memorias Tecidas/Neto/1 - Aplicar no protagonista (cena aberta)")]
        public static void Aplicar()
        {
            string backup = "";
            int grupo = -1;
            try
            {
                Contexto c = Encontrar();
                Folha folha = LerFolha();
                // Valida/prepara assets antes de tocar na cena.
                Sprite[] quadros = PrepararSprites(folha);
                Material pintura = MTMNetoMaterial.Preparar(false);
                Material sombra = MTMNetoMaterial.Preparar(true);
                backup = FazerBackup(c.jogo.gameObject.scene);

                Undo.IncrementCurrentGroup();
                grupo = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Aplicar Neto no protagonista");
                Undo.RegisterFullObjectHierarchyUndo(c.jogo.gameObject, "Aplicar Neto");

                foreach (var antigo in c.jogo.GetComponentsInChildren<MTMAnimacao>(true))
                {
                    antigo.enabled = false;
                    Registrar(antigo);
                }
                c.ator.enabled = true;
                c.ator.tipo = MTMAtorV22.Tipo.Jogador;
                c.ator.jogo = c.jogo;
                c.ator.vida = c.vida;
                c.ator.quadros = quadros;
                c.ator.cicloCompleto = true;
                c.ator.fpsCaminhada = 8f;
                c.ator.fpsCorrida = 12f;
                c.renderer.sprite = quadros[0];
                c.renderer.sharedMaterial = pintura;
                c.renderer.flipX = false;
                c.renderer.flipY = false;
                c.renderer.forceRenderingOff = false;
                c.desenho.localRotation = Quaternion.identity;
                c.desenho.localScale = Vector3.one;

                // V1.1: elimina o alongamento generico que deformava a regiao da barba/pescoco.
                c.deformacao.enabled = false;
                c.deformacao.tipo = MTMDeformacaoV22.Tipo.Pessoa;
                c.deformacao.vida = c.vida;
                c.deformacao.material = pintura;
                c.deformacao.escalaCabeca = 1f;
                c.deformacao.alongamentoCorpo = 1f;
                c.deformacao.fixarCorpo = true;
                c.deformacao.ventoNoCabelo = .003f;
                c.deformacao.respiracao = 0f;
                if (c.neto == null) c.neto = Undo.AddComponent<MTMNetoVisual>(c.desenho.gameObject);
                c.neto.enabled = true;
                c.neto.jogo = c.jogo; c.neto.vida = c.vida; c.neto.material = pintura;
                c.neto.escalaVisual = 1f;
                c.neto.inclinacaoCorridaGraus = 8f;
                c.neto.projecaoFrenteCostas = .35f;
                c.neto.tempoEntrar = .10f; c.neto.tempoVoltar = .14f;
                c.neto.ventoNoCabelo = .0025f;
                if (c.visual != null)
                {
                    c.visual.vento = 0f;
                    c.visual.respirar = false;
                    if (c.visual.sombra != null)
                    {
                        c.visual.sombra.sprite = quadros[0];
                        c.visual.sombra.sharedMaterial = sombra;
                        Registrar(c.visual.sombra);
                    }
                    Registrar(c.visual);
                }
                Registrar(c.ator); Registrar(c.renderer);
                Registrar(c.deformacao); Registrar(c.desenho); Registrar(c.neto);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(c.jogo.gameObject.scene);
                Undo.CollapseUndoOperations(grupo);
                Selection.activeGameObject = c.desenho.gameObject;
                Debug.Log("Neto 1.1 aplicado: corpo sem alongamento, rosto revisado e inclinacao na corrida. Salve com Ctrl+S e teste WASD/setas e Shift. " +
                    "Backup anterior: " + backup + ". Ctrl+Z desfaz a troca enquanto o historico de Undo estiver disponivel.", c.jogo);
            }
            catch (Exception e)
            {
                if (grupo >= 0) Undo.RevertAllDownToGroup(grupo);
                Debug.LogError("Nao foi possivel aplicar Neto: " + e.Message +
                    (backup.Length > 0 ? " Backup: " + backup : " A cena nao foi alterada."));
            }
        }

        [MenuItem("Memorias Tecidas/Neto/2 - Conferir protagonista")]
        public static void Conferir()
        {
            try
            {
                Contexto c = Encontrar();
                var erros = new List<string>();
                Folha folha = LerFolha();
                if (!c.ator.enabled || !c.ator.cicloCompleto || c.ator.tipo != MTMAtorV22.Tipo.Jogador)
                    erros.Add("Ator desativado, tipo incorreto ou ciclo completo desmarcado.");
                if (c.ator.quadros == null || c.ator.quadros.Length != 32)
                    erros.Add("O ator precisa dos 32 quadros de Neto.");
                else
                    for (int i = 0; i < 32; i++)
                    {
                        Sprite s = c.ator.quadros[i];
                        string esperado = "Neto_" + Direcoes[i / 4] + "_" + (i % 4).ToString("00");
                        if (s == null || s.name != esperado) erros.Add("Quadro " + i + ": esperado " + esperado);
                        else
                        {
                            Quadro q = folha.quadros[i];
                            if (s.rect != new Rect(q.x, q.y, q.largura, q.altura) ||
                                Mathf.Abs(s.pixelsPerUnit - q.pixelsPorUnidade) > .001f)
                                erros.Add("Quadro " + i + " pertence a outra revisao. Reaplique o passo 1.");
                        }
                    }
                if (c.deformacao.enabled)
                    erros.Add("Desative a deformacao generica de Neto; V1.1 usa MTMNetoVisual.");
                if (c.neto == null || !c.neto.enabled)
                    erros.Add("Falta MTMNetoVisual ativo: aplique o passo 1 desta revisao.");
                else
                {
                    if (c.neto.material == null || c.renderer.sharedMaterial != c.neto.material)
                        erros.Add("Desenho e MTMNetoVisual precisam do mesmo material Neto.");
                    if (c.neto.inclinacaoCorridaGraus <= 0f) erros.Add("A inclinacao da corrida esta em zero.");
                    if (c.neto.jogo != c.jogo || c.neto.vida != c.vida)
                        erros.Add("Confira Jogo e Vida no MTMNetoVisual.");
                }
                if (Mathf.Abs(c.desenho.localScale.x - c.desenho.localScale.y) > .001f)
                    erros.Add("Desenho esta esticado: mantenha Scale X/Y iguais e use Escala Visual.");
                if (c.renderer.sharedMaterial == null || c.renderer.sharedMaterial.shader == null ||
                    c.renderer.sharedMaterial.shader.name != MTMNetoMaterial.NomeShader)
                    erros.Add("Material de Neto ausente: execute o passo 1.");
                if (c.visual != null && (Mathf.Abs(c.visual.vento) > .0001f || c.visual.respirar))
                    erros.Add("O vento antigo ainda esta movimentando o corpo.");
                foreach (var antigo in c.jogo.GetComponentsInChildren<MTMAnimacao>(true))
                    if (antigo.enabled) erros.Add("Ha uma animacao antiga ativa junto do ator.");
                if (erros.Count > 0) Debug.LogWarning("Conferencia Neto:\n- " + string.Join("\n- ", erros), c.jogo);
                else Debug.Log("Neto 1.1: referencias, 32 quadros, oito direcoes e postura conferidos. " +
                    "Esta conferencia nao substitui o teste visual no Play.", c.jogo);
            }
            catch (Exception e) { Debug.LogError(e.Message); }
        }

        private static Contexto Encontrar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Saia do Play antes de aplicar ou conferir.");
            Scene cena = SceneManager.GetActiveScene();
            if (!cena.IsValid() || string.IsNullOrEmpty(cena.path))
                throw new InvalidOperationException("Abra sua cena jogavel e salve com Ctrl+S primeiro.");
            MTMJogo jogo = null;
            foreach (var candidato in UnityEngine.Object.FindObjectsByType<MTMJogo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidato.gameObject.scene != cena) continue;
                if (jogo != null) throw new InvalidOperationException("A cena ativa tem mais de um MTMJogo. Selecione a cena correta.");
                jogo = candidato;
            }
            if (jogo == null) throw new InvalidOperationException("Nenhum MTMJogo na cena ativa. Abra a cena do Mundo Conectado.");
            var c = new Contexto { jogo = jogo, vida = jogo.GetComponent<MTMVidaV22>(),
                desenho = jogo.transform.Find("Desenho"), visual = jogo.GetComponent<MTMVisualV2>() };
            if (c.vida == null || c.vida.versaoInstalada < 221)
                throw new InvalidOperationException("Esta atualizacao requer a Vida V2.2.1 aplicada na cena (versaoInstalada 221). Consulte LEIA_PRIMEIRO.");
            if (c.desenho == null) throw new InvalidOperationException("Nao foi encontrado o filho Desenho do jogador.");
            c.renderer = c.desenho.GetComponent<SpriteRenderer>();
            c.ator = c.desenho.GetComponent<MTMAtorV22>();
            c.deformacao = c.desenho.GetComponent<MTMDeformacaoV22>();
            c.neto = c.desenho.GetComponent<MTMNetoVisual>();
            if (c.renderer == null || c.ator == null || c.deformacao == null)
                throw new InvalidOperationException("Desenho precisa de SpriteRenderer, MTMAtorV22 e MTMDeformacaoV22. Instale a Vida V2.2.1 primeiro.");
            return c;
        }

        private static Folha LerFolha()
        {
            string path = Raiz + "/Arte/neto_recortes.json";
            if (!File.Exists(path)) throw new FileNotFoundException("Faltam os recortes. Copie a pasta NetoProtagonista completa.");
            Folha dados = JsonUtility.FromJson<Folha>(File.ReadAllText(path));
            if (dados == null || dados.quadros == null || dados.quadros.Length != 32)
                throw new InvalidDataException("neto_recortes.json deve ter exatamente 32 quadros.");
            if (dados.versao != 110)
                throw new InvalidDataException("Os recortes nao sao da revisao 1.1. Copie tambem a pasta Arte completa do ZIP atualizado.");
            for (int i = 0; i < dados.quadros.Length; i++)
            {
                Quadro q = dados.quadros[i];
                if (q == null || q.direcao != Direcoes[i / 4] || q.passo != i % 4 ||
                    q.nome != "Neto_" + Direcoes[i / 4] + "_" + (i % 4).ToString("00"))
                    throw new InvalidDataException("Ordem incorreta no quadro " + i + ". Ordem: S, SW, W, NW, N, NE, E, SE.");
                if ((q.textura != "neto_cardinais.png" && q.textura != "neto_diagonais.png") ||
                    q.x < 0 || q.y < 0 || q.largura <= 0 || q.altura <= 0 || q.pixelsPorUnidade <= 0 ||
                    q.pivotX < 0 || q.pivotX > 1 || q.pivotY < 0 || q.pivotY > 1)
                    throw new InvalidDataException("Recorte invalido: " + q.nome);
            }
            return dados;
        }

        private static Sprite[] PrepararSprites(Folha dados)
        {
            var texturas = new Dictionary<string, Texture2D>();
            foreach (Quadro q in dados.quadros)
            {
                if (texturas.ContainsKey(q.textura)) continue;
                string path = Raiz + "/Arte/" + q.textura;
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) throw new FileNotFoundException("Imagem nao importada: " + path);
                imp.textureType = TextureImporterType.Default;
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = true; imp.sRGBTexture = true;
                imp.mipmapEnabled = false; imp.npotScale = TextureImporterNPOTScale.None;
                imp.maxTextureSize = 4096; imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport();
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (t == null) throw new IOException("Textura nao carregada: " + path);
                texturas.Add(q.textura, t);
            }
            // Conteudo novo recebe novos assets; reaplicacao identica reutiliza os existentes.
            string assinatura;
            using (var hash = SHA256.Create())
            using (var stream = new MemoryStream())
            {
                foreach (string arquivo in new[] { "neto_recortes.json", "neto_cardinais.png", "neto_diagonais.png" })
                {
                    byte[] bytes = File.ReadAllBytes(Raiz + "/Arte/" + arquivo);
                    stream.Write(bytes, 0, bytes.Length);
                }
                assinatura = BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").Substring(0, 12);
            }
            string pasta = Raiz + "/Gerados/Sprites_" + assinatura;
            Directory.CreateDirectory(pasta); AssetDatabase.Refresh();
            var sprites = new Sprite[32];
            for (int i = 0; i < 32; i++)
            {
                Quadro q = dados.quadros[i]; Texture2D t = texturas[q.textura];
                if (q.x + q.largura > t.width || q.y + q.altura > t.height)
                    throw new InvalidDataException("Recorte ultrapassa a imagem: " + q.nome);
                string path = pasta + "/" + q.nome + ".asset";
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s == null)
                {
                    s = Sprite.Create(t, new Rect(q.x, q.y, q.largura, q.altura),
                        new Vector2(q.pivotX, q.pivotY), q.pixelsPorUnidade, 0, SpriteMeshType.FullRect, Vector4.zero, false);
                    s.name = q.nome; AssetDatabase.CreateAsset(s, path);
                }
                sprites[i] = s;
            }
            return sprites;
        }

        private static string FazerBackup(Scene cena)
        {
            const string pasta = "Assets/MemoriasTecidas/Backups_Neto";
            Directory.CreateDirectory(pasta); AssetDatabase.Refresh();
            string path = AssetDatabase.GenerateUniqueAssetPath(pasta + "/" +
                Path.GetFileNameWithoutExtension(cena.path) + "_antes_Neto_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
            if (!EditorSceneManager.SaveScene(cena, path, true)) throw new IOException("Nao foi possivel criar o backup da cena.");
            return path;
        }

        private static void Registrar(UnityEngine.Object objeto)
        {
            if (objeto == null) return;
            EditorUtility.SetDirty(objeto);
            if (PrefabUtility.IsPartOfPrefabInstance(objeto)) PrefabUtility.RecordPrefabInstancePropertyModifications(objeto);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMUrbanaMontagemV24
    {
        public static readonly string[] Alvos = { "entrada", "largo", "mercado", "prefeitura" };
        public static MTMJogo Jogo() => MTMMontagemLugaresV23.JogoDaCena();
        public static T Copia<T>(T v) => JsonUtility.FromJson<T>(JsonUtility.ToJson(v));

        [MenuItem("Memorias Tecidas/Expansao Urbana V2.4/2 - Montar lugares com backup")]
        public static void Montar()
        {
            string original = null, backup = null; bool iniciou = false;
            try
            {
                var jogo = Jogo(); var vida = jogo.GetComponent<MTMVidaV22>();
                if (vida == null || vida.versaoInstalada < 221 || jogo.GetComponent<MTMTravessiaV221>() == null)
                    throw new InvalidOperationException("Instale Vida V2.2.1 e sua correção de passagens primeiro.");
                if (string.IsNullOrEmpty(jogo.gameObject.scene.path)) throw new InvalidOperationException("Salve a cena com Ctrl+S antes de montar.");
                if (jogo.regioes == null || jogo.regioes.Any(r => r == null || r.dados == null)) throw new InvalidOperationException("Lista de regiões incompleta.");
                var todos = jogo.regioes.ToDictionary(r => r.dados.id);
                if (Alvos.All(id => todos.ContainsKey(id) && todos[id].GetComponent<MTMUrbanaV24>() != null))
                { Debug.Log("A expansão já está instalada. Menu 8: monumento da entrada V2.4.3. Menu 6: fotos da praça V2.4.1. Menu 7: apoio da torre V2.4.2. Menu 4: editar lugares."); return; }
                if (Alvos.Any(id => todos.ContainsKey(id) && todos[id].GetComponent<MTMUrbanaV24>() != null))
                    throw new InvalidOperationException("Instalação parcial detectada. Abra o backup anterior à V2.4 para repetir a montagem completa.");
                var pacote = MTMUrbanaArtesV24.Locais();
                foreach (var d in pacote.locais)
                {
                    if (d.id == "prefeitura")
                    { if (todos.ContainsKey(d.id)) throw new InvalidOperationException("Já existe uma região personalizada chamada prefeitura. Altere seu ID antes de instalar."); continue; }
                    if (!todos.TryGetValue(d.id, out var r)) throw new InvalidOperationException("Região necessária ausente: " + d.id);
                    if (r.transform.position.sqrMagnitude > .0001f || Quaternion.Angle(r.transform.rotation, Quaternion.identity) > .01f || (r.transform.lossyScale - Vector3.one).sqrMagnitude > .0001f)
                        throw new InvalidOperationException("A raiz da região " + d.id + " deve estar em posição zero, rotação zero e escala 1.");
                    foreach (var p in d.moverPontos)
                        if (r.pontos == null || !r.pontos.Any(x => x != null && x.dados != null && x.dados.id == p.id)) throw new InvalidOperationException("Interação original ausente: " + p.id);
                }
                MTMUrbanaArtesV24.Preparar();
                var cena = jogo.gameObject.scene;
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("A cena não pôde ser salva.");
                original = cena.path; Directory.CreateDirectory("Assets/MemoriasTecidas/Backups_V24"); AssetDatabase.Refresh();
                backup = "Assets/MemoriasTecidas/Backups_V24/" + Path.GetFileNameWithoutExtension(original) + "_antes_V24_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".unity";
                if (!EditorSceneManager.SaveScene(cena, backup, true)) throw new IOException("Falha ao criar backup. Nada foi montado.");
                iniciou = true;
                vida.jogo = jogo;
                var raizNova = Novo("prefeitura_Prefeitura_e_Praca", todos["mercado"].transform.parent);
                raizNova.SetActive(false); var nova = raizNova.AddComponent<MTMRegiao>();
                nova.dados = new MTMAreaDados { id = "prefeitura", bioma = "urbano", objetos = Array.Empty<MTMObjetoDados>(), pontos = Array.Empty<MTMPontoDados>() };
                nova.pontos = Array.Empty<MTMPonto>(); nova.aparicoes = Array.Empty<GameObject>();
                todos.Add("prefeitura", nova); jogo.regioes = jogo.regioes.Concat(new[] { nova }).ToArray();
                foreach (var d in pacote.locais) MontarLocal(todos[d.id], d, vida);
                // Atualiza somente as chegadas que levam aos quatro locais reconstruídos.
                foreach (var origem in jogo.regioes)
                {
                    bool mudou = false;
                    foreach (var p in origem.pontos ?? Array.Empty<MTMPonto>())
                    {
                        if (p == null || p.dados == null || p.dados.tipo != "saida" || !Alvos.Contains(p.dados.destino)) continue;
                        var destino = todos[p.dados.destino];
                        var volta = destino.pontos.FirstOrDefault(q => q.dados.tipo == "saida" && q.dados.destino == origem.dados.id);
                        Vector2 seguro = new Vector2(destino.dados.seguroX, destino.dados.seguroY), chegada = seguro;
                        if (volta != null)
                        {
                            Vector2 v = volta.transform.position, direcao = (seguro - v).normalized;
                            if (direcao.sqrMagnitude < .1f) direcao = Vector2.down;
                            chegada = v + direcao * 3.2f;
                        }
                        p.dados = Copia(p.dados); p.dados.chegadaX = chegada.x; p.dados.chegadaY = chegada.y;
                        p.dados.nome = destino.dados.nome; EditorUtility.SetDirty(p); mudou = true;
                    }
                    if (mudou) MTMMontagemLugaresV23.SincronizarPontos(origem);
                }
                // Miniaturas iniciais usam a arte do lugar; a captura opcional produz imagens da cena montada.
                var cards = new List<MTMVidaV22.Cartao>(vida.lugares ?? Array.Empty<MTMVidaV22.Cartao>());
                string[] imagens = { "letreiro", "fachadas_urbanas", "mercado", "prefeitura" };
                for (int i = 0; i < Alvos.Length; i++)
                {
                    cards.RemoveAll(c => c != null && c.regiao == Alvos[i]);
                    cards.Add(new MTMVidaV22.Cartao { regiao = Alvos[i], imagem = AssetDatabase.LoadAssetAtPath<Texture2D>(MTMUrbanaArtesV24.Raiz + "/Arte/" + imagens[i] + ".png") });
                }
                vida.lugares = cards.ToArray(); EditorUtility.SetDirty(vida); EditorUtility.SetDirty(jogo);
                MTMUrbanaValidacaoV24.Validar(jogo, true);
                foreach (var r in jogo.regioes) r.gameObject.SetActive(r.dados.id == "praca");
                MTMMonumentoEntradaV243.PrepararSombrasParaSalvar(jogo);
                EditorSceneManager.MarkSceneDirty(cena);
                if (!EditorSceneManager.SaveScene(cena)) throw new IOException("Falha ao salvar a expansão.");
                iniciou = false; Selection.activeGameObject = todos["mercado"].gameObject;
                Debug.Log("Expansão Urbana V2.4 instalada. Entrada, Largo e Mercado reconstruídos; Prefeitura e praça conectadas. Backup: " + backup + ". Abra o menu 4 para editar e o menu 3 para capturar as imagens do mapa.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (iniciou && !string.IsNullOrEmpty(original))
                { EditorSceneManager.OpenScene(original, OpenSceneMode.Single); Debug.LogWarning("A cena salva antes da montagem foi reaberta. Backup: " + backup); }
            }
            finally { EditorUtility.ClearProgressBar(); }
        }
        internal static void MontarLocal(MTMRegiao r, MTMUrbanaLocalV24 d, MTMVidaV22 vida, bool reposicionarAnimais = true)
        {
            var tag = r.GetComponent<MTMUrbanaV24>();
            if (tag != null && tag.cenarioAtual != null) PreservarInterativos(r, tag.cenarioAtual.transform);
            var filhos = r.transform.Cast<Transform>().ToArray();
            var arquivo = Novo(tag == null ? "Layout_Anterior_V24" : "Layout_Antes_Praca241", r.transform); arquivo.SetActive(false);
            foreach (var t in filhos)
            {
                bool preservar = t.GetComponentInChildren<MTMPonto>(true) != null || t.GetComponentInChildren<MTMAtorV22>(true) != null;
                if (r.aparicoes != null && r.aparicoes.Any(a => a != null && (a.transform == t || a.transform.IsChildOf(t)))) preservar = true;
                if (!preservar) t.SetParent(arquivo.transform, true);
            }
            r.dados = Copia(r.dados); r.dados.nome = d.nome; r.dados.descricao = d.descricao;
            r.dados.largura = d.largura; r.dados.altura = d.altura; r.dados.seguroX = d.seguroX; r.dados.seguroY = d.seguroY;
            r.dados.mapaX = d.mapaX; r.dados.mapaY = d.mapaY; r.dados.interior = false;
            r.dados.agua = Array.Empty<MTMVetor>(); r.dados.pisos = Array.Empty<MTMPisoDados>(); r.dados.luzes = Array.Empty<MTMLuzDados>();
            var cenario = Novo("Cenario_V24", r.transform);
            var piso = Novo("Pisos_e_Calcadas", cenario.transform);
            var construcoes = Novo("Construcoes", cenario.transform);
            var vegetacao = Novo("Vegetacao", cenario.transform);
            var mobiliario = Novo("Mobiliario", cenario.transform);
            var luzes = Novo("Iluminacao", cenario.transform);
            if (tag == null) tag = r.gameObject.AddComponent<MTMUrbanaV24>();
            tag.versao = d.id == "prefeitura" ? 241 : 240; tag.layoutAnterior = arquivo; tag.cenarioAtual = cenario; tag.referencia = d.descricao;
            var visual = r.GetComponent<MTMRegiaoVisualV23>(); if (visual == null) visual = r.gameObject.AddComponent<MTMRegiaoVisualV23>();
            visual.versao = tag.versao; visual.layoutAnterior = arquivo; visual.cenarioAtual = cenario; visual.referenciaVisual = d.descricao;
            var matPiso = AssetDatabase.LoadAssetAtPath<Material>(MTMArtesLugaresV23.Raiz + "/Gerados/Superficie_V23.mat");
            if (matPiso == null) throw new FileNotFoundException("Prepare os materiais de Lugares V2.3 primeiro.");
            foreach (var p in d.pisos)
            {
                var go = Novo(p.nome, piso.transform); go.transform.localPosition = new Vector3(p.x, p.y, 0);
                var s = go.AddComponent<MTMSuperficieV23>(); s.material = matPiso;
                s.textura = string.IsNullOrEmpty(p.textura) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(MTMArtesLugaresV23.Raiz + "/Arte/" + p.textura + ".png");
                if (!string.IsNullOrEmpty(p.textura) && s.textura == null) throw new FileNotFoundException(p.textura);
                s.tamanho = new Vector2(p.w, p.h); s.unidadesPorRepeticao = p.repeticao; s.ordem = p.ordem;
                s.cor = new Color(p.r, p.g, p.b, p.a); s.contorno = p.contorno == null ? null : p.contorno.Select(v => new Vector2(v.x, v.y)).ToArray(); s.Reconstruir();
            }
            var defs = MTMArtesLugaresV23.LerArtes().artes.Concat(MTMUrbanaArtesV24.Artes().artes).ToDictionary(a => a.id);
            foreach (var o in d.objetos)
            {
                var def = defs[o.id]; Transform pai = def.tipo == "construcao" ? construcoes.transform : def.tipo == "arvore" || def.tipo == "palmeira" ? vegetacao.transform : def.tipo == "poste" ? luzes.transform : mobiliario.transform;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(MTMUrbanaArtesV24.Prefab(o.id), pai);
                go.name = o.nome; go.transform.localPosition = new Vector3(o.x, o.y, 0); go.transform.localScale = Vector3.one * o.escala;
                foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) sr.flipX = o.flipX;
                MTMMontagemLugaresV23.LigarVida(go, vida);
            }
            var limites = Novo("Limites", cenario.transform);
            Parede(limites.transform, "Norte", 0, d.altura / 2 - .2f, d.largura, .4f);
            Parede(limites.transform, "Sul", 0, -d.altura / 2 + .2f, d.largura, .4f);
            Parede(limites.transform, "Oeste", -d.largura / 2 + .2f, 0, .4f, d.altura);
            Parede(limites.transform, "Leste", d.largura / 2 - .2f, 0, .4f, d.altura);
            foreach (var alvo in d.moverPontos)
            {
                var p = r.pontos.First(x => x.dados.id == alvo.id); p.dados = Copia(p.dados);
                MTMMontagemLugaresV23.MoverPonto(r, p, new Vector2(alvo.x, alvo.y));
            }
            var pontos = new List<MTMPonto>(r.pontos);
            foreach (var pd in d.novosPontos)
            {
                var existente = pontos.FirstOrDefault(p => p.dados.id == pd.id);
                if (existente != null)
                {
                    // Só descrições não ligadas às recompensas recebem texto coerente com o novo lugar.
                    existente.dados = Copia(pd); existente.transform.position = new Vector3(pd.x, pd.y, 0); continue;
                }
                var go = Novo("Interacao_" + pd.id, r.transform); go.transform.position = new Vector3(pd.x, pd.y, 0);
                var p = go.AddComponent<MTMPonto>(); p.dados = Copia(pd); pontos.Add(p);
            }
            r.pontos = pontos.ToArray();
            foreach (var p in r.pontos)
                if (p.dados.tipo == "saida")
                {
                    p.alcance = 2.2f; var m = p.GetComponent<MTMMarcadorSaidaV221>(); if (m == null) m = p.gameObject.AddComponent<MTMMarcadorSaidaV221>();
                    m.material = MTMUrbanaArtesV24.Tinta; m.vida = vida;
                }
            // Mantém animais já presentes numa faixa aberta, com percurso local curto.
            int animal = 0;
            foreach (var a in reposicionarAnimais ? r.GetComponentsInChildren<MTMAtorV22>(true) : Array.Empty<MTMAtorV22>())
            {
                if (a.tipo != MTMAtorV22.Tipo.Animal || a.transform.IsChildOf(arquivo.transform)) continue;
                var v = a.GetComponentInParent<MTMVisualV2>(); if (v == null) continue;
                Vector2 pos = new Vector2(-8 + animal++ * 16, -11);
                v.transform.position = pos; var cam = v.GetComponent<MTMCaminhanteV22>();
                if (cam != null) cam.pontos = new[] { pos, pos + Vector2.right * 2, pos, pos - Vector2.right * 2 }.Select(p => (Vector2)v.transform.parent.InverseTransformPoint(p)).ToArray();
            }
            MTMMontagemLugaresV23.SincronizarPontos(r); visual.InvalidarLuzes(); EditorUtility.SetDirty(r);
        }
        private static void PreservarInterativos(MTMRegiao regiao, Transform cenario)
        {
            // Objetos de jogo colocados dentro do cenário antigo saem dele antes do arquivamento.
            var manter = new HashSet<Transform>();
            foreach (var p in cenario.GetComponentsInChildren<MTMPonto>(true))
            {
                var v = p.GetComponentInParent<MTMVisualV2>(true);
                manter.Add(v != null && v.transform.IsChildOf(cenario) ? v.transform : p.transform);
            }
            foreach (var a in cenario.GetComponentsInChildren<MTMAtorV22>(true))
            {
                var v = a.GetComponentInParent<MTMVisualV2>(true);
                manter.Add(v != null && v.transform.IsChildOf(cenario) ? v.transform : a.transform);
            }
            foreach (var a in regiao.aparicoes ?? Array.Empty<GameObject>())
                if (a != null && a.transform.IsChildOf(cenario)) manter.Add(a.transform);
            foreach (var t in manter)
                if (!manter.Any(outro => outro != t && t.IsChildOf(outro))) t.SetParent(regiao.transform, true);
        }
        private static GameObject Novo(string nome, Transform pai) { var g = new GameObject(nome); g.transform.SetParent(pai, false); return g; }
        private static void Parede(Transform pai, string nome, float x, float y, float w, float h) { var g = Novo(nome, pai); g.transform.localPosition = new Vector3(x, y, 0); g.AddComponent<BoxCollider2D>().size = new Vector2(w, h); }
    }
}

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2
{
    public static class MTMOrganizacaoCena
    {
        public static MTMJogo Jogo()
        {
            var jogos = Componentes<MTMJogo>();
            if (jogos.Length != 1)
                throw new InvalidOperationException("Abra a cena Jaguaruana, com um único MTMJogo, fora do Play.");
            return jogos[0];
        }

        public static T[] Componentes<T>()
            where T : Component
        {
            var lista = new List<T>();
            foreach (var raiz in SceneManager.GetActiveScene().GetRootGameObjects())
                lista.AddRange(raiz.GetComponentsInChildren<T>(true));
            return lista.ToArray();
        }

        public static void ForaDoPlay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Saia do Play antes de editar a organização.");
        }

        public static void Alterado(UnityEngine.Object objeto)
        {
            EditorUtility.SetDirty(objeto);
            if (PrefabUtility.IsPartOfPrefabInstance(objeto))
                PrefabUtility.RecordPrefabInstancePropertyModifications(objeto);
        }

        private static void CenaAlterada()
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static void Focar(GameObject objeto)
        {
            Selection.activeGameObject = objeto;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        public static void Mostrar(MTMRegiao escolhida)
        {
            ForaDoPlay();
            var jogo = Jogo();
            if (escolhida == null || !Array.Exists(jogo.regioes, r => r == escolhida))
                throw new InvalidOperationException("A região não pertence ao jogo aberto.");
            foreach (var r in jogo.regioes)
                if (r != null)
                {
                    Undo.RecordObject(r.gameObject, "Visualizar região");
                    r.gameObject.SetActive(r == escolhida);
                    Alterado(r.gameObject);
                }

            CenaAlterada();
            Focar(escolhida.gameObject);
        }

        public static MTMPonto Ponto(string id)
        {
            var lista = Array.FindAll(Componentes<MTMPonto>(), p => p.dados != null && p.dados.id == id);
            if (lista.Length > 1)
                throw new InvalidOperationException("Interação duplicada: " + id);
            return lista.Length == 1 ? lista[0] : null;
        }

        public static MTMMarcoRoteiro Marco(string id)
        {
            var lista = Array.FindAll(Componentes<MTMMarcoRoteiro>(), m => m.idPonto == id);
            if (lista.Length > 1)
                throw new InvalidOperationException("Marco duplicado: " + id);
            return lista.Length == 1 ? lista[0] : null;
        }

        public static Transform RaizPersonagem(MTMPonto ponto)
        {
            var regiao = MTMVinculosRegiao.RegiaoAncestral(ponto.transform);
            if (regiao == null)
                throw new InvalidOperationException("A interação não está dentro de uma região.");
            var atores = Array.FindAll(regiao.GetComponentsInChildren<MTMAtorV22>(true), a => a.nomeDaConversa == ponto.dados.nome);
            Transform alvo = ponto.transform;
            if (atores.Length == 1)
            {
                var prefab = PrefabUtility.GetOutermostPrefabInstanceRoot(atores[0].gameObject);
                if (prefab != null)
                    alvo = prefab.transform;
                else
                    for (var t = atores[0].transform; t != null && t != regiao.transform; t = t.parent)
                        if (t.GetComponent<MTMVisualV2>() != null)
                        {
                            alvo = t;
                            break;
                        }

                if (alvo == ponto.transform && !ponto.transform.IsChildOf(atores[0].transform))
                    throw new InvalidOperationException("A arte está separada da interação. Confira a raiz do personagem antes de mover.");
            }
            else if (atores.Length > 1)
                throw new InvalidOperationException("Mais de uma arte usa o nome desta conversa. Renomeie ou retire a duplicata antes de mover.");
            else
            {
                var prefab = PrefabUtility.GetOutermostPrefabInstanceRoot(ponto.gameObject);
                if (prefab != null)
                    alvo = prefab.transform;
                else
                    for (var t = ponto.transform; t != null && t != regiao.transform; t = t.parent)
                        if (t.GetComponent<MTMVisualV2>() != null)
                        {
                            alvo = t;
                            break;
                        }
            }

            if (alvo.GetComponentInChildren<MTMRegiao>(true) != null || alvo.GetComponentInChildren<MTMJogo>(true) != null)
                throw new InvalidOperationException("A raiz contém a região inteira. A operação foi cancelada.");
            foreach (var outro in alvo.GetComponentsInChildren<MTMPonto>(true))
                if (outro != ponto)
                    throw new InvalidOperationException("A raiz contém mais de uma interação. Separe os personagens antes de mover.");
            return alvo;
        }

        public static void SelecionarPessoa(string id)
        {
            var ponto = Ponto(id);
            if (ponto != null)
            {
                var r = MTMVinculosRegiao.RegiaoAncestral(ponto.transform);
                Mostrar(r);
                Focar(RaizPersonagem(ponto).gameObject);
                return;
            }

            var marco = Marco(id);
            if (marco != null)
            {
                Mostrar(MTMVinculosRegiao.RegiaoAncestral(marco.transform));
                Focar(marco.gameObject);
                return;
            }

            throw new InvalidOperationException("Este personagem é criado no Play. Use Criar marcos do roteiro para editar sua posição na cena.");
        }

        public static void MoverPessoa(string id, MTMRegiao destino)
        {
            ForaDoPlay();
            if (destino == null)
                throw new InvalidOperationException("Escolha uma região de destino.");
            VerificarAcessoAoMover(id, destino.dados.id);
            var ponto = Ponto(id);
            var marco = Marco(id);
            Transform raiz = ponto != null ? RaizPersonagem(ponto) : marco != null ? marco.transform : null;
            if (raiz == null)
                throw new InvalidOperationException("Crie os marcos do roteiro antes de transferir este encontro.");
            if (MTMVinculosRegiao.RegiaoAncestral(raiz) == destino)
            {
                Mostrar(destino);
                Focar(raiz.gameObject);
                return;
            }

            var percursos = CapturarPercursos(raiz);
            Vector3 novaPosicao = destino.transform.TransformPoint(new Vector3(destino.dados.seguroX + 2, destino.dados.seguroY, 0));
            Vector3 deslocamento = novaPosicao - raiz.position;
            foreach (var caminho in percursos.Values)
                foreach (var pos in caminho)
                {
                    var local = destino.transform.InverseTransformPoint(pos + deslocamento);
                    if (Mathf.Abs(local.x) >= destino.dados.largura * .5f || Mathf.Abs(local.y) >= destino.dados.altura * .5f)
                        throw new InvalidOperationException("O percurso deste personagem não cabe no destino. Ajuste o percurso antes de transferir.");
                }

            int grupo = IniciarUndo("Transferir personagem e diálogo");
            try
            {
                if (ponto != null && ponto.transform != raiz && !ponto.transform.IsChildOf(raiz))
                {
                    Undo.SetTransformParent(ponto.transform, raiz, "Vincular interação");
                    Undo.RecordObject(ponto.transform, "Aproximar diálogo");
                    ponto.transform.position = raiz.position + Vector3.up * .35f;
                    Alterado(ponto.transform);
                }

                Undo.SetTransformParent(raiz, destino.transform, "Transferir personagem");
                Undo.RecordObject(raiz, "Posicionar personagem");
                raiz.position = novaPosicao;
                RestaurarPercursos(percursos, deslocamento);
                Alterado(raiz);
                Reindexar();
                Mostrar(destino);
                Focar(raiz.gameObject);
                Undo.CollapseUndoOperations(grupo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(grupo);
                throw;
            }
        }

        private static Dictionary<MTMCaminhanteV22, Vector3[]> CapturarPercursos(Transform raiz)
        {
            var resultado = new Dictionary<MTMCaminhanteV22, Vector3[]>();
            foreach (var caminhante in raiz.GetComponentsInChildren<MTMCaminhanteV22>(true))
            {
                if (caminhante.pontos == null)
                    continue;
                var pontos = new Vector3[caminhante.pontos.Length];
                for (int i = 0; i < pontos.Length; i++)
                    pontos[i] = caminhante.transform.parent == null ? (Vector3)caminhante.pontos[i] : caminhante.transform.parent.TransformPoint(caminhante.pontos[i]);
                resultado.Add(caminhante, pontos);
            }

            return resultado;
        }

        private static void RestaurarPercursos(Dictionary<MTMCaminhanteV22, Vector3[]> percursos, Vector3 deslocamento)
        {
            foreach (var par in percursos)
            {
                var caminhante = par.Key;
                Undo.RecordObject(caminhante, "Preservar percurso do personagem");
                var pontos = new Vector2[par.Value.Length];
                for (int i = 0; i < pontos.Length; i++)
                {
                    Vector3 mundo = par.Value[i] + deslocamento;
                    pontos[i] = caminhante.transform.parent == null ? (Vector2)mundo : (Vector2)caminhante.transform.parent.InverseTransformPoint(mundo);
                }

                caminhante.pontos = pontos;
                Alterado(caminhante);
            }
        }

        private static void VerificarAcessoAoMover(string id, string destino)
        {
            var trama = JsonUtility.FromJson<MTMTrama>(File.ReadAllText(MTMPainelProjeto.Caminho));
            var bloqueio = Array.Find(trama.bloqueios, b => b.regiao == destino);
            if (bloqueio == null)
                return;
            bool liberado = false;
            foreach (var missao in trama.missoes)
                foreach (var etapa in missao.etapas)
                {
                    if (etapa.ponto == id && !liberado)
                        throw new InvalidOperationException("Mover este encontro para " + destino + " bloquearia uma partida nova: essa região só abre depois desta etapa. Escolha uma região acessível ou ajuste o bloqueio no roteiro primeiro.");
                    if (etapa.eventos != null && Array.IndexOf(etapa.eventos, bloqueio.evento) >= 0)
                        liberado = true;
                }
        }

        private static int IniciarUndo(string titulo)
        {
            Undo.IncrementCurrentGroup();
            int grupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(titulo);
            return grupo;
        }

        public static void Reindexar()
        {
            ForaDoPlay();
            var jogo = Jogo();
            foreach (var r in jogo.regioes)
                if (r != null)
                    Undo.RecordObject(r, "Atualizar interações da região");
            MTMVinculosRegiao.Reindexar(jogo.regioes);
            foreach (var r in jogo.regioes)
                if (r != null)
                    Alterado(r);
            CenaAlterada();
        }

        private static Transform Pasta(Transform pai, string nome)
        {
            var existente = pai.Find(nome);
            if (existente != null)
                return existente;
            var go = new GameObject(nome);
            Undo.RegisterCreatedObjectUndo(go, "Criar grupo de organização");
            go.transform.SetParent(pai, false);
            return go.transform;
        }

        public static void CriarMarcos(MTMTrama trama)
        {
            ForaDoPlay();
            var jogo = Jogo();
            int grupo = IniciarUndo("Criar marcos de edição do roteiro");
            try
            {
                foreach (var def in trama.pontosNovos)
                {
                    if (Marco(def.id) != null || Ponto(def.id) != null)
                        continue;
                    var r = Array.Find(jogo.regioes, a => a != null && a.dados != null && a.dados.id == def.regiao);
                    if (r == null)
                        throw new InvalidOperationException("Região não encontrada: " + def.regiao);
                    var go = new GameObject(def.nome + " [roteiro]");
                    Undo.RegisterCreatedObjectUndo(go, "Criar marco do roteiro");
                    go.transform.SetParent(Pasta(r.transform, "04 · Marcos do roteiro"), false);
                    go.transform.position = r.transform.TransformPoint(new Vector3(def.x, def.y, 0));
                    var marco = Undo.AddComponent<MTMMarcoRoteiro>(go);
                    marco.idPonto = def.id;
                    Alterado(marco);
                }

                CenaAlterada();
                Undo.CollapseUndoOperations(grupo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(grupo);
                throw;
            }
        }

        public static int Organizar()
        {
            ForaDoPlay();
            var jogo = Jogo();
            int grupo = IniciarUndo("Organizar interações por região"), quantidade = 0;
            try
            {
                foreach (var r in jogo.regioes)
                {
                    if (r == null)
                        continue;
                    var movidos = new HashSet<Transform>();
                    foreach (var ponto in r.GetComponentsInChildren<MTMPonto>(true))
                    {
                        if (ponto.dados == null || MTMVinculosRegiao.RegiaoAncestral(ponto.transform) != r)
                            continue;
                        Transform raiz;
                        // Só agrupa raízes individuais verificadas; mantém objetos ambíguos como estão.
                        try
                        {
                            raiz = RaizPersonagem(ponto);
                        }
                        catch (InvalidOperationException)
                        {
                            continue;
                        }

                        if (!movidos.Add(raiz))
                            continue;
                        string nome = ponto.dados.tipo == "saida" ? "03 · Saídas" : ponto.dados.id.StartsWith("npc_", StringComparison.Ordinal) ? "01 · Personagens" : "02 · Interações";
                        if (raiz.parent != null && raiz.parent.parent == r.transform && raiz.parent.name == nome)
                            continue;
                        if (!ponto.transform.IsChildOf(raiz) && ponto.transform != raiz)
                            continue;
                        var percursos = CapturarPercursos(raiz);
                        Undo.SetTransformParent(raiz, Pasta(r.transform, nome), "Agrupar interação");
                        RestaurarPercursos(percursos, Vector3.zero);
                        Alterado(raiz);
                        quantidade++;
                    }
                }

                Reindexar();
                Undo.CollapseUndoOperations(grupo);
                return quantidade;
            }
            catch
            {
                Undo.RevertAllDownToGroup(grupo);
                throw;
            }
        }

        // ---------- cenário, limites, animais e nomes das regiões ----------

        private const string GrupoCenario = "05 · Cenário", GrupoAmbiente = "06 · Animais e ambiente";

        [MenuItem("Memorias Tecidas/Organizar cenário e nomes das regiões", false, 1)]
        private static void OrganizarCenarioMenu()
        {
            try
            {
                EditorUtility.DisplayDialog("Organizar cenário", OrganizarCenario(), "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Organizar cenário", "Nada foi alterado: " + e.Message, "OK");
            }
        }

        // Em cada região: o cenário solto vai para "05 · Cenário" (nos mesmos subgrupos das regiões
        // novas), animais e atmosfera para "06 · Animais e ambiente", os grupos numerados ficam em
        // ordem e a região recebe o nome "id_Nome". As posições no mundo não mudam.
        // Objetos com interação, marcos, aparições e moradores ficam onde estão.
        public static string OrganizarCenario()
        {
            ForaDoPlay();
            var jogo = Jogo();
            int grupo = IniciarUndo("Organizar cenário por região"), movidos = 0, renomeados = 0;
            var preservados = new List<string>();
            try
            {
                foreach (var r in jogo.regioes)
                {
                    if (r == null || r.dados == null || string.IsNullOrEmpty(r.dados.id))
                        continue;
                    // Barra no nome de objeto quebra caminhos como Transform.Find("a/b").
                    string nomeRegiao = r.dados.id + "_" + r.dados.nome.Replace(" / ", " - ").Replace('/', '-');
                    if (r.name != nomeRegiao)
                    {
                        Undo.RecordObject(r.gameObject, "Padronizar nome da região");
                        r.name = nomeRegiao;
                        Alterado(r.gameObject);
                        renomeados++;
                    }

                    var filhos = new List<Transform>();
                    foreach (Transform f in r.transform)
                        filhos.Add(f);
                    // As regiões novas já têm o grupo de cenário, com outro nome.
                    if (r.transform.Find(GrupoCenario) == null)
                        foreach (var f in filhos)
                            if (f.name.StartsWith("Cenario_V", StringComparison.Ordinal))
                            {
                                Undo.RecordObject(f.gameObject, "Padronizar grupo de cenário");
                                f.name = GrupoCenario;
                                Alterado(f.gameObject);
                                renomeados++;
                                break;
                            }

                    foreach (var f in filhos)
                    {
                        if (GrupoNumerado(f))
                            continue;
                        string destino = DestinoCenario(r, f);
                        if (destino == null)
                        {
                            preservados.Add(r.dados.id + " / " + f.name);
                            continue;
                        }

                        Transform pasta = destino == GrupoAmbiente ? Pasta(r.transform, GrupoAmbiente) : Pasta(Pasta(r.transform, GrupoCenario), destino);
                        var percursos = CapturarPercursos(f);
                        Undo.SetTransformParent(f, pasta, "Agrupar cenário");
                        RestaurarPercursos(percursos, Vector3.zero);
                        Alterado(f);
                        movidos++;
                    }

                    OrdenarGrupos(r.transform);
                }

                CenaAlterada();
                Undo.CollapseUndoOperations(grupo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(grupo);
                throw;
            }

            string resumo = movidos + " objetos agrupados e " + renomeados + " nomes padronizados. Ctrl+Z desfaz; Ctrl+S salva a cena.";
            if (preservados.Count > 0)
            {
                Debug.Log("Organizar cenário: objetos mantidos no lugar (têm interação, marco, aparição ou morador):\n" + string.Join("\n", preservados.ToArray()));
                resumo += "\n\n" + preservados.Count + " objetos com interação, aparição ou morador ficaram onde estavam (lista no Console).";
            }

            return resumo;
        }

        private static bool GrupoNumerado(Transform t)
        {
            string n = t.name;
            return n.Length > 5 && char.IsDigit(n[0]) && char.IsDigit(n[1]) && n.IndexOf(" · ", StringComparison.Ordinal) == 2;
        }

        private static bool ComecaCom(string nome, params string[] prefixos)
        {
            foreach (string p in prefixos)
                if (nome.StartsWith(p, StringComparison.Ordinal))
                    return true;
            return false;
        }

        // Subgrupo de destino; null mantém o objeto onde está.
        private static string DestinoCenario(MTMRegiao regiao, Transform t)
        {
            if (t.GetComponentInChildren<MTMPonto>(true) != null || t.GetComponentInChildren<MTMMarcoRoteiro>(true) != null)
                return null;
            if (regiao.aparicoes != null)
                foreach (var aparicao in regiao.aparicoes)
                    if (aparicao != null && (aparicao.transform == t || aparicao.transform.IsChildOf(t)))
                        return null;
            string n = t.name.ToLowerInvariant();
            var ator = t.GetComponentInChildren<MTMAtorV22>(true);
            if (ator != null)
                return ator.tipo == MTMAtorV22.Tipo.Animal ? GrupoAmbiente : null;
            if (ComecaCom(n, "cachorro", "gato", "atmosfera"))
                return GrupoAmbiente;
            if (t.GetComponentInChildren<ParticleSystem>(true) != null && t.GetComponentInChildren<SpriteRenderer>(true) == null && t.GetComponentInChildren<MeshRenderer>(true) == null)
                return GrupoAmbiente;
            if (ComecaCom(n, "limite"))
                return "Limites";
            if (ComecaCom(n, "chao", "piso", "rio_margem", "deque", "plantas_aquaticas"))
                return "Pisos_e_Agua";
            if (ComecaCom(n, "poste"))
                return "Iluminacao";
            if (ComecaCom(n, "arbusto", "caatinga", "carnauba", "arvore", "algodoeiro", "vasos"))
                return "Vegetacao";
            if (ComecaCom(n, "casa", "casario", "sobrado", "igreja", "fabrica", "mercado", "prefeitura", "coreto", "memorial", "oficina", "quiosque", "muro", "cruzeiro", "parede", "loja", "abrigo"))
                return "Construcoes";
            return "Mobiliario";
        }

        // Grupos numerados primeiro, em ordem; o resto mantém a ordem em que estava.
        private static void OrdenarGrupos(Transform regiao)
        {
            var grupos = new List<Transform>();
            var outros = new List<Transform>();
            foreach (Transform f in regiao)
                (GrupoNumerado(f) ? grupos : outros).Add(f);
            grupos.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            grupos.AddRange(outros);
            for (int i = 0; i < grupos.Count; i++)
                if (grupos[i].GetSiblingIndex() != i)
                    Undo.SetSiblingIndex(grupos[i], i, "Ordenar grupos da região");
        }

        public static List<string> Validar(MTMTrama trama)
        {
            var erros = new List<string>();
            MTMJogo jogo;
            try
            {
                jogo = Jogo();
            }
            catch (Exception e)
            {
                erros.Add(e.Message);
                return erros;
            }

            var ids = new HashSet<string>();
            var locais = new HashSet<string>();
            foreach (var r in jogo.regioes)
            {
                if (r == null || r.dados == null)
                {
                    erros.Add("Região vazia no jogo.");
                    continue;
                }

                if (!locais.Add(r.dados.id))
                    erros.Add("ID de região duplicado: " + r.dados.id);
            }

            foreach (var p in Componentes<MTMPonto>())
            {
                if (p.dados == null || string.IsNullOrEmpty(p.dados.id))
                {
                    erros.Add("Interação sem ID: " + p.name);
                    continue;
                }

                if (!ids.Add(p.dados.id))
                    erros.Add("Interação duplicada: " + p.dados.id);
                var r = MTMVinculosRegiao.RegiaoAncestral(p.transform);
                if (r == null || !Array.Exists(jogo.regioes, a => a == r))
                    erros.Add("Interação fora de uma região do jogo: " + p.dados.id);
                if (p.alcance <= 0)
                    erros.Add("Alcance inválido: " + p.dados.id);
            }

            foreach (var marco in Componentes<MTMMarcoRoteiro>())
            {
                var r = MTMVinculosRegiao.RegiaoAncestral(marco.transform);
                if (r == null || !Array.Exists(jogo.regioes, a => a == r))
                    erros.Add("Marco fora de uma região do jogo: " + marco.idPonto);
            }

            foreach (var p in trama.pontosNovos)
            {
                if (!ids.Add(p.id))
                    erros.Add("Ponto do roteiro duplicado ou já montado: " + p.id);
                if (!locais.Contains(p.regiao))
                    erros.Add("Região ausente: " + p.regiao);
            }

            foreach (var m in trama.missoes)
                foreach (var etapa in m.etapas)
                    if (!ids.Contains(etapa.ponto))
                        erros.Add("Alvo ausente: " + m.id + " / " + etapa.ponto);
            try
            {
                var copia = JsonUtility.FromJson<MTMTrama>(JsonUtility.ToJson(trama));
                var donos = new Dictionary<string, MTMRegiao>();
                foreach (var p in Componentes<MTMPonto>())
                {
                    var r = MTMVinculosRegiao.RegiaoAncestral(p.transform);
                    if (p.dados != null && !string.IsNullOrEmpty(p.dados.id) && r != null)
                        donos[p.dados.id] = r;
                }

                MTMVinculosRegiao.SincronizarRoteiro(copia, donos);
                MTMAutoriaRoteiro.AplicarMarcos(copia, jogo.regioes);
                var concluidos = new HashSet<string>();
                foreach (var missao in copia.missoes)
                    foreach (var etapa in missao.etapas)
                    {
                        var bloqueio = Array.Find(copia.bloqueios, b => b.regiao == etapa.regiao);
                        if (bloqueio != null && !concluidos.Contains(bloqueio.evento))
                            erros.Add("Partida nova sem acesso ao alvo: " + missao.id + " / " + etapa.ponto + " em " + etapa.regiao);
                        if (etapa.eventos != null)
                            foreach (var evento in etapa.eventos)
                                concluidos.Add(evento);
                    }
            }
            catch (Exception e)
            {
                erros.Add(e.Message);
            }

            return erros;
        }
    }

    [CustomEditor(typeof(MTMMarcoRoteiro))]
    public sealed class MTMMarcoRoteiroInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
                DrawDefaultInspector();
            EditorGUILayout.HelpBox("Arraste este objeto para posicionar o encontro. Para trocar de região, use o painel Organizar projeto. O conteúdo é criado no Play; este marco não tem arte nem colisão. Salve a cena.", MessageType.Info);
            if (GUILayout.Button("Abrir organização do projeto"))
                MTMPainelProjeto.Abrir();
        }

        private void OnSceneGUI()
        {
            var marco = (MTMMarcoRoteiro)target;
            Handles.Label(marco.transform.position + Vector3.up * 2, marco.name + "\n" + marco.idPonto);
        }
    }
}
#endif

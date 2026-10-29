#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMPainelProjeto : EditorWindow
    {
        public const string Caminho = "Assets/MemoriasTecidas/Resources/MemoriasTecidas/trama_01.json";
        private MTMTrama trama;
        private string original = "", busca = "", selecionado = "", destinoPessoa = "", mensagem = "";
        private bool pendente, houveEdicao;
        private readonly Dictionary<string, string> regioesCena = new Dictionary<string, string>();
        private int aba;
        private Vector2 listaScroll, detalheScroll;
        private readonly MTMIconesTecido icones = new MTMIconesTecido();
        private readonly HashSet<int> etapasAbertas = new HashSet<int>();
        private static string Chave
        {
            get
            {
                return "MTM_Painel_" + Application.dataPath;
            }
        }

        private readonly string[] abas =
        {
            "Visão geral",
            "Lugares",
            "Personagens",
            "Missões",
            "Materiais e objetos",
            "Encontros do roteiro"
        };
        [MenuItem("Memorias Tecidas/Organizar projeto", false, 0)]
        public static void Abrir()
        {
            var janela = GetWindow<MTMPainelProjeto>("Memórias Tecidas");
            janela.minSize = new Vector2(860, 600);
            janela.Show();
        }

        private void OnEnable()
        {
            try
            {
                original = File.ReadAllText(Caminho);
                string rascunho = SessionState.GetString(Chave + "_rascunho", "");
                if (!string.IsNullOrEmpty(rascunho) && SessionState.GetString(Chave + "_base", "") == original)
                {
                    trama = JsonUtility.FromJson<MTMTrama>(rascunho);
                    pendente = true;
                    mensagem = "Rascunho de edição restaurado desta sessão. Salve para aplicar ao jogo.";
                }
                else
                {
                    trama = JsonUtility.FromJson<MTMTrama>(original);
                    if (!string.IsNullOrEmpty(rascunho))
                        mensagem = "O JSON mudou fora do painel. O rascunho anterior continua guardado; use Exportar rascunho antes de editar.";
                }
            }
            catch (Exception e)
            {
                mensagem = e.Message;
                trama = null;
            }
        }

        private void OnDisable()
        {
            GuardarRascunho();
            icones.Dispose();
        }

        private void GuardarRascunho()
        {
            if (pendente && trama != null)
            {
                SessionState.SetString(Chave + "_rascunho", JsonUtility.ToJson(trama, true));
                SessionState.SetString(Chave + "_base", original);
            }
        }

        private void LimparRascunho()
        {
            SessionState.EraseString(Chave + "_rascunho");
            SessionState.EraseString(Chave + "_base");
        }

        private void Executar(Action acao)
        {
            try
            {
                acao();
            }
            catch (Exception e)
            {
                mensagem = e.Message;
                Debug.LogWarning("Memórias Tecidas: " + e.Message);
            }
        }

        private void OnGUI()
        {
            regioesCena.Clear();
            foreach (var p in MTMOrganizacaoCena.Componentes<MTMPonto>())
            {
                var r = MTMVinculosRegiao.RegiaoAncestral(p.transform);
                if (p.dados != null && !string.IsNullOrEmpty(p.dados.id) && r != null && r.dados != null)
                    regioesCena[p.dados.id] = r.dados.id;
            }

            foreach (var m in MTMOrganizacaoCena.Componentes<MTMMarcoRoteiro>())
            {
                var r = MTMVinculosRegiao.RegiaoAncestral(m.transform);
                if (!string.IsNullOrEmpty(m.idPonto) && r != null && r.dados != null)
                    regioesCena[m.idPonto] = r.dados.id;
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("MEMÓRIAS TECIDAS", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Lugares, pessoas e histórias conectados pelo mesmo fio.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUI.enabled = trama != null && pendente && !EditorApplication.isPlayingOrWillChangePlaymode;
                if (GUILayout.Button("Salvar conteúdo", EditorStyles.toolbarButton, GUILayout.Width(120)))
                    Executar(Salvar);
                GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
                if (GUILayout.Button("Recarregar", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    Executar(() =>
                    {
                        if (pendente && !EditorUtility.DisplayDialog("Descartar rascunho?", "As edições ainda não salvas serão descartadas.", "Descartar", "Continuar editando"))
                            return;
                        LimparRascunho();
                        pendente = false;
                        OnEnable();
                    });
                if (GUILayout.Button("Exportar rascunho", EditorStyles.toolbarButton, GUILayout.Width(135)))
                    Executar(ExportarRascunho);
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                GUILayout.Label(pendente ? "Edições ainda não salvas" : "Conteúdo carregado", EditorStyles.miniLabel);
            }

            if (!string.IsNullOrEmpty(mensagem))
                EditorGUILayout.HelpBox(mensagem, MessageType.Info);
            if (trama == null)
            {
                EditorGUILayout.HelpBox("Não foi possível ler o roteiro. Confira o arquivo " + Caminho, MessageType.Error);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorGUILayout.HelpBox("Saia do Play para editar. A partida usa uma cópia do roteiro carregada ao iniciar.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                int nova = GUILayout.Toolbar(aba, abas);
                if (nova != aba)
                {
                    aba = nova;
                    selecionado = "";
                    busca = "";
                    listaScroll = detalheScroll = Vector2.zero;
                    etapasAbertas.Clear();
                }

                if (aba == 0)
                {
                    Visao();
                    return;
                }

                busca = EditorGUILayout.TextField("Buscar nome ou ID", busca);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(255)))
                    {
                        listaScroll = EditorGUILayout.BeginScrollView(listaScroll);
                        if (aba == 1)
                            foreach (var x in trama.lugares)
                                Linha(x.id, x.nome, x.grupo);
                        if (aba == 2)
                            foreach (var x in trama.personagens)
                                Linha(x.id, x.nome, RegiaoEfetiva(x.id, x.regiao));
                        if (aba == 3)
                            foreach (var x in trama.missoes)
                                Linha(x.id, x.numero.ToString("00") + " · " + x.titulo, MTMAutoriaRoteiro.Capitulo(x));
                        if (aba == 4)
                            foreach (var x in trama.itens)
                                Linha(x.id, x.nome, x.categoria);
                        if (aba == 5)
                            foreach (var x in trama.pontosNovos)
                                Linha(x.id, x.nome, RegiaoEfetiva(x.id, x.regiao));
                        EditorGUILayout.EndScrollView();
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        detalheScroll = EditorGUILayout.BeginScrollView(detalheScroll);
                        houveEdicao = false;
                        if (aba == 1)
                            Lugar();
                        else if (aba == 2)
                            Pessoa();
                        else if (aba == 3)
                            Missao();
                        else if (aba == 4)
                            Item();
                        else
                            Encontro();
                        if (houveEdicao)
                        {
                            pendente = true;
                            GuardarRascunho();
                        }

                        EditorGUILayout.EndScrollView();
                    }
                }
            }
        }

        private void Linha(string id, string nome, string grupo)
        {
            if (!string.IsNullOrWhiteSpace(busca) && (nome + " " + id + " " + grupo).IndexOf(busca, StringComparison.OrdinalIgnoreCase) < 0)
                return;
            var estilo = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                fontStyle = selecionado == id ? FontStyle.Bold : FontStyle.Normal
            };
            Color antes = GUI.backgroundColor;
            if (selecionado == id)
                GUI.backgroundColor = new Color(.55f, .76f, .71f);
            if (GUILayout.Button(nome + "\n" + grupo, estilo, GUILayout.Height(56)))
            {
                selecionado = id;
                destinoPessoa = "";
                detalheScroll = Vector2.zero;
                etapasAbertas.Clear();
            }

            GUI.backgroundColor = antes;
        }

        private void Visao()
        {
            detalheScroll = EditorGUILayout.BeginScrollView(detalheScroll);
            EditorGUILayout.Space(14);
            int etapas = 0;
            foreach (var m in trama.missoes)
                etapas += m.etapas.Length;
            EditorGUILayout.LabelField(trama.lugares.Length + " lugares  ·  " + trama.personagens.Length + " personagens  ·  " + trama.missoes.Length + " missões  ·  " + etapas + " etapas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("O painel edita o roteiro do projeto atual. As posições dos personagens existentes ficam na cena. Os encontros criados durante o jogo podem ser posicionados por marcos de edição. Salve o conteúdo aqui e a cena com Ctrl+S.", MessageType.Info);
            EditorGUILayout.Space(8);
            if (GUILayout.Button("1. Conferir regiões, personagens e alvos das missões", GUILayout.Height(34)))
                Executar(() =>
                {
                    var erros = MTMOrganizacaoCena.Validar(trama);
                    mensagem = erros.Count == 0 ? "Vínculos conferidos. Não foram encontrados IDs duplicados, alvos ausentes ou marcos inválidos." : string.Join("\n", erros.ToArray());
                });
            if (GUILayout.Button("2. Organizar interações em grupos na Hierarchy", GUILayout.Height(34)))
                Executar(() =>
                {
                    int n = MTMOrganizacaoCena.Organizar();
                    mensagem = n + " raízes individuais agrupadas. Objetos ambíguos foram preservados. Ctrl+Z desfaz; Ctrl+S salva a cena.";
                });
            if (GUILayout.Button("3. Criar marcos de edição para os encontros do roteiro", GUILayout.Height(34)))
                Executar(() =>
                {
                    ExigirSalvo();
                    MTMOrganizacaoCena.CriarMarcos(trama);
                    mensagem = "Marcos criados sem duplicar os existentes. Abra Encontros do roteiro, selecione um e arraste no Scene. Ctrl+S salva.";
                });
            EditorGUILayout.Space(18);
            EditorGUILayout.LabelField("Organização da jornada", EditorStyles.boldLabel);
            string cap = "";
            foreach (var m in trama.missoes)
            {
                string c = MTMAutoriaRoteiro.Capitulo(m);
                if (c != cap)
                {
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField(c, EditorStyles.boldLabel);
                    cap = c;
                }

                EditorGUILayout.LabelField(m.numero.ToString("00") + " · " + m.titulo + (m.opcional ? "  (opcional)" : ""), EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.HelpBox("Os grupos são feitos dentro de cada região, mantendo coordenadas, prefabs e IDs. Os comandos podem ser desfeitos com Ctrl+Z. Não execute montadores antigos de cidade sobre a cena editada.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void Identidade(string id)
        {
            EditorGUILayout.LabelField("ID preservado", id);
            EditorGUILayout.Space(5);
        }

        private string Texto(string nome, string valor)
        {
            string novo = EditorGUILayout.TextField(nome, valor ?? "");
            if (novo != (valor ?? ""))
                houveEdicao = true;
            return novo;
        }

        private string Paragrafo(string nome, string valor)
        {
            EditorGUILayout.LabelField(nome);
            string novo = EditorGUILayout.TextArea(valor ?? "", new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.MinHeight(60));
            if (novo != (valor ?? ""))
                houveEdicao = true;
            return novo;
        }

        private void Lugar()
        {
            var l = Array.Find(trama.lugares, x => x.id == selecionado);
            if (l == null)
            {
                DicaSelecao();
                return;
            }

            Identidade(l.id);
            l.nome = Texto("Nome completo", l.nome);
            l.nomeCurto = Texto("Nome no mapa", l.nomeCurto);
            l.grupo = Texto("Grupo de lugares", l.grupo);
            l.descricao = Paragrafo("Papel deste lugar na jornada", l.descricao);
            l.ambiente = Paragrafo("Direção visual do ambiente", l.ambiente);
            if (GUILayout.Button("Mostrar somente este lugar na cena"))
                Executar(() =>
                {
                    var r = Array.Find(MTMOrganizacaoCena.Jogo().regioes, a => a != null && a.dados.id == l.id);
                    if (r == null)
                        throw new InvalidOperationException("Região ausente.");
                    MTMOrganizacaoCena.Mostrar(r);
                });
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Personagens associados", EditorStyles.boldLabel);
            foreach (var p in trama.personagens)
                if (RegiaoEfetiva(p.id, p.regiao) == l.id)
                    EditorGUILayout.LabelField(p.nome, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Missões que passam por aqui", EditorStyles.boldLabel);
            foreach (var m in trama.missoes)
                if (Array.Exists(m.etapas, e => RegiaoEfetiva(e.ponto, e.regiao) == l.id))
                    EditorGUILayout.LabelField(m.numero.ToString("00") + " · " + m.titulo, EditorStyles.wordWrappedLabel);
        }

        private void Pessoa()
        {
            var p = Array.Find(trama.personagens, x => x.id == selecionado);
            if (p == null)
            {
                DicaSelecao();
                return;
            }

            Identidade(p.id);
            // O nome da conversa liga a arte ao diálogo. Alterá-lo isoladamente quebra esse vínculo.
            EditorGUILayout.LabelField("Nome da conversa", p.nome);
            p.papel = Texto("Papel na história", p.papel);
            p.natureza = Texto("Natureza do encontro", p.natureza);
            p.descricao = Paragrafo("Descrição no caderno", p.descricao);
            Movimento(p.id, p.regiao);
            var dialogo = Array.Find(trama.dialogos, d => d.id == p.id);
            if (dialogo != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Falas livres, fora das etapas de missão", EditorStyles.boldLabel);
                Falas(dialogo.falas);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Participação nas missões", EditorStyles.boldLabel);
            foreach (var m in trama.missoes)
                if (Array.Exists(m.etapas, e => e.ponto == p.id))
                    EditorGUILayout.LabelField(m.numero.ToString("00") + " · " + m.titulo, EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox("Para mudar uma fala usada pela missão, abra Missões e edite a etapa correspondente. Mova a raiz completa do personagem, junto da interação.", MessageType.Info);
        }

        private void Movimento(string id, string padrao)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Região atual na cena", RegiaoEfetiva(id, padrao));
            if (GUILayout.Button("Selecionar personagem / marco completo"))
                Executar(() => MTMOrganizacaoCena.SelecionarPessoa(id));
            var nomes = Array.ConvertAll(trama.lugares, l => l.nomeCurto);
            int atual = Array.FindIndex(trama.lugares, l => l.id == (string.IsNullOrEmpty(destinoPessoa) ? RegiaoEfetiva(id, padrao) : destinoPessoa));
            int nova = EditorGUILayout.Popup("Levar para", Mathf.Max(0, atual), nomes);
            destinoPessoa = trama.lugares[nova].id;
            if (GUILayout.Button("Mover com a interação para este lugar"))
                Executar(() =>
                {
                    var destino = Array.Find(MTMOrganizacaoCena.Jogo().regioes, r => r != null && r.dados != null && r.dados.id == destinoPessoa);
                    MTMOrganizacaoCena.MoverPessoa(id, destino);
                    mensagem = "Encontro transferido. Ajuste a posição do objeto selecionado e salve a cena com Ctrl+S. As etapas acompanharão a região ao iniciar o Play.";
                });
        }

        private void Missao()
        {
            var m = Array.Find(trama.missoes, x => x.id == selecionado);
            if (m == null)
            {
                DicaSelecao();
                return;
            }

            Identidade(m.id);
            m.titulo = Texto("Título", m.titulo);
            m.capitulo = Texto("Capítulo", m.capitulo);
            m.descricao = Paragrafo("Descrição", m.descricao);
            EditorGUILayout.LabelField("Tipo", m.opcional ? "Opcional" : "Principal");
            EditorGUILayout.LabelField("Depende de", string.Join(", ", m.requerMissoes));
            EditorGUILayout.HelpBox("IDs, pré-requisitos, recompensas e condições permanecem protegidos aqui. O JSON continua sendo a fonte para mudanças avançadas nas regras.", MessageType.Info);
            for (int i = 0; i < m.etapas.Length; i++)
            {
                var e = m.etapas[i];
                bool aberta = EditorGUILayout.Foldout(etapasAbertas.Contains(i), (i + 1) + ". " + e.texto, true);
                if (aberta)
                    etapasAbertas.Add(i);
                else
                    etapasAbertas.Remove(i);
                if (!aberta)
                    continue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    e.texto = Texto("Objetivo", e.texto);
                    e.registro = Paragrafo("Registro no caderno", e.registro);
                    EditorGUILayout.LabelField("Alvo", e.ponto + " / " + RegiaoEfetiva(e.ponto, e.regiao));
                    EditorGUILayout.LabelField("Ação / horário", e.acao + " / " + e.horario);
                    EditorGUILayout.LabelField("Evento preservado", string.Join(", ", e.eventos));
                    EditorGUILayout.LabelField("Falas de início", EditorStyles.boldLabel);
                    Falas(e.falas);
                    EditorGUILayout.LabelField("Falas depois de concluir", EditorStyles.boldLabel);
                    Falas(e.falasConclusao);
                    if (GUILayout.Button("Localizar alvo na cena"))
                        Executar(() => MTMOrganizacaoCena.SelecionarPessoa(e.ponto));
                }
            }
        }

        private void Falas(string[] falas)
        {
            if (falas == null || falas.Length == 0)
            {
                EditorGUILayout.LabelField("Sem falas nesta seção.", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < falas.Length; i++)
                falas[i] = Paragrafo("Fala " + (i + 1), falas[i]);
        }

        private void Item()
        {
            var item = Array.Find(trama.itens, x => x.id == selecionado);
            if (item == null)
            {
                DicaSelecao();
                return;
            }

            Identidade(item.id);
            item.nome = Texto("Nome na bolsa", item.nome);
            EditorGUILayout.LabelField("Categoria", item.categoria);
            int index = Array.IndexOf(MTMIconesTecido.Nomes, item.icone);
            int novo = EditorGUILayout.Popup("Ícone desenhado", Mathf.Max(0, index), MTMIconesTecido.Nomes);
            if (novo != index)
                houveEdicao = true;
            item.icone = MTMIconesTecido.Nomes[novo];
            GUILayout.Label(icones.Obter(item.icone), GUILayout.Width(90), GUILayout.Height(90));
            item.descricao = Paragrafo("Descrição do material / objeto", item.descricao);
            item.origem = Texto("Origem", item.origem);
            item.dica = Paragrafo("Dica de obtenção", item.dica);
            int quantidade = MTMAutoriaRoteiro.Receita(item.id);
            EditorGUILayout.HelpBox(quantidade > 0 ? "Receita da rede: " + quantidade + " unidade(s). O ID deste material não deve ser alterado." : "Este objeto não é consumido na receita da rede.", MessageType.Info);
        }

        private void Encontro()
        {
            var p = Array.Find(trama.pontosNovos, x => x.id == selecionado);
            if (p == null)
            {
                DicaSelecao();
                return;
            }

            Identidade(p.id);
            p.nome = Texto("Nome do encontro", p.nome);
            EditorGUILayout.LabelField("Visual utilizado", p.visual);
            EditorGUILayout.LabelField("Modelo", p.modeloPonto);
            Movimento(p.id, p.regiao);
            EditorGUILayout.HelpBox("A posição vem do marco salvo na cena. Crie os marcos em Visão geral, selecione o encontro e use a ferramenta Mover (W). Os trajetos de caminhada/escolta acompanham o deslocamento; confira as colisões e os limites no Play.", MessageType.Info);
        }

        private string RegiaoEfetiva(string id, string padrao)
        {
            string regiao;
            return id != null && regioesCena.TryGetValue(id, out regiao) ? regiao : padrao;
        }

        private static void DicaSelecao()
        {
            EditorGUILayout.HelpBox("Selecione um registro na lista à esquerda.", MessageType.Info);
        }

        private void ExigirSalvo()
        {
            if (pendente)
                throw new InvalidOperationException("Salve o conteúdo antes de criar marcos, para usar as mesmas definições no Editor e no Play.");
        }

        private string PastaBackup()
        {
            string raiz = Directory.GetParent(Application.dataPath).FullName;
            string pasta = Path.Combine(Directory.GetParent(raiz).FullName, Path.GetFileName(raiz) + "_Backup_Conteudo");
            Directory.CreateDirectory(pasta);
            return pasta;
        }

        private void ExportarRascunho()
        {
            string txt = pendente ? JsonUtility.ToJson(trama, true) : SessionState.GetString(Chave + "_rascunho", "");
            if (string.IsNullOrEmpty(txt))
                throw new InvalidOperationException("Não há rascunho pendente.");
            string caminho = Path.Combine(PastaBackup(), "rascunho_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".json");
            File.WriteAllText(caminho, txt, new UTF8Encoding(false));
            mensagem = "Rascunho exportado: " + caminho;
        }

        private void Salvar()
        {
            MTMOrganizacaoCena.ForaDoPlay();
            if (File.ReadAllText(Caminho) != original)
                throw new InvalidOperationException("O JSON mudou fora deste painel. Exporte seu rascunho e recarregue antes de mesclar as edições.");
            foreach (var m in trama.missoes)
                if (string.IsNullOrWhiteSpace(m.titulo) || string.IsNullOrWhiteSpace(m.capitulo))
                    throw new InvalidOperationException("Preencha título e capítulo de " + m.id);
            foreach (var l in trama.lugares)
                if (string.IsNullOrWhiteSpace(l.nome) || string.IsNullOrWhiteSpace(l.nomeCurto))
                    throw new InvalidOperationException("Preencha os nomes de " + l.id);
            foreach (var i in trama.itens)
                if (string.IsNullOrWhiteSpace(i.nome) || Array.IndexOf(MTMIconesTecido.Nomes, i.icone) < 0)
                    throw new InvalidOperationException("Nome ou ícone inválido: " + i.id);
            string backup = Path.Combine(PastaBackup(), "trama_01_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".json");
            File.WriteAllText(backup, original, new UTF8Encoding(false));
            string novo = JsonUtility.ToJson(trama, true) + "\n";
            // Substituição atômica no mesmo volume. O backup externo é feito antes.
            string temporario = Caminho + ".tmp";
            try
            {
                File.WriteAllText(temporario, novo, new UTF8Encoding(false));
                File.Replace(temporario, Caminho, null);
            }
            finally
            {
                if (File.Exists(temporario))
                    File.Delete(temporario);
            }

            original = novo;
            pendente = false;
            LimparRascunho();
            AssetDatabase.ImportAsset(Caminho);
            mensagem = "Conteúdo salvo. Backup em " + backup + ". Salve também a cena se moveu personagens ou marcos.";
        }
    }
}
#endif

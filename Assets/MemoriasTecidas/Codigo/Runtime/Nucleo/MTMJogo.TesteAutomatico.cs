using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Teste automático do jogo, ligado só com "-testeAutomatico <pasta>" na linha de comando:
    // percorre as regiões e fotografa cada uma, abre as leituras da Casa das Memórias, confere se os
    // moradores andam (de perfil, virados para onde vão) e freiam perto do jogador e se os móveis novos
    // bloqueiam a passagem, e anota
    // qualquer erro. Não carrega nem grava o progresso e não mexe nas preferências. No fim fecha o
    // jogo (código 1 se algo falhou) e deixa relatorio.txt e as fotos na pasta pedida.
    // Com "-telaTeste <página> <arquivo.png>", só abre a tela inicial ("inicio") ou uma página dos menus
    // (sobre, creditos, ajuda...) sobre ela, salva uma imagem da tela e fecha. Atenção: ao fechar, o Windows guarda o tamanho
    // de janela usado (ex.: -screen-fullscreen 0 -screen-width 1280); exporte e reimporte a chave
    // HKCU\Software\Equipe Memórias Tecidas\Memórias Tecidas em volta da execução para não mudar o do jogador.
    public sealed partial class MTMJogo
    {
        private string pastaTeste;

        private void PrepararTesteAutomatico()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-testeAutomatico")
                    pastaTeste = args[i + 1];
                if (args[i] == "-telaTeste" && i + 2 < args.Length && args[i + 2].EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    carregarProgresso = false;
                    StartCoroutine(FotografarTelaTeste(args[i + 1], args[i + 2]));
                    return;
                }
            }

            if (string.IsNullOrEmpty(pastaTeste))
                return;
            carregarProgresso = false;
            Directory.CreateDirectory(pastaTeste);
            StartCoroutine(RodarTesteAutomatico());
        }

        private IEnumerator FotografarTelaTeste(string pagina, string arquivo)
        {
            // A foto não pode travar se outra janela ganhar o foco: sem isto o jogo pausa e nunca fecha.
            Application.runInBackground = true;
            float limite = Time.realtimeSinceStartup + 90f;
            while (!pronto && Time.realtimeSinceStartup < limite)
                yield return null;
            yield return new WaitForSecondsRealtime(1f);
            // "<página>:alternar": depois da foto, troca o modo de tela e volta (o log anota cada modo).
            bool alternar = pagina.EndsWith(":alternar", StringComparison.Ordinal);
            if (alternar)
                pagina = pagina.Substring(0, pagina.Length - ":alternar".Length);
            inicioAbertoUI = true;
            retornoOpcoesUI = "inicio";
            painel = pagina == "inicio" ? "" : pagina; // "inicio": a própria tela inicial
            // "diario:<aba>": o caderno numa aba; os encontros contam como vividos só nesta execução (nada é salvo).
            if (pagina.StartsWith("diario:", StringComparison.Ordinal))
            {
                painel = "diario";
                abaCaderno = pagina.Substring(7);
                foreach (var r in regioes)
                    if (r != null && r.pontos != null)
                        foreach (var p in r.pontos)
                            if (p != null && p.dados != null && !string.IsNullOrEmpty(p.dados.evento))
                                estado.Concluir(p.dados.evento);
                // As etapas das primeiras missões também, para a aba Memórias ter registros.
                if (tramaTecida != null && tramaTecida.missoes != null)
                    for (int m = 0; m < tramaTecida.missoes.Length && m < 4; m++)
                        if (tramaTecida.missoes[m].etapas != null)
                            foreach (var e in tramaTecida.missoes[m].etapas)
                                if (e.eventos != null)
                                    foreach (var ev in e.eventos)
                                        estado.Concluir(ev);
            }
            // "foto_viva:<id>": a atividade da foto antiga aberta no próprio lugar, já com uma diferença achada.
            if (pagina.StartsWith("foto_viva:", StringComparison.Ordinal))
            {
                string id = pagina.Substring("foto_viva:".Length);
                var f = memoriaViva != null && memoriaViva.fotos != null ? Array.Find(memoriaViva.fotos, x => x != null && x.id == id) : null;
                if (f != null)
                {
                    inicioAbertoUI = false;
                    jornadaIniciadaUI = true;
                    possuiJornadaUI = true;
                    painel = "";
                    estado.Concluir("visitou_" + f.regiao);
                    if (!string.IsNullOrEmpty(f.requer))
                        estado.Concluir(f.requer);
                    IrParaPontoSeguro(f.regiao);
                    yield return new WaitForSecondsRealtime(2f);
                    LimparPainelTeste();
                    var ponto = PontoTecido(f.regiao, PrefixoPontoVivo + f.id);
                    if (ponto != null)
                        Reposicionar((Vector2)ponto.transform.position + Vector2.down * .5f);
                    yield return new WaitForSecondsRealtime(1.5f);
                    AbrirFotoViva(f);
                    AcharDiferencaViva(0);
                }
            }

            // "entrevista_viva:<id>": a entrevista aberta no lugar, depois da primeira pergunta.
            if (pagina.StartsWith("entrevista_viva:", StringComparison.Ordinal))
            {
                string id = pagina.Substring("entrevista_viva:".Length);
                var e = memoriaViva != null && memoriaViva.entrevistas != null ? Array.Find(memoriaViva.entrevistas, x => x != null && x.id == id) : null;
                if (e != null)
                {
                    inicioAbertoUI = false;
                    jornadaIniciadaUI = true;
                    possuiJornadaUI = true;
                    painel = "";
                    estado.Concluir("visitou_" + e.regiao);
                    if (!string.IsNullOrEmpty(e.requer))
                        estado.Concluir(e.requer);
                    IrParaPontoSeguro(e.regiao);
                    yield return new WaitForSecondsRealtime(2f);
                    LimparPainelTeste();
                    var ponto = PontoTecido(e.regiao, PrefixoPontoVivo + e.id);
                    if (ponto != null)
                        Reposicionar((Vector2)ponto.transform.position + Vector2.down * .5f);
                    yield return new WaitForSecondsRealtime(1.5f);
                    AbrirEntrevistaViva(e);
                    for (int passo = 0; passo < 12 && falas != null && painel != "entrevista_viva"; passo++)
                    {
                        Avancar();
                        yield return null;
                    }

                    EscolherEntrevistaViva(0);
                }
            }

            // "jogo:<região>[:noite][:perto]": o jogo rodando na região, com o HUD na tela.
            // "conversa:<região>:<ponto>": Neto ao lado da pessoa, com a conversa aberta (câmera perto).
            // Só nesta execução: a distância da câmera não é gravada nas preferências.
            bool jogo = pagina.StartsWith("jogo:", StringComparison.Ordinal), conversa = pagina.StartsWith("conversa:", StringComparison.Ordinal);
            if (jogo || conversa)
            {
                string[] partes = pagina.Split(':');
                inicioAbertoUI = false;
                jornadaIniciadaUI = true;
                possuiJornadaUI = true;
                painel = "";
                estado.Concluir("visitou_" + partes[1]);
                IrParaPontoSeguro(partes[1]);
                estado.relogio = Array.IndexOf(partes, "noite") > 0 ? .9f : .42f;
                if (conversa || Array.IndexOf(partes, "perto") > 0)
                    zoomPreferido = .65f;
                yield return new WaitForSecondsRealtime(3f);
                LimparPainelTeste();
                var pessoa = conversa && partes.Length > 2 ? PontoTecido(partes[1], partes[2]) : null;
                if (pessoa != null)
                {
                    Reposicionar((Vector2)pessoa.transform.position + new Vector2(1.2f, -.6f));
                    yield return new WaitForSecondsRealtime(1.5f);
                    Interagir(pessoa);
                }

                yield return new WaitForSecondsRealtime(1.5f);
            }

            // "luta:<id>": a luta contra a lenda já começada, à noite, no lugar dela.
            if (pagina.StartsWith("luta:", StringComparison.Ordinal))
            {
                string id = pagina.Substring("luta:".Length);
                MTMConfrontoDados c = null;
                foreach (var par in confrontosPorPonto)
                    if (par.Value != null && par.Value.id == id)
                        c = par.Value;
                if (c != null)
                {
                    inicioAbertoUI = false;
                    jornadaIniciadaUI = true;
                    possuiJornadaUI = true;
                    painel = "";
                    estado.Concluir("visitou_" + c.regiao);
                    estado.Concluir(c.requer);
                    IrParaPontoSeguro(c.regiao);
                    estado.relogio = 20f / 24f;
                    yield return new WaitForSecondsRealtime(2f);
                    LimparPainelTeste();
                    var regiao = RegiaoTecido(c.regiao);
                    Reposicionar(regiao.transform.TransformPoint(new Vector3(c.x - 2.5f, c.y - 1f, 0f)));
                    IniciarConfronto(c);
                    yield return new WaitForSecondsRealtime(3f);
                }
            }

            LimparFocoUI();
            yield return new WaitForSecondsRealtime(1.5f);
            string caminho = Path.GetFullPath(arquivo);
            Directory.CreateDirectory(Path.GetDirectoryName(caminho));
            ScreenCapture.CaptureScreenshot(caminho);
            yield return new WaitForSecondsRealtime(1.5f);
            bool trocou = true;
            if (alternar)
            {
                var inicial = Screen.fullScreenMode;
                Debug.Log("Tela de teste: antes " + Screen.fullScreenMode + " " + Screen.width + "x" + Screen.height);
                AlternarTelaCheia();
                yield return new WaitForSecondsRealtime(2f);
                Debug.Log("Tela de teste: trocada " + Screen.fullScreenMode + " " + Screen.width + "x" + Screen.height);
                trocou = Screen.fullScreenMode != inicial;
                ScreenCapture.CaptureScreenshot(Path.ChangeExtension(caminho, null) + "_trocada.png");
                yield return new WaitForSecondsRealtime(1.5f);
                AlternarTelaCheia();
                yield return new WaitForSecondsRealtime(2f);
                Debug.Log("Tela de teste: de volta " + Screen.fullScreenMode + " " + Screen.width + "x" + Screen.height);
                trocou &= Screen.fullScreenMode == inicial;
            }

            Application.Quit(File.Exists(caminho) && trocou ? 0 : 1);
        }

        private IEnumerator RodarTesteAutomatico()
        {
            var relatorio = new StringBuilder();
            var erros = new List<string>();
            int falhas = 0;
            Application.logMessageReceived += (texto, pilha, tipo) =>
            {
                if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
                    erros.Add(tipo + ": " + texto + (string.IsNullOrEmpty(pilha) ? "" : "\n" + pilha.Trim()));
            };

            void Anotar(bool ok, string texto)
            {
                if (!ok)
                    falhas++;
                relatorio.AppendLine((ok ? "OK     " : "FALHA  ") + texto);
            }

            relatorio.AppendLine("Teste automático · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " · " + SystemInfo.graphicsDeviceName);
            float limite = Time.realtimeSinceStartup + 90f;
            while (!pronto && Time.realtimeSinceStartup < limite)
                yield return null;
            Anotar(pronto, "o jogo ficou pronto (história, kit HD2D e regiões carregados)");
            if (pronto)
            {
                StartCoroutine(ManterFocoNoTeste());
                // Vai direto para a jornada, sem abrir a abertura nem tocar nas preferências.
                inicioAbertoUI = false;
                jornadaIniciadaUI = true;
                possuiJornadaUI = true;
                LimparPainelTeste();
                yield return new WaitForSecondsRealtime(1f);

                foreach (var r in regioes)
                {
                    if (r == null || r.dados == null)
                        continue;
                    string id = r.dados.id;
                    int antes = erros.Count;
                    estado.Concluir("visitou_" + id); // libera a região só nesta execução (nada é salvo)
                    bool entrou = IrParaPontoSeguro(id);
                    yield return new WaitForSecondsRealtime(3f);
                    LimparPainelTeste();
                    float inicio = Time.realtimeSinceStartup;
                    int quadro = Time.frameCount;
                    yield return new WaitForSecondsRealtime(2f);
                    float ms = (Time.realtimeSinceStartup - inicio) * 1000f / Mathf.Max(1, Time.frameCount - quadro);
                    FotografarTeste(id);
                    Anotar(entrou && atual == r && erros.Count == antes, string.Format(CultureInfo.InvariantCulture,
                        "{0}: entrou e foi fotografada · {1:0.0} ms por quadro · {2} erro(s) novo(s)", id, ms, erros.Count - antes));
                }

                TestarMoradores(Anotar);
                TestarPontosDasPessoas(Anotar);
                TestarFalasSemBastidores(Anotar);
                yield return TestarConversaDePerto("mercado", "npc_feirante", Anotar);
                yield return TestarConversaDePerto("beira", "npc_v23_barraqueiro", Anotar);
                yield return TestarCasaDasMemorias(Anotar);
                // Os moradores que andam de um lado para o outro: Zeca, Seu Bento e Dona Cida.
                yield return TestarCaminhantes("rua", "Rua do Comércio", Anotar);
                yield return TestarCaminhantes("mercado", "Mercado", Anotar);
                yield return TestarCaminhantes("vila", "Vila", Anotar);
                yield return TestarArteDoRoteiro(Anotar);
                yield return TestarMusicas(Anotar);
                yield return TestarCasasDoRio(Anotar);
                TestarAbaFontes(Anotar); // por último: registra encontros na memória desta execução
                yield return TestarMemoriaViva(Anotar);
                // Por último: troca o estado da memória por uma Nova jornada e joga a história toda.
                yield return TestarHistoriaCompleta(Anotar);
            }

            relatorio.AppendLine();
            relatorio.AppendLine("Falhas: " + falhas + " · erros e exceções no log: " + erros.Count);
            foreach (var e in erros)
                relatorio.AppendLine("  " + e.Replace("\n", "\n    "));
            File.WriteAllText(Path.Combine(pastaTeste, "relatorio.txt"), relatorio.ToString(), new UTF8Encoding(true));
            MTMEntrada.MovimentoDeTeste = null;
            Application.Quit(falhas > 0 || erros.Count > 0 ? 1 : 0);
        }

        private IEnumerator TestarCasaDasMemorias(Action<bool, string> anotar)
        {
            estado.Concluir("visitou_memorias");
            if (!IrParaPontoSeguro("memorias"))
            {
                anotar(false, "Casa das Memórias: não foi possível entrar");
                yield break;
            }

            yield return new WaitForSecondsRealtime(2f);
            LimparPainelTeste();
            foreach (var id in new[] { "memorias_galeria", "memorias_moldura", "memorias_escolas", "memorias_cheia1985" })
            {
                MTMPonto p = atual.pontos == null ? null : Array.Find(atual.pontos, x => x != null && x.dados != null && x.dados.id == id);
                anotar(p != null, "Casa das Memórias: a leitura " + id + " existe");
                if (p == null)
                    continue;
                Reposicionar((Vector2)p.transform.position + Vector2.down * .6f);
                yield return new WaitForSecondsRealtime(.6f);
                var maisPerto = MaisProximo();
                anotar(maisPerto == p, "  chegando perto, " + id + " é o que o [E] pega (pegou: " + (maisPerto != null ? maisPerto.dados.id : "nenhum") + ")");
                Interagir(p);
                anotar(falas != null && falas.Length > 0, "  " + id + " abre a leitura (" + (falas != null ? falas.Length : 0) + " falas)");
                if (falas != null)
                {
                    PrepararFalaLegivelUI();
                    anotar(retratoExibidoDialogo != null, "  " + id + " mostra imagem na conversa (" + (retratoExibidoDialogo != null ? retratoExibidoDialogo.name : "nenhuma") + ")");
                }

                LimparPainelTeste();
                yield return null;
            }

            // Os móveis novos bloqueiam a passagem: empurra o Neto para cima, contra a mesa de redação.
            Transform mesa = null;
            foreach (var t in atual.GetComponentsInChildren<Transform>(true))
                if (t.name == "Mesa de redação")
                    mesa = t;
            anotar(mesa != null, "Casa das Memórias: a mesa de redação está na sala");
            if (mesa == null)
                yield break;
            Vector2 partida = (Vector2)mesa.position + new Vector2(-.6f, -.9f);
            Reposicionar(partida);
            yield return new WaitForSecondsRealtime(.3f);
            MTMEntrada.MovimentoDeTeste = Vector2.up;
            yield return new WaitForSecondsRealtime(1.5f);
            MTMEntrada.MovimentoDeTeste = null;
            float subiu = corpo.position.y - partida.y;
            anotar(subiu > .1f && corpo.position.y < mesa.position.y + .05f, string.Format(CultureInfo.InvariantCulture,
                "  a mesa barra o Neto (andou {0:0.00} para cima e parou em y={1:0.00}; a mesa começa em y={2:0.00})", subiu, corpo.position.y, mesa.position.y));
            FotografarTeste("memorias_mesa");
        }

        // Moradores com a arte revisada ficam nas proporções do desenho: a malha não estica o corpo nem
        // diminui a cabeça (o ajuste da arte antiga os deixava finos e tortos).
        private void TestarMoradores(Action<bool, string> anotar)
        {
            int n = 0;
            var ajustados = new List<string>();
            foreach (var r in regioes)
            {
                if (r == null)
                    continue;
                foreach (var ator in r.GetComponentsInChildren<MTMAtorV22>(true))
                {
                    var sr = ator.GetComponent<SpriteRenderer>();
                    var malha = ator.GetComponent<MTMDeformacaoV22>();
                    if (ator.tipo != MTMAtorV22.Tipo.Morador || sr == null || sr.sprite == null || !sr.sprite.name.StartsWith("HD2D_", StringComparison.Ordinal) ||
                        malha == null || malha.tipo != MTMDeformacaoV22.Tipo.Pessoa)
                        continue;
                    n++;
                    if (malha.escalaCabeca != 1f || malha.alongamentoCorpo != 1f)
                        ajustados.Add(ator.transform.parent != null ? ator.transform.parent.name : ator.name);
                }
            }

            anotar(n > 0 && ajustados.Count == 0, string.Format(CultureInfo.InvariantCulture,
                "moradores com a arte revisada nas proporções do desenho: {0} de {1}{2}", n - ajustados.Count, n,
                ajustados.Count > 0 ? " (ainda esticados: " + string.Join(", ", ajustados) + ")" : ""));
        }

        // O ponto de conversa de cada pessoa fica em cima dela, então o balão "E Conversar" aparece sobre a
        // pessoa (o do Seu Bento ficava 1,6 à esquerda e o balão aparecia sobre o Neto).
        private void TestarPontosDasPessoas(Action<bool, string> anotar)
        {
            int n = 0;
            var fora = new List<string>();
            foreach (var r in regioes)
            {
                if (r == null)
                    continue;
                var atores = r.GetComponentsInChildren<MTMAtorV22>(true);
                foreach (var p in r.GetComponentsInChildren<MTMPonto>(true))
                {
                    if (p.dados == null || string.IsNullOrEmpty(p.dados.nome))
                        continue;
                    var ator = Array.Find(atores, a => a.tipo == MTMAtorV22.Tipo.Morador && a.nomeDaConversa == p.dados.nome);
                    if (ator == null)
                        continue;
                    n++;
                    float dx = p.transform.position.x - ator.transform.position.x;
                    if (Mathf.Abs(dx) > .3f)
                        fora.Add(string.Format(CultureInfo.InvariantCulture, "{0} ({1:+0.00;-0.00})", p.dados.nome, dx));
                }
            }

            anotar(n > 0 && fora.Count == 0, "pontos de conversa em cima da própria pessoa: " + (n - fora.Count) + " de " + n +
                (fora.Count > 0 ? " (fora: " + string.Join(", ", fora) + ")" : ""));
        }

        // As falas ficam dentro da história: quem conversa não diz que foi "criado para o jogo" nem fala da
        // "versão" do jogo. O aviso de ficção mora na fonte de cada encontro, no caderno.
        private static readonly System.Text.RegularExpressions.Regex Bastidores = new System.Text.RegularExpressions.Regex(
            @"nesta vers[aã]o|criad[oa]s? para (?:o|este) jogo|fict[ií]ci[oa] desta aventura|(?:foi|foram) inspirad|adaptad[oa] ao jogo|s[aã]o decorativ|\(fic[cç][aã]o\)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private void TestarFalasSemBastidores(Action<bool, string> anotar)
        {
            int falasVistas = 0;
            var achadas = new List<string>();
            foreach (var r in regioes)
            {
                if (r == null)
                    continue;
                foreach (var p in r.GetComponentsInChildren<MTMPonto>(true))
                {
                    if (p.dados == null)
                        continue;
                    if (!string.IsNullOrEmpty(p.dados.nome) && Bastidores.IsMatch(p.dados.nome))
                        achadas.Add(p.dados.id + " (nome)");
                    if (p.dados.falas == null)
                        continue;
                    foreach (var f in p.dados.falas)
                    {
                        falasVistas++;
                        if (!string.IsNullOrEmpty(f) && Bastidores.IsMatch(f))
                            achadas.Add(p.dados.id);
                    }
                }
            }

            anotar(falasVistas > 0 && achadas.Count == 0, "falas dentro da história (sem \"criado para o jogo\" nem \"(ficção)\"): " + falasVistas + " falas" +
                (achadas.Count > 0 ? " · fora: " + string.Join(", ", achadas) : ""));
        }

        // Memória Viva: cada foto antiga tem seu ponto no lugar certo, a imagem carrega, o [E] pega o ponto e
        // achar o mínimo de diferenças registra a foto; as escutas também carregam. Registros só nesta execução.
        private IEnumerator TestarMemoriaViva(Action<bool, string> anotar)
        {
            int fotos = memoriaViva != null && memoriaViva.fotos != null ? memoriaViva.fotos.Length : 0;
            anotar(memoriaViva != null && fotos > 0, "Memória Viva ligada: " + fotos + " foto(s) antiga(s), " + sonsVivos.Count + " escuta(s)");
            if (memoriaViva == null || fotos == 0)
                yield break;
            anotar(sonsVivos.Count == 3, "  escutas carregadas: " + sonsVivos.Count + " de 3");
            foreach (var f in memoriaViva.fotos)
            {
                var ponto = PontoTecido(f.regiao, PrefixoPontoVivo + f.id);
                var textura = Resources.Load<Texture2D>(f.imagem);
                anotar(ponto != null && textura != null, string.Format(CultureInfo.InvariantCulture, "  {0}: ponto em {1} {2}, imagem {3}",
                    f.nome, f.regiao, ponto != null ? "ok" : "AUSENTE", textura != null ? textura.width + "x" + textura.height : "AUSENTE"));
                if (ponto == null || textura == null)
                    continue;
                estado.Concluir("visitou_" + f.regiao);
                if (!string.IsNullOrEmpty(f.requer))
                    estado.Concluir(f.requer);
                if (!IrParaPontoSeguro(f.regiao))
                {
                    anotar(false, "  " + f.regiao + ": não foi possível entrar");
                    continue;
                }

                yield return new WaitForSecondsRealtime(1f);
                LimparPainelTeste();
                Reposicionar((Vector2)ponto.transform.position + Vector2.down * .5f);
                yield return new WaitForSecondsRealtime(.5f);
                var pegou = MaisProximo();
                anotar(pegou == ponto, "  " + f.nome + ": chegando perto, o [E] pega " + (pegou != null && pegou.dados != null ? pegou.dados.id : "nenhum"));
                AbrirFotoViva(f);
                bool abriu = painel == "foto_viva" && fotoTextura != null;
                for (int i = 0; i < f.minimo; i++)
                    AcharDiferencaViva(i);
                anotar(abriu && estado.Tem(f.evento), "  " + f.nome + ": abre a foto e registra com " + f.minimo + " diferença(s)");
                LimparPainelTeste();
                yield return null;
            }

            // Entrevistas: chega perto, conversa até o fim buscando o que a pessoa guarda (final dourado).
            if (memoriaViva.entrevistas != null)
                foreach (var e in memoriaViva.entrevistas)
                {
                    var ponto = PontoTecido(e.regiao, PrefixoPontoVivo + e.id);
                    anotar(ponto != null, "  " + e.titulo + ": ponto em " + e.regiao + (ponto != null ? " ok" : " AUSENTE"));
                    if (ponto == null)
                        continue;
                    estado.Concluir("visitou_" + e.regiao);
                    if (!string.IsNullOrEmpty(e.requer))
                        estado.Concluir(e.requer);
                    if (!IrParaPontoSeguro(e.regiao))
                    {
                        anotar(false, "  " + e.regiao + ": não foi possível entrar");
                        continue;
                    }

                    yield return new WaitForSecondsRealtime(1f);
                    LimparPainelTeste();
                    Reposicionar((Vector2)ponto.transform.position + Vector2.down * .5f);
                    yield return new WaitForSecondsRealtime(.5f);
                    var pegou = MaisProximo();
                    anotar(pegou == ponto, "  " + e.titulo + ": chegando perto, o [E] pega " + (pegou != null && pegou.dados != null ? pegou.dados.id : "nenhum"));
                    AbrirEntrevistaViva(e);
                    // A entrevista começa com a conversa de abertura; as perguntas vêm quando ela termina.
                    for (int passo = 0; passo < 12 && falas != null && painel != "entrevista_viva"; passo++)
                    {
                        Avancar();
                        yield return null;
                    }

                    bool abriu = painel == "entrevista_viva" && sessaoViva != null;
                    for (int volta = 0; abriu && volta < 8 && !sessaoViva.Encerrada; volta++)
                    {
                        var opcoes = OpcoesVivas();
                        int escolha = opcoes.FindIndex(p => p != null && p.id == "guardado");
                        if (escolha < 0)
                            escolha = opcoes.FindIndex(p => p != null && p != e.silencio);
                        quadroDaAcao = -1;
                        EscolherEntrevistaViva(Mathf.Max(0, escolha));
                    }

                    if (abriu)
                        ConcluirEntrevistaViva();
                    bool dourado = estado.Tem(e.evento) && estado.Tem(e.evento + "_f0");
                    anotar(abriu && dourado, "  " + e.titulo + ": conversa até o fim e guarda o fio dourado" + (falas != null && falas.Length > 0 ? " (\"" + falas[0] + "\")" : ""));
                    LimparPainelTeste();
                    yield return null;
                }
        }

        // Aba Fontes do caderno: depois de conversar, a fonte do encontro aparece na lista. Usa encontros com fonte
        // própria (uma pessoa fictícia e uma real); os registros valem só nesta execução.
        private void TestarAbaFontes(Action<bool, string> anotar)
        {
            int antes = FontesDosEncontros().Count;
            var esperados = new List<string>();
            foreach (var r in regioes)
                if (r != null && r.pontos != null)
                    foreach (var p in r.pontos)
                        if (p != null && p.dados != null && (p.dados.id == "npc_v23_barraqueiro" || p.dados.id == "npc_ivonete") && !string.IsNullOrEmpty(p.dados.evento))
                        {
                            estado.Concluir(p.dados.evento);
                            esperados.Add(p.dados.id);
                        }

            var lista = FontesDosEncontros();
            bool todos = esperados.Count == 2 && esperados.TrueForAll(id => lista.Exists(f => f.id == id && !string.IsNullOrEmpty(f.fonte)));
            int semFonte = 0;
            foreach (var r in regioes)
                if (r != null && r.pontos != null)
                    foreach (var p in r.pontos)
                        if (p != null && p.dados != null && p.dados.tipo == "conversa" && p.dados.falas != null && p.dados.falas.Length > 0 && string.IsNullOrEmpty(p.dados.fonte))
                            semFonte++;
            anotar(todos, string.Format(CultureInfo.InvariantCulture, "aba Fontes: {0} encontro(s) antes, {1} depois de conversar com Tião e Ivonete", antes, lista.Count));
            anotar(semFonte == 0, "conversas e observações com texto têm fonte: " + (semFonte == 0 ? "todas" : semFonte + " sem fonte"));
        }

        // Chegando à pessoa pela esquerda, pela direita ou por baixo, o [E] pega a conversa com ela
        // (Seu Bento e Tião tinham o ponto de conversa longe do desenho).
        private IEnumerator TestarConversaDePerto(string regiao, string idPonto, Action<bool, string> anotar)
        {
            estado.Concluir("visitou_" + regiao);
            if (!IrParaPontoSeguro(regiao))
            {
                anotar(false, regiao + ": não foi possível entrar");
                yield break;
            }

            yield return new WaitForSecondsRealtime(1.5f);
            LimparPainelTeste();
            MTMPonto ponto = null;
            foreach (var p in atual.GetComponentsInChildren<MTMPonto>(true))
                if (p.dados != null && p.dados.id == idPonto)
                    ponto = p;
            MTMAtorV22 pessoa = null;
            if (ponto != null)
                foreach (var a in atual.GetComponentsInChildren<MTMAtorV22>(true))
                    if (a.tipo == MTMAtorV22.Tipo.Morador && a.nomeDaConversa == ponto.dados.nome)
                        pessoa = a;
            string nome = ponto != null ? ponto.dados.nome : idPonto;
            anotar(ponto != null && pessoa != null, regiao + ": " + nome + " e o ponto de conversa estão na cena");
            if (ponto == null || pessoa == null)
                yield break;
            Vector2 pe = pessoa.transform.position;
            foreach (var lado in new[] { Vector2.left, Vector2.right, Vector2.down })
            {
                Reposicionar(pe + lado * 2.4f);
                yield return new WaitForSecondsRealtime(.3f);
                MTMEntrada.MovimentoDeTeste = -lado;
                float fim = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < fim && Vector2.Distance(corpo.position, pe) > .9f)
                    yield return null;
                MTMEntrada.MovimentoDeTeste = null;
                yield return new WaitForSecondsRealtime(.3f);
                var pegou = MaisProximo();
                string nomeLado = lado.x < 0f ? "pela esquerda" : lado.x > 0f ? "pela direita" : "por baixo";
                anotar(pegou == ponto, string.Format(CultureInfo.InvariantCulture, "  {0} {1}: o [E] pega {2} (Neto a {3:0.00} do desenho)",
                    nome, nomeLado, pegou != null && pegou.dados != null ? pegou.dados.id : "nenhum", Vector2.Distance(corpo.position, pe)));
            }

            FotografarTeste(regiao + "_" + idPonto);
        }

        private IEnumerator TestarCaminhantes(string regiao, string nomeRegiao, Action<bool, string> anotar)
        {
            estado.Concluir("visitou_" + regiao);
            if (!IrParaPontoSeguro(regiao))
            {
                anotar(false, nomeRegiao + ": não foi possível entrar");
                yield break;
            }

            yield return new WaitForSecondsRealtime(2f);
            LimparPainelTeste();
            int pessoas = 0, fotos = 0;
            foreach (var c in atual.GetComponentsInChildren<MTMCaminhanteV22>())
                if (!c.animal)
                    pessoas++;
            anotar(pessoas > 0, nomeRegiao + ": " + pessoas + " morador(es) andando pela região");
            foreach (var c in atual.GetComponentsInChildren<MTMCaminhanteV22>())
            {
                if (c.animal)
                    continue;
                string nome = c.transform.parent != null ? c.transform.parent.name + "/" + c.name : c.name;
                var ator = c.GetComponentInChildren<MTMAtorV22>();
                var desenho = ator != null ? ator.GetComponent<SpriteRenderer>() : null;
                // Sem a arte de perfil, o teste usa cópias da pose de frente para conferir a troca e o espelhamento.
                bool provisorio = ator != null && (ator.quadrosLado == null || ator.quadrosLado.Length == 0);
                if (provisorio)
                    ator.quadrosLado = PosesProvisorias(ator);
                var ladosVistos = new HashSet<Sprite>();
                int viradas = 0, viradasErradas = 0;
                // Longe do Neto, o morador anda o seu caminho.
                if (((Vector2)c.transform.position - corpo.position).magnitude < 4f)
                    Reposicionar(corpo.position + new Vector2(0f, -4f));
                float percorrido = 0f;
                Vector3 anterior = c.transform.position;
                float fim = Time.realtimeSinceStartup + 9f;
                while (Time.realtimeSinceStartup < fim)
                {
                    percorrido += Vector3.Distance(c.transform.position, anterior);
                    anterior = c.transform.position;
                    yield return null;
                    if (desenho == null || ator.quadrosLado == null || ator.RapidezReal <= 0f)
                        continue;
                    if (Array.IndexOf(ator.quadrosLado, desenho.sprite) >= 0)
                        ladosVistos.Add(desenho.sprite);
                    if (Mathf.Abs(ator.VelocidadeReal.x) > .05f)
                    {
                        viradas++;
                        if (desenho.flipX != ator.VelocidadeReal.x < 0f)
                            viradasErradas++;
                    }
                }

                anotar(percorrido > .3f, string.Format(CultureInfo.InvariantCulture, "{0}: {1} andou {2:0.00} unidades em 9 s", nomeRegiao, nome, percorrido));
                if (desenho != null)
                    anotar(ladosVistos.Count >= 2 && viradas > 0 && viradasErradas == 0, string.Format(CultureInfo.InvariantCulture,
                        "  {0} anda de perfil ({1}): {2} pose(s) de lado, virado para onde vai em {3} de {4} quadros",
                        nome, provisorio ? "poses provisórias" : "arte de perfil", ladosVistos.Count, viradas - viradasErradas, viradas));
                // Com o Neto ao lado, ele freia e fica parado.
                Reposicionar((Vector2)c.transform.position + new Vector2(1.3f, -.4f));
                yield return new WaitForSecondsRealtime(1.5f);
                Vector3 parado = c.transform.position;
                yield return new WaitForSecondsRealtime(1.5f);
                float mexeu = Vector3.Distance(parado, c.transform.position);
                anotar(mexeu < .02f, string.Format(CultureInfo.InvariantCulture, "  {0} para com o Neto por perto (mexeu {1:0.000})", nome, mexeu));
                if (desenho != null)
                    anotar((ator.quadrosLado == null || Array.IndexOf(ator.quadrosLado, desenho.sprite) < 0) && !desenho.flipX,
                        "  " + nome + " parado volta à pose de frente, sem espelhar");
                FotografarTeste(regiao + "_caminhante_" + (++fotos));
                if (provisorio)
                    ator.quadrosLado = null;
            }
        }

        // Joga a história inteira a partir de uma Nova jornada (só na memória): para cada etapa, na ordem em que
        // o jogo as oferece, acerta o horário, viaja pela regra real de liberação das regiões, confere se o ponto
        // está visível e disponível e interage com ele como o E faz (falas, atividade com a resposta certa,
        // escolta acompanhando o grupo, aparição). Só a luta da lenda é pulada (as lutas têm teste próprio).
        // Se uma etapa travar, anota o motivo e a conclui à força para o resto da história ser testado.
        private IEnumerator TestarHistoriaCompleta(Action<bool, string> anotar)
        {
            LimparPainelTeste();
            NovaJornada();
            yield return new WaitForSecondsRealtime(1f);
            LimparPainelTeste();
            int etapas = 0;
            var problemas = new List<string>();
            float limite = Time.realtimeSinceStartup + 900f;
            while (Time.realtimeSinceStartup < limite)
            {
                MTMMissaoTecido m;
                MTMEtapaTecido e;
                if (!ProximaEtapaHistoria(out m, out e))
                    break;
                string nome = m.id + "." + (Array.IndexOf(m.etapas, e) + 1) + " (" + e.texto + ")";
                estado.relogio = e.horario == "noite" ? .9f : e.horario == "entardecer" ? .72f : .45f;
                if (atual == null || atual.dados.id != e.regiao)
                {
                    if (!IrParaPontoSeguro(e.regiao))
                    {
                        problemas.Add(nome + ": a região '" + e.regiao + "' continua bloqueada");
                        ForcarEtapaTeste(e);
                        continue;
                    }

                    yield return new WaitForSecondsRealtime(.4f);
                }

                LimparPainelTeste();
                AtualizarVisibilidadeRoteiro();
                yield return null;
                var p = PontoTecido(e.regiao, e.ponto);
                if (p == null || !p.gameObject.activeInHierarchy || !DisponivelRoteiro(p))
                {
                    problemas.Add(nome + ": o ponto '" + e.ponto + "' " + (p == null ? "não existe" : "não está visível ou disponível"));
                    ForcarEtapaTeste(e);
                    continue;
                }

                Reposicionar((Vector2)p.transform.position + new Vector2(0f, -1.1f));
                yield return null;
                string primeiraFala = "";
                if (e.acao == "confronto")
                    ConcluirEtapaRoteiro(m, e, null, false);
                else if (e.acao != "aproximar")
                {
                    Interagir(p);
                    primeiraFala = falas != null && falas.Length > 0 ? falas[0] : "";
                }

                float fim = Time.realtimeSinceStartup + (e.acao == "escolta" ? 120f : 15f);
                while (!MTMTrama.Concluida(estado, e) && Time.realtimeSinceStartup < fim)
                {
                    yield return null;
                    if (falas != null)
                        Avancar();
                    else if (painel == "atividade" && atividadeRoteiro != null)
                    {
                        respostaAtividade.Clear();
                        if (atividadeRoteiro.resposta != null)
                            respostaAtividade.AddRange(atividadeRoteiro.resposta);
                        if (!ConferirAtividadeRoteiro(atividadeRoteiro))
                        {
                            primeiraFala = "atividade: " + retornoAtividade;
                            break;
                        }
                    }
                    else if (etapaEscolta == e)
                    {
                        var grupo = PontoTecido(e.regiao, e.ponto);
                        if (grupo != null && Vector2.Distance(corpo.position, grupo.transform.position) > 2.5f)
                            Reposicionar((Vector2)grupo.transform.position + new Vector2(0f, -1.1f));
                    }
                }

                for (int k = 0; k < 30 && falas != null; k++)
                {
                    yield return null;
                    Avancar();
                }

                if (!MTMTrama.Concluida(estado, e))
                {
                    problemas.Add(nome + ": não concluiu" + (primeiraFala.Length > 0 ? " (\"" + primeiraFala + "\")" : ""));
                    LimparAtividadeRoteiro();
                    etapaEscolta = null;
                    etapaAparicao = null;
                    ForcarEtapaTeste(e);
                }

                LimparPainelTeste();
                etapas++;
            }

            int concluidas = 0;
            foreach (var missao in tramaTecida.missoes)
                if (MTMTrama.Concluida(estado, missao))
                    concluidas++;
            anotar(problemas.Count == 0 && concluidas == tramaTecida.missoes.Length, string.Format(CultureInfo.InvariantCulture,
                "história inteira jogada a partir de uma nova jornada: {0} etapas, {1} de {2} missões concluídas, {3} problema(s)",
                etapas, concluidas, tramaTecida.missoes.Length, problemas.Count));
            foreach (var problema in problemas)
                anotar(false, "  " + problema);
            TestarSalvarCarregar(anotar);
        }

        // A próxima etapa que o jogo oferece: missões principais antes das opcionais, na ordem do roteiro.
        private bool ProximaEtapaHistoria(out MTMMissaoTecido missao, out MTMEtapaTecido etapa)
        {
            for (int prioridade = 0; prioridade < 2; prioridade++)
                foreach (var m in tramaTecida.missoes)
                {
                    if (m.opcional != (prioridade == 1) || !DisponivelTecido(m))
                        continue;
                    var e = EtapaAtualTecido(m);
                    if (e != null)
                    {
                        missao = m;
                        etapa = e;
                        return true;
                    }
                }

            missao = null;
            etapa = null;
            return false;
        }

        // Só para o teste seguir depois de uma etapa que travou: marca os eventos e dá os objetos da etapa.
        private void ForcarEtapaTeste(MTMEtapaTecido e)
        {
            if (e.conceder != null)
                foreach (var item in e.conceder)
                    estado.DefinirQuantidade(item.id, MTMTrama.Quantidade(estado, item.id) + item.quantidade);
            foreach (string id in e.eventos)
                if (!estado.Tem(id))
                    estado.marcos.Add(id);
        }

        // Salva o progresso do fim da história num arquivo da pasta do teste, apaga o estado da memória e
        // carrega de volta: tudo tem de voltar igual. O save do jogador não é lido nem escrito.
        private void TestarSalvarCarregar(Action<bool, string> anotar)
        {
            string caminho = Path.Combine(pastaTeste, "save_teste.json");
            bool carregava = carregarProgresso;
            string antes = "", depois = "";
            try
            {
                caminhoSaveTeste = caminho;
                carregarProgresso = true;
                Salvar();
                antes = JsonUtility.ToJson(estado);
                estado = new MTMEstado();
                Carregar();
                depois = JsonUtility.ToJson(estado);
            }
            finally
            {
                carregarProgresso = carregava;
                caminhoSaveTeste = null;
            }

            anotar(File.Exists(caminho) && string.IsNullOrEmpty(erroFatalUI) && antes == depois && estado.marcos.Count > 0, string.Format(CultureInfo.InvariantCulture,
                "salvar e carregar numa pasta de teste devolve o mesmo progresso ({0} marcos, {1} bytes)", estado.marcos.Count,
                File.Exists(caminho) ? new FileInfo(caminho).Length : 0));
        }

        // Música do momento: exploração de dia, noite nas horas escuras, festa na praça decorada; e a vinheta.
        // O relógio e a festa mudam só na memória desta execução e voltam no fim.
        private IEnumerator TestarMusicas(Action<bool, string> anotar)
        {
            float relogio = estado.relogio;
            estado.Concluir("visitou_rua");
            if (!IrParaPontoSeguro("rua"))
            {
                anotar(false, "músicas: não foi possível entrar na Rua do Comércio");
                yield break;
            }

            estado.relogio = .45f;
            yield return new WaitForSecondsRealtime(1f);
            LimparPainelTeste();
            anotar(musicaTocando == "musica_exploracao", "de dia, na rua, toca " + musicaTocando);
            estado.relogio = .9f;
            yield return new WaitForSecondsRealtime(1f);
            var noite = SonsDe("musica_noite");
            anotar(musicaTocando == "musica_noite" && noite.Length > 0 && noite[0].length > 30f,
                string.Format(CultureInfo.InvariantCulture, "de noite toca {0} ({1:0.0} s, em loop: {2})", musicaTocando,
                    noite.Length > 0 ? noite[0].length : 0f, musicaSom[musicaAtiva] != null && musicaSom[musicaAtiva].loop));
            estado.relogio = .45f;

            bool festaAntes = estado.Tem(tramaTecida.eventoFesta);
            estado.Concluir(tramaTecida.eventoFesta);
            estado.Concluir("visitou_praca");
            AtualizarVisibilidadeRoteiro();
            if (IrParaPontoSeguro("praca"))
            {
                yield return new WaitForSecondsRealtime(1.5f);
                LimparPainelTeste();
                var festa = SonsDe("musica_festa");
                anotar(musicaTocando == "musica_festa" && festa.Length > 0 && festa[0].length > 30f,
                    string.Format(CultureInfo.InvariantCulture, "na praça em festa toca {0} ({1:0.0} s)", musicaTocando, festa.Length > 0 ? festa[0].length : 0f));
                estado.relogio = .9f;
                yield return new WaitForSecondsRealtime(1f);
                anotar(musicaTocando == "musica_festa", "a festa continua à noite na praça: " + musicaTocando);
            }
            else
                anotar(false, "músicas: não foi possível entrar na praça");

            if (!festaAntes)
                estado.marcos.Remove(tramaTecida.eventoFesta);
            AtualizarVisibilidadeRoteiro();
            estado.relogio = relogio;
            var vinheta = SonsDe("vinheta_missao");
            TocarSom("vinheta_missao", .8f, 0f);
            anotar(vinheta.Length > 0 && vinheta[0].length > 2f && vinheta[0].length < 8f,
                string.Format(CultureInfo.InvariantCulture, "vinheta de missão concluída carregada ({0:0.0} s)", vinheta.Length > 0 ? vinheta[0].length : 0f));
        }

        // Pessoas que o roteiro cria copiando outro morador, mas que têm desenho próprio: ponto, região e folha.
        private static readonly string[,] ArtesDoRoteiro =
        {
            { "pdf_visitante", "serra", "Expedito" }, { "pdf_moradora", "vila", "Moradora" },
            { "pdf_morador_placa", "rua", "MoradorPlaca" }, { "pdf_ajuda_morador", "mercado", "MoradorPlaca" },
            { "pdf_artesao", "fabrica", "Artesao" }, { "pdf_morador_rio", "rio", "MoradorMargem" },
        };

        // Confere se cada uma ficou com o próprio desenho (e não com o do modelo) e a fotografa de perto.
        private IEnumerator TestarArteDoRoteiro(Action<bool, string> anotar)
        {
            for (int i = 0; i < ArtesDoRoteiro.GetLength(0); i++)
            {
                string id = ArtesDoRoteiro[i, 0], regiao = ArtesDoRoteiro[i, 1], arte = ArtesDoRoteiro[i, 2];
                estado.Concluir("visitou_" + regiao);
                if (!IrParaPontoSeguro(regiao))
                {
                    anotar(false, id + ": não foi possível entrar em " + regiao);
                    continue;
                }

                yield return new WaitForSecondsRealtime(1f);
                LimparPainelTeste();
                MTMAtorV22 pessoa = null;
                foreach (var a in atual.GetComponentsInChildren<MTMAtorV22>(true))
                {
                    var p = a.GetComponentInParent<MTMPonto>(true);
                    if (a.tipo == MTMAtorV22.Tipo.Morador && p != null && p.dados != null && p.dados.id == id)
                        pessoa = a;
                }

                var sr = pessoa != null ? pessoa.GetComponent<SpriteRenderer>() : null;
                string sprite = sr != null && sr.sprite != null ? sr.sprite.name : "nenhum";
                anotar(sprite.StartsWith("HD2D_" + arte + "_", StringComparison.Ordinal), id + " usa o desenho próprio (" + sprite + ")");
                if (pessoa == null)
                    continue;
                // Quem só aparece mais tarde na história fica desligado: liga só durante a foto.
                var ligados = new List<GameObject>();
                for (var t = pessoa.transform; t != null && t != atual.transform; t = t.parent)
                    if (!t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(true);
                        ligados.Add(t.gameObject);
                    }

                yield return null;
                yield return null;
                FotografarPessoa(pessoa, "arte_" + id);
                for (int k = ligados.Count - 1; k >= 0; k--)
                    ligados[k].SetActive(false);
            }
        }

        private void FotografarPessoa(MTMAtorV22 pessoa, string nome)
        {
            if (cameraDoMapa == null)
                return;
            var go = new GameObject("Teste_Foto_Pessoa");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(cameraDoMapa);
            cam.enabled = false;
            cam.orthographic = true;
            cam.orthographicSize = 1.6f;
            Vector3 pe = pessoa.transform.position;
            cam.transform.position = new Vector3(pe.x, pe.y + 1f, cameraDoMapa.transform.position.z);
            var rt = new RenderTexture(400, 520, 24) { antiAliasing = 2 };
            cam.targetTexture = rt;
            cam.aspect = 400f / 520f;
            cam.Render();
            var ativo = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(400, 520, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 400, 520), 0, 0);
            tex.Apply();
            RenderTexture.active = ativo;
            File.WriteAllBytes(Path.Combine(pastaTeste, nome + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            Destroy(tex);
            rt.Release();
            Destroy(rt);
            Destroy(go);
        }

        private static Sprite[] PosesProvisorias(MTMAtorV22 ator)
        {
            var s = ator.quadros != null && ator.quadros.Length > 0 ? ator.quadros[0] : null;
            if (s == null)
                return null;
            var poses = new Sprite[4];
            for (int i = 0; i < poses.Length; i++)
            {
                poses[i] = Sprite.Create(s.texture, s.rect, new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height), s.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                poses[i].name = "teste_lado_" + i;
                poses[i].hideFlags = HideFlags.DontSave;
            }

            return poses;
        }

        // As casas da beira do rio (restaurante e abrigo de madeira) barram o Neto pela frente,
        // em três pontos: esquerda, meio e direita da fachada.
        private IEnumerator TestarCasasDoRio(Action<bool, string> anotar)
        {
            foreach (var id in new[] { "rio", "beira" })
            {
                estado.Concluir("visitou_" + id);
                if (!IrParaPontoSeguro(id))
                {
                    anotar(false, id + ": não foi possível entrar");
                    continue;
                }

                yield return new WaitForSecondsRealtime(1.5f);
                LimparPainelTeste();
                int n = 0;
                foreach (var casa in atual.GetComponentsInChildren<MTMEscalaConstrucaoV221>())
                {
                    if (!casa.name.StartsWith("barraca_azul", StringComparison.Ordinal) && !casa.name.StartsWith("abrigo_rustico", StringComparison.Ordinal))
                        continue;
                    n++;
                    Vector2 baseCasa = casa.transform.position;
                    foreach (float dx in new[] { -2f, 0f, 2f })
                    {
                        Vector2 partida = baseCasa + new Vector2(dx, -1.4f);
                        Reposicionar(partida);
                        yield return new WaitForSecondsRealtime(.25f);
                        MTMEntrada.MovimentoDeTeste = Vector2.up;
                        yield return new WaitForSecondsRealtime(1.4f);
                        MTMEntrada.MovimentoDeTeste = null;
                        float y = corpo.position.y;
                        anotar(y < baseCasa.y + .15f && y > partida.y + .2f, string.Format(CultureInfo.InvariantCulture,
                            "{0}: {1} em ({2:0.0}; {3:0.0}) barra o Neto pela frente (x {4:+0;-0;0}): parou a {5:0.00} da base",
                            id, casa.name, baseCasa.x, baseCasa.y, dx, y - baseCasa.y));
                    }

                    Reposicionar(baseCasa + new Vector2(0f, -1.2f));
                    yield return new WaitForSecondsRealtime(.3f);
                    FotografarTeste(id + "_" + casa.name + "_" + n);
                }

                anotar(n > 0, id + ": " + n + " casa(s) da beira do rio testada(s)");
            }
        }

        private IEnumerator ManterFocoNoTeste()
        {
            // Sem janela o jogo nunca "recebe foco": o teste mantém o foco ligado para tudo andar.
            while (true)
            {
                possuiFoco = true;
                yield return null;
            }
        }

        private void LimparPainelTeste()
        {
            falas = null;
            aoConcluir = null;
            painel = "";
        }

        private void FotografarTeste(string nome)
        {
            if (cameraDoMapa == null)
                return;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 2 };
            var alvoAnterior = cameraDoMapa.targetTexture;
            cameraDoMapa.targetTexture = rt;
            cameraDoMapa.Render();
            cameraDoMapa.targetTexture = alvoAnterior;
            var ativo = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = ativo;
            File.WriteAllBytes(Path.Combine(pastaTeste, nome + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
            Destroy(rt);
        }
    }
}

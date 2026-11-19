using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Confrontos com as lendas: luta (Mulher-Cavalo) e esquiva/fuga (Curupira).
    // As regras ficam em MTMConfronto.cs;
    // o conteúdo da classe vem de Resources/MemoriasTecidas/confrontos.json.
    public sealed partial class MTMJogo
    {
        private const string FonteConfronto = "Lendas da noite · Memórias Tecidas";
        private const string PrefixoPontoConfronto = "cfp_";
        private MTMConfrontosDados confrontos;
        private readonly Dictionary<string, MTMConfrontoDados> confrontosPorPonto = new Dictionary<string, MTMConfrontoDados>();
        private readonly Dictionary<MTMPonto, SpriteRenderer> marcadoresConfronto = new Dictionary<MTMPonto, SpriteRenderer>();
        private readonly List<SpriteRenderer> fogueirasConfronto = new List<SpriteRenderer>();
        private MTMConfrontoDados confrontoAtual;
        private MTMSessaoConfronto sessaoConfronto;
        private Vector2 centroConfronto, frenteJogador = Vector2.down;
        private GameObject inimigoConfronto;
        private SpriteRenderer desenhoInimigo, sombraInimigo;
        private Vector3 escalaInimigo = Vector3.one;
        // Folha de animação da lenda (linhas = direções, colunas = quadros).
        private Sprite[] quadrosLenda, quadrosInvestida, quadrosPreparo;
        private Sprite poseParadaLenda;
        private int colunasLenda, linhasLenda, linhaLenda;
        private float tempoLenda;
        private Vector2 posicaoAnteriorLenda, direcaoLenda = Vector2.down;
        private Sprite fogueiraAcesa, fogueiraApagada;
        private readonly Dictionary<string, int> derrotasConfronto = new Dictionary<string, int>();
        private float golpeAte, danoAte, brilhoInimigoAte, relogioAntesConfronto;
        // Reação da lenda ao golpe certo: recua para longe de Neto, treme, amassa e pisca avermelhada.
        private const float DuracaoDanoLenda = .45f;
        private float danoLendaInicio = -10f;
        private Vector2 empurraoLenda = Vector2.up;
        // Neto atingido: um instante parado no impacto (a lenda também congela), depois é jogado
        // para longe sem controle, olhando para ela, e o losango de fôlego perdido estoura.
        private const float ImpactoNeto = .08f, DuracaoRecuoNeto = .34f, ForcaRecuoNeto = 10f;
        private float recuoNetoInicio = -10f, netoAtingidoInicio = -10f, pausaLendaAte, anguloEstalo;
        private int folegoPerdido = -1;
        private Vector2 recuoNetoDirecao = Vector2.down, olharLendaNeto = Vector2.down;
        private Texture2D texEstaloHUD;
        private bool noiteForcadaConfronto;
        private string regiaoConfrontoConferida = "";
        // Preparação (contagem antes da luta) e encerramento (aviso de vitória ou derrota antes do diálogo).
        private const float DuracaoContagem = 3.2f, DuracaoFimVitoria = 2.6f, DuracaoFimDerrota = 2.1f;
        private bool contandoConfronto, encerrandoConfronto, fimComVitoria;
        private float contagemInicio = -10f, fimInicio = -10f, alfaSombraFim = 1f;
        private int numeroContagem = -1;
        private Vector2 largadaJogador, posicaoFimLenda, direcaoFimLenda = Vector2.up;
        // Quem prepara o jogador: cópia do desenho de um personagem da cena, na margem da arena.
        private GameObject guiaConfronto;
        private string guiaDeQual = "", guiaFicaAte = "";
        private readonly HashSet<string> guiasSemModelo = new HashSet<string>();

        private bool EmConfronto
        {
            get { return sessaoConfronto != null; }
        }

        // Verdadeiro no impacto e no recuo de um golpe recebido: Neto não anda nem golpeia.
        public bool NetoAtordoado
        {
            get { return EmConfronto && Time.time < recuoNetoInicio + DuracaoRecuoNeto; }
        }

        // Para onde Neto olha enquanto está atordoado (a lenda que o acertou) ou na contagem antes da luta.
        public Vector2 OlharDoAtordoado
        {
            get
            {
                if (NetoAtordoado)
                    return olharLendaNeto;
                if (contandoConfronto && sessaoConfronto != null)
                    return centroConfronto + new Vector2(sessaoConfronto.Ix, sessaoConfronto.Iy) - (Vector2)transform.position;
                return Vector2.zero;
            }
        }

        private bool PontoConfronto(MTMPonto p)
        {
            return p != null && p.dados != null && p.dados.id != null && p.dados.id.StartsWith(PrefixoPontoConfronto, StringComparison.Ordinal);
        }

        // ---------- carregamento ----------

        private void PrepararConfrontos()
        {
            confrontos = null;
            try
            {
                var arquivo = Resources.Load<TextAsset>("MemoriasTecidas/confrontos");
                if (arquivo == null)
                    return;
                var dados = JsonUtility.FromJson<MTMConfrontosDados>(arquivo.text);
                MTMConfrontos.Validar(dados);
                confrontos = dados;
            }
            catch (Exception erro)
            {
                Debug.LogWarning("Os confrontos não foram carregados: " + erro.Message + " O resto do jogo continua.", this);
                return;
            }

            foreach (var c in confrontos.confrontos)
            {
                var regiao = RegiaoTecido(c.regiao);
                if (regiao == null)
                {
                    Debug.LogWarning("Confronto '" + c.id + "': a região '" + c.regiao + "' não existe na cena. O confronto foi ignorado.", this);
                    continue;
                }

                var go = new GameObject("Confronto · " + c.id) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(regiao.transform, false);
                go.transform.localPosition = new Vector3(c.x, c.y, 0f);
                var ponto = go.AddComponent<MTMPonto>();
                ponto.dados = new MTMPontoDados { id = PrefixoPontoConfronto + c.id, nome = c.nome, tipo = "confronto", falas = new string[0] };
                ponto.alcance = 2.2f;
                var marcador = new GameObject("Marcador") { hideFlags = HideFlags.DontSave };
                marcador.transform.SetParent(go.transform, false);
                marcador.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                var desenho = marcador.AddComponent<SpriteRenderer>();
                desenho.sprite = SpriteMarcadorVivo();
                marcador.AddComponent<MTMOrdemY>();
                marcadoresConfronto[ponto] = desenho;
                var lista = new List<MTMPonto>(regiao.pontos ?? new MTMPonto[0]);
                lista.Add(ponto);
                regiao.pontos = lista.ToArray();
                confrontosPorPonto[ponto.dados.id] = c;
            }

            MigrarEtapasDeConfronto();
        }

        // Saves anteriores a uma etapa de confronto não voltam atrás: se o jogador já passou dela na campanha, a etapa conta como feita.
        private void MigrarEtapasDeConfronto()
        {
            if (tramaTecida == null || tramaTecida.missoes == null)
                return;
            foreach (var m in tramaTecida.missoes)
                for (int i = 0; i < m.etapas.Length; i++)
                {
                    var e = m.etapas[i];
                    if (e.acao != "confronto" || MTMTrama.Concluida(estado, e))
                        continue;
                    bool passou = false;
                    for (int j = i + 1; j < m.etapas.Length && !passou; j++)
                        passou = MTMTrama.Concluida(estado, m.etapas[j]);
                    foreach (var outra in tramaTecida.missoes)
                        if (!passou && outra.requerMissoes != null && Array.IndexOf(outra.requerMissoes, m.id) >= 0)
                            foreach (var etapa in outra.etapas)
                                passou |= MTMTrama.Concluida(estado, etapa);
                    if (passou)
                        foreach (string evento in e.eventos)
                            estado.Concluir(evento);
                }
        }

        // Etapa da campanha que pede este confronto, se for a etapa atual da missão.
        private bool EtapaDoConfronto(MTMConfrontoDados c, out MTMMissaoTecido missao, out MTMEtapaTecido etapa)
        {
            missao = null;
            etapa = null;
            if (tramaTecida == null || tramaTecida.missoes == null)
                return false;
            foreach (var m in tramaTecida.missoes)
            {
                if (!DisponivelTecido(m))
                    continue;
                var e = EtapaAtualTecido(m);
                if (e == null || e.acao != "confronto" || e.ponto != PrefixoPontoConfronto + c.id)
                    continue;
                missao = m;
                etapa = e;
                return true;
            }

            return false;
        }

        // Chama o jogador para o confronto uma única vez, quando ele fica disponível.
        private void ConvidarParaConfronto()
        {
            if (confrontos == null || GuiaPrimeiroFioAtivo())
                return;
            foreach (var c in confrontos.confrontos)
            {
                if (c.convite == null || c.convite.Length == 0 || estado.Tem(c.evento) || estado.Tem(c.evento + "_convite"))
                    continue;
                if (!string.IsNullOrEmpty(c.requer) && !estado.Tem(c.requer))
                    continue;
                estado.Concluir(c.evento + "_convite");
                Salvar();
                AbrirLenda(c, c.convite, null);
                return;
            }
        }

        // ---------- disponibilidade e interação ----------

        private bool DisponivelConfronto(MTMPonto p)
        {
            MTMConfrontoDados c;
            if (EmConfronto || !confrontosPorPonto.TryGetValue(p.dados.id, out c))
                return false;
            return string.IsNullOrEmpty(c.requer) || estado.Tem(c.requer);
        }

        private string AcaoConfronto(MTMPonto p)
        {
            MTMConfrontoDados c;
            if (!confrontosPorPonto.TryGetValue(p.dados.id, out c))
                return "Examinar";
            if (estado.Tem(c.evento))
                return "Enfrentar de novo";
            if (GuiaRecebe(c))
                return "Falar com " + (string.IsNullOrEmpty(c.nomeGuia) ? "quem espera na margem" : c.nomeGuia);
            return string.IsNullOrEmpty(c.chamada) ? "Enfrentar " + c.nome : c.chamada;
        }

        private bool InteragirConfronto(MTMPonto ponto)
        {
            if (!PontoConfronto(ponto))
                return false;
            MTMConfrontoDados c;
            if (!confrontosPorPonto.TryGetValue(ponto.dados.id, out c))
                return true;
            var linhas = new List<string>();
            bool forcarNoite = c.noite && !Noite;
            if (forcarNoite)
                linhas.Add("Neto espera. O sol baixa devagar e a noite chega.");
            // Antes da primeira vitória, quem espera na margem prepara o jogador.
            if (GuiaRecebe(c) && c.preparo != null)
                linhas.AddRange(c.preparo);
            linhas.AddRange(c.abertura);
            AbrirLenda(c, linhas.ToArray(), () =>
            {
                // As lendas só aparecem à noite: o relógio salta para as 20h, mas só quando
                // a luta começa de fato. Sair do diálogo com Esc não mexe no horário.
                relogioAntesConfronto = estado.relogio;
                noiteForcadaConfronto = forcarNoite;
                if (forcarNoite)
                    estado.relogio = 20f / 24f;
                IniciarConfronto(c);
            });
            return true;
        }

        private void IniciarConfronto(MTMConfrontoDados c)
        {
            var regiao = RegiaoTecido(c.regiao);
            if (regiao == null || regiao != atual)
                return;
            int derrotas;
            derrotasConfronto.TryGetValue(c.id, out derrotas);
            c = MTMConfrontos.ComAjuda(c, derrotas);
            confrontoAtual = c;
            sessaoConfronto = new MTMSessaoConfronto(c);
            centroConfronto = regiao.transform.TransformPoint(new Vector3(c.x, c.y, 0f));
            golpeAte = danoAte = brilhoInimigoAte = 0f;
            danoLendaInicio = -10f;
            recuoNetoInicio = netoAtingidoInicio = -10f;
            pausaLendaAte = 0f;
            folegoPerdido = -1;
            MontarLenda(c, regiao);
            MontarArma(c);
            PosicionarInimigo();
            perto = null;
            // A luta só começa depois da contagem: Neto entra na clareira e encara a lenda, que espera parada.
            contandoConfronto = true;
            encerrandoConfronto = false;
            contagemInicio = Time.unscaledTime;
            numeroContagem = -1;
            Vector2 rel = corpo.position - centroConfronto;
            float dentro = c.raio - 1.6f;
            largadaJogador = rel.magnitude > dentro ? (rel.sqrMagnitude > .01f ? rel.normalized : Vector2.down) * dentro : rel;
        }

        private void LimparConfronto()
        {
            LimparArma();
            if (inimigoConfronto != null)
                Destroy(inimigoConfronto);
            foreach (var f in fogueirasConfronto)
                if (f != null)
                    Destroy(f.gameObject);
            fogueirasConfronto.Clear();
            inimigoConfronto = null;
            desenhoInimigo = sombraInimigo = null;
            quadrosLenda = null;
            contandoConfronto = encerrandoConfronto = false;
            sessaoConfronto = null;
            confrontoAtual = null;
        }

        // ---------- laço do confronto ----------

        // Chamado a cada quadro de jogo livre. Verdadeiro enquanto um confronto está em curso.
        private bool AtualizarConfronto()
        {
            AtualizarGuiaConfronto();
            AtualizarMarcadoresConfronto();
            if (!EmConfronto)
            {
                ConferirEtapasDeConfrontoVencido();
                ConvidarParaConfronto();
                return false;
            }

            var c = confrontoAtual;
            var s = sessaoConfronto;
            if (atual == null || atual.dados.id != c.regiao)
            {
                LimparConfronto();
                DevolverRelogioConfronto();
                return false;
            }

            perto = null;
            bool atordoado = NetoAtordoado;
            if (atordoado)
                movimento = Vector2.zero;
            if (movimento.sqrMagnitude > .01f)
                frenteJogador = movimento.normalized;
            Vector2 rel = corpo.position - centroConfronto;
            if (rel.magnitude > c.raio + 6f)
            {
                LimparConfronto();
                DevolverRelogioConfronto();
                Avisar("Você se afastou e a lenda sumiu no escuro. Volte para tentar de novo.");
                return false;
            }

            if (contandoConfronto)
            {
                AtualizarContagem(rel);
                PosicionarInimigo();
                AtualizarArma();
                return true;
            }

            if (encerrandoConfronto)
            {
                movimento = Vector2.zero;
                AtualizarEncerramento();
                return true;
            }

            if (rel.magnitude > c.raio)
            {
                rel = rel.normalized * c.raio;
                corpo.position = centroConfronto + rel;
            }

            var faseAntes = s.Fase;
            // No impacto de um golpe em Neto a lenda congela por um instante.
            var sinais = s.Avancar(Time.unscaledTime < pausaLendaAte ? 0f : Time.deltaTime, rel.x, rel.y);
            if ((sinais & MTMSinalConfronto.JogadorAtingido) != 0)
            {
                AtingirNeto(rel);
                atordoado = true;
            }

            if ((sinais & MTMSinalConfronto.Perdeu) != 0)
            {
                IniciarEncerramento(false);
                return true;
            }

            if (!atordoado && s.Fase == MTMFaseConfronto.Luta && MTMEntrada.Golpeou() && s.PodeGolpear)
            {
                golpeAte = Time.unscaledTime + .18f;
                int golpe = s.Golpear(rel.x, rel.y, frenteJogador.x, frenteJogador.y);
                GolpearComArma(frenteJogador, golpe);
                TocarSom("golpe_ar", .7f, .08f);
                if (golpe != 0)
                    TocarSom(golpe > 0 ? "golpe_acerto" : "golpe_aparado", golpe > 0 ? 1f : .8f, .06f);
                MTMEntrada.Vibrar(golpe > 0 ? .3f : .12f, golpe > 0 ? .45f : 0f, golpe > 0 ? .12f : .06f);
                if (golpe > 0)
                {
                    brilhoInimigoAte = Time.unscaledTime + .16f;
                    danoLendaInicio = Time.unscaledTime;
                    Vector2 lenda = centroConfronto + new Vector2(s.Ix, s.Iy);
                    empurraoLenda = (lenda - corpo.position).sqrMagnitude > .0001f ? (lenda - corpo.position).normalized : frenteJogador;
                }
                else if (golpe < 0)
                    Avisar("Ela aparou o golpe. Espere a investida passar e ataque quando ela cansar.");
            }
            else if (!atordoado && s.Fase == MTMFaseConfronto.Cerco && MTMEntrada.Apertou('e') && quadroDaAcao != Time.frameCount)
            {
                int i = FogueiraPerto(rel);
                if (i >= 0)
                {
                    quadroDaAcao = Time.frameCount;
                    var r = s.AcenderFogueira(i);
                    TocarSom(r == MTMResultadoFogueira.Errada ? "tear_erro" : "fogueira", .9f, .05f);
                    if (r == MTMResultadoFogueira.Errada)
                        Avisar("As fogueiras se apagaram. A ordem está ao contrário, como as pegadas dele.");
                }
            }
            else if (!atordoado && s.Fase == MTMFaseConfronto.Escuta && MTMEntrada.Escutando())
            {
                multiplicador = .55f;
                s.Escutar(Time.deltaTime, rel.x, rel.y);
            }

            if (s.Fase != faseAntes)
                AoMudarFaseConfronto();
            if (s.Fase == MTMFaseConfronto.Vitoria)
            {
                IniciarEncerramento(true);
                return true;
            }

            PosicionarInimigo();
            AtualizarArma();
            AtualizarFogueiras();
            return true;
        }

        // Neto leva o golpe: pausa curta no impacto, recuo para longe da lenda, pose de dor e clarão
        // (MTMNetoVisual.Atingir) e, no HUD, o estalo no peito e o losango de fôlego estourando.
        private void AtingirNeto(Vector2 netoRel)
        {
            var s = sessaoConfronto;
            Vector2 investida = new Vector2(s.DirX, s.DirY);
            Vector2 afasta = netoRel - new Vector2(s.Ix, s.Iy);
            // Atropelado de lado, ele sai pela lateral; de frente, vai na direção da investida.
            Vector2 direcao = (afasta.sqrMagnitude > .0001f ? afasta.normalized : investida) * .55f + investida * .45f;
            recuoNetoDirecao = direcao.sqrMagnitude > .0001f ? direcao.normalized : Vector2.down;
            olharLendaNeto = afasta.sqrMagnitude > .0001f ? -afasta : -investida;
            recuoNetoInicio = Time.time + ImpactoNeto;
            pausaLendaAte = Time.unscaledTime + ImpactoNeto;
            netoAtingidoInicio = Time.unscaledTime;
            danoAte = Time.unscaledTime + .35f;
            anguloEstalo = UnityEngine.Random.Range(0f, 45f);
            folegoPerdido = s.VidaJogador;
            movimento = Vector2.zero;
            if (corpo != null)
                corpo.linearVelocity = Vector2.zero;
            var visual = VisualDoNeto();
            if (visual != null)
                visual.Atingir(recuoNetoDirecao, MTMSessaoConfronto.Invulnerabilidade);
            MTMEntrada.Vibrar(.55f, .85f, .28f);
            TocarSom("dano", .9f, .05f);
        }

        // Velocidade imposta a Neto enquanto está atordoado: parado no impacto, depois o recuo perdendo força.
        private bool VelocidadeDoRecuo(out Vector2 velocidadeRecuo)
        {
            velocidadeRecuo = Vector2.zero;
            if (!NetoAtordoado)
                return false;
            float t = (Time.time - recuoNetoInicio) / DuracaoRecuoNeto;
            if (t > 0f)
            {
                float resto = 1f - Mathf.Clamp01(t);
                velocidadeRecuo = recuoNetoDirecao * (ForcaRecuoNeto * resto * resto);
            }

            return true;
        }

        private MTMNetoVisual VisualDoNeto()
        {
            if (visualNeto != null)
                return visualNeto;
            if (atorNeto == null)
                foreach (var ator in GetComponentsInChildren<MTMAtorV22>(true))
                    if (ator.tipo == MTMAtorV22.Tipo.Jogador)
                    {
                        atorNeto = ator;
                        break;
                    }

            visualNeto = atorNeto == null ? null : atorNeto.GetComponent<MTMNetoVisual>();
            return visualNeto;
        }

        private void AoMudarFaseConfronto()
        {
            var c = confrontoAtual;
            if (sessaoConfronto.Fase == MTMFaseConfronto.Cerco)
            {
                CriarFogueiras();
                Avisar(string.IsNullOrEmpty(c.avisoCerco) ? "Acenda as fogueiras com E, na ordem certa." : c.avisoCerco);
            }
            else if (sessaoConfronto.Fase == MTMFaseConfronto.Escuta)
                Avisar(string.IsNullOrEmpty(c.avisoEscuta) ? "Chegue perto e segure L para ouvir." : c.avisoEscuta);
        }

        private void VencerConfronto()
        {
            var c = confrontoAtual;
            bool comGuia = GuiaRecebe(c);
            LimparConfronto();
            Parar();
            derrotasConfronto.Remove(c.id);
            bool primeira = estado.Concluir(c.evento);
            var linhas = new List<string>(c.vitoria);
            if (comGuia && c.guiaVitoria != null)
            {
                linhas.AddRange(c.guiaVitoria);
                // Quem preparou o jogador fica na margem até ele sair do lugar.
                guiaFicaAte = c.regiao;
            }

            if (noiteForcadaConfronto)
            {
                // A noite foi adiantada para o encontro; o dia volta para a campanha seguir.
                estado.relogio = 7f / 24f;
                estado.dia++;
                noiteForcadaConfronto = false;
                linhas.Add("Quando Neto percebe, o céu já está clareando.");
            }

            Salvar();
            // A etapa da campanha é concluída antes do diálogo: sair dele com Esc não perde a vitória.
            MTMMissaoTecido m;
            MTMEtapaTecido e;
            bool etapaConcluida = EtapaDoConfronto(c, out m, out e) && ConcluirEtapaRoteiro(m, e, null, false);
            var missao = m;
            AbrirLenda(c, linhas.ToArray(), () =>
            {
                if (etapaConcluida)
                    Avisar(MTMTrama.Concluida(estado, missao) ? "Missão concluída: " + missao.titulo + "." : "Novo objetivo");
                else
                    Avisar(primeira ? "Lenda registrada. Veja em J → Achados." : "A lenda voltou para a noite.");
            });
        }

        // Saves em que a lenda já foi vencida mas a etapa ficou pendente (diálogo fechado com Esc
        // em versões anteriores): conclui a etapa ao entrar na região, sem exigir outra luta.
        private void ConferirEtapasDeConfrontoVencido()
        {
            if (confrontos == null || atual == null || regiaoConfrontoConferida == atual.dados.id)
                return;
            regiaoConfrontoConferida = atual.dados.id;
            foreach (var c in confrontos.confrontos)
            {
                if (c.regiao != atual.dados.id || !estado.Tem(c.evento))
                    continue;
                MTMMissaoTecido m;
                MTMEtapaTecido e;
                if (EtapaDoConfronto(c, out m, out e))
                    ConcluirEtapaRoteiro(m, e, null, false);
            }
        }

        private void DevolverRelogioConfronto()
        {
            if (!noiteForcadaConfronto)
                return;
            estado.relogio = relogioAntesConfronto;
            noiteForcadaConfronto = false;
        }

        private void PerderConfronto()
        {
            var c = confrontoAtual;
            bool comGuia = GuiaRecebe(c);
            // Com quem prepara o jogador na margem, Neto volta para o lado dele; senão, para o caminho central.
            Vector2 volta = comGuia ? (Vector2)guiaConfronto.transform.position + new Vector2(-1.3f, -.4f) : new Vector2(atual.dados.seguroX, atual.dados.seguroY);
            LimparConfronto();
            DevolverRelogioConfronto();
            int derrotas;
            derrotasConfronto.TryGetValue(c.id, out derrotas);
            derrotasConfronto[c.id] = derrotas + 1;
            Reposicionar(volta);
            var linhas = new List<string>
            {
                string.IsNullOrEmpty(c.derrota) ? "Neto perde o fôlego e recua até a trilha. A lenda continua lá, esperando." : c.derrota
            };
            if (comGuia && c.guiaDerrota != null)
                linhas.AddRange(c.guiaDerrota);
            linhas.Add("Na próxima tentativa, Neto já conhece melhor os movimentos dela e terá mais fôlego.");
            AbrirLenda(c, linhas.ToArray(), null);
        }

        private int FogueiraPerto(Vector2 rel)
        {
            var f = confrontoAtual.fogueiras;
            int melhor = -1;
            float distancia = 1.8f;
            for (int i = 0; f != null && i < f.Length; i++)
            {
                float d = Vector2.Distance(rel, new Vector2(f[i].x, f[i].y));
                if (d < distancia)
                {
                    distancia = d;
                    melhor = i;
                }
            }

            return melhor;
        }

        // Diálogo com o retrato da própria lenda: o primeiro quadro da folha (Curupira)
        // ou o desenho do personagem de aparição do projeto (Mulher-Cavalo).
        private void AbrirLenda(MTMConfrontoDados c, string[] linhas, Action concluir)
        {
            Texture2D textura;
            Rect recorte;
            RetratoDaLenda(c, out textura, out recorte);
            // O encontro leva o nome do lugar: linhas de narração não saem na voz da lenda.
            // As falas "Nome da lenda: ..." ganham o retrato dela em RetratoDeLendaPeloNome.
            Abrir(atual != null && atual.dados != null ? atual.dados.nome : c.nome, FonteConfronto, linhas, concluir);
            retratoAtual = null;
            linhaPreparadaDialogo = null;
        }

        private bool RetratoDeLendaPeloNome(string nome, out Texture2D textura, out Rect recorte)
        {
            textura = null;
            recorte = new Rect(0, 0, 1, 1);
            if (confrontos == null || confrontos.confrontos == null)
                return false;
            foreach (var c in confrontos.confrontos)
                if (c != null && string.Equals(c.nome, nome, StringComparison.OrdinalIgnoreCase))
                {
                    RetratoDaLenda(c, out textura, out recorte);
                    return textura != null;
                }

            return false;
        }

        // Sprite avulso em Resources, com o pé embaixo e a altura pedida no mundo.
        private Sprite CarregarSpriteLenda(string caminho, float alturaMundo)
        {
            var pronto = Resources.Load<Sprite>(caminho);
            var textura = pronto != null ? pronto.texture : Resources.Load<Texture2D>(caminho);
            if (textura == null)
            {
                Debug.Log("Lenda: a arte '" + caminho + "' ainda não está em Resources.", this);
                return null;
            }

            var sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(.5f, .03f), textura.height / Mathf.Max(.1f, alturaMundo), 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private void RetratoDaLenda(MTMConfrontoDados c, out Texture2D textura, out Rect recorte)
        {
            textura = null;
            recorte = new Rect(0, 0, 1, 1);
            if (!string.IsNullOrEmpty(c.retrato))
            {
                var busto = Resources.Load<Texture2D>(c.retrato);
                if (busto == null)
                {
                    var bustoSprite = Resources.Load<Sprite>(c.retrato);
                    busto = bustoSprite == null ? null : bustoSprite.texture;
                }

                if (busto != null)
                {
                    textura = busto;
                    return;
                }
            }

            if (!string.IsNullOrEmpty(c.imagem))
            {
                textura = Resources.Load<Texture2D>(c.imagem);
                if (textura == null)
                {
                    var inteira = Resources.Load<Sprite>(c.imagem);
                    textura = inteira == null ? null : inteira.texture;
                }

                if (textura != null)
                {
                    float w = 1f / Mathf.Max(1, c.colunas), h = 1f / Mathf.Max(1, c.linhas);
                    // Linha 0 da folha (de frente) fica no alto da imagem; o busto é a parte de cima, no centro.
                    recorte = new Rect(w * .2f, 1f - h * .62f, w * .6f, h * .62f);
                    return;
                }
            }

            var regiao = RegiaoTecido(c.regiao);
            var referencia = regiao == null ? null : ModeloDeAparicao(regiao);
            var desenho = referencia == null ? null : referencia.GetComponentInChildren<SpriteRenderer>(true);
            if (desenho == null || desenho.sprite == null)
                return;
            var sprite = desenho.sprite;
            textura = sprite.texture;
            Rect r = sprite.textureRect;
            recorte = new Rect(r.x / textura.width, r.y / textura.height, r.width / textura.width, r.height / textura.height);
        }

        // ---------- desenho no mundo ----------

        private void AtualizarMarcadoresConfronto()
        {
            if (marcadoresConfronto.Count == 0)
                return;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            float pulso = reduzir ? 1f : 1f + .16f * Mathf.Sin(Time.unscaledTime * 4f);
            foreach (var par in marcadoresConfronto)
            {
                if (par.Key == null || par.Value == null)
                    continue;
                bool feito = estado.Tem(confrontosPorPonto[par.Key.dados.id].evento);
                // Sem marca dupla: quando é o alvo da missão, o losango dourado basta.
                // Lenda já vencida só reaparece à noite, que é quando ela anda.
                bool ativo = par.Key.gameObject.activeInHierarchy && DisponivelConfronto(par.Key)
                    && par.Key != alvoGuiaHUD && (!feito || Noite);
                par.Value.enabled = ativo;
                if (!ativo)
                    continue;
                par.Value.color = feito ? new Color(.97f, .92f, .8f, .4f) : new Color(.86f, .36f, .24f, .95f);
                par.Value.transform.localScale = Vector3.one * pulso * (feito ? .8f : 1.25f);
                // Com quem prepara o jogador no lugar do ponto, a marca fica sobre a cabeça dele.
                par.Value.transform.localPosition = new Vector3(0f, GuiaRecebe(confrontosPorPonto[par.Key.dados.id]) ? 1.95f : 1.1f, 0f);
            }
        }

        private void PosicionarInimigo()
        {
            if (inimigoConfronto == null || sessaoConfronto == null)
                return;
            var s = sessaoConfronto;
            float desdeDano = Time.unscaledTime - danoLendaInicio;
            float dano = desdeDano >= 0f && desdeDano < DuracaoDanoLenda ? 1f - desdeDano / DuracaoDanoLenda : 0f;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            Vector2 desvio = Vector2.zero;
            if (dano > 0f && !reduzir)
            {
                // Recua rápido, volta ao lugar e o tremor some junto.
                float recuo = Mathf.Sin(Mathf.PI * Mathf.Min(1f, desdeDano / (DuracaoDanoLenda * .7f)));
                desvio = empurraoLenda * (.45f * recuo) + Vector2.right * (Mathf.Sin(Time.unscaledTime * 95f) * .06f * dano);
            }

            inimigoConfronto.transform.position = new Vector3(centroConfronto.x + s.Ix + desvio.x, centroConfronto.y + s.Iy + desvio.y, 0f);
            // A arte dos personagens olha para a esquerda; vira quando a lenda vai para a direita.
            desenhoInimigo.flipX = s.DirX > 0f;
            if (sombraInimigo != null)
            {
                sombraInimigo.flipX = desenhoInimigo.flipX;
                sombraInimigo.enabled = true;
            }

            if (poseParadaLenda != null && s.Estado == MTMEstadoInimigo.Parado)
            {
                desenhoInimigo.sprite = poseParadaLenda;
                desenhoInimigo.flipX = false;
                if (sombraInimigo != null)
                    sombraInimigo.sprite = poseParadaLenda;
            }
            else if (quadrosLenda != null)
                AnimarLenda(s);

            Color cor = Color.white;
            float altura = 1f, largura = 1f;
            if (dano > 0f)
            {
                // Pisca avermelhada, como quem sentiu a pancada, e amassa no impacto.
                bool pisca = Mathf.Repeat(desdeDano * 14f, 1f) < .5f;
                cor = pisca ? new Color(1f, .38f, .32f, 1f) : new Color(1f, .9f, .85f, .75f);
                if (!reduzir)
                {
                    float amassa = Mathf.Sin(Mathf.PI * Mathf.Min(1f, desdeDano / .22f)) * .14f;
                    altura = 1f - amassa;
                    largura = 1f + amassa * .8f;
                }
            }
            else if (s.Estado == MTMEstadoInimigo.Preparando)
                cor = Color.Lerp(Color.white, new Color(1f, .45f, .35f, 1f), s.ProgressoAviso);
            else if (s.Estado == MTMEstadoInimigo.Cansado)
            {
                cor = new Color(.72f, .78f, .9f, .85f);
                altura = .88f;
            }

            desenhoInimigo.color = cor;
            inimigoConfronto.transform.localScale = new Vector3(escalaInimigo.x * largura, escalaInimigo.y * altura, escalaInimigo.z);
        }

        private void CriarFogueiras()
        {
            var f = confrontoAtual.fogueiras;
            var regiao = RegiaoTecido(confrontoAtual.regiao);
            for (int i = 0; f != null && i < f.Length; i++)
            {
                var go = new GameObject("Fogueira " + (i + 1)) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(regiao.transform, false);
                go.transform.position = new Vector3(centroConfronto.x + f[i].x, centroConfronto.y + f[i].y, 0f);
                var desenho = go.AddComponent<SpriteRenderer>();
                desenho.sprite = SpriteFogueira(false);
                desenho.spriteSortPoint = SpriteSortPoint.Pivot;
                go.AddComponent<MTMOrdemY>();
                fogueirasConfronto.Add(desenho);
            }
        }

        private void AtualizarFogueiras()
        {
            if (sessaoConfronto == null)
                return;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            for (int i = 0; i < fogueirasConfronto.Count; i++)
            {
                bool acesa = sessaoConfronto.Fase != MTMFaseConfronto.Cerco || sessaoConfronto.Acesas.Contains(i);
                float chama = acesa && !reduzir ? 1f + .07f * Mathf.Sin(Time.unscaledTime * 11f + i * 1.7f) : 1f;
                fogueirasConfronto[i].sprite = SpriteFogueira(acesa);
                fogueirasConfronto[i].transform.localScale = new Vector3(1f, chama, 1f);
            }
        }

        // Personagem que o projeto já usa para as aparições da noite (prefab com desenho e sombra).
        private GameObject ModeloDeAparicao(MTMRegiao regiao)
        {
            var candidatas = new List<MTMRegiao> { regiao };
            candidatas.AddRange(regioes);
            foreach (var r in candidatas)
                if (r != null && r.aparicoes != null)
                    foreach (var go in r.aparicoes)
                        if (go != null && go.GetComponentInChildren<SpriteRenderer>(true) != null)
                            return go;
            return null;
        }

        // Monta a lenda na cena. Ordem de preferência: o personagem de aparição do projeto
        // (modelo "aparicao"), a arte indicada em "imagem" e, por fim, uma silhueta simples.
        private void MontarLenda(MTMConfrontoDados c, MTMRegiao regiao)
        {
            sombraInimigo = null;
            quadrosLenda = quadrosInvestida = quadrosPreparo = null;
            poseParadaLenda = string.IsNullOrEmpty(c.imagemParado) ? null : CarregarSpriteLenda(c.imagemParado, c.alturaQuadro > 0f ? c.alturaQuadro : 1.85f);
            tempoLenda = 0f;
            linhaLenda = 0;
            direcaoLenda = Vector2.down;
            posicaoAnteriorLenda = Vector2.zero;
            var referencia = ModeloDeAparicao(regiao);
            var visualReferencia = referencia == null ? null : referencia.GetComponent<MTMVisualV2>();
            if (c.modelo == "aparicao" && referencia != null)
            {
                inimigoConfronto = Instantiate(referencia, regiao.transform);
                inimigoConfronto.name = "Lenda · " + c.id;
                inimigoConfronto.hideFlags = HideFlags.DontSave;
                // O personagem serve de enfeite na cena; aqui ele não pode barrar o caminho nem abrir conversa.
                foreach (var colisor in inimigoConfronto.GetComponentsInChildren<Collider2D>(true))
                    colisor.enabled = false;
                foreach (var ponto in inimigoConfronto.GetComponentsInChildren<MTMPonto>(true))
                    Destroy(ponto);
                inimigoConfronto.SetActive(true);
                var visual = inimigoConfronto.GetComponent<MTMVisualV2>();
                desenhoInimigo = visual != null && visual.desenho != null ? visual.desenho : inimigoConfronto.GetComponentInChildren<SpriteRenderer>(true);
                sombraInimigo = visual == null ? null : visual.sombra;
            }
            else
            {
                bool arteDoProjeto;
                var sprite = SpriteLenda(c, out arteDoProjeto);
                quadrosInvestida = FolhaExtraLenda(c, c.imagemInvestida, "investida");
                quadrosPreparo = FolhaExtraLenda(c, c.imagemPreparo, "preparo");
                inimigoConfronto = new GameObject("Lenda · " + c.id) { hideFlags = HideFlags.DontSave };
                inimigoConfronto.transform.SetParent(regiao.transform, false);
                var desenho = new GameObject("Desenho") { hideFlags = HideFlags.DontSave };
                desenho.transform.SetParent(inimigoConfronto.transform, false);
                desenhoInimigo = desenho.AddComponent<SpriteRenderer>();
                desenhoInimigo.sprite = sprite;
                desenhoInimigo.spriteSortPoint = SpriteSortPoint.Pivot;
                desenho.AddComponent<MTMOrdemY>();
                // Com arte de verdade, usa o mesmo material e a mesma sombra dos outros personagens.
                if (arteDoProjeto && visualReferencia != null && visualReferencia.desenho != null)
                {
                    desenhoInimigo.sharedMaterial = visualReferencia.desenho.sharedMaterial;
                    if (visualReferencia.sombra != null)
                    {
                        var sombra = new GameObject("Sombra") { hideFlags = HideFlags.DontSave };
                        sombra.transform.SetParent(inimigoConfronto.transform, false);
                        sombra.transform.localScale = visualReferencia.sombra.transform.localScale;
                        sombraInimigo = sombra.AddComponent<SpriteRenderer>();
                        sombraInimigo.sprite = sprite;
                        sombraInimigo.sharedMaterial = visualReferencia.sombra.sharedMaterial;
                        sombraInimigo.color = visualReferencia.sombra.color;
                        sombraInimigo.sortingLayerID = visualReferencia.sombra.sortingLayerID;
                        sombraInimigo.sortingOrder = visualReferencia.sombra.sortingOrder;
                    }
                }
            }

            float escala = c.escala > 0f ? c.escala : 1f;
            escalaInimigo = inimigoConfronto.transform.localScale * escala;
        }

        // Corta a folha em quadros iguais; a linha 0 fica no alto da imagem.
        private Sprite CortarFolhaLenda(MTMConfrontoDados c, Texture2D arte)
        {
            colunasLenda = c.colunas;
            linhasLenda = c.linhas;
            quadrosLenda = CortarFolha(c, arte, "corrida");
            return quadrosLenda[0];
        }

        private Sprite[] CortarFolha(MTMConfrontoDados c, Texture2D arte, string nome)
        {
            int w = arte.width / c.colunas, h = arte.height / c.linhas;
            float alturaMundo = c.alturaQuadro > 0f ? c.alturaQuadro : 1.85f;
            var quadros = new Sprite[c.colunas * c.linhas];
            for (int linha = 0; linha < c.linhas; linha++)
                for (int coluna = 0; coluna < c.colunas; coluna++)
                {
                    var rect = new Rect(coluna * w, arte.height - (linha + 1) * h, w, h);
                    var quadro = Sprite.Create(arte, rect, new Vector2(.5f, .03f), h / alturaMundo, 0, SpriteMeshType.FullRect);
                    quadro.name = c.id + "_" + nome + "_" + linha + "_" + coluna;
                    quadro.hideFlags = HideFlags.DontSave;
                    quadros[linha * c.colunas + coluna] = quadro;
                }

            return quadros;
        }

        // Folha extra (investida ou preparo) na mesma grade da folha principal.
        private Sprite[] FolhaExtraLenda(MTMConfrontoDados c, string caminho, string nome)
        {
            if (string.IsNullOrEmpty(caminho) || quadrosLenda == null)
                return null;
            var textura = Resources.Load<Texture2D>(caminho);
            if (textura == null)
            {
                var inteira = Resources.Load<Sprite>(caminho);
                textura = inteira == null ? null : inteira.texture;
            }

            if (textura == null)
            {
                Debug.Log("Lenda: a folha '" + caminho + "' ainda não está em Resources; usando a corrida.", this);
                return null;
            }

            return CortarFolha(c, textura, nome);
        }

        // Escolhe o quadro: a linha segue a direção do movimento e o ritmo segue o estado.
        private void AnimarLenda(MTMSessaoConfronto s)
        {
            Vector2 posicao = new Vector2(s.Ix, s.Iy);
            Vector2 passo = posicao - posicaoAnteriorLenda;
            posicaoAnteriorLenda = posicao;
            if (s.Estado == MTMEstadoInimigo.Preparando || s.Estado == MTMEstadoInimigo.Investindo)
                direcaoLenda = new Vector2(s.DirX, s.DirY);
            else if (passo.sqrMagnitude > 1e-6f)
                direcaoLenda = Vector2.Lerp(direcaoLenda, passo.normalized, .2f);
            float ritmo;
            switch (s.Estado)
            {
                case MTMEstadoInimigo.Investindo:
                    ritmo = 1.6f;
                    break;
                case MTMEstadoInimigo.Rondando:
                    ritmo = 1f;
                    break;
                case MTMEstadoInimigo.Cansado:
                    ritmo = .35f;
                    break;
                default:
                    ritmo = 0f;
                    break;
            }

            if (s.Estado == MTMEstadoInimigo.Parado)
                direcaoLenda = Vector2.down;
            if (contandoConfronto)
            {
                // Na contagem ela fica parada, de frente para Neto.
                ritmo = 0f;
                Vector2 paraNeto = corpo.position - (centroConfronto + posicao);
                if (paraNeto.sqrMagnitude > .01f)
                    direcaoLenda = paraNeto.normalized;
            }

            if (linhasLenda == 8)
                linhaLenda = MTMConfrontos.LinhaDaDirecao(direcaoLenda.x, direcaoLenda.y);
            tempoLenda += (Time.unscaledTime < pausaLendaAte ? 0f : Time.deltaTime) * confrontoAtual.fps * ritmo;
            var folha = quadrosLenda;
            int coluna = ritmo <= 0f ? 0 : (int)tempoLenda % colunasLenda;
            if (contandoConfronto && quadrosPreparo != null)
            {
                // Empina uma vez durante a contagem e termina junto com o "Já!".
                folha = quadrosPreparo;
                coluna = Mathf.Clamp(Mathf.FloorToInt(ProgressoContagem * colunasLenda), 0, colunasLenda - 1);
            }
            else if (s.Estado == MTMEstadoInimigo.Investindo && quadrosInvestida != null)
                folha = quadrosInvestida;
            else if (s.Estado == MTMEstadoInimigo.Preparando && quadrosPreparo != null)
            {
                // Toca uma vez, junto com a faixa: termina de empinar quando ela parte.
                folha = quadrosPreparo;
                coluna = Mathf.Clamp(Mathf.FloorToInt(s.ProgressoAviso * colunasLenda), 0, colunasLenda - 1);
            }

            desenhoInimigo.sprite = folha[linhaLenda * colunasLenda + coluna];
            desenhoInimigo.flipX = false;
            if (sombraInimigo != null)
            {
                sombraInimigo.sprite = desenhoInimigo.sprite;
                sombraInimigo.flipX = false;
            }
        }

        // Usa a arte indicada em "imagem" (Resources); sem ela, desenha uma silhueta simples.
        private Sprite SpriteLenda(MTMConfrontoDados c, out bool arteDoProjeto)
        {
            arteDoProjeto = true;
            if (!string.IsNullOrEmpty(c.imagem))
            {
                bool folha = c.colunas > 1 || c.linhas > 1;
                // O Unity importa o PNG como sprite: carregar como Sprite traria a folha inteira
                // num quadro só. Para folhas, usa sempre a textura e corta aqui.
                if (folha)
                {
                    var textura = Resources.Load<Texture2D>(c.imagem);
                    if (textura == null)
                    {
                        var inteira = Resources.Load<Sprite>(c.imagem);
                        textura = inteira == null ? null : inteira.texture;
                    }

                    if (textura != null)
                        return CortarFolhaLenda(c, textura);
                }

                var spritePronto = folha ? null : Resources.Load<Sprite>(c.imagem);
                if (spritePronto != null)
                    return spritePronto;
                var arte = folha ? null : Resources.Load<Texture2D>(c.imagem);
                if (arte != null)
                    return Sprite.Create(arte, new Rect(0, 0, arte.width, arte.height), new Vector2(.5f, .03f), arte.height / 1.85f);
                Debug.Log("Confronto '" + c.id + "': a arte '" + c.imagem + "' ainda não está em Resources. Usando a silhueta provisória.", this);
            }

            arteDoProjeto = false;

            const int w = 64, h = 96;
            var pixels = new Color[w * h];
            Action<float, float, float, float, Color> elipse = (cx, cy, rx, ry, cor) =>
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        float a = Mathf.Clamp01((1f - (dx * dx + dy * dy)) * Mathf.Min(rx, ry) * .6f);
                        if (a <= 0f)
                            continue;
                        var antes = pixels[y * w + x];
                        var nova = Color.Lerp(antes, cor, a);
                        nova.a = Mathf.Max(antes.a, a * cor.a);
                        pixels[y * w + x] = nova;
                    }
            };
            float alturaMundo;
            if (c.tipo == "luta")
            {
                // Figura alta, de cabeça comprida e crina.
                var corpoCor = new Color(.10f, .13f, .17f, 1f);
                var crina = new Color(.30f, .26f, .36f, 1f);
                elipse(32, 30, 16, 28, corpoCor);
                elipse(32, 60, 8, 12, corpoCor);
                elipse(24, 62, 7, 20, crina);
                elipse(36, 76, 9, 13, corpoCor);
                elipse(30, 90, 3, 6, corpoCor);
                elipse(40, 90, 3, 6, corpoCor);
                elipse(34, 79, 2, 2, new Color(1f, .86f, .5f, 1f));
                elipse(41, 79, 2, 2, new Color(1f, .86f, .5f, 1f));
                alturaMundo = 2.3f;
            }
            else
            {
                // Figura pequena, de cabelo em chama.
                var fogo = new Color(.96f, .45f, .12f, 1f);
                elipse(32, 24, 12, 20, new Color(.27f, .17f, .10f, 1f));
                elipse(22, 66, 6, 12, fogo);
                elipse(32, 71, 7, 15, fogo);
                elipse(42, 66, 6, 12, fogo);
                elipse(32, 52, 11, 11, new Color(.46f, .29f, .17f, 1f));
                elipse(28, 53, 2, 2, new Color(.8f, 1f, .5f, 1f));
                elipse(37, 53, 2, 2, new Color(.8f, 1f, .5f, 1f));
                alturaMundo = 1.6f;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Lenda_" + c.id, hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, 0f), h / alturaMundo);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        // Fogueira desenhada por código: lenha cruzada e, quando acesa, chama e clarão.
        private Sprite SpriteFogueira(bool acesa)
        {
            if (acesa && fogueiraAcesa != null)
                return fogueiraAcesa;
            if (!acesa && fogueiraApagada != null)
                return fogueiraApagada;
            const int n = 96;
            var pixels = new Color[n * n];
            Action<int, int, Color, float> pintar = (x, y, cor, a) =>
            {
                if (a <= 0f)
                    return;
                var antes = pixels[y * n + x];
                var nova = Color.Lerp(antes, cor, Mathf.Clamp01(a));
                nova.a = Mathf.Max(antes.a, Mathf.Clamp01(a) * cor.a);
                pixels[y * n + x] = nova;
            };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    if (acesa)
                    {
                        // Clarão no chão.
                        float gx = (x - 48f) / 44f, gy = (y - 26f) / 22f;
                        pintar(x, y, new Color(1f, .55f, .15f, .30f), 1f - (gx * gx + gy * gy));
                    }

                    // Duas toras cruzadas.
                    float t1 = Mathf.Abs((y - 20f) - (x - 48f) * .22f), t2 = Mathf.Abs((y - 20f) + (x - 48f) * .22f);
                    if (Mathf.Abs(x - 48f) < 28f)
                    {
                        float tora = Mathf.Min(t1, t2);
                        pintar(x, y, new Color(.20f, .12f, .07f, 1f), 7.5f - tora);
                        pintar(x, y, acesa ? new Color(.62f, .30f, .12f, 1f) : new Color(.38f, .25f, .15f, 1f), 5f - tora);
                    }

                    if (!acesa)
                        continue;
                    // Chama em forma de gota, mais clara no miolo.
                    float t = (y - 22f) / 66f;
                    if (t < 0f || t > 1f)
                        continue;
                    float largura = 21f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, .62f)), .9f);
                    float d = Mathf.Abs(x - 48f - 3f * Mathf.Sin(t * 5f)) / Mathf.Max(.01f, largura);
                    if (d >= 1f)
                        continue;
                    pintar(x, y, new Color(.86f, .22f, .08f, 1f), (1f - d) * 4f);
                    pintar(x, y, new Color(1f, .58f, .12f, 1f), (.78f - d) * 4f * (1f - t * .3f));
                    pintar(x, y, new Color(1f, .93f, .62f, 1f), (.42f - d - t * .35f) * 5f);
                }

            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = acesa ? "Fogueira_acesa" : "Fogueira_apagada", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .14f), 64f);
            sprite.hideFlags = HideFlags.DontSave;
            if (acesa)
                fogueiraAcesa = sprite;
            else
                fogueiraApagada = sprite;
            return sprite;
        }

        // Estrela de oito pontas, longas e curtas alternadas: o "pá!" do golpe recebido.
        private Texture2D TexturaEstaloHUD()
        {
            if (texEstaloHUD != null)
                return texEstaloHUD;
            const int n = 96;
            var p = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                    float ponta = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 4f)), 7f);
                    float comprida = Mathf.Lerp(.68f, .98f, Mathf.Abs(Mathf.Cos(ang * 2f)));
                    float borda = Mathf.Lerp(.36f, comprida, ponta);
                    p[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((borda - r) * n * .5f));
                }

            texEstaloHUD = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Estalo_golpe", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texEstaloHUD.SetPixels(p);
            texEstaloHUD.Apply(false, true);
            return texEstaloHUD;
        }

        // ---------- HUD ----------

        private Vector2 HudDoMundo(Vector2 mundo)
        {
            return CoordenadaHUDCompacta(cameraDoMapa.WorldToScreenPoint(new Vector3(mundo.x, mundo.y, 0f)));
        }

        private void ConfrontoHUD(Color fundo, Color claro, Color dourado)
        {
            if (!EmConfronto || cameraDoMapa == null)
                return;
            var c = confrontoAtual;
            var s = sessaoConfronto;
            Color perigo = PerigoHUD;
            float escala = Vector2.Distance(HudDoMundo(centroConfronto), HudDoMundo(centroConfronto + Vector2.right));
            Vector2 inimigo = centroConfronto + new Vector2(s.Ix, s.Iy);
            Color antiga = GUI.color;
            Matrix4x4 matriz = GUI.matrix;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;

            // Limite da arena.
            Vector2 meio = HudDoMundo(centroConfronto);
            float lado = c.raio * 2f * escala * 1.14f;
            GUI.color = new Color(claro.r, claro.g, claro.b, .10f);
            GUI.DrawTexture(new Rect(meio.x - lado * .5f, meio.y - lado * .5f, lado, lado), TexturaAnelVivo());
            GUI.color = antiga;

            // Aviso da investida: faixa com setas andando e borda tracejada; firma quando a mira trava.
            if (s.Estado == MTMEstadoInimigo.Preparando)
            {
                PrepararTexturasHUD();
                Vector2 a = HudDoMundo(inimigo);
                Vector2 b = HudDoMundo(inimigo + new Vector2(s.DirX, s.DirY) * MTMSessaoConfronto.AlcanceInvestida);
                float largura = MTMSessaoConfronto.RaioAcerto * 2f * escala;
                float comprimento = Vector2.Distance(a, b);
                float firme = s.MiraTravada ? 1f : .45f;
                GirarEmVolta(Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, a);
                Rect faixa = new Rect(a.x, a.y - largura * .5f, comprimento, largura);
                Fundo(faixa, new Color(perigo.r, perigo.g, perigo.b, .22f * firme + .06f));
                Fundo(new Rect(faixa.x, faixa.y, comprimento * s.ProgressoAviso, largura), new Color(perigo.r, perigo.g, perigo.b, .18f));
                GUI.color = new Color(1f, .78f, .68f, .75f * firme);
                float passo = Mathf.Max(18f, largura * .9f);
                float corre = reduzir ? 0f : Time.unscaledTime * 1.6f;
                GUI.DrawTextureWithTexCoords(new Rect(faixa.x + 4, faixa.y + largura * .15f, comprimento * Mathf.Max(.15f, s.ProgressoAviso) - 8, largura * .7f),
                    texSetasHUD, new Rect(-corre, 0, (comprimento * Mathf.Max(.15f, s.ProgressoAviso) - 8) / passo, 1));
                GUI.color = antiga;
                var borda = new Color(1f, .55f, .43f, .9f * firme);
                TracoHUD(new Rect(faixa.x, faixa.y, faixa.width, 2), borda, false);
                TracoHUD(new Rect(faixa.x, faixa.yMax - 2, faixa.width, 2), borda, false);
                TracoHUD(new Rect(faixa.xMax - 2, faixa.y, 2, faixa.height), borda, true);
                GUI.matrix = matriz;
            }

            // Arco do golpe (sem arma; com arma, o rastro é desenhado no mundo).
            if (Time.unscaledTime < golpeAte && desenhoArma == null)
            {
                Vector2 p = HudDoMundo(corpo.position + frenteJogador * (c.alcanceGolpe * .55f) + Vector2.up * .6f);
                float tamanho = c.alcanceGolpe * escala * 1.2f;
                GUI.color = new Color(claro.r, claro.g, claro.b, Mathf.Clamp01((golpeAte - Time.unscaledTime) / .18f));
                GUI.DrawTexture(new Rect(p.x - tamanho * .5f, p.y - tamanho * .5f, tamanho, tamanho), TexturaAnelVivo());
                GUI.color = antiga;
            }

            // Números das fogueiras, em discos.
            int fogueiraPerto = -1;
            if (s.Fase == MTMFaseConfronto.Cerco && c.fogueiras != null)
            {
                fogueiraPerto = FogueiraPerto(corpo.position - centroConfronto);
                for (int i = 0; i < c.fogueiras.Length; i++)
                {
                    Vector2 p = HudDoMundo(centroConfronto + new Vector2(c.fogueiras[i].x, c.fogueiras[i].y) + Vector2.up * 1.25f);
                    Rect r = new Rect(p.x - 14, p.y - 14, 28, 28);
                    bool acesa = s.Acesas.Contains(i);
                    bool proxima = i == fogueiraPerto && !acesa;
                    DiscoHUD(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), proxima ? dourado : new Color(0, 0, 0, .55f));
                    DiscoHUD(r, acesa ? dourado : TintaAtualHUD);
                    TextoTecido(r, (i + 1).ToString(), 14, acesa ? TintaAtualHUD : claro, true, TextAnchor.MiddleCenter);
                }
            }

            // Fôlego sob os pés do Neto: os olhos já estão nele durante a luta.
            Vector2 pes = HudDoMundo(corpo.position + Vector2.down * .25f);
            bool pisca = s.JogadorInvulneravel && !reduzir && Mathf.Repeat(Time.unscaledTime * 8f, 1f) < .5f;
            float larguraFolego = c.vidaJogador * 15f - 3f;
            for (int i = 0; i < c.vidaJogador; i++)
            {
                Rect l = new Rect(pes.x - larguraFolego * .5f + i * 15f, pes.y + 4f, 12, 12);
                LosangoCompacta(new Rect(l.x - 2, l.y - 2, l.width + 4, l.height + 4), new Color(.08f, .06f, .05f, .85f));
                LosangoCompacta(l, i < s.VidaJogador ? (pisca ? claro : dourado) : new Color(claro.r, claro.g, claro.b, .22f));
            }

            // O losango de fôlego perdido estoura para cima, em vermelho, e some.
            float perdido = (Time.unscaledTime - netoAtingidoInicio) / .6f;
            if (folegoPerdido >= 0 && folegoPerdido < c.vidaJogador && perdido >= 0f && perdido < 1f)
            {
                Vector2 centroPerdido = new Vector2(pes.x - larguraFolego * .5f + folegoPerdido * 15f + 6f, pes.y + 10f - (reduzir ? 0f : 14f * perdido));
                float ladoPerdido = 12f * (reduzir ? 1.3f : 1f + 1.2f * perdido);
                LosangoCompacta(new Rect(centroPerdido.x - ladoPerdido * .5f, centroPerdido.y - ladoPerdido * .5f, ladoPerdido, ladoPerdido),
                    new Color(perigo.r, perigo.g, perigo.b, .95f * (1f - perdido)));
            }

            // Estalo no peito de Neto no instante do golpe.
            float estalo = (Time.unscaledTime - netoAtingidoInicio) / .26f;
            if (estalo >= 0f && estalo < 1f)
            {
                Vector2 peito = HudDoMundo(corpo.position + Vector2.up * .9f);
                float tamanhoEstalo = escala * (reduzir ? 1.1f : .8f + .6f * Mathf.Sqrt(estalo));
                float miolo = tamanhoEstalo * .58f, alfaEstalo = 1f - estalo * estalo;
                var textura = TexturaEstaloHUD();
                if (!reduzir)
                    GirarEmVolta(anguloEstalo + estalo * 20f, peito);
                GUI.color = new Color(perigo.r, perigo.g, perigo.b, alfaEstalo);
                GUI.DrawTexture(new Rect(peito.x - tamanhoEstalo * .5f, peito.y - tamanhoEstalo * .5f, tamanhoEstalo, tamanhoEstalo), textura);
                GUI.color = new Color(1f, .95f, .82f, alfaEstalo);
                GUI.DrawTexture(new Rect(peito.x - miolo * .5f, peito.y - miolo * .5f, miolo, miolo), textura);
                GUI.color = antiga;
                GUI.matrix = matriz;
            }

            // Clarão ao ser atingido.
            if (Time.unscaledTime < danoAte)
                Fundo(new Rect(0, 0, 1280, 720), new Color(perigo.r, perigo.g, perigo.b, .24f * Mathf.Clamp01((danoAte - Time.unscaledTime) / .35f)));

            BarraDaLendaHUD(c, s, claro, dourado, perigo);
            if (contandoConfronto)
                ContagemHUD(c, claro, dourado, perigo);
            else if (encerrandoConfronto)
                FimConfrontoHUD(claro, dourado, perigo);
            else
                ControlesConfrontoHUD(c, s, fogueiraPerto, inimigo);
        }

        // Nome entre losangos e fios, como abertura de cordel, e a barra logo abaixo.
        private void BarraDaLendaHUD(MTMConfrontoDados c, MTMSessaoConfronto s, Color claro, Color dourado, Color perigo)
        {
            string nome = (c.nome ?? "").ToUpperInvariant();
            float larguraNome = Mathf.Min(LarguraTextoHUD(nome, 21, true), 420f);
            float meio = 640f, linhaY = 33f;
            Fundo(new Rect(meio - 300f, linhaY, 300f - larguraNome * .5f - 26f, 1), AlfaTecido(dourado, .8f));
            Fundo(new Rect(meio + larguraNome * .5f + 26f, linhaY, 300f - larguraNome * .5f - 26f, 1), AlfaTecido(dourado, .8f));
            LosangoCompacta(new Rect(meio - larguraNome * .5f - 20f, linhaY - 5f, 11, 11), dourado);
            LosangoCompacta(new Rect(meio + larguraNome * .5f + 9f, linhaY - 5f, 11, 11), dourado);
            TextoSombraHUD(new Rect(meio - larguraNome * .5f - 2f, 18, larguraNome + 4f, 30), nome, 21, claro, true, TextAnchor.MiddleCenter);

            if (s.Fase == MTMFaseConfronto.Luta)
            {
                // Vida em segmentos: cada golpe certo apaga um.
                int total = Mathf.Max(1, c.vidaInimigo);
                Rect barra = new Rect(380, 50, 520, 28);
                PainelHUD(barra);
                float gap = 3f, interno = barra.width - 16f;
                float seg = (interno - gap * (total - 1)) / total;
                bool brilho = Time.unscaledTime < brilhoInimigoAte;
                for (int i = 0; i < total; i++)
                {
                    Rect r = new Rect(barra.x + 8 + i * (seg + gap), barra.y + 8, seg, 12);
                    bool cheio = i < s.VidaInimigo;
                    ArredondadoHUD(r, cheio ? (brilho && i == s.VidaInimigo - 1 ? new Color(1f, .85f, .7f) : perigo) : new Color(claro.r, claro.g, claro.b, .14f));
                    if (cheio)
                        Fundo(new Rect(r.x + 2, r.y + 2, r.width - 4, 2), new Color(1f, 1f, 1f, .22f));
                }
            }
            else
                EtapasDoEncantoHUD(c, s, claro, dourado);

            // Estado do momento numa pílula sob a barra.
            string estadoTexto = null;
            Color corEstado = perigo;
            if (s.Estado == MTMEstadoInimigo.Preparando || s.Estado == MTMEstadoInimigo.Investindo)
                estadoTexto = "Saia da frente!";
            else if (s.Fase == MTMFaseConfronto.Luta && s.Estado == MTMEstadoInimigo.Cansado)
            {
                estadoTexto = "Ela cansou: golpeie agora!";
                corEstado = new Color(.80f, .60f, .26f, 1f);
            }

            if (estadoTexto != null)
            {
                float w = LarguraTextoHUD(estadoTexto, 14, true) + 46f;
                Rect pilula = new Rect(640 - w * .5f, s.Fase == MTMFaseConfronto.Luta ? 86 : 100, w, 28);
                ArredondadoHUD(new Rect(pilula.x, pilula.y + 2, pilula.width, pilula.height), new Color(0, 0, 0, .35f));
                ArredondadoHUD(pilula, corEstado);
                DiscoHUD(new Rect(pilula.x + 10, pilula.y + 6, 16, 16), Color.white);
                TextoTecido(new Rect(pilula.x + 10, pilula.y + 6, 16, 16), "!", 12, corEstado, true, TextAnchor.MiddleCenter);
                TextoTecido(new Rect(pilula.x + 32, pilula.y, w - 40, pilula.height), estadoTexto, 14, Color.white, true, TextAnchor.MiddleLeft);
            }
        }

        // Curupira: três etapas ligadas por um fio, com o progresso da etapa atual.
        private void EtapasDoEncantoHUD(MTMConfrontoDados c, MTMSessaoConfronto s, Color claro, Color dourado)
        {
            Rect painel = new Rect(420, 46, 440, 50);
            PainelHUD(painel);
            string[] nomes = { "Esquiva", "Cerco", "Escuta" };
            int atualFase = s.Fase == MTMFaseConfronto.Esquiva ? 0 : s.Fase == MTMFaseConfronto.Cerco ? 1 : 2;
            float progresso = atualFase == 0 ? s.EsquivasFeitas / (float)Mathf.Max(1, c.esquivas)
                : atualFase == 1 ? s.Acesas.Count / (float)Mathf.Max(1, c.fogueiras == null ? 1 : c.fogueiras.Length)
                : s.EscutaAcumulada / Mathf.Max(.1f, c.escuta);
            string contagem = atualFase == 0 ? s.EsquivasFeitas + "/" + c.esquivas
                : atualFase == 1 ? s.Acesas.Count + "/" + (c.fogueiras == null ? 0 : c.fogueiras.Length) : "";
            float x0 = painel.x + 72f, passo = (painel.width - 144f) / 2f, y = painel.y + 17f;
            for (int i = 0; i < 2; i++)
            {
                float ini = x0 + i * passo + 12f, fim = x0 + (i + 1) * passo - 12f;
                Fundo(new Rect(ini, y - 1, fim - ini, 2), AlfaTecido(claro, .2f));
                float cheio = i < atualFase ? 1f : i == atualFase ? Mathf.Clamp01(progresso) : 0f;
                if (cheio > 0f)
                    Fundo(new Rect(ini, y - 1, (fim - ini) * cheio, 2), dourado);
            }

            for (int i = 0; i < 3; i++)
            {
                float cx = x0 + i * passo;
                Rect no = new Rect(cx - 10, y - 10, 20, 20);
                if (i < atualFase)
                {
                    DiscoHUD(no, dourado);
                    IconeTecido("check", new Rect(no.x + 4, no.y + 4, 12, 12), TintaAtualHUD);
                }
                else if (i == atualFase)
                {
                    DiscoHUD(new Rect(no.x - 4, no.y - 4, 28, 28), AlfaTecido(dourado, .25f));
                    DiscoHUD(no, dourado);
                    DiscoHUD(new Rect(no.x + 3, no.y + 3, 14, 14), TintaAtualHUD);
                }
                else
                {
                    DiscoHUD(no, AlfaTecido(claro, .3f));
                    DiscoHUD(new Rect(no.x + 2, no.y + 2, 16, 16), TintaAtualHUD);
                }

                string rotulo = i == atualFase && contagem.Length > 0 ? nomes[i] + " " + contagem : nomes[i];
                TextoTecido(new Rect(cx - 60, y + 12, 120, 16), rotulo, 12, i == atualFase ? claro : AlfaTecido(claro, .6f), i == atualFase, TextAnchor.MiddleCenter);
            }
        }

        // Teclas que valem agora, no rodapé.
        private void ControlesConfrontoHUD(MTMConfrontoDados c, MTMSessaoConfronto s, int fogueiraPerto, Vector2 inimigo)
        {
            string[] teclas, rotulos;
            switch (s.Fase)
            {
                case MTMFaseConfronto.Luta:
                    teclas = new[] { "Espaço", "Shift" };
                    rotulos = new[] { string.IsNullOrEmpty(c.nomeArma) ? "Golpear" : "Golpear com o " + c.nomeArma, "Correr" };
                    break;
                case MTMFaseConfronto.Cerco:
                    teclas = new[] { "E", "Shift" };
                    rotulos = new[] { fogueiraPerto >= 0 && !s.Acesas.Contains(fogueiraPerto) ? "Acender a fogueira " + (fogueiraPerto + 1) : "Acender (de trás para frente)", "Correr" };
                    break;
                case MTMFaseConfronto.Escuta:
                    bool pertoDele = (corpo.position - inimigo).magnitude <= MTMSessaoConfronto.AlcanceEscuta;
                    teclas = new[] { "L" };
                    rotulos = new[] { pertoDele ? "Segure para ouvir" : "Chegue perto e segure para ouvir" };
                    break;
                default:
                    teclas = new[] { "Shift" };
                    rotulos = new[] { "Correr e escapar" };
                    break;
            }

            float largura = DicasHUD(0, 0, teclas, rotulos, true) + 36f;
            Rect r = new Rect(640 - largura * .5f, 656, largura, 44);
            PainelHUD(r);
            DicasHUD(r.x + 18, r.y + 9, teclas, rotulos, false);
        }

        // ---------- preparação e encerramento ----------

        // Quem prepara o jogador está na margem esperando por este confronto (antes da primeira vitória).
        private bool GuiaRecebe(MTMConfrontoDados c)
        {
            return c != null && guiaConfronto != null && guiaDeQual == c.id && !estado.Tem(c.evento);
        }

        // O guia (ex.: Simão de Góis) espera na margem da arena enquanto o confronto está liberado e ainda não
        // foi vencido; fica durante a luta e, depois da vitória, até o jogador sair do lugar. Enquanto ele
        // recebe o jogador, o ponto da luta fica ao lado dele: falar com ele é o que começa o encontro.
        private void AtualizarGuiaConfronto()
        {
            if (!string.IsNullOrEmpty(guiaFicaAte) && (atual == null || atual.dados == null || atual.dados.id != guiaFicaAte))
                guiaFicaAte = "";
            MTMConfrontoDados quem = null;
            foreach (var p in marcadoresConfronto.Keys)
            {
                MTMConfrontoDados c;
                if (p == null || p.dados == null || !confrontosPorPonto.TryGetValue(p.dados.id, out c) || string.IsNullOrEmpty(c.guia))
                    continue;
                bool aqui = atual != null && atual.dados != null && atual.dados.id == c.regiao;
                bool lutando = EmConfronto && confrontoAtual != null && confrontoAtual.id == c.id;
                bool liberado = (string.IsNullOrEmpty(c.requer) || estado.Tem(c.requer)) && !estado.Tem(c.evento);
                if (aqui && (lutando || liberado || guiaFicaAte == c.regiao))
                {
                    quem = c;
                    break;
                }
            }

            if (guiaConfronto != null && (quem == null || guiaDeQual != quem.id))
            {
                Destroy(guiaConfronto);
                guiaConfronto = null;
                guiaDeQual = "";
            }

            if (quem != null && guiaConfronto == null)
            {
                guiaConfronto = CriarGuiaConfronto(quem);
                guiaDeQual = guiaConfronto != null ? quem.id : "";
            }

            foreach (var p in marcadoresConfronto.Keys)
            {
                MTMConfrontoDados c;
                if (p == null || p.dados == null || !confrontosPorPonto.TryGetValue(p.dados.id, out c))
                    continue;
                // Como nos moradores, o ponto fica 0,35 acima dos pés de quem recebe o jogador.
                Vector3 lugar = GuiaRecebe(c) ? new Vector3(c.x + c.guiaX, c.y + c.guiaY + .35f, 0f) : new Vector3(c.x, c.y, 0f);
                if (p.transform.localPosition != lugar)
                    p.transform.localPosition = lugar;
            }
        }

        // Cópia do desenho e da sombra do personagem da cena (mesma arte e escala), sem conversa nem colisão.
        private GameObject CriarGuiaConfronto(MTMConfrontoDados c)
        {
            if (guiasSemModelo.Contains(c.id))
                return null;
            var regiao = RegiaoTecido(c.regiao);
            MTMPonto modelo = null;
            foreach (var r in regioes)
                if (r != null && r.pontos != null)
                    foreach (var p in r.pontos)
                        if (modelo == null && p != null && p.dados != null && p.dados.id == c.guia)
                            modelo = p;
            // A conversa dos moradores fica num filho do personagem; o desenho, no personagem.
            Transform raiz = modelo == null ? null
                : modelo.GetComponentInChildren<SpriteRenderer>(true) == null && modelo.transform.parent != null ? modelo.transform.parent : modelo.transform;
            var visual = raiz == null ? null : raiz.GetComponent<MTMVisualV2>();
            var desenho = visual != null && visual.desenho != null ? visual.desenho : raiz == null ? null : raiz.GetComponentInChildren<SpriteRenderer>(true);
            if (regiao == null || desenho == null || desenho.sprite == null)
            {
                guiasSemModelo.Add(c.id);
                Debug.LogWarning("Confronto '" + c.id + "': o personagem '" + c.guia + "' não foi achado na cena; o encontro começa sem ele.", this);
                return null;
            }

            var go = new GameObject("Guia · " + c.id) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(regiao.transform, false);
            go.transform.localPosition = new Vector3(c.x + c.guiaX, c.y + c.guiaY, 0f);
            go.transform.localScale = raiz.localScale;
            CopiarDesenhoDoGuia(desenho, raiz, go.transform, true);
            if (visual != null && visual.sombra != null && visual.sombra != desenho)
                CopiarDesenhoDoGuia(visual.sombra, raiz, go.transform, false);
            return go;
        }

        private static void CopiarDesenhoDoGuia(SpriteRenderer origem, Transform raiz, Transform destino, bool ordenar)
        {
            var go = new GameObject(origem.gameObject.name) { hideFlags = HideFlags.DontSave, layer = origem.gameObject.layer };
            go.transform.SetParent(destino, false);
            go.transform.localPosition = raiz.InverseTransformPoint(origem.transform.position);
            go.transform.localRotation = Quaternion.Inverse(raiz.rotation) * origem.transform.rotation;
            go.transform.localScale = origem.transform.localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = origem.sprite;
            sr.sharedMaterial = origem.sharedMaterial;
            sr.color = origem.color;
            sr.flipX = origem.flipX;
            sr.flipY = origem.flipY;
            sr.sortingLayerID = origem.sortingLayerID;
            sr.sortingOrder = origem.sortingOrder;
            sr.spriteSortPoint = origem.spriteSortPoint;
            sr.drawMode = origem.drawMode;
            if (origem.drawMode != SpriteDrawMode.Simple)
                sr.size = origem.size;
            if (ordenar && origem.GetComponent<MTMOrdemY>() != null)
                go.AddComponent<MTMOrdemY>();
        }

        private float ProgressoContagem
        {
            get { return Mathf.Clamp01((Time.unscaledTime - contagemInicio) / DuracaoContagem); }
        }

        // Número que a contagem mostra agora: 3, 2, 1 e 0 para o "Já!" (os últimos 0,6 s).
        private int NumeroContagem(out float dentro)
        {
            float t = Time.unscaledTime - contagemInicio;
            float passo = (DuracaoContagem - .6f) / 3f;
            if (t >= DuracaoContagem - .6f)
            {
                dentro = Mathf.Clamp01((t - (DuracaoContagem - .6f)) / .6f);
                return 0;
            }

            int i = Mathf.Clamp(Mathf.FloorToInt(t / passo), 0, 2);
            dentro = Mathf.Clamp01((t - i * passo) / passo);
            return 3 - i;
        }

        // Antes da luta: Neto entra na clareira e encara a lenda, que espera parada; ninguém ataca.
        private void AtualizarContagem(Vector2 rel)
        {
            Vector2 falta = largadaJogador - rel;
            movimento = falta.magnitude > .2f ? falta.normalized : Vector2.zero;
            multiplicador = 1f;
            Vector2 paraLenda = new Vector2(sessaoConfronto.Ix, sessaoConfronto.Iy) - rel;
            if (paraLenda.sqrMagnitude > .01f)
                frenteJogador = paraLenda.normalized;
            float dentro;
            int numero = NumeroContagem(out dentro);
            if (numero != numeroContagem)
            {
                numeroContagem = numero;
                if (numero > 0)
                    TocarSom("ui_clique", .9f, 0f);
                else
                    TocarSom(confrontoAtual.tipo == "luta" ? "empina" : "assobio", 1f, .02f);
            }

            if (Time.unscaledTime - contagemInicio >= DuracaoContagem)
                contandoConfronto = false;
        }

        // Último golpe da luta: a lenda recua e some (vitória) ou fica onde está (derrota), o aviso aparece
        // e só depois vem o diálogo do fim.
        private void IniciarEncerramento(bool vitoria)
        {
            encerrandoConfronto = true;
            fimComVitoria = vitoria;
            fimInicio = Time.unscaledTime;
            TocarSom(vitoria ? "vitoria" : "derrota", .9f, 0f);
            if (inimigoConfronto != null)
            {
                inimigoConfronto.transform.localScale = escalaInimigo;
                posicaoFimLenda = inimigoConfronto.transform.position;
                Vector2 longe = posicaoFimLenda - corpo.position;
                direcaoFimLenda = longe.sqrMagnitude > .01f ? longe.normalized : Vector2.up;
            }

            alfaSombraFim = sombraInimigo != null ? sombraInimigo.color.a : 1f;
        }

        private void AtualizarEncerramento()
        {
            float t = Time.unscaledTime - fimInicio;
            if (fimComVitoria && inimigoConfronto != null && desenhoInimigo != null)
            {
                float k = Mathf.Clamp01((t - .3f) / 1.6f);
                float suave = k * k * (3f - 2f * k);
                inimigoConfronto.transform.position = posicaoFimLenda + direcaoFimLenda * (3.2f * suave);
                if (quadrosLenda != null && colunasLenda > 0 && k > 0f)
                {
                    int linha = linhasLenda == 8 ? MTMConfrontos.LinhaDaDirecao(direcaoFimLenda.x, direcaoFimLenda.y) : 0;
                    int coluna = (int)(t * confrontoAtual.fps * 1.2f) % colunasLenda;
                    desenhoInimigo.sprite = quadrosLenda[linha * colunasLenda + coluna];
                    desenhoInimigo.flipX = false;
                    if (sombraInimigo != null)
                    {
                        sombraInimigo.sprite = desenhoInimigo.sprite;
                        sombraInimigo.flipX = false;
                    }
                }

                desenhoInimigo.color = new Color(1f, 1f, 1f, 1f - suave);
                if (sombraInimigo != null)
                {
                    var cs = sombraInimigo.color;
                    cs.a = alfaSombraFim * (1f - suave);
                    sombraInimigo.color = cs;
                }
            }

            if (t < (fimComVitoria ? DuracaoFimVitoria : DuracaoFimDerrota))
                return;
            encerrandoConfronto = false;
            if (fimComVitoria)
                VencerConfronto();
            else
                PerderConfronto();
        }

        private static Color ComAlfa(Color cor, float alfa)
        {
            cor.a *= alfa;
            return cor;
        }

        // Antes da luta: "Prepare-se" entre fios, a contagem (3, 2, 1, Já!) e os lembretes de como lutar.
        private void ContagemHUD(MTMConfrontoDados c, Color claro, Color dourado, Color perigo)
        {
            float dentro;
            int numero = NumeroContagem(out dentro);
            float t = Time.unscaledTime - contagemInicio;
            float a = Mathf.Clamp01(t / .25f) * (numero == 0 ? 1f - Mathf.Clamp01((dentro - .5f) / .5f) : 1f);
            if (a <= 0f)
                return;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            const float meio = 640f, y = 148f;
            const string titulo = "PREPARE-SE";
            float lt = LarguraTextoHUD(titulo, 22, true);
            Fundo(new Rect(meio - 230f, y + 15f, 230f - lt * .5f - 26f, 1), ComAlfa(dourado, .8f * a));
            Fundo(new Rect(meio + lt * .5f + 26f, y + 15f, 230f - lt * .5f - 26f, 1), ComAlfa(dourado, .8f * a));
            LosangoCompacta(new Rect(meio - lt * .5f - 20f, y + 10f, 11, 11), ComAlfa(dourado, a));
            LosangoCompacta(new Rect(meio + lt * .5f + 9f, y + 10f, 11, 11), ComAlfa(dourado, a));
            TextoSombraHUD(new Rect(meio - lt * .5f - 4f, y, lt + 8f, 30), titulo, 22, ComAlfa(dourado, a), true, TextAnchor.MiddleCenter);

            // Número grande: chega maior e assenta; o "Já!" sai na cor do perigo.
            Matrix4x4 matriz = GUI.matrix;
            float escala = reduzir ? 1f : Mathf.Lerp(1.5f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dentro * 2.5f)));
            GUIUtility.ScaleAroundPivot(new Vector2(escala, escala), GUI.matrix.MultiplyPoint3x4(new Vector3(meio, y + 92f, 0f)));
            TextoSombraHUD(new Rect(meio - 170f, y + 40f, 340f, 104f), numero == 0 ? "JÁ!" : numero.ToString(), 88,
                ComAlfa(numero == 0 ? perigo : claro, a), true, TextAnchor.MiddleCenter);
            GUI.matrix = matriz;

            if (c.regras == null || c.regras.Length == 0)
                return;
            float larg = 0f;
            foreach (var r in c.regras)
                larg = Mathf.Max(larg, LarguraTextoHUD(r, 16, false));
            larg = Mathf.Min(larg + 70f, 640f);
            Rect painel = new Rect(meio - larg * .5f, y + 158f, larg, 16f + c.regras.Length * 28f);
            PainelHUD(painel, a);
            for (int i = 0; i < c.regras.Length; i++)
            {
                float ly = painel.y + 8f + i * 28f;
                LosangoCompacta(new Rect(painel.x + 22f, ly + 9f, 10, 10), ComAlfa(dourado, a));
                TextoTecido(new Rect(painel.x + 42f, ly, larg - 54f, 28f), c.regras[i], 16, ComAlfa(claro, a), false, TextAnchor.MiddleLeft);
            }
        }

        // Fim da luta: "Vitória" ou "Sem fôlego" e "A luta terminou", antes do diálogo.
        private void FimConfrontoHUD(Color claro, Color dourado, Color perigo)
        {
            float dur = fimComVitoria ? DuracaoFimVitoria : DuracaoFimDerrota;
            float t = Time.unscaledTime - fimInicio;
            float a = Mathf.Clamp01((t - .15f) / .3f) * (1f - Mathf.Clamp01((t - (dur - .35f)) / .35f));
            if (a <= 0f)
                return;
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            Fundo(new Rect(0, 0, 1280, 720), new Color(0f, 0f, 0f, .26f * a));
            const float meio = 640f, y = 250f;
            Matrix4x4 matriz = GUI.matrix;
            float escala = reduzir ? 1f : Mathf.Lerp(1.25f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .15f) / .45f)));
            GUIUtility.ScaleAroundPivot(new Vector2(escala, escala), GUI.matrix.MultiplyPoint3x4(new Vector3(meio, y + 40f, 0f)));
            TextoSombraHUD(new Rect(meio - 380f, y, 760f, 84f), fimComVitoria ? "VITÓRIA" : "SEM FÔLEGO", 66,
                ComAlfa(fimComVitoria ? dourado : perigo, a), true, TextAnchor.MiddleCenter);
            GUI.matrix = matriz;
            const string sub = "A luta terminou";
            float ls = LarguraTextoHUD(sub, 20, true);
            float yl = y + 92f;
            Fundo(new Rect(meio - ls * .5f - 190f, yl + 14f, 160f, 1), ComAlfa(dourado, .8f * a));
            Fundo(new Rect(meio + ls * .5f + 30f, yl + 14f, 160f, 1), ComAlfa(dourado, .8f * a));
            LosangoCompacta(new Rect(meio - ls * .5f - 24f, yl + 9f, 11, 11), ComAlfa(dourado, a));
            LosangoCompacta(new Rect(meio + ls * .5f + 13f, yl + 9f, 11, 11), ComAlfa(dourado, a));
            TextoSombraHUD(new Rect(meio - ls * .5f - 4f, yl, ls + 8f, 28f), sub, 20, ComAlfa(claro, a), true, TextAnchor.MiddleCenter);
            if (!fimComVitoria)
                TextoSombraHUD(new Rect(meio - 300f, yl + 36f, 600f, 26f), "Respire e tente de novo.", 16, ComAlfa(claro, .9f * a), false, TextAnchor.MiddleCenter);
        }

        // ---------- Caderno ----------

        private void AdicionarAchadosConfronto(List<AchadoViva> lista)
        {
            if (confrontos == null)
                return;
            foreach (var c in confrontos.confrontos)
                if (estado.Tem(c.evento))
                    lista.Add(new AchadoViva
                    {
                        chave = c.id,
                        titulo = c.nome,
                        tipo = "LENDA DA NOITE",
                        texto = string.IsNullOrEmpty(c.resumo) ? string.Join("\n\n", c.vitoria) : c.resumo,
                        fio = c.fio
                    });
        }
    }
}

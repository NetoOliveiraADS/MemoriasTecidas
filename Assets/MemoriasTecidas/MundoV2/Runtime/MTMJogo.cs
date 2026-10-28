using System;
using System.IO;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [RequireComponent(typeof(Rigidbody2D)), DisallowMultipleComponent]
    public sealed partial class MTMJogo : MonoBehaviour
    {
        public Camera cameraDoMapa;
        public MTMRegiao[] regioes;
        public MTMAmbienteV2 ambienteVisual;
        [Range(5f,30f)] public float tamanhoCamera = 8f;
        [Min(1f)] public float velocidade = 3.5f;
        [Min(60f)] public float segundosPorDia = 720f;
        public bool carregarProgresso = true;
        public bool importarProgressoAnterior = true;
        public MTMEstado estado = new MTMEstado();
        private Rigidbody2D corpo;
        private Vector2 movimento;
        private Vector3 velocidadeCamera;
        private float multiplicador = 1f;
        private Texture2D retratoAtual;
        private Rect recorteAtual;
        private MTMRegiao atual;
        private MTMPonto perto;
        private string[] falas;
        private string locutor, contexto;
        private int pagina, quadroDaAcao = -1;
        private Action aoConcluir;
        private string painel = "", destinoMapa = "";
        private string aviso = "";
        private float avisoAte;
        private bool pronto, confirmarNovo, migracaoBloqueadaRoteiro;
        public bool Noite { get { return estado.relogio < .25f || estado.relogio >= .75f; } }
        private bool Bloqueado { get { return inicioAbertoUI || falas != null || painel.Length > 0; } }
        private string CaminhoSave { get { return Path.Combine(Application.persistentDataPath, "memorias-tecidas-mundo-v2.json"); } }

        private void Awake() { corpo = GetComponent<Rigidbody2D>(); PrepararApresentacao(); }
        private void Start()
        {
            if (cameraDoMapa == null || regioes == null || regioes.Length == 0)
            {
                Debug.LogError("Mundo V2 sem câmera ou regiões. Use o menu de criação de cena.", this);
                enabled = false; return;
            }
            PrepararMenusUI();
            if (carregarProgresso) Carregar();
            if(migracaoBloqueadaRoteiro){enabled=false;return;}
            PrepararTramaTecida();
            if(tramaTecida==null){enabled=false;return;}
            PrepararPrimeiroFio();
            PrepararKitHD2D();
            string inicio=RegiaoInicialPrimeiroFio();
            pronto = true;
            if (!RetomarJornadaSalva() && !IrParaPontoSeguro(inicio)) IrParaPontoSeguro("praca");
            var localFeliciana=MTMVinculosRegiao.RegiaoDoPonto(regioes,"npc_feliciana");
            string encontro=localFeliciana==null?"Consulte o caderno.":"Dona Feliciana: "+localFeliciana.dados.nome+".";
            Avisar("WASD/setas: caminhar. "+encontro+" E: conversar. J: caderno. M: mapa.");
        }

        private void Update()
        {
            if (!pronto) return;
            if(!possuiFoco){Parar();return;}
            if (MTMEntrada.Apertou('x')) { VoltarInterfaceUI(); return; }
            if(inicioAbertoUI || painel=="pausa" || painel=="visual" || painel=="ajuda" || painel=="sobre" || painel=="mapaCidade")
            { Parar(); return; }
            AtualizarRoteiro();
            // Digitar nomes na bolsa não aciona I/M/J/Q nem move o protagonista.
            if(painel=="bolsa"&&buscaBolsaEmFoco&&!MTMEntrada.Apertou('x')){Parar();return;}
            if (MTMEntrada.ApertouF1())
            {
                interfaceVisivel = !interfaceVisivel;
            }
            AjustarCamera();
            if (atual != null) atual.Ambiente(Noite, false);
            if (ambienteVisual != null && atual != null) ambienteVisual.Atualizar(atual.dados,estado.relogio);
            if (MTMEntrada.Apertou('q'))
            {
                MTMAmbienteV2.Qualidade=(MTMAmbienteV2.Qualidade+1)%3;
                Avisar("Qualidade dos efeitos: "+MTMAmbienteV2.Nomes[MTMAmbienteV2.Qualidade]);
            }
            if (falas != null)
            {
                Parar(); if (MTMEntrada.Apertou('e')&&!ConfirmacaoComFocoUI) Avancar(); return;
            }
            if(painel=="primeiro_fio"){if(!ConfirmacaoComFocoUI)TeclasPrimeiroFio();return;}
            if(painel=="atividade"||painel=="observacao"){Parar();return;}
            if (MTMEntrada.Apertou('m')) AlternarPainel("mapa");
            else if (MTMEntrada.Apertou('j')) AlternarPainel("diario");
            else if (MTMEntrada.Apertou('i') || MTMEntrada.Apertou('b')) AlternarPainel("bolsa");
            if (Bloqueado || ConfirmacaoComFocoUI) { Parar(); return; }
            estado.relogio += Time.deltaTime / segundosPorDia;
            if (estado.relogio >= 1f) { estado.relogio -= 1f; estado.dia++; }
            if (MTMEntrada.Apertou('r'))
            {
                Reposicionar(new Vector2(atual.dados.seguroX, atual.dados.seguroY)); Avisar("Você voltou ao caminho central desta região."); return;
            }
            movimento = MTMEntrada.Movimento();
            if(movimento.sqrMagnitude<.0001f&&corpo!=null)corpo.linearVelocity=Vector2.zero;
            multiplicador = MTMEntrada.Correndo()?1.55f:1f;
            perto = MaisProximo();
            if (MTMEntrada.Apertou('e') && perto != null && quadroDaAcao != Time.frameCount)
                Interagir(perto);
        }

        private void FixedUpdate() { MoverComSuavidade(); }
        private void LateUpdate() { AtualizarCameraFluida(); }
        private void OnDisable() { Parar(); }
        private void OnApplicationQuit() { SalvarPreferenciasInterface(); SalvarPreferenciasVisuais(); if (pronto && carregarProgresso) Salvar(); }
        private void OnApplicationPause(bool pausa) { if(pausa){Parar();SalvarPreferenciasInterface();SalvarPreferenciasVisuais();if(pronto&&carregarProgresso)Salvar();} }
        private void Parar() { movimento = Vector2.zero; if (corpo != null) corpo.linearVelocity = Vector2.zero; }

        private bool IrParaPontoSeguro(string id)
        {
            MTMRegiao alvo = Array.Find(regioes, r => r != null && r.dados.id == id);
            return alvo != null && IrPara(id, new Vector2(alvo.dados.seguroX, alvo.dados.seguroY));
        }

        private bool IrPara(string id, Vector2 chegada)
        {
            if(!RegiaoLiberadaRoteiro(id))
            {var b=Array.Find(tramaTecida.bloqueios,x=>x.regiao==id);Avisar(b==null?"Continue as missões para abrir este lugar.":b.mensagem);return false;}
            MTMRegiao alvo = Array.Find(regioes, r => r != null && r.dados.id == id);
            if (alvo == null) { Avisar("Essa ligação não tem região de destino. Confira mundo.json."); return false; }
            foreach (MTMRegiao r in regioes) if (r != null) r.gameObject.SetActive(r == alvo);
            atual = alvo; estado.area = id; perto = null; painel = "";
            bool chegouDestinoGuiado=destinoGuiadoTecido==id;
            if (chegouDestinoGuiado) destinoGuiadoTecido = "";
            MTMContatoCenario.Preparar(atual);
            PrepararAmostraPrimeiroFio();
            Reposicionar(chegada);
            estado.Concluir("visitou_" + id); Salvar();
            if(chegouDestinoGuiado)Avisar("Você chegou a "+NomeMapaGuia(id)+".");
            return true;
        }

        private void Reposicionar(Vector2 p)
        {
            Parar(); corpo.position = p; transform.position = new Vector3(p.x, p.y, 0f);
            Physics2D.SyncTransforms();CentralizarCamera();
        }

        private bool Disponivel(MTMPonto p)
        {
            if (p == null || p.dados == null || !p.gameObject.activeInHierarchy) return false;
            if(p.dados.id==IdAmostraPrimeiroFio)return aberturaMemorias!=null&&MTMVinculosRegiao.RegiaoAncestral(p.transform)==LocalPrimeiroFio();
            if(tramaTecida!=null)return DisponivelRoteiro(p);
            if (p.dados.noturno && (!Noite || !estado.Tem("andre"))) return false;
            return p.dados.tipo != "coleta" || !estado.Tem(p.dados.evento);
        }
        private MTMPonto MaisProximo()
        {
            MTMPonto melhor = null; float distancia = float.PositiveInfinity;
            if (atual == null || atual.pontos == null) return null;
            foreach (MTMPonto p in atual.pontos)
            {
                if (!Disponivel(p)) continue;
                float d = Vector2.Distance(corpo.position, p.transform.position);
                if (d <= p.alcance && d < distancia) { distancia = d; melhor = p; }
            }
            return melhor;
        }

        private void Interagir(MTMPonto ponto)
        {
            MTMPontoDados d=ponto.dados; retratoAtual=ponto.retrato; recorteAtual=ponto.recorteRosto;
            Parar(); quadroDaAcao = Time.frameCount;
            if (d.tipo == "saida") { IrPara(d.destino, new Vector2(d.chegadaX, d.chegadaY)); return; }
            if(InteragirPrimeiroFio(ponto))return;
            if(InteragirRoteiro(ponto))return;
            if (!string.IsNullOrEmpty(d.condicao) && !estado.Tem(d.condicao))
            {
                Abrir(d.nome, d.fonte, new[] { "Vamos continuar essa conversa depois desta etapa: " + ObjetivoTecido() }, null); return;
            }
            if (d.tipo == "descanso")
            {
                bool eraNoite = Noite;
                Abrir("Uma pausa na praça", "", new[] { eraNoite ? "Descansar até as 8h da manhã?" : "Descansar até as 20h para observar as histórias da noite?", "Confirme para descansar. Esc permite continuar explorando agora." }, () =>
                {
                    if (eraNoite && estado.relogio >= .75f) estado.dia++;
                    estado.relogio = eraNoite ? 8f / 24f : 20f / 24f; Salvar();
                }); return;
            }
            if (d.evento == "neto" && estado.rede)
            {
                Abrir(d.nome, MTMTextoDialogo.Assinatura, tramaTecida != null && tramaTecida.encerramento != null ? tramaTecida.encerramento : new[] { "A rede reuniu materiais, encontros e trabalho." }, () => Registrar("entrega_final")); return;
            }
            if (d.evento == "mercado_entrega" && !estado.Tem("mercado_entrega"))
            {
                if (!estado.Tem("pacote1") || !estado.Tem("pacote2") || !estado.Tem("pacote3"))
                {
                    Abrir(d.nome, d.fonte, new[] { "Preciso conferir três encomendas. Visite cada uma das três bancas aqui em frente ao mercado e depois volte.", "Você recebe os punhos para a rede quando as três encomendas estiverem conferidas." }, null); return;
                }
            }
            if (d.evento == "fiar" && !estado.Tem("fiar") && estado.algodao < 6)
            {
                Abrir(d.nome, d.fonte, new[] { "Vamos preparar três conjuntos de fios. Para esta atividade do jogo, traga seis algodões do sítio.", "Você trouxe " + estado.algodao + " de 6. O caminho do sítio sai do pátio da fábrica." }, null); return;
            }
            if (d.evento == "tecer" && !estado.rede && !estado.PodeTecelagem)
            {
                Abrir("Tear da oficina", "Receita simplificada para o jogo.", new[] { "Para começar: 3 fios, 1 corda, 1 pano, 1 conjunto de punhos e 1 enfeite.", "Fios: Rosa. Corda e pano: Cruzeiro. Punhos: mercado. Enfeite: coreto. Pressione I para conferir sua bolsa." }, null); return;
            }
            if (d.tipo == "coleta" && estado.Tem(d.evento)) return;
            if ((d.evento == "fiar" || d.evento == "tecer" || d.evento == "mercado_entrega") && estado.Tem(d.evento))
            {
                Abrir(d.nome, d.fonte, new[] { "Essa etapa já foi concluída. " + ObjetivoTecido() }, null); return;
            }
            Abrir(d.nome, d.fonte, d.falas, () => Registrar(d.evento));
        }

        private void Registrar(string evento)
        {
            if (estado.Concluir(evento)) { Salvar(); Avisar("Registro atualizado. " + ObjetivoTecido()); }
        }
        private void Abrir(string nome, string fonte, string[] linhas, Action concluir)
        {
            if (linhas == null || linhas.Length == 0) return;
            locutor = nome; contexto = MTMTextoDialogo.Assinatura; falas = linhas; pagina = 0; aoConcluir = concluir;
            linhaPreparadaDialogo=null;locutorPreparadoDialogo=null;
            quadroDaAcao = Time.frameCount; Parar();
        }
        private void Avancar()
        {
            if (falas == null || quadroDaAcao == Time.frameCount) return;
            quadroDaAcao = Time.frameCount;
            if (pagina + 1 < falas.Length) { pagina++; return; }
            Action acao = aoConcluir; falas = null; aoConcluir = null; acao?.Invoke();
        }
        private void AlternarPainel(string nome) { retornoColecaoUI=""; painel = painel == nome ? "" : nome; confirmarNovo = false; Parar(); }
        private void Avisar(string texto) { aviso = texto; avisoAte = Time.unscaledTime + 5f; }
        private void AjustarCamera()
        {
            if (Screen.height <= 0) return;
            cameraDoMapa.rect = new Rect(0f,0f,1f,1f);
        }
        private void Salvar()
        {
            if (!pronto || !carregarProgresso || !jornadaIniciadaUI) return;
            try
            {
                RegistrarPontoDaJornada();
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporario = CaminhoSave + ".tmp";
                File.WriteAllText(temporario, JsonUtility.ToJson(estado, true));
                File.Copy(temporario, CaminhoSave, true); File.Delete(temporario);
            }
            catch (Exception e) { Debug.LogWarning("Não foi possível salvar Mundo V2: " + e.Message, this); Avisar("O progresso desta sessão não pôde ser salvo."); }
        }
        private void Carregar()
        {
            string origem=CaminhoSave;
            if(!File.Exists(origem)&&importarProgressoAnterior)
                origem=Path.Combine(Application.persistentDataPath,"memorias-tecidas-mundo-v1.json");
            if (!File.Exists(origem)) return;
            try
            {
                MTMEstado s = JsonUtility.FromJson<MTMEstado>(File.ReadAllText(origem));
                if (s == null || s.versao != 1 || s.marcos == null || float.IsNaN(s.relogio) || float.IsInfinity(s.relogio)) throw new InvalidDataException("Formato de save inválido.");
                s.relogio = Mathf.Repeat(s.relogio, 1f); s.dia = Mathf.Max(1, s.dia);
                estado = s;
                if(s.versaoRoteiro<2&&origem==CaminhoSave)
                {
                    string reserva=CaminhoSave+".antes-roteiro24-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+".json";
                    try{File.Copy(CaminhoSave,reserva,false);Debug.Log("Backup do progresso anterior criado: "+reserva,this);}
                    catch(Exception backupErro){migracaoBloqueadaRoteiro=true;Debug.LogError("Não foi possível criar o backup do progresso. O jogo foi interrompido sem sobrescrever o save: "+backupErro.Message,this);}
                }
                if(origem!=CaminhoSave)Debug.Log("Mundo V2: progresso anterior importado. O arquivo do Mundo V1 foi preservado.",this);
            }
            catch (Exception e) { migracaoBloqueadaRoteiro=true;Debug.LogError("O save não pôde ser carregado e foi preservado sem alterações: " + e.Message, this); }
        }
        private void NovaJornada()
        {
            retomadaNoPontoSeguro=false;
            estado = new MTMEstado(); confirmarNovo = false; painel = ""; missaoFixadaTecido = ""; missaoSelecionadaTecido = ""; destinoMapa = ""; destinoGuiadoTecido = "";
            estado.versaoRoteiro=2;LimparAtividadeRoteiro();etapaEscolta=null;etapaAparicao=null;AtualizarVisibilidadeRoteiro();
            LimparPrimeiroFio();AtualizarAmostraPrimeiroFio();
            if(!IrParaPontoSeguro(RegiaoInicialPrimeiroFio()))IrParaPontoSeguro("praca");
            Avisar("Uma nova jornada começou. Converse com Dona Feliciana para guardar a primeira lembrança.");
        }
    }
}

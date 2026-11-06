using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private MTMTrama tramaTecida;
        private readonly HashSet<string> pontosControladosRoteiro = new HashSet<string>();
        private const string FonteRoteiro = "Memorias Tecidas - Rede de Memorias";
        private string missaoFixadaTecido = "", missaoSelecionadaTecido = "";
        private void PrepararTramaTecida()
        {
            var recurso = Resources.Load<TextAsset>("MemoriasTecidas/trama_01");
            try
            {
                if (recurso == null)
                    throw new InvalidOperationException("Resources/MemoriasTecidas/trama_01 ausente.");
                var d = JsonUtility.FromJson<MTMTrama>(recurso.text);
                if (d == null || d.versao != 2 || d.missoes == null || d.missoes.Length == 0 || d.itens == null || d.atividades == null || d.pistas == null || d.pontosNovos == null)
                    throw new InvalidOperationException("O pacote precisa conter o roteiro versão 2 completo.");
                var ids = new HashSet<string>();
                var eventos = new HashSet<string>();
                foreach (var m in d.missoes)
                {
                    if (m == null || !ids.Add(m.id) || m.etapas == null || m.etapas.Length == 0)
                        throw new InvalidOperationException("Missão inválida ou repetida.");
                    foreach (var e in m.etapas)
                    {
                        if (e == null || e.eventos == null || e.eventos.Length != 1 || !eventos.Add(e.eventos[0]))
                            throw new InvalidOperationException("Etapa inválida ou evento repetido.");
                        pontosControladosRoteiro.Add(e.ponto);
                    }
                }

                tramaTecida = d;
                var locais = MTMVinculosRegiao.Reindexar(regioes);
                MTMVinculosRegiao.SincronizarRoteiro(d, locais);
                MTMAutoriaRoteiro.AplicarMarcos(d, regioes);
                MTMAutoriaRoteiro.SincronizarOrigens(d);
                if (estado.marcos == null)
                    estado.marcos = new List<string>();
                if (estado.colecao == null)
                    estado.colecao = new List<MTMQuantidadeTecida>();
                estado.versaoRoteiro = 2;
                foreach (var r in regioes)
                {
                    if (r == null || r.dados == null)
                        continue;
                    var l = Array.Find(d.lugares, x => x.id == r.dados.id);
                    if (l != null && !string.IsNullOrEmpty(l.nome))
                        r.dados.nome = l.nome;
                    if (r.pontos == null)
                        continue;
                    foreach (var p in r.pontos)
                    {
                        if (p == null || p.dados == null)
                            continue;
                        var novo = Array.Find(d.dialogos, x => x.id == p.dados.id);
                        if (novo == null)
                            continue;
                        string anterior = p.dados.nome;
                        p.dados.nome = novo.nome;
                        p.dados.falas = novo.falas;
                        p.dados.fonte = novo.fonte;
                        foreach (var a in r.GetComponentsInChildren<MTMAtorV22>(true))
                            if (a.nomeDaConversa == anterior)
                                a.nomeDaConversa = novo.nome;
                    }
                }

                foreach (var r in regioes)
                    if (r != null && r.pontos != null)
                        foreach (var p in r.pontos)
                            if (p != null && p.dados != null && p.dados.tipo == "saida")
                            {
                                var destino = RegiaoTecido(p.dados.destino);
                                if (destino != null)
                                    p.dados.nome = destino.dados.nome;
                            }

                CriarPontosRoteiro();
            }
            catch (Exception erro)
            {
                tramaTecida = null;
                Debug.LogError("Não foi possível preparar o roteiro: " + erro.Message, this);
            }
        }

        private MTMRegiao RegiaoTecido(string id)
        {
            return regioes == null ? null : Array.Find(regioes, r => r != null && r.dados != null && r.dados.id == id);
        }

        private MTMPonto PontoTecido(string regiao, string id)
        {
            var r = RegiaoTecido(regiao);
            return r == null || r.pontos == null ? null : Array.Find(r.pontos, p => p != null && p.dados != null && p.dados.id == id);
        }

        private bool DisponivelTecido(MTMMissaoTecido missao)
        {
            return MTMMotorMissoes.Disponivel(estado, tramaTecida, missao);
        }

        private MTMMissaoTecido MissaoTecido(string id)
        {
            return tramaTecida == null ? null : Array.Find(tramaTecida.missoes, m => m.id == id);
        }

        private MTMMissaoTecido MissaoAtualTecido()
        {
            var fixada = MissaoTecido(missaoFixadaTecido);
            if (fixada != null && !MTMTrama.Concluida(estado, fixada) && DisponivelTecido(fixada))
                return fixada;
            if (tramaTecida != null)
                foreach (var m in tramaTecida.missoes)
                    if (!m.opcional && DisponivelTecido(m) && !MTMTrama.Concluida(estado, m))
                        return m;
            return null;
        }

        private MTMEtapaTecido EtapaAtualTecido(MTMMissaoTecido m)
        {
            return MTMMotorMissoes.Proxima(estado, m);
        }

        private string ObjetivoTecido()
        {
            var e = EtapaAtualTecido(MissaoAtualTecido());
            if (e == null)
                return "Capítulo concluído. Consulte o novo destino no mapa.";
            if (!MTMMotorMissoes.HorarioCorreto(e, estado.relogio))
                return e.horario == "dia" ? "Descanse no banco até o dia para continuar." : "Descanse no banco até a noite para continuar.";
            return e.texto + ".";
        }

        private MTMPonto AlvoDaEtapaTecido(MTMEtapaTecido e)
        {
            if (e == null)
                return estado.Tem(tramaTecida.proximoCapitulo.revelarEvento) ? PontoTecido(tramaTecida.proximoCapitulo.regiao, tramaTecida.proximoCapitulo.ponto) : null;
            if (!MTMMotorMissoes.HorarioCorreto(e, estado.relogio))
                return PontoTecido("praca", "banco_descanso");
            return PontoTecido(e.regiao, e.ponto);
        }

        private string RegiaoDaEtapaTecido(MTMEtapaTecido e)
        {
            return e == null ? (estado.Tem(tramaTecida.proximoCapitulo.revelarEvento) ? tramaTecida.proximoCapitulo.regiao : "") : !MTMMotorMissoes.HorarioCorreto(e, estado.relogio) ? "praca" : e.regiao;
        }

        private string NomeCurtoTecido(string id)
        {
            if (tramaTecida != null)
            {
                var l = Array.Find(tramaTecida.lugares, x => x != null && x.id == id);
                if (l != null && !string.IsNullOrEmpty(l.nomeCurto))
                    return l.nomeCurto;
            }

            var r = RegiaoTecido(id);
            return r == null ? id : r.dados.nome;
        }

        private List<string> CaminhoTecido(string destino)
        {
            var resultado = new List<string>();
            if (atual == null || RegiaoTecido(destino) == null || !RegiaoLiberadaRoteiro(destino))
                return resultado;
            var anterior = new Dictionary<string, string>();
            var fila = new Queue<string>();
            anterior[atual.dados.id] = null;
            fila.Enqueue(atual.dados.id);
            while (fila.Count > 0)
            {
                string id = fila.Dequeue();
                if (id == destino)
                    break;
                var r = RegiaoTecido(id);
                if (r == null || r.pontos == null)
                    continue;
                foreach (var p in r.pontos)
                {
                    if (p == null || p.dados == null || p.dados.tipo != "saida")
                        continue;
                    string proxima = p.dados.destino;
                    if (string.IsNullOrEmpty(proxima) || anterior.ContainsKey(proxima) || RegiaoTecido(proxima) == null)
                        continue;
                    if (!RegiaoLiberadaRoteiro(proxima))
                        continue;
                    anterior[proxima] = id;
                    fila.Enqueue(proxima);
                }
            }

            if (!anterior.ContainsKey(destino))
                return resultado;
            for (string id = destino; id != null; id = anterior[id])
                resultado.Add(id);
            resultado.Reverse();
            return resultado;
        }

        private string Rota(string destino)
        {
            if (!RegiaoLiberadaRoteiro(destino))
                return "Continue as missões anteriores para abrir este lugar.";
            var ids = CaminhoTecido(destino);
            if (ids.Count == 0)
                return "Este lugar não tem uma ligação disponível nesta cena.";
            if (ids.Count == 1)
                return "Você já está neste lugar. Explore os pontos do cenário.";
            return "Caminho: " + string.Join("  ›  ", ids.ConvertAll(NomeCurtoTecido).ToArray());
        }

        private MTMPonto ProximaSaidaTecido(string destino)
        {
            var ids = CaminhoTecido(destino);
            if (ids.Count < 2 || atual == null || atual.pontos == null)
                return null;
            return Array.Find(atual.pontos, p => p != null && p.dados != null && p.dados.tipo == "saida" && p.dados.destino == ids[1]);
        }

        private List<MTMPonto> MemoriasTecido()
        {
            var lista = new List<MTMPonto>();
            var eventos = new HashSet<string>();
            if (regioes != null)
                foreach (var r in regioes)
                    if (r != null && r.pontos != null)
                        foreach (var p in r.pontos)
                            if (p != null && p.dados != null && p.dados.tipo != "saida" && p.dados.tipo != "descanso" && estado.Tem(p.dados.evento) && eventos.Add(p.dados.evento))
                                lista.Add(p);
            return lista;
        }

        private string PapelTecido(string nome)
        {
            if (tramaTecida != null)
                foreach (var pessoa in tramaTecida.personagens)
                    if (nome == pessoa.nome || nome.StartsWith(pessoa.nome + " ·", StringComparison.Ordinal))
                        return pessoa.papel;
            return "Um fio da sua jornada";
        }

        private void AbrirMapaTecido(string id)
        {
            destinoMapa = id;
            painel = "mapa";
            confirmarNovo = false;
            Parar();
        }

        private bool RegiaoLiberadaRoteiro(string id)
        {
            if (tramaTecida == null || estado.Tem("visitou_" + id))
                return true;
            var b = Array.Find(tramaTecida.bloqueios, x => x.regiao == id);
            return b == null || estado.Tem(b.evento);
        }

        private bool EtapaNoPontoRoteiro(string ponto, out MTMMissaoTecido missao, out MTMEtapaTecido etapa)
        {
            missao = MissaoTecido(missaoFixadaTecido);
            etapa = EtapaAtualTecido(missao);
            if (missao != null && DisponivelTecido(missao) && etapa != null && etapa.ponto == ponto)
                return true;
            for (int prioridade = 0; prioridade < 2; prioridade++)
                foreach (var m in tramaTecida.missoes)
                {
                    if (m.opcional != (prioridade == 1) || !DisponivelTecido(m))
                        continue;
                    var e = EtapaAtualTecido(m);
                    if (e != null && e.ponto == ponto)
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

        private bool DisponivelRoteiro(MTMPonto p)
        {
            if (tramaTecida == null || p == null || p.dados == null)
                return false;
            if (p.dados.id == "lenda_rio")
                return false;
            if (p.dados.id.StartsWith("pdf_figura_", StringComparison.Ordinal))
                return false;
            if (p.dados.tipo == "saida" || p.dados.tipo == "descanso")
                return true;
            if (p.dados.id == "pdf_fiar")
                return estado.Tem("r24_m10_01");
            if (p.dados.id == tramaTecida.proximoCapitulo.ponto)
                return estado.Tem(tramaTecida.eventoNovaArea);
            MTMMissaoTecido m;
            MTMEtapaTecido e;
            if (EtapaNoPontoRoteiro(p.dados.id, out m, out e))
                return true;
            if (pontosControladosRoteiro.Contains(p.dados.id))
                return p.dados.tipo != "coleta" && p.dados.tipo != "roteiro";
            return p.dados.tipo != "coleta" || !estado.Tem(p.dados.evento);
        }

        private bool InteragirRoteiro(MTMPonto ponto)
        {
            if (tramaTecida == null)
                return false;
            string id = ponto.dados.id;
            if (id == "npc_neto")
            {
                Abrir(ponto.dados.nome, FonteRoteiro, ponto.dados.falas, null);
                return true;
            }

            if (id == "pdf_fiar")
            {
                if (!estado.Tem("r24_m10_01") || estado.rede || estado.fio >= 3 || estado.algodao < 6)
                    Abrir("Preparar fios", "Atividade didática.", new[] { "Depois do tutorial, seis algodões permitem completar até três fios preparados. Confira os materiais na bolsa." }, null);
                else
                    Abrir("Preparar fios", "Atividade didática.", new[] { "Preparar seis algodões para completar os três fios da receita?" }, () =>
                    {
                        if (estado.algodao >= 6 && !estado.rede && estado.fio < 3)
                        {
                            estado.algodao -= 6;
                            estado.fio = 3;
                            Salvar();
                            Avisar("Fios preparados para o tear.");
                        }
                    });
                return true;
            }

            if (id == tramaTecida.proximoCapitulo.ponto)
            {
                bool revelou = estado.Tem(tramaTecida.proximoCapitulo.revelarEvento);
                Abrir(tramaTecida.proximoCapitulo.nome, FonteRoteiro, new[] { revelou ? tramaTecida.proximoCapitulo.descricao : "Este caminho precisa ser comparado com os registros. Reúna os personagens na praça.", revelou ? "O símbolo e a figura noturna continuam sem explicação. A jornada continua." : "Consulte a missão Quando todas as histórias se encontram." }, null);
                return true;
            }

            if (!pontosControladosRoteiro.Contains(id))
                return false;
            MTMMissaoTecido m;
            MTMEtapaTecido e;
            if (!EtapaNoPontoRoteiro(id, out m, out e))
            {
                MTMEtapaTecido ultima = null;
                foreach (var item in tramaTecida.missoes)
                    foreach (var etapa in item.etapas)
                        if (etapa.ponto == id && MTMTrama.Concluida(estado, etapa))
                            ultima = etapa;
                Abrir(ponto.dados.nome, FonteRoteiro, ultima == null ? new[] { ObjetivoTecido() } : ultima.falas, null);
                return true;
            }

            string motivo;
            if (!MTMMotorMissoes.PodeConcluir(estado, tramaTecida, m, e, out motivo))
            {
                Abrir(ponto.dados.nome, FonteRoteiro, new[] { motivo }, null);
                return true;
            }

            if (e.acao == "aproximar")
            {
                ConcluirEtapaRoteiro(m, e, null, false);
                return true;
            }

            if (e.acao == "aparicao")
            {
                IniciarAparicaoRoteiro(m, e);
                return true;
            }

            string[] falasEncontro = e.falas;
            if (aberturaMemorias != null && e.ponto == aberturaMemorias.npc &&
                Array.IndexOf(e.eventos, aberturaMemorias.eventoEncontro) >= 0 && e.falas.Length > 2)
                falasEncontro = new[] { e.falas[0], e.falas[1] };
            Abrir(ponto.dados.nome, FonteRoteiro, falasEncontro, () =>
            {
                if (e.acao == "atividade")
                    AbrirAtividadeRoteiro(m, e);
                else if (e.acao == "escolta")
                    IniciarEscoltaRoteiro(m, e);
                else
                    ConcluirEtapaRoteiro(m, e, null, false);
            });
            return true;
        }

        private bool ConcluirEtapaRoteiro(MTMMissaoTecido m, MTMEtapaTecido e, IList<int> resposta, bool percurso)
        {
            if (atual == null || atual.dados.id != e.regiao)
                return false;
            string motivo;
            int motivosAntes = MTMPrimeiroFio.Quantidade(estado, aberturaMemorias);
            if (!MTMMotorMissoes.Concluir(estado, tramaTecida, m, e, resposta, percurso, out motivo))
            {
                Avisar(motivo);
                return false;
            }

            Salvar();
            AtualizarVisibilidadeRoteiro();
            Avisar(MTMTrama.Concluida(estado, m) ? "Missão concluída: " + m.titulo + "." : "Registro atualizado. " + ObjetivoTecido());
            AoConcluirEtapaPrimeiroFio(e, motivosAntes);
            return true;
        }
    }
}

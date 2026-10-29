using System;
using System.Collections.Generic;

namespace MemoriasTecidas.MundoV2
{
    [Serializable]
    public sealed class MTMAberturaMemorias
    {
        public int versao;
        public string titulo, npc, eventoEncontro, fonte;
        public string[] convite, observacao, conclusao, reencontro;
        public string[] nomes, cores;
        public int[] padrao;
        public MTMMotivoMemoria[] motivos;
    }

    [Serializable]
    public sealed class MTMMotivoMemoria
    {
        public string id, titulo, resumo, cor, icone, registro;
        public string[] eventos;
    }

    // Regras independentes da cena: a abertura não concede materiais nem conclui missões.
    public static class MTMPrimeiroFio
    {
        public const string Convite = "abertura_fio_convite";
        public const string Observou = "abertura_fio_observou";
        public const string Teceu = "abertura_fio_teceu";
        public const string Memoria = "abertura_fio_memoria";
        public const string Adiou = "abertura_fio_adiou";
        public static int Etapa(MTMEstado estado)
        {
            if (estado.Tem(Memoria))
                return 4;
            if (estado.Tem(Teceu))
                return 3;
            if (estado.Tem(Observou))
                return 2;
            if (estado.Tem(Convite))
                return 1;
            return 0;
        }

        public static bool JornadaVazia(MTMEstado estado)
        {
            return estado != null && (estado.marcos == null || estado.marcos.Count == 0) && !estado.rede && estado.algodao == 0 && estado.fio == 0 && estado.corda == 0 && estado.pano == 0 && estado.punho == 0 && estado.enfeite == 0 && estado.cera == 0 && (estado.colecao == null || estado.colecao.Count == 0);
        }

        public static bool Acompanhar(MTMEstado estado, MTMAberturaMemorias dados)
        {
            if (estado == null || dados == null || estado.Tem(Memoria) || estado.Tem(Adiou))
                return false;
            if (Etapa(estado) > 0)
                return true;
            // Uma atualização não retira a orientação da missão de quem já avançou.
            if (estado.marcos != null)
                foreach (string marco in estado.marcos)
                    if (marco != null && marco.StartsWith("r24_", StringComparison.Ordinal) && marco != dados.eventoEncontro)
                        return false;
            return true;
        }

        public static bool Avancar(MTMEstado estado, MTMAberturaMemorias dados, string marco, IList<int> resposta = null)
        {
            if (estado == null || dados == null || estado.Tem(marco))
                return false;
            bool permitido = marco == Convite ? estado.Tem(dados.eventoEncontro) : marco == Observou ? estado.Tem(Convite) : marco == Teceu ? estado.Tem(Observou) && RespostaCorreta(dados, resposta) : marco == Memoria && estado.Tem(Teceu);
            return permitido && estado.Concluir(marco);
        }

        public static bool RespostaCorreta(MTMAberturaMemorias dados, IList<int> resposta)
        {
            if (dados == null || dados.padrao == null || resposta == null || resposta.Count != dados.padrao.Length)
                return false;
            for (int i = 0; i < dados.padrao.Length; i++)
                if (resposta[i] != dados.padrao[i])
                    return false;
            return true;
        }

        public static bool Revelado(MTMEstado estado, MTMMotivoMemoria motivo)
        {
            if (estado == null || motivo == null || motivo.eventos == null)
                return false;
            foreach (string evento in motivo.eventos)
                if (estado.Tem(evento))
                    return true;
            return false;
        }

        public static int Quantidade(MTMEstado estado, MTMAberturaMemorias dados)
        {
            int total = 0;
            if (dados != null && dados.motivos != null)
                foreach (var motivo in dados.motivos)
                    if (Revelado(estado, motivo))
                        total++;
            return total;
        }

        public static void Validar(MTMAberturaMemorias dados, MTMTrama trama)
        {
            if (dados == null || dados.versao != 1 || trama == null)
                throw new InvalidOperationException("Abertura de memórias inválida.");
            if (string.IsNullOrEmpty(dados.titulo) || string.IsNullOrEmpty(dados.npc))
                throw new InvalidOperationException("A abertura precisa de título e personagem.");
            if (dados.nomes == null || dados.cores == null || dados.nomes.Length != 3 || dados.cores.Length != 3 || dados.padrao == null || dados.padrao.Length != 4)
                throw new InvalidOperationException("A amostra usa três cores e quatro passagens.");
            foreach (int indice in dados.padrao)
                if (indice < 0 || indice >= 3)
                    throw new InvalidOperationException("Cor da amostra fora das opções.");
            foreach (string[] falas in new[]
            {
                dados.convite,
                dados.observacao,
                dados.conclusao,
                dados.reencontro
            }

            )
                if (falas == null || falas.Length == 0 || Array.Exists(falas, string.IsNullOrWhiteSpace))
                    throw new InvalidOperationException("Diálogo da abertura vazio.");
            var eventos = new HashSet<string>();
            foreach (var missao in trama.missoes)
                foreach (var etapa in missao.etapas)
                    foreach (string evento in etapa.eventos)
                        eventos.Add(evento);
            if (!eventos.Contains(dados.eventoEncontro))
                throw new InvalidOperationException("Evento do encontro inicial não existe na campanha.");
            eventos.Add(Memoria);
            if (dados.motivos == null || dados.motivos.Length != 9)
                throw new InvalidOperationException("A rede de memórias usa nove motivos.");
            var ids = new HashSet<string>();
            foreach (var motivo in dados.motivos)
            {
                if (motivo == null || string.IsNullOrEmpty(motivo.id) || !ids.Add(motivo.id) || string.IsNullOrEmpty(motivo.titulo) || string.IsNullOrEmpty(motivo.resumo))
                    throw new InvalidOperationException("Motivo de memória incompleto ou repetido.");
                if (motivo.eventos == null || motivo.eventos.Length == 0)
                    throw new InvalidOperationException("Motivo sem evento de descoberta.");
                foreach (string evento in motivo.eventos)
                    if (!eventos.Contains(evento))
                        throw new InvalidOperationException("Evento desconhecido na rede: " + evento);
                if (!eventos.Contains(motivo.registro))
                    throw new InvalidOperationException("Registro desconhecido na rede: " + motivo.registro);
            }
        }
    }
}

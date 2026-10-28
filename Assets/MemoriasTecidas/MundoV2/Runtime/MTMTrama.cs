using System;

namespace MemoriasTecidas.MundoV2
{
    [Serializable] public sealed class MTMTrama
    {
        public int versao;
        public string titulo, subtitulo;
        public MTMPaletaTecido paleta;
        public MTMMissaoTecido[] missoes;
        public MTMItemTecido[] itens;
        public MTMPessoaTecido[] personagens;
        public MTMLugarTecido[] lugares;
        public MTMDialogoTecido[] dialogos;
        public string[] encerramento;
        public MTMPontoRoteiro[] pontosNovos;
        public MTMPuzzleTecido[] atividades;
        public MTMPistaTecida[] pistas;
        public MTMBloqueioTecido[] bloqueios;
        public MTMProximoCapitulo proximoCapitulo;
        public string eventoFesta, eventoNovaArea;

        public static bool EventoConcluido(MTMEstado estado, string evento)
        {
            return estado != null && (estado.Tem(evento) || (evento == "tecer" && estado.rede));
        }
        public static bool Concluida(MTMEstado estado, MTMEtapaTecido etapa)
        {
            if (estado == null || etapa == null) return false;
            if (etapa.alternativas != null)
                foreach (string id in etapa.alternativas) if (EventoConcluido(estado, id)) return true;
            if (etapa.eventos == null || etapa.eventos.Length == 0) return false;
            foreach (string id in etapa.eventos) if (!EventoConcluido(estado, id)) return false;
            return true;
        }
        public static int Progresso(MTMEstado estado, MTMMissaoTecido missao)
        {
            int n = 0;
            if (missao != null && missao.etapas != null)
                foreach (var e in missao.etapas) if (Concluida(estado, e)) n++;
            return n;
        }
        public static bool Concluida(MTMEstado estado, MTMMissaoTecido missao)
        {
            return missao != null && missao.etapas != null && missao.etapas.Length > 0 &&
                Progresso(estado, missao) == missao.etapas.Length;
        }
        public static int Quantidade(MTMEstado e, string id)
        {
            if (e == null) return 0;
            switch (id)
            {
                case "algodao": return Math.Max(0, e.algodao);
                case "fio": return Math.Max(0, e.fio);
                case "corda": return Math.Max(0, e.corda);
                case "pano": return Math.Max(0, e.pano);
                case "punho": return Math.Max(0, e.punho);
                case "enfeite": return Math.Max(0, e.enfeite);
                case "cera": return Math.Max(0, e.cera);
                case "rede": return e.rede ? 1 : 0;
                default: return e.QuantidadeExtra(id);
            }
        }
    }
    [Serializable] public sealed class MTMPaletaTecido
    { public string petroleo, profundo, algodao, papel, ouro, terracota, folha, texto; }
    [Serializable] public sealed class MTMMissaoTecido
    { public string id, titulo, descricao, recompensa, capitulo; public int numero; public bool opcional; public string[] requerMissoes; public MTMEtapaTecido[] etapas; }
    [Serializable] public sealed class MTMEtapaTecido
    {
        public string texto, regiao, ponto, horario, acao, atividade, registro;
        public string[] eventos, alternativas, falas, falasConclusao;
        public MTMQuantidadeTecida[] conceder, consumir, exigir;
        public bool completarMateriais, exigeReceita, fabricarRede;
        public string pista;
        public MTMPosicaoTecida[] percurso;
    }
    [Serializable] public sealed class MTMItemTecido
    { public string id, nome, categoria, descricao, regiao, origem, dica, icone; public bool ocultoAteObter; }
    [Serializable] public sealed class MTMPessoaTecido
    { public string id, nome, papel, natureza, descricao, regiao, evento; }
    [Serializable] public sealed class MTMLugarTecido
    { public string id, nomeCurto, nome, grupo, descricao, ambiente; }
    [Serializable] public sealed class MTMDialogoTecido
    { public string id, nome, fonte; public string[] falas; }
    [Serializable] public sealed class MTMQuantidadeTecida
    { public string id; public int quantidade; public bool minimo; }
    [Serializable] public sealed class MTMPosicaoTecida
    { public float x,y; }
    [Serializable] public sealed class MTMPontoRoteiro
    {
        public string id, nome, regiao, tipo, modeloRegiao, modeloPonto, modeloObjeto, visual, habilitarEvento;
        public float x,y;
        public bool andar;
        public MTMPosicaoTecida[] patrulha;
    }
    [Serializable] public sealed class MTMPuzzleTecido
    {
        public string id, tipo, titulo, instrucao, dica, sucesso;
        public string[] opcoes, cores, requerPistas;
        public int[] resposta;
        public bool mostrarModelo;
    }
    [Serializable] public sealed class MTMPistaTecida
    { public string id, titulo, texto, evento, lugar; public int ordem; }
    [Serializable] public sealed class MTMBloqueioTecido
    { public string regiao, evento, mensagem; }
    [Serializable] public sealed class MTMProximoCapitulo
    { public string regiao, ponto, nome, descricao, revelarEvento; public float x,y; }
}

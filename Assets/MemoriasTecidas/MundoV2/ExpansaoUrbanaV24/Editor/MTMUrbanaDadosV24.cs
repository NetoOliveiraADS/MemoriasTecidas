using System;

namespace MemoriasTecidas.MundoV2.Editor
{
    [Serializable] public sealed class MTMUrbanaPacoteV24
    {
        public int versao;
        public MTMUrbanaLocalV24[] locais;
    }
    [Serializable] public sealed class MTMUrbanaLocalV24
    {
        public string id, nome, descricao;
        public float largura, altura, seguroX, seguroY, mapaX, mapaY;
        public MTMPisoV23[] pisos;
        public MTMColocacaoV23[] objetos;
        public MTMAlvoV23[] moverPontos;
        public MTMPontoDados[] novosPontos;
    }
}

using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [Serializable]
    public sealed class MTMDados
    {
        public string versao;
        public MTMArte[] artes;
        public MTMAreaDados[] areas;
        public MTMQuadro[] quadros;
        public int folhaLargura, folhaAltura;
    }

    [Serializable]
    public sealed class MTMArte
    {
        public string id, arquivo;
        public float ppu, pivotX = .5f, pivotY, colW, colH, colY;
        public bool piso, folha, repetivel, personagem;
        public float vento, rostoX, rostoY, rostoW, rostoH;
    }

    [Serializable]
    public sealed class MTMQuadro
    {
        public float x, y, largura, altura, pivotX, pivotY, ppu;
    }

    [Serializable]
    public sealed class MTMAreaDados
    {
        public string id, nome, descricao, piso;
        public bool interior;
        public float mapaX, mapaY;
        public float largura = 32f, altura = 18f, seguroX, seguroY = -6f;
        public string bioma;
        public MTMPisoDados[] pisos;
        public MTMLuzDados[] luzes;
        public MTMObjetoDados[] objetos;
        public MTMPontoDados[] pontos;
        public MTMVetor[] agua;
    }

    [Serializable]
    public sealed class MTMObjetoDados
    {
        public string nome, arte;
        public float x, y, escala = 1f;
        public bool semColisao, noturno;
    }

    [Serializable]
    public sealed class MTMPontoDados
    {
        public string id, nome, tipo, destino, evento, condicao, fonte;
        public string[] falas;
        public float x, y, chegadaX, chegadaY;
        public bool noturno;
    }

    [Serializable]
    public sealed class MTMVetor
    {
        public float x, y;
    }

    [Serializable]
    public sealed class MTMPisoDados
    {
        public string arte;
        public float x, y, largura, altura;
        public int ordem;
    }

    [Serializable]
    public sealed class MTMLuzDados
    {
        public float x, y, raio = 5f, intensidade = .8f;
        public float r = 1f, g = .68f, b = .30f;
        public bool sempre;
    }
}

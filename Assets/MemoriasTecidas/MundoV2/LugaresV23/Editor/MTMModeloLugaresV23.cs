using System;
using UnityEngine;
namespace MemoriasTecidas.MundoV2.Editor
{
    [Serializable] public sealed class MTMPacoteArtesV23{public MTMArteV23[] artes;}
    [Serializable] public sealed class MTMArteV23
    {
        public string id,arquivo,tipo;
        public float x,y,largura,altura,pivotX=.5f,pivotY,alturaMundo=2f,colW,colH,colY;
        public bool sombra=true;
    }
    [Serializable] public sealed class MTMPacoteCenasV23{public MTMLocalV23[] locais;}
    [Serializable] public sealed class MTMLocalV23
    {
        public string id,nome,descricao;
        public float largura,altura,seguroX,seguroY;
        public MTMPisoV23[] pisos;public MTMColocacaoV23[] objetos;public MTMAlvoV23[] pontos;
    }
    [Serializable] public sealed class MTMPisoV23
    {
        public string nome,textura;public float x,y,w,h,repeticao=6f;public int ordem=-30000;
        public float r=1,g=1,b=1,a=1;public bool agua,bloqueia;public MTMVetor[] contorno;
    }
    [Serializable] public sealed class MTMColocacaoV23
    {public string id,nome;public float x,y,escala=1;public bool flipX;}
    [Serializable] public sealed class MTMAlvoV23{public string id;public float x,y;}
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMPonto : MonoBehaviour
    {
        public MTMPontoDados dados;
        public Texture2D retrato;
        public Rect recorteRosto = new Rect(0,0,1,1);
        public float alcance = 1.45f;
    }
}

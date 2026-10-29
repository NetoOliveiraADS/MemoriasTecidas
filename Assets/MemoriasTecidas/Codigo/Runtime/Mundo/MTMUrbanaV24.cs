using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [DisallowMultipleComponent]
    public sealed class MTMUrbanaV24 : MonoBehaviour
    {
        [HideInInspector]
        public int versao = 240;
        public GameObject layoutAnterior;
        public GameObject cenarioAtual;
        [TextArea]
        public string referencia;
    }
}

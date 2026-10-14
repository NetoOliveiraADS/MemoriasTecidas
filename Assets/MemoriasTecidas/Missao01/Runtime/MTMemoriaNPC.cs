using UnityEngine;

namespace MemoriasTecidas.Missao01
{
    public enum MTMemoriaPapel { Livre, Neto, AndreLucio }

    [DisallowMultipleComponent]
    public sealed class MTMemoriaNPC : MonoBehaviour
    {
        public MTMemoriaPapel papel;
        public string nome;
        public MTMemoriaDialogo dialogoLivre;
        [Min(0.3f)] public float alcance = 1.6f;
        public Vector3 deslocamentoMarcador = new Vector3(0f, 1.65f, 0f);
    }
}

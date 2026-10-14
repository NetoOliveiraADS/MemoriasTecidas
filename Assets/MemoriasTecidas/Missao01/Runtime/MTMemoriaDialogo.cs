using UnityEngine;

namespace MemoriasTecidas.Missao01
{
    [CreateAssetMenu(menuName = "Memorias Tecidas/Dialogo de memoria", fileName = "NovoDialogo")]
    public sealed class MTMemoriaDialogo : ScriptableObject
    {
        public string id;
        public string personagem;
        [TextArea(1, 3)] public string contexto;
        [TextArea(1, 3)] public string fonte;
        [TextArea(3, 6)] public string[] falas;

        public bool Valido => falas != null && falas.Length > 0;
    }
}

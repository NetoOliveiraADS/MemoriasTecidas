using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    /// <summary>Baú reutilizável: a abertura e a recompensa são controladas pelo MTMJogo.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MTMPonto))]
    public sealed class MTMBau : MonoBehaviour
    {
        [Tooltip("ID único e estável do tesouro no save.")]
        public string idTesouro = "praca_01";
        [Tooltip("ID de material existente na bolsa. O primeiro baú usa cera.")]
        public string idRecompensa = "cera";
        [Min(1)] public int quantidade = 1;
        public SpriteRenderer aparencia;
        public Color corFechado = Color.white;
        public Color corAberto = new Color(.65f, .65f, .65f, 1f);

        public bool Aberto(MTMEstado estado)
        {
            return estado != null && estado.Tem("exploracao_bau_" + idTesouro);
        }

        public void Sincronizar(MTMEstado estado)
        {
            if (aparencia == null) aparencia = GetComponent<SpriteRenderer>();
            if (aparencia != null) aparencia.color = Aberto(estado) ? corAberto : corFechado;
        }

        private void OnValidate()
        {
            quantidade = Mathf.Max(1, quantidade);
        }
    }
}

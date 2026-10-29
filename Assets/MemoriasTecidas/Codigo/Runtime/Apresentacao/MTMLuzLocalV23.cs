using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MTMLuzLocalV23 : MonoBehaviour
    {
        public Color cor = new Color(1f, .73f, .4f);
        [Min(.1f)]
        public float raio = 6f;
        [Range(0, 2)]
        public float intensidade = .8f;
        public bool sempre;
        private void Avisar()
        {
            var r = GetComponentInParent<MTMRegiaoVisualV23>();
            if (r != null)
                r.InvalidarLuzes();
        }

        private void OnEnable()
        {
            Avisar();
        }

        private void OnDisable()
        {
            Avisar();
        }

        private void OnValidate()
        {
            Avisar();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = cor;
            Gizmos.DrawWireSphere(transform.position, raio);
        }
    }
}

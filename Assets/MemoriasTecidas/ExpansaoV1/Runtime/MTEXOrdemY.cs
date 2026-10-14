using UnityEngine;

namespace MemoriasTecidas.ExpansaoV1
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTEXOrdemY : MonoBehaviour
    {
        public Transform baseNoChao;
        public int ajuste;
        private SpriteRenderer desenho;
        private void OnEnable() { desenho = GetComponent<SpriteRenderer>(); Aplicar(); }
        private void LateUpdate() { Aplicar(); }
        private void Aplicar()
        {
            if (desenho == null) return;
            float y = baseNoChao != null ? baseNoChao.position.y : transform.position.y;
            desenho.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-y * 100f) + ajuste, -28000, 28000);
        }
    }
}

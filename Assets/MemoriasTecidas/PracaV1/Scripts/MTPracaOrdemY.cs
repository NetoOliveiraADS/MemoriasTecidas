using UnityEngine;

namespace MemoriasTecidas.PracaV1
{
    // O Transform do objeto representa o ponto onde seus pés/base tocam o chão.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTPracaOrdemY : MonoBehaviour
    {
        public Transform baseNoChao;
        public int ajuste;
        private SpriteRenderer desenho;

        private void LateUpdate()
        {
            if (desenho == null) desenho = GetComponent<SpriteRenderer>();
            float y = baseNoChao != null ? baseNoChao.position.y : transform.position.y;
            desenho.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-y * 100f) + ajuste, -28000, 28000);
        }
    }
}

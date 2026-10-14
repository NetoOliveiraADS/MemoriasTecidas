using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [ExecuteAlways, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTMOrdemY : MonoBehaviour
    {
        private SpriteRenderer sr;
        private void LateUpdate()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            sr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-transform.position.y * 100f), -28000, 28000);
        }
    }
}

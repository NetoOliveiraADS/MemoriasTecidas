using UnityEngine;

namespace MemoriasTecidas.PracaV1
{
    // Mantém o enquadramento de referência em janelas com proporções diferentes.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class MTPracaCamera : MonoBehaviour
    {
        private Camera alvo;
        private void OnEnable() { alvo = GetComponent<Camera>(); Ajustar(); }
        private void Update() { Ajustar(); }
        private void Ajustar()
        {
            if (alvo == null || Screen.height <= 0 || Screen.width <= 0) return;
            float escala = (Screen.width / (float)Screen.height) / (16f / 9f);
            alvo.rect = escala < 1f ? new Rect(0f, (1f - escala) * .5f, 1f, escala)
                : new Rect((1f - 1f / escala) * .5f, 0f, 1f / escala, 1f);
        }
        private void OnDisable() { if (alvo != null) alvo.rect = new Rect(0f, 0f, 1f, 1f); }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [DisallowMultipleComponent]
    public sealed class MTMMarcadorSaidaV221 : MonoBehaviour
    {
        public Material material;
        public MTMVidaV22 vida;
        private LineRenderer linha;
        private void Start()
        {
            var go = new GameObject("Passagem_No_Chao_V221");
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;
            linha = go.AddComponent<LineRenderer>();
            linha.sharedMaterial = material;
            linha.useWorldSpace = false;
            linha.loop = true;
            linha.positionCount = 32;
            linha.widthMultiplier = .035f;
            linha.sortingOrder = -5000;
            linha.startColor = linha.endColor = new Color(1f, .76f, .30f, .9f);
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32;
                linha.SetPosition(i, new Vector3(Mathf.Cos(a) * .48f, Mathf.Sin(a) * .24f, 0));
            }
        }

        private void LateUpdate()
        {
            if (linha == null)
                return;
            // Só de perto: a seta e o losango já guiam de longe; o anel diz "a passagem é aqui".
            bool ligado = vida != null && vida.mostrarInterface && (vida.jogo == null || !vida.jogo.V22Bloqueado);
            float distancia = vida != null && vida.jogo != null ? Vector2.Distance(vida.jogo.transform.position, transform.position) : 0f;
            float perto = Mathf.Clamp01((6f - distancia) / 2.5f);
            linha.enabled = ligado && perto > 0f;
            if (linha.enabled)
            {
                float pulso = .55f + .45f * Mathf.Sin(Time.unscaledTime * 4f);
                linha.startColor = linha.endColor = new Color(1f, .76f, .30f, .9f * perto * pulso);
            }
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.ExpansaoV1
{
    // Prévia de cor e halos para arte unlit. Não simula sombras nem passagem de dias.
    public sealed class MTEXAmbiente : MonoBehaviour
    {
        public SpriteRenderer[] tingir;
        public GameObject luzes;
        public GameObject mulherDoRelato, centauraDaReferencia;
        public bool noite;
        public bool usarCentaura;
        public Color corNoturna = new Color(.34f, .41f, .62f, 1f);
        private Color[] originais;
        private float mistura;

        private void OnEnable()
        {
            if (tingir == null) tingir = new SpriteRenderer[0];
            originais = new Color[tingir.Length];
            for (int i = 0; i < tingir.Length; i++) originais[i] = tingir[i] != null ? tingir[i].color : Color.white;
            mistura = noite ? 1f : 0f;
            Aplicar();
        }

        private void Update()
        {
            mistura = Mathf.MoveTowards(mistura, noite ? 1f : 0f, Time.unscaledDeltaTime * 1.8f);
            Aplicar();
        }

        private void Aplicar()
        {
            if (originais == null) return;
            Color multiplicador = Color.Lerp(Color.white, corNoturna, mistura);
            for (int i = 0; i < tingir.Length && i < originais.Length; i++)
            {
                if (tingir[i] == null) continue;
                Color c = originais[i] * multiplicador;
                c.a = originais[i].a;
                tingir[i].color = c;
            }
            if (luzes != null) luzes.SetActive(mistura > .45f);
            if (mulherDoRelato != null) mulherDoRelato.SetActive(mistura > .75f && !usarCentaura);
            if (centauraDaReferencia != null) centauraDaReferencia.SetActive(mistura > .75f && usarCentaura);
        }

        private void OnDisable()
        {
            if (originais == null || tingir == null) return;
            for (int i = 0; i < tingir.Length && i < originais.Length; i++)
                if (tingir[i] != null) tingir[i].color = originais[i];
            if (luzes != null) luzes.SetActive(false);
            if (mulherDoRelato != null) mulherDoRelato.SetActive(false);
            if (centauraDaReferencia != null) centauraDaReferencia.SetActive(false);
        }
    }
}

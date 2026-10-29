using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Acabamento dos volumes 2D; não altera a arte, os pivôs ou a colisão do objeto.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MTMVolumeCenario : MonoBehaviour
    {
        public SpriteRenderer fachada;
        [Range(0f, 1f)]
        public float relevo = .3f, desgasteBase = .25f;
        public bool sombraContato = true;
        [Tooltip("Zero usa a largura da arte. Valores em unidades do mundo.")]
        public Vector2 raioContato = Vector2.zero;
        public Vector2 deslocamentoContato = Vector2.zero;
        private MaterialPropertyBlock bloco;
        private void OnEnable()
        {
            if (fachada == null)
                fachada = GetComponentInChildren<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            Aplicar(fachada, relevo, desgasteBase, ref bloco);
        }

        public static void Aplicar(SpriteRenderer sr, float relevo, float desgaste, ref MaterialPropertyBlock propriedades)
        {
            if (sr == null || sr.sprite == null)
                return;
            if (propriedades == null)
                propriedades = new MaterialPropertyBlock();
            sr.GetPropertyBlock(propriedades);
            Bounds b = sr.bounds;
            propriedades.SetFloat("_MTM_Relevo", Mathf.Clamp01(relevo));
            propriedades.SetVector("_MTM_Base", new Vector4(b.min.y, b.size.y, Mathf.Clamp01(desgaste), 0f));
            sr.SetPropertyBlock(propriedades);
        }

        private void OnDisable()
        {
            if (fachada == null)
                return;
            if (bloco == null)
                bloco = new MaterialPropertyBlock();
            fachada.GetPropertyBlock(bloco);
            bloco.SetFloat("_MTM_Relevo", 0f);
            bloco.SetVector("_MTM_Base", new Vector4(0, 1, 0, 0));
            fachada.SetPropertyBlock(bloco);
        }

        private void OnDrawGizmosSelected()
        {
            if (fachada == null)
                return;
            Gizmos.color = new Color(.8f, .65f, .35f, .75f);
            Gizmos.DrawWireCube(fachada.bounds.center, fachada.bounds.size);
        }
    }
}

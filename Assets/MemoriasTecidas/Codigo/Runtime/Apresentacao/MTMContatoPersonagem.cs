using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Contato curto no pé, complementar à sombra projetada pelo horário.
    [DefaultExecutionOrder(175), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTMContatoPersonagem : MonoBehaviour
    {
        private SpriteRenderer pessoa;
        private GameObject objeto;
        private MeshRenderer desenho;
        private Mesh malha;
        private Material material;

        private void Awake()
        {
            pessoa = GetComponent<SpriteRenderer>();
            Shader shader = Resources.Load<Shader>("MemoriasTecidas/ContatoSuave");
            if (shader == null) return;
            malha = new Mesh { name = "Contato do personagem", hideFlags = HideFlags.DontSave };
            malha.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) };
            malha.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            malha.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            malha.RecalculateBounds();
            material = new Material(shader) { hideFlags = HideFlags.DontSave };
            objeto = new GameObject("Contato · pés") { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
            objeto.transform.SetParent(transform, false);
            objeto.AddComponent<MeshFilter>().sharedMesh = malha;
            desenho = objeto.AddComponent<MeshRenderer>();
            desenho.sharedMaterial = material;
            desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            desenho.receiveShadows = false;
            desenho.sortingLayerID = pessoa.sortingLayerID;
            desenho.sortingOrder = -21990;
        }

        private void LateUpdate()
        {
            if (desenho == null || pessoa == null) return;
            desenho.enabled = pessoa.enabled && pessoa.sprite != null && MTMAmbienteV2.Qualidade > 0;
            if (!desenho.enabled) return;
            Vector3 pe = pessoa.transform.TransformPoint(new Vector3(0, pessoa.sprite.bounds.min.y, 0));
            objeto.transform.position = pe + Vector3.up * .025f;
            objeto.transform.rotation = Quaternion.identity;
            Vector3 escala = transform.lossyScale;
            float largura = Mathf.Clamp(pessoa.bounds.size.x * .34f, .13f, .42f);
            objeto.transform.localScale = new Vector3(largura / Mathf.Max(.001f, Mathf.Abs(escala.x)),
                largura * .32f / Mathf.Max(.001f, Mathf.Abs(escala.y)), 1);
        }

        private void OnDisable() { if (desenho != null) desenho.enabled = false; }

        private void OnDestroy()
        {
            if (objeto != null) Destroy(objeto);
            if (malha != null) Destroy(malha);
            if (material != null) Destroy(material);
        }
    }
}

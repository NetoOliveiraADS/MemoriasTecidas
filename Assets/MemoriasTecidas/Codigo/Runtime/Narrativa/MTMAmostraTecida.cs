using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Pequeno expositor de geometria 2D. Só existe durante o Play; sem textura externa.
    public sealed class MTMAmostraTecida : MonoBehaviour
    {
        private Mesh malha;
        private Material material;
        private MeshRenderer desenho;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color> cores = new List<Color>();
        private readonly List<int> indices = new List<int>();
        private bool pronta, criada;
        private int assinaturaAnterior;
        public void Mostrar(bool tecida, MTMAberturaMemorias dados, IList<int> passagens = null)
        {
            int assinatura = 0;
            if (!tecida && passagens != null)
                foreach (int cor in passagens) assinatura = assinatura * 4 + cor + 1;
            if (criada && pronta == tecida && assinatura == assinaturaAnterior) return;
            assinaturaAnterior = assinatura;
            if (malha == null)
            {
                var shader = Resources.Load<Shader>("MemoriasTecidas/AmostraTecida");
                if (shader == null)
                {
                    Debug.LogWarning("Shader da amostra não encontrado.", this);
                    return;
                }

                malha = new Mesh
                {
                    name = "Amostra de tecido",
                    hideFlags = HideFlags.DontSave
                };
                material = new Material(shader)
                {
                    name = "Tecido da abertura",
                    hideFlags = HideFlags.DontSave
                };
                gameObject.AddComponent<MeshFilter>().sharedMesh = malha;
                desenho = gameObject.AddComponent<MeshRenderer>();
                desenho.sharedMaterial = material;
                desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                desenho.receiveShadows = false;
            }

            pronta = tecida;
            criada = true;
            vertices.Clear();
            cores.Clear();
            indices.Clear();
            Color madeira = new Color(.35f, .24f, .17f), fundo = new Color(.16f, .27f, .25f), fio = new Color(.78f, .71f, .57f);
            Quad(-.85f, 0, 1.7f, .12f, new Color(0, 0, 0, .18f));
            Quad(-.70f, .04f, .11f, 1.45f, madeira);
            Quad(.59f, .04f, .11f, 1.45f, madeira);
            Quad(-.72f, .42f, 1.44f, 1.02f, fundo);
            Quad(-.82f, 1.42f, 1.64f, .10f, madeira);
            for (int i = 0; i < 12; i++)
                Quad(-.60f + i * .108f, .40f, .025f, 1.03f, fio);
            for (int linha = 0; linha < 4; linha++)
            {
                Color cor;
                bool inserida = tecida || (passagens != null && linha < passagens.Count &&
                    passagens[linha] >= 0 && passagens[linha] < dados.cores.Length);
                int indiceCor = tecida ? dados.padrao[linha] : inserida ? passagens[linha] : 0;
                ColorUtility.TryParseHtmlString(dados.cores[indiceCor], out cor);
                if (!inserida) cor = Color.Lerp(fundo, fio, .18f);
                float y = 1.18f - linha * .20f;
                Quad(-.65f, y, 1.30f, .14f, cor);
                for (int coluna = 0; coluna < 12; coluna++)
                    if ((linha + coluna) % 2 == 0)
                        Quad(-.60f + coluna * .108f, y, .025f, .14f, fio);
            }

            Quad(-.82f, .32f, 1.64f, .09f, madeira);
            malha.Clear();
            malha.SetVertices(vertices);
            malha.SetColors(cores);
            malha.SetTriangles(indices, 0);
            malha.RecalculateBounds();
            AtualizarOrdem();
        }

        private void Quad(float x, float y, float largura, float altura, Color cor)
        {
            int inicio = vertices.Count;
            vertices.Add(new Vector3(x, y, 0));
            vertices.Add(new Vector3(x + largura, y, 0));
            vertices.Add(new Vector3(x + largura, y + altura, 0));
            vertices.Add(new Vector3(x, y + altura, 0));
            for (int i = 0; i < 4; i++)
                cores.Add(cor);
            indices.Add(inicio);
            indices.Add(inicio + 1);
            indices.Add(inicio + 2);
            indices.Add(inicio);
            indices.Add(inicio + 2);
            indices.Add(inicio + 3);
        }

        private void LateUpdate()
        {
            AtualizarOrdem();
        }

        private void AtualizarOrdem()
        {
            if (desenho != null)
                desenho.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-transform.position.y * 100f), -28000, 28000);
        }

        private void OnDestroy()
        {
            if (malha != null)
                Destroy(malha);
            if (material != null)
                Destroy(material);
        }
    }
}

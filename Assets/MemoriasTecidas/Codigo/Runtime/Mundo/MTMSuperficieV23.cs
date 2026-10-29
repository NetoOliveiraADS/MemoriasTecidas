using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Piso/água editável. A mesma borda gera a imagem e a colisão da água.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class MTMSuperficieV23 : MonoBehaviour
    {
        public Texture2D textura;
        public Material material;
        public Color cor = Color.white;
        public Vector2 tamanho = new Vector2(8, 8);
        [Min(.1f)]
        public float unidadesPorRepeticao = 6f;
        [Tooltip("Vazio = retângulo. Com 3 ou mais pontos = contorno em coordenadas locais, sem repetir o primeiro ponto.")]
        public Vector2[] contorno;
        public int ordem = -30000;
        public bool agua, bloquearPassagem;
        [Range(0f, .04f)]
        public float ondulacao = .009f;
        [Range(0f, 2f)]
        public float velocidadeAgua = .65f;
        private Mesh malha;
        private Texture2D distanciaBorda;
        private Color32[] dadosBorda;
        private Vector2[] pontosBorda;
        private Vector2 eixoXBorda, eixoYBorda;
        private MaterialPropertyBlock bloco;
        private bool reconstruir = true;
        private void OnEnable()
        {
            reconstruir = true;
        }

        private void OnValidate()
        {
            reconstruir = true;
        }

        private void Update()
        {
            if (!reconstruir)
                return;
            reconstruir = false;
            try
            {
                Reconstruir();
            }
            catch (Exception e)
            {
                Debug.LogError("Superfície V2.3: " + e.Message, this);
            }
        }

        public Vector2[] Pontos()
        {
            if (contorno != null && contorno.Length >= 3)
                return (Vector2[])contorno.Clone();
            Vector2 m = new Vector2(Mathf.Max(.1f, tamanho.x), Mathf.Max(.1f, tamanho.y)) * .5f;
            return new[]
            {
                new Vector2(-m.x, -m.y),
                new Vector2(m.x, -m.y),
                new Vector2(m.x, m.y),
                new Vector2(-m.x, m.y)
            };
        }

        [ContextMenu("Reconstruir superficie e colisao")]
        public void Reconstruir()
        {
            Vector2[] pontos = Pontos();
            int[] indices = MTMTriangulacaoV23.Triangular(pontos);
            if (malha == null)
            {
                malha = new Mesh
                {
                    name = "Superficie_V23",
                    hideFlags = HideFlags.DontSave
                };
            }

            var vertices = new Vector3[pontos.Length];
            var uv = new Vector2[pontos.Length];
            var uvBorda = new Vector2[pontos.Length];
            Rect limites = MTMContornoVisual.Limites(pontos);
            for (int i = 0; i < pontos.Length; i++)
            {
                vertices[i] = pontos[i];
                uv[i] = pontos[i] / Mathf.Max(.1f, unidadesPorRepeticao);
                uvBorda[i] = new Vector2((pontos[i].x - limites.xMin) / limites.width, (pontos[i].y - limites.yMin) / limites.height);
            }

            malha.Clear();
            malha.vertices = vertices;
            malha.uv = uv;
            malha.uv2 = uvBorda;
            malha.triangles = indices;
            malha.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = malha;
            var r = GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = ordem;
            if (bloco == null)
                bloco = new MaterialPropertyBlock();
            bloco.SetTexture("_MainTex", textura != null ? textura : Texture2D.whiteTexture);
            bloco.SetColor("_Color", cor);
            bloco.SetFloat("_Agua", agua ? 1f : 0f);
            bloco.SetFloat("_Ondulacao", ondulacao);
            bloco.SetFloat("_Velocidade", velocidadeAgua);
            string nomeTextura = textura != null ? textura.name.ToLowerInvariant() : "";
            bool natural = agua || nomeTextura.Contains("areia") || nomeTextura.Contains("grama") || nomeTextura.Contains("terra");
            if (natural)
            {
                PrepararBorda(pontos, limites);
                bloco.SetTexture("_MTM_DistanciaBorda", distanciaBorda);
            }
            else
                LiberarBorda();
            bloco.SetFloat("_MTM_ContornoAtivo", natural ? 1f : 0f);
            bloco.SetFloat("_MTM_LarguraBorda", agua ? .08f : .12f);
            r.SetPropertyBlock(bloco);
            var c = GetComponent<PolygonCollider2D>();
            if (bloquearPassagem)
            {
                if (c == null)
                    c = gameObject.AddComponent<PolygonCollider2D>();
                c.enabled = true;
                c.isTrigger = false;
                c.pathCount = 1;
                c.SetPath(0, pontos);
            }
            else if (c != null)
                c.enabled = false;
            reconstruir = false;
        }

        private void OnDisable()
        {
            LiberarMalha();
        }

        private void OnDestroy()
        {
            LiberarMalha();
            dadosBorda = null;
            pontosBorda = null;
        }

        private void LiberarMalha()
        {
            LiberarBorda();
            if (malha == null)
                return;
            var filtro = GetComponent<MeshFilter>();
            if (filtro != null && filtro.sharedMesh == malha)
                filtro.sharedMesh = null;
            if (Application.isPlaying)
                Destroy(malha);
            else
                DestroyImmediate(malha);
            malha = null;
            reconstruir = true;
        }

        private void PrepararBorda(Vector2[] pontos, Rect limites)
        {
            // Guarda os dados em memória gerenciada; libera a textura nativa no OnDisable.
            // Voltar à região só exige reenviar os dados, sem repetir o cálculo de distâncias.
            Vector2 x = transform.TransformVector(Vector3.right), y = transform.TransformVector(Vector3.up);
            bool igual = dadosBorda != null && pontosBorda != null && pontosBorda.Length == pontos.Length && x == eixoXBorda && y == eixoYBorda;
            if (igual)
                for (int i = 0; i < pontos.Length; i++)
                    if (pontos[i] != pontosBorda[i])
                    {
                        igual = false;
                        break;
                    }

            if (igual && distanciaBorda != null)
                return;
            LiberarBorda();
            distanciaBorda = MTMContornoVisual.Criar(pontos, transform, limites, out dadosBorda, igual ? dadosBorda : null);
            pontosBorda = (Vector2[])pontos.Clone();
            eixoXBorda = x;
            eixoYBorda = y;
        }

        private void LiberarBorda()
        {
            if (distanciaBorda == null)
                return;
            if (Application.isPlaying)
                Destroy(distanciaBorda);
            else
                DestroyImmediate(distanciaBorda);
            distanciaBorda = null;
        }

        private void OnDrawGizmosSelected()
        {
            var p = Pontos();
            Gizmos.color = agua ? Color.cyan : new Color(1f, .75f, .2f);
            for (int i = 0; i < p.Length; i++)
                Gizmos.DrawLine(transform.TransformPoint(p[i]), transform.TransformPoint(p[(i + 1) % p.Length]));
        }
    }

    public static class MTMTriangulacaoV23
    {
        public static int[] Triangular(Vector2[] p)
        {
            if (p == null || p.Length < 3 || p.Length > 256)
                throw new ArgumentException("Use de 3 a 256 vértices no contorno.");
            ValidarContorno(p);
            float area = 0;
            for (int i = 0; i < p.Length; i++)
                area += Cruz(p[i], p[(i + 1) % p.Length]);
            if (Mathf.Abs(area) < .00001f)
                throw new ArgumentException("Contorno sem área.");
            var vivos = new List<int>();
            for (int i = 0; i < p.Length; i++)
                vivos.Add(area > 0 ? i : p.Length - 1 - i);
            var tr = new List<int>();
            while (vivos.Count > 3)
            {
                bool achou = false;
                for (int i = 0; i < vivos.Count; i++)
                {
                    int a = vivos[(i + vivos.Count - 1) % vivos.Count], b = vivos[i], c = vivos[(i + 1) % vivos.Count];
                    if (Cruz(p[b] - p[a], p[c] - p[b]) <= .000001f)
                        continue;
                    bool ocupado = false;
                    foreach (int k in vivos)
                        if (k != a && k != b && k != c && Dentro(p[k], p[a], p[b], p[c]))
                        {
                            ocupado = true;
                            break;
                        }

                    if (ocupado)
                        continue;
                    tr.Add(a);
                    tr.Add(b);
                    tr.Add(c);
                    vivos.RemoveAt(i);
                    achou = true;
                    break;
                }

                if (!achou)
                    throw new ArgumentException("Contorno inválido: retire cruzamentos, vértices repetidos ou alinhados desnecessários.");
            }

            tr.AddRange(vivos);
            return tr.ToArray();
        }

        private static void ValidarContorno(Vector2[] p)
        {
            const float eps = .00001f;
            for (int i = 0; i < p.Length; i++)
            {
                if (float.IsNaN(p[i].x) || float.IsNaN(p[i].y) || float.IsInfinity(p[i].x) || float.IsInfinity(p[i].y))
                    throw new ArgumentException("Vértice com coordenada inválida.");
                for (int j = i + 1; j < p.Length; j++)
                {
                    if ((p[i] - p[j]).sqrMagnitude < eps * eps)
                        throw new ArgumentException("O contorno tem vértices repetidos.");
                    int a = (i + 1) % p.Length, b = (j + 1) % p.Length;
                    if (a == j || b == i)
                        continue;
                    if (CruzaSegmentos(p[i], p[a], p[j], p[b], eps))
                        throw new ArgumentException("As bordas do contorno não podem se cruzar.");
                }
            }
        }

        private static bool CruzaSegmentos(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float e)
        {
            float u = Cruz(b - a, c - a), v = Cruz(b - a, d - a), w = Cruz(d - c, a - c), z = Cruz(d - c, b - c);
            if (((u > e && v < -e) || (u < -e && v > e)) && ((w > e && z < -e) || (w < -e && z > e)))
                return true;
            return (Mathf.Abs(u) <= e && NoSegmento(c, a, b, e)) || (Mathf.Abs(v) <= e && NoSegmento(d, a, b, e)) || (Mathf.Abs(w) <= e && NoSegmento(a, c, d, e)) || (Mathf.Abs(z) <= e && NoSegmento(b, c, d, e));
        }

        private static bool NoSegmento(Vector2 p, Vector2 a, Vector2 b, float e)
        {
            return p.x >= Mathf.Min(a.x, b.x) - e && p.x <= Mathf.Max(a.x, b.x) + e && p.y >= Mathf.Min(a.y, b.y) - e && p.y <= Mathf.Max(a.y, b.y) + e;
        }

        private static float Cruz(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static bool Dentro(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            return Cruz(b - a, p - a) >= -.000001f && Cruz(c - b, p - b) >= -.000001f && Cruz(a - c, p - c) >= -.000001f;
        }
    }
}

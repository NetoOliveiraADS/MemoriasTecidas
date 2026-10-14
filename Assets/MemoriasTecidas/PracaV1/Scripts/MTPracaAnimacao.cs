using System;
using UnityEngine;

namespace MemoriasTecidas.PracaV1
{
    // Correção 1.0.1: dimensões de referência explícitas e diagnóstico por quadro.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTPracaAnimacao : MonoBehaviour
    {
        [Serializable] public struct Quadro { public Rect rect; public Vector2 pivot; public float pixelsPorUnidade; }
        public Texture2D folha;
        public Quadro[] quadros;
        public Vector2Int tamanhoReferencia = new Vector2Int(1024, 1536);
        [Min(1f)] public float pixelsPorUnidade = 300f;
        [Range(1f, 12f)] public float quadrosPorSegundo = 6f;
        private SpriteRenderer desenho;
        private Rigidbody2D corpo;
        private Sprite[] sprites;
        private bool reconstruir = true;
        private int direcao;
        private float tempo;
        private static readonly int[] Passos = { 0, 1, 2, 1 };

        private void OnEnable() { reconstruir = true; }
        private void OnValidate() { reconstruir = true; }
        public void Recarregar() { reconstruir = true; }

        private void Update()
        {
            if (reconstruir) Preparar();
            if (sprites == null || desenho == null) return;
            if (!Application.isPlaying) { desenho.sprite = sprites[1]; return; }
            if (corpo == null) corpo = GetComponentInParent<Rigidbody2D>();
            Vector2 velocidade = corpo != null ? corpo.linearVelocity : Vector2.zero;
            bool andando = velocidade.sqrMagnitude > 0.0025f;
            if (andando)
            {
                direcao = Mathf.Abs(velocidade.x) > Mathf.Abs(velocidade.y)
                    ? (velocidade.x < 0f ? 1 : 2) : (velocidade.y < 0f ? 0 : 3);
                tempo += Time.deltaTime;
            }
            else tempo = 0f;
            int coluna = andando ? Passos[Mathf.FloorToInt(tempo * quadrosPorSegundo) % 4] : 1;
            desenho.sprite = sprites[direcao * 3 + coluna];
        }

        private void Preparar()
        {
            reconstruir = false;
            Liberar();
            desenho = GetComponent<SpriteRenderer>();
            if (folha == null || quadros == null || quadros.Length != 12)
            {
                Erro("faltam a textura ou os 12 quadros."); return;
            }
            if (tamanhoReferencia.x <= 0 || tamanhoReferencia.y <= 0)
            {
                Erro("as dimensões de referência estão vazias."); return;
            }
            float sx = folha.width / (float)tamanhoReferencia.x;
            float sy = folha.height / (float)tamanhoReferencia.y;
            if (Mathf.Abs(sx / sy - 1f) > .02f)
            {
                Erro("a textura foi redimensionada sem preservar a proporção; reimporte com Non Power of 2 = None."); return;
            }
            var recortes = new Rect[12];
            var escalas = new float[12];
            for (int i = 0; i < quadros.Length; i++)
            {
                Quadro q = quadros[i];
                Rect r = q.rect;
                float ppu = q.pixelsPorUnidade > 0f ? q.pixelsPorUnidade : pixelsPorUnidade;
                if (!Finito(r.x) || !Finito(r.y) || !Finito(r.width) || !Finito(r.height)
                    || r.width <= 0f || r.height <= 0f || r.xMin < 0f || r.yMin < 0f
                    || r.xMax > tamanhoReferencia.x || r.yMax > tamanhoReferencia.y)
                {
                    Erro("quadro " + (i + 1) + " inválido: " + r + "."); return;
                }
                if (!Finito(q.pivot.x) || !Finito(q.pivot.y) || q.pivot.x < 0f || q.pivot.x > 1f
                    || q.pivot.y < 0f || q.pivot.y > 1f || !Finito(ppu) || ppu <= 0f)
                {
                    Erro("pivô ou PPU inválido no quadro " + (i + 1) + "."); return;
                }
                // Os dados continuam em pixels da imagem original. Ajusta à textura importada.
                float x0 = Mathf.Round(r.xMin * sx), y0 = Mathf.Round(r.yMin * sy);
                float x1 = Mathf.Min(folha.width, Mathf.Round(r.xMax * sx));
                float y1 = Mathf.Min(folha.height, Mathf.Round(r.yMax * sy));
                if (x1 <= x0 || y1 <= y0)
                {
                    Erro("a resolução importada é pequena demais para o quadro " + (i + 1) + "."); return;
                }
                recortes[i] = Rect.MinMaxRect(x0, y0, x1, y1);
                escalas[i] = ppu * sy;
            }
            sprites = new Sprite[12];
            try
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i] = Sprite.Create(folha, recortes[i], quadros[i].pivot,
                        escalas[i], 0, SpriteMeshType.FullRect);
                    sprites[i].name = "Praca_Jogador_" + i;
                    sprites[i].hideFlags = HideFlags.DontSave;
                }
            }
            catch (Exception e)
            {
                Liberar(); Erro("Sprite.Create falhou: " + e.Message);
            }
        }
        private static bool Finito(float valor) { return !float.IsNaN(valor) && !float.IsInfinity(valor); }
        private void Erro(string motivo)
        {
            string tamanho = folha != null ? folha.width + " x " + folha.height : "ausente";
            Debug.LogError("Praça 1.0.1: " + motivo + " Textura importada: " + tamanho
                + "; referência: " + tamanhoReferencia.x + " x " + tamanhoReferencia.y
                + ". Fora do Play, execute Memorias Tecidas > Praca V1 > 3 - Reparar jogador da cena aberta.", this);
        }
        private void OnDisable() { Liberar(); reconstruir = true; }
        private void Liberar()
        {
            if (sprites == null) return;
            if (desenho != null && Array.IndexOf(sprites, desenho.sprite) >= 0) desenho.sprite = null;
            foreach (Sprite sprite in sprites)
            {
                if (sprite == null) continue;
                if (Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite);
            }
            sprites = null;
        }
    }
}

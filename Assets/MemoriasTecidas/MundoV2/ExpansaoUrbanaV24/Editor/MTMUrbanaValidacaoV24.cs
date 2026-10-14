using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMUrbanaValidacaoV24
    {
        [MenuItem("Memorias Tecidas/Expansao Urbana V2.4/5 - Conferir caminhos e conexoes")]
        public static void MenuValidar()
        {
            try { Validar(MTMUrbanaMontagemV24.Jogo(), true); }
            catch (Exception e) { Debug.LogException(e); }
            finally { EditorUtility.ClearProgressBar(); }
        }
        public static void Validar(MTMJogo jogo, bool gravar)
        {
            var log = new StringBuilder("CONFERENCIA DA EXPANSAO URBANA V2.4\n");
            var regs = jogo.regioes.ToDictionary(r => r.dados.id);
            foreach (string id in MTMUrbanaMontagemV24.Alvos)
                if (!regs.ContainsKey(id) || regs[id].GetComponent<MTMUrbanaV24>() == null) throw new InvalidOperationException("Monte os quatro lugares antes de conferir: falta " + id);
            var ids = new HashSet<string>();
            foreach (var r in jogo.regioes)
                foreach (var p in r.pontos ?? Array.Empty<MTMPonto>())
                {
                    if (p == null || p.dados == null || !ids.Add(p.dados.id)) throw new InvalidOperationException("Interação ausente ou ID repetido em " + r.dados.nome);
                    if (p.dados.tipo != "saida") continue;
                    if (!regs.TryGetValue(p.dados.destino, out var destino)) throw new InvalidOperationException("Destino ausente: " + p.dados.id);
                    if (!destino.pontos.Any(q => q.dados.tipo == "saida" && q.dados.destino == r.dados.id)) throw new InvalidOperationException("Passagem sem volta: " + p.dados.id);
                }
            var visitados = new HashSet<string> { "praca" }; var fila = new Queue<string>(); fila.Enqueue("praca");
            while (fila.Count > 0)
                foreach (var p in regs[fila.Dequeue()].pontos)
                    if (p.dados.tipo == "saida" && visitados.Add(p.dados.destino)) fila.Enqueue(p.dados.destino);
            if (visitados.Count != regs.Count) throw new InvalidOperationException("Há regiões desconectadas da Praça Matriz.");
            foreach (string evento in new[] { "pacote1", "pacote2", "pacote3", "mercado_entrega", "raimundo" })
                if (!regs["mercado"].pontos.Any(p => p.dados.evento == evento)) throw new InvalidOperationException("Evento do Mercado ausente: " + evento);
            bool[] ativos = jogo.regioes.Select(r => r.gameObject.activeSelf).ToArray();
            try
            {
                foreach (string id in MTMUrbanaMontagemV24.Alvos)
                {
                    var r = regs[id]; foreach (var outra in jogo.regioes) outra.gameObject.SetActive(outra == r);
                    foreach (var s in r.GetComponentsInChildren<MTMSuperficieV23>()) s.Reconstruir();
                    Physics2D.SyncTransforms();
                    EditorUtility.DisplayProgressBar("Conferindo circulação", r.dados.nome, Array.IndexOf(MTMUrbanaMontagemV24.Alvos, id) / 4f);
                    var grade = new Grade(r, jogo);
                    foreach (var p in r.pontos)
                    {
                        if (!p.gameObject.activeInHierarchy || p.dados.noturno) continue;
                        float alcance = p.dados.tipo == "saida" ? .55f : Mathf.Max(.2f, p.alcance - .30f);
                        if (!grade.Acessivel(p.transform.position, alcance)) throw new InvalidOperationException("Interação sem acesso: " + p.dados.id + ". Afaste o objeto ou a colisão pelo painel de edição.");
                    }
                    foreach (var origem in jogo.regioes)
                        foreach (var p in origem.pontos)
                        {
                            if (p.dados.tipo != "saida" || p.dados.destino != id) continue;
                            var chegada = new Vector2(p.dados.chegadaX, p.dados.chegadaY);
                            if (!grade.Livre(chegada) || !grade.Acessivel(chegada, .8f)) throw new InvalidOperationException("Chegada bloqueada: " + p.dados.id);
                            if (r.pontos.Any(q => q.dados.tipo == "saida" && Vector2.Distance(chegada, q.transform.position) < 1.3f)) throw new InvalidOperationException("Chegada muito perto de uma saída: " + p.dados.id);
                        }
                    log.AppendLine(r.dados.nome + ": " + grade.Contagem + " células acessíveis; interações e chegadas livres.");
                }
            }
            finally
            {
                for (int i = 0; i < jogo.regioes.Length; i++) jogo.regioes[i].gameObject.SetActive(ativos[i]);
                Physics2D.SyncTransforms(); EditorUtility.ClearProgressBar();
            }
            log.AppendLine("Grafo com ida e volta e cinco eventos essenciais do Mercado preservados.");
            log.AppendLine("A conferência é estática. Verifique movimento, animação, iluminação e trocas de região no Play.");
            if (gravar)
            {
                Directory.CreateDirectory(MTMUrbanaArtesV24.Raiz + "/Gerados");
                File.WriteAllText(MTMUrbanaArtesV24.Raiz + "/Gerados/Conferencia_Caminhos.txt", log.ToString()); AssetDatabase.Refresh();
            }
            Debug.Log(log.ToString());
        }
        private sealed class Grade
        {
            private const float Passo = .5f;
            private readonly MTMJogo jogo; private readonly MTMRegiao regiao;
            private readonly List<Vector2> acessiveis = new List<Vector2>();
            private readonly List<Collider2D> contatos = new List<Collider2D>();
            private ContactFilter2D filtro;
            private Vector2 tamanho = new Vector2(.56f, .39f), deslocamento = new Vector2(0, .12f);
            public int Contagem => acessiveis.Count;
            public Grade(MTMRegiao r, MTMJogo j)
            {
                regiao = r; jogo = j; filtro.SetLayerMask(Physics2D.GetLayerCollisionMask(j.gameObject.layer)); filtro.useTriggers = false;
                var jogador = j.GetComponent<Collider2D>();
                if (jogador != null && jogador.bounds.size.sqrMagnitude > .01f)
                { tamanho = (Vector2)jogador.bounds.size + Vector2.one * .14f; deslocamento = (Vector2)(jogador.bounds.center - j.transform.position); }
                Vector2 inicio = new Vector2(r.dados.seguroX, r.dados.seguroY);
                if (!Livre(inicio)) throw new InvalidOperationException("Ponto seguro bloqueado em " + r.dados.nome);
                float mx = -r.dados.largura / 2 + .75f, my = -r.dados.altura / 2 + .75f;
                int nx = Mathf.FloorToInt((r.dados.largura - 1.5f) / Passo) + 1, ny = Mathf.FloorToInt((r.dados.altura - 1.5f) / Passo) + 1;
                if ((long)nx * ny > 80000) throw new InvalidOperationException("Região grande demais para esta conferência: " + r.dados.nome);
                var livre = new bool[nx * ny]; var visto = new bool[nx * ny]; int semente = -1; float menor = float.PositiveInfinity;
                for (int y = 0; y < ny; y++) for (int x = 0; x < nx; x++)
                {
                    int i = y * nx + x; Vector2 p = new Vector2(mx + x * Passo, my + y * Passo); livre[i] = Livre(p);
                    float distancia = (p - inicio).sqrMagnitude;
                    if (livre[i] && distancia < menor) { menor = distancia; semente = i; }
                }
                if (semente < 0) throw new InvalidOperationException("Sem circulação em " + r.dados.nome);
                var fila = new Queue<int>(); fila.Enqueue(semente); visto[semente] = true;
                while (fila.Count > 0)
                {
                    int i = fila.Dequeue(), x = i % nx, y = i / nx; acessiveis.Add(new Vector2(mx + x * Passo, my + y * Passo));
                    foreach (var delta in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                    {
                        int xx = x + delta.x, yy = y + delta.y; if (xx < 0 || yy < 0 || xx >= nx || yy >= ny) continue;
                        int k = yy * nx + xx; if (!livre[k] || visto[k]) continue; visto[k] = true; fila.Enqueue(k);
                    }
                }
            }
            public bool Livre(Vector2 p)
            {
                if (Mathf.Abs(p.x) > regiao.dados.largura / 2 - .6f || Mathf.Abs(p.y) > regiao.dados.altura / 2 - .6f) return false;
                contatos.Clear(); Physics2D.OverlapBox(p + deslocamento, tamanho, 0, filtro, contatos);
                foreach (var c in contatos)
                {
                    if (c.transform == jogo.transform || c.transform.IsChildOf(jogo.transform)) continue;
                    if (c.GetComponentInParent<MTMAtorV22>() != null || c.GetComponentInChildren<MTMAtorV22>(true) != null) continue;
                    return false;
                }
                return true;
            }
            public bool Acessivel(Vector2 p, float alcance) => acessiveis.Any(q => (q - p).sqrMagnitude <= alcance * alcance);
        }
    }
}

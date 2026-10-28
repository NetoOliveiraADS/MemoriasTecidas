using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [Serializable] public sealed class MTMKitHD2DDados
    {
        public int versao;
        public bool ativo;
        public bool preservarCenarioOriginal;
        public MTMQuadroHD2D[] neto, objetos;
        public MTMTrocaHD2D[] trocas;
        public MTMSuperficieHD2D[] superficies;
        public MTMPisoHD2D[] pisos;
        public MTMCartaoHD2D[] cartoes;
    }
    [Serializable] public sealed class MTMQuadroHD2D
    {
        public string id, folha;
        public int x, y, largura, altura, folhaLargura, folhaAltura;
        public float pivotX, pivotY, alturaReferencia;
    }
    [Serializable] public sealed class MTMTrocaHD2D
    {
        public string[] origens, regioes;
        public string destino;
        public int lado;
        public bool ajustarAltura;
    }
    [Serializable] public class MTMSuperficieHD2D
    {
        public string[] origens;
        public string textura;
    }
    [Serializable] public sealed class MTMPisoHD2D : MTMSuperficieHD2D
    {
        public float mistura = .28f, repeticao = 6f;
    }
    [Serializable] public sealed class MTMCartaoHD2D
    {
        public string regiao, textura;
    }

    // Instâncias apenas durante o Play. Não grava cenas, prefabs nem dados de missões.
    public sealed class MTMKitHD2D : IDisposable
    {
        public const string Pasta = "MemoriasTecidas/HD2D/";
        private readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, MTMQuadroHD2D> objetos = new Dictionary<string, MTMQuadroHD2D>();
        private readonly Dictionary<string, Texture2D> cartoes = new Dictionary<string, Texture2D>();
        private readonly List<Action> restaurar = new List<Action>();
        private Material materialPiso;
        private MTMKitHD2DDados dados;
        private bool aplicado;
        public int Personagens { get; private set; }
        public int Objetos { get; private set; }
        public int Superficies { get; private set; }
        public int Pisos { get; private set; }
        public int Retratos { get; private set; }

        private Texture2D Textura(string nome)
        {
            if (string.IsNullOrEmpty(nome)) throw new InvalidOperationException("Nome de textura vazio.");
            Texture2D t;
            if (texturas.TryGetValue(nome, out t)) return t;
            t = Resources.Load<Texture2D>(Pasta + nome);
            if (t == null) throw new InvalidOperationException("Recurso ausente: " + Pasta + nome);
            texturas.Add(nome, t);
            return t;
        }

        public void ValidarRecursos()
        {
            var json = Resources.Load<TextAsset>(Pasta + "kit_hd2d");
            if (json == null) throw new InvalidOperationException("kit_hd2d.json ausente.");
            dados = JsonUtility.FromJson<MTMKitHD2DDados>(json.text);
            if (dados == null || dados.versao != 1 || dados.neto == null || dados.neto.Length != 32 ||
                dados.objetos == null || dados.trocas == null || dados.superficies == null ||
                dados.pisos == null || dados.cartoes == null)
                throw new InvalidOperationException("Configuração HD2D incompleta ou versão incompatível.");
            var ids = new HashSet<string>();
            string[] direcoes = { "S", "SW", "W", "NW", "N", "NE", "E", "SE" };
            for (int i = 0; i < dados.neto.Length; i++)
            {
                ValidarQuadro(dados.neto[i], ids);
                if (dados.neto[i].id != "Neto_" + direcoes[i / 4] + "_" + (i % 4).ToString("00"))
                    throw new InvalidOperationException("Ordem das oito direções do Neto incorreta.");
            }
            objetos.Clear();
            if (!dados.preservarCenarioOriginal)
            {
                foreach (var q in dados.objetos) { ValidarQuadro(q, ids); objetos.Add(q.id, q); }
                foreach (var t in dados.trocas)
                    if (t == null || t.origens == null || t.origens.Length == 0 || t.regioes == null ||
                        string.IsNullOrEmpty(t.destino) || !objetos.ContainsKey(t.destino) || t.lado < -1 || t.lado > 1)
                        throw new InvalidOperationException("Regra de objeto HD2D inválida.");
                foreach (var s in dados.superficies) ValidarSuperficie(s);
                foreach (var p in dados.pisos)
                {
                    ValidarSuperficie(p);
                    if (!Finito(p.mistura) || p.mistura < 0 || p.mistura > .5f || !Finito(p.repeticao) || p.repeticao <= 0)
                        throw new InvalidOperationException("Mistura do piso deve preservar ao menos metade da arte original.");
                }
                cartoes.Clear();
                foreach (var c in dados.cartoes)
                {
                    if (c == null || string.IsNullOrEmpty(c.regiao) || cartoes.ContainsKey(c.regiao))
                        throw new InvalidOperationException("Região de ilustração ausente ou repetida.");
                    cartoes.Add(c.regiao, Textura(c.textura));
                }
            }
            else cartoes.Clear();
            Textura("FelicianaRetrato");
            if (!dados.preservarCenarioOriginal && Resources.Load<Shader>(Pasta + "TerrenoDetalhe") == null)
                throw new InvalidOperationException("Shader TerrenoDetalhe ausente.");
        }

        private void ValidarSuperficie(MTMSuperficieHD2D s)
        {
            if (s == null || s.origens == null || s.origens.Length == 0)
                throw new InvalidOperationException("Regra de superfície vazia.");
            Textura(s.textura);
        }
        private void ValidarQuadro(MTMQuadroHD2D q, HashSet<string> ids)
        {
            if (q == null || string.IsNullOrEmpty(q.id) || !ids.Add(q.id))
                throw new InvalidOperationException("Quadro ausente ou repetido.");
            var t = Textura(q.folha);
            if (t.width != q.folhaLargura || t.height != q.folhaAltura || q.x < 0 || q.y < 0 ||
                q.largura <= 0 || q.altura <= 0 || (long)q.x + q.largura > t.width || (long)q.y + q.altura > t.height ||
                !Finito(q.pivotX) || !Finito(q.pivotY) || q.pivotX < 0 || q.pivotX > 1 || q.pivotY < 0 || q.pivotY > 1 ||
                !Finito(q.alturaReferencia) || q.alturaReferencia <= 0)
                throw new InvalidOperationException("Recorte ou importação inválida: " + q.id + ". Use tamanho 2048 e Non Power of 2 = None.");
        }
        private static bool Finito(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }

        private Sprite CriarSprite(MTMQuadroHD2D q, float ppu, string chave)
        {
            Sprite s;
            if (sprites.TryGetValue(chave, out s)) return s;
            s = Sprite.Create(Textura(q.folha), new Rect(q.x, q.y, q.largura, q.altura),
                new Vector2(q.pivotX, q.pivotY), Mathf.Max(1f, ppu), 0, SpriteMeshType.FullRect);
            s.name = "HD2D_" + q.id;
            s.hideFlags = HideFlags.DontSave;
            sprites.Add(chave, s);
            return s;
        }

        public void Aplicar(MTMJogo jogo)
        {
            if (aplicado) return;
            ValidarRecursos(); // Verifica tudo antes de substituir qualquer referência.
            if (!dados.ativo) return;
            try
            {
                foreach (var ator in jogo.GetComponentsInChildren<MTMAtorV22>(true))
                    if (ator.tipo == MTMAtorV22.Tipo.Jogador) AplicarNeto(ator);
                var vistos = new HashSet<int>();
                var pontos = new HashSet<MTMPonto>();
                if (jogo.regioes != null) foreach (var regiao in jogo.regioes)
                {
                    if (regiao == null || regiao.dados == null) continue;
                    if (!dados.preservarCenarioOriginal)
                    {
                        foreach (var sr in regiao.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            if (!vistos.Add(sr.GetInstanceID()) || sr.sprite == null || Sombra(sr)) continue;
                            AplicarObjeto(sr, regiao);
                            AplicarPiso(sr);
                        }
                        foreach (var sup in regiao.GetComponentsInChildren<MTMSuperficieV23>(true)) AplicarSuperficie(sup);
                    }
                    foreach (var ponto in regiao.GetComponentsInChildren<MTMPonto>(true)) pontos.Add(ponto);
                    if (regiao.pontos != null) foreach (var ponto in regiao.pontos) if (ponto != null) pontos.Add(ponto);
                }
                foreach (var ponto in pontos) AplicarRetrato(ponto);
                aplicado = true;
                if (Personagens == 0) Debug.LogWarning("HD2D: não foi encontrado o animador MTMAtorV22 do Neto. A aparência anterior foi mantida.", jogo);
                if (dados.preservarCenarioOriginal)
                    Debug.Log("Cenário original preservado: sprites, texturas, materiais e ilustrações dos lugares mantidos. " +
                        Personagens + " protagonista(s) e " + Retratos + " retrato(s) atualizados.", jogo);
                else Debug.Log("HD2D integrado: " + Personagens + " protagonista(s), " + Objetos + " objetos, " +
                    Superficies + " superfícies, " + Pisos + " pisos e " + Retratos + " retrato(s).", jogo);
            }
            catch { Dispose(); throw; }
        }

        private void AplicarNeto(MTMAtorV22 ator)
        {
            var sr = ator.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            Sprite anterior = sr.sprite;
            Sprite[] anteriores = ator.quadros;
            float altura = Mathf.Max(.1f, anterior.rect.height / anterior.pixelsPerUnit);
            var novos = new Sprite[32];
            for (int i = 0; i < novos.Length; i++)
                novos[i] = CriarSprite(dados.neto[i], dados.neto[i].alturaReferencia / altura,
                    "neto:" + ator.GetInstanceID() + ":" + i);
            restaurar.Add(() => { if (ator != null) ator.quadros = anteriores; if (sr != null) sr.sprite = anterior; });
            ator.quadros = novos;
            sr.sprite = novos[0];
            Personagens++;
        }

        private static bool Sombra(SpriteRenderer sr)
        {
            if (sr.name.IndexOf("sombra", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var visual = sr.GetComponentInParent<MTMVisualV2>();
            return visual != null && visual.sombra == sr;
        }
        private static bool Nome(string[] nomes, string nome)
        {
            if (nomes == null || string.IsNullOrEmpty(nome)) return false;
            foreach (var n in nomes) if (string.Equals(n, nome, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static bool Corresponde(string[] nomes, Sprite s)
        { return s != null && (Nome(nomes, s.name) || (s.texture != null && Nome(nomes, s.texture.name))); }

        private void AplicarObjeto(SpriteRenderer sr, MTMRegiao regiao)
        {
            foreach (var t in dados.trocas)
            {
                if (!Corresponde(t.origens, sr.sprite) || (t.regioes.Length > 0 && !Nome(t.regioes, regiao.dados.id))) continue;
                float x = regiao.transform.InverseTransformPoint(sr.transform.position).x;
                if ((t.lado < 0 && x >= 0) || (t.lado > 0 && x < 0)) continue;
                Sprite anterior = sr.sprite;
                var q = objetos[t.destino];
                float tamanho = t.ajustarAltura ? anterior.rect.height / anterior.pixelsPerUnit : anterior.rect.width / anterior.pixelsPerUnit;
                float ppu = (t.ajustarAltura ? q.altura : q.largura) / Mathf.Max(.01f, tamanho);
                var novo = CriarSprite(q, ppu, "objeto:" + anterior.GetInstanceID() + ":" + q.id + ":" + t.ajustarAltura);
                restaurar.Add(() => { if (sr != null) sr.sprite = anterior; });
                sr.sprite = novo;
                Objetos++;
                return;
            }
        }

        private void AplicarSuperficie(MTMSuperficieV23 sup)
        {
            if (sup.textura == null) return;
            foreach (var regra in dados.superficies)
            {
                if (!Nome(regra.origens, sup.textura.name)) continue;
                var renderer = sup.GetComponent<MeshRenderer>();
                var anterior = sup.textura;
                var blocoAnterior = new MaterialPropertyBlock();
                if (renderer != null) renderer.GetPropertyBlock(blocoAnterior);
                restaurar.Add(() => { if (sup != null) sup.textura = anterior; if (renderer != null) renderer.SetPropertyBlock(blocoAnterior); });
                sup.textura = Textura(regra.textura);
                if (renderer != null)
                {
                    var bloco = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(bloco);
                    bloco.SetTexture("_MainTex", sup.textura);
                    renderer.SetPropertyBlock(bloco);
                }
                // Contorno, material, água animada, colisor e ordenação permanecem no componente original.
                Superficies++;
                return;
            }
        }

        private void AplicarPiso(SpriteRenderer sr)
        {
            foreach (var regra in dados.pisos)
            {
                if (!Corresponde(regra.origens, sr.sprite)) continue;
                if (materialPiso == null)
                {
                    materialPiso = new Material(Resources.Load<Shader>(Pasta + "TerrenoDetalhe"));
                    materialPiso.name = "HD2D_PisoInstancia";
                    materialPiso.hideFlags = HideFlags.DontSave;
                }
                Material anterior = sr.sharedMaterial;
                var blocoAnterior = new MaterialPropertyBlock();
                sr.GetPropertyBlock(blocoAnterior);
                restaurar.Add(() => { if (sr != null) { sr.sharedMaterial = anterior; sr.SetPropertyBlock(blocoAnterior); } });
                sr.sharedMaterial = materialPiso;
                var bloco = new MaterialPropertyBlock();
                sr.GetPropertyBlock(bloco);
                bloco.SetTexture("_DetailTex", Textura(regra.textura));
                bloco.SetFloat("_Mistura", regra.mistura);
                bloco.SetFloat("_Repeticao", regra.repeticao);
                sr.SetPropertyBlock(bloco);
                Pisos++;
                return;
            }
        }
        private void AplicarRetrato(MTMPonto ponto)
        {
            if (ponto == null || ponto.dados == null || ponto.dados.id != "npc_feliciana") return;
            var anterior = ponto.retrato;
            var recorte = ponto.recorteRosto;
            restaurar.Add(() => { if (ponto != null) { ponto.retrato = anterior; ponto.recorteRosto = recorte; } });
            ponto.retrato = Textura("FelicianaRetrato");
            ponto.recorteRosto = new Rect(0, 0, 1, 1);
            Retratos++;
        }
        public Texture2D Imagem(string regiao)
        {
            Texture2D t;
            return aplicado && regiao != null && cartoes.TryGetValue(regiao, out t) ? t : null;
        }
        public void Dispose()
        {
            for (int i = restaurar.Count - 1; i >= 0; i--) restaurar[i]();
            restaurar.Clear();
            foreach (var s in sprites.Values) Destruir(s);
            sprites.Clear();
            Destruir(materialPiso);
            materialPiso = null;
            texturas.Clear(); objetos.Clear(); cartoes.Clear();
            Personagens = Objetos = Superficies = Pisos = Retratos = 0;
            aplicado = false;
        }
        private static void Destruir(UnityEngine.Object objeto)
        {
            if (objeto == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(objeto);
            else UnityEngine.Object.DestroyImmediate(objeto);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private readonly Dictionary<string, MTMPonto> pontosNovosRoteiro = new Dictionary<string, MTMPonto>();
        private readonly List<UnityEngine.Object> recursosRoteiro = new List<UnityEngine.Object>();
        private GameObject festaRoteiro;
        private MTMMissaoTecido missaoEscolta, missaoAparicao;
        private MTMEtapaTecido etapaEscolta, etapaAparicao;
        private int passoEscolta;
        private float fimAparicao, proximaRevisaoRoteiro;
        private AudioSource sonsRioRoteiro;
        private Sprite placaRoteiro;
        private MTMAtorV22 ModeloAtorRoteiro(string regiao, string ponto)
        {
            var r = RegiaoTecido(regiao);
            var p = PontoTecido(regiao, ponto);
            if (p == null)
            {
                r = MTMVinculosRegiao.RegiaoDoPonto(regioes, ponto);
                if (r != null)
                    p = PontoTecido(r.dados.id, ponto);
            }

            if (r == null || p == null)
                return null;
            foreach (var ator in r.GetComponentsInChildren<MTMAtorV22>(true))
                if (ator.nomeDaConversa == p.dados.nome)
                    return ator;
            return null;
        }

        private void CopiarPessoaRoteiro(Transform pai, MTMAtorV22 modelo, string nome, Vector2 offset)
        {
            if (modelo == null)
            {
                CopiarObjetoRoteiro(pai, "", offset);
                return;
            }

            var original = modelo.GetComponent<SpriteRenderer>();
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localPosition = offset;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = original.sprite;
            sr.sharedMaterial = original.sharedMaterial;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            go.transform.localScale = original.transform.lossyScale;
            go.AddComponent<MTMOrdemY>();
            var ator = go.AddComponent<MTMAtorV22>();
            ator.tipo = MTMAtorV22.Tipo.Morador;
            ator.quadros = modelo.quadros;
            ator.jogo = this;
            ator.vida = GetComponent<MTMVidaV22>();
            ator.nomeDaConversa = nome;
        }

        private Sprite PlacaRoteiro()
        {
            if (placaRoteiro != null)
                return placaRoteiro;
            var tex = new Texture2D(48, 48, TextureFormat.RGBA32, false)
            {
                name = "MT_Placa_Roteiro",
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.DontSave
            };
            var cores = new Color32[48 * 48];
            for (int y = 0; y < 48; y++)
                for (int x = 0; x < 48; x++)
                {
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (x >= 22 && x <= 25 && y < 18)
                        c = new Color32(111, 70, 42, 255);
                    if (x >= 3 && x < 45 && y >= 16 && y < 44)
                        c = new Color32(79, 57, 40, 255);
                    if (x >= 6 && x < 42 && y >= 19 && y < 41)
                        c = new Color32(234, 220, 192, 255);
                    if (x >= 11 && x < 37 && y >= 24 && y < 36 && (Math.Abs((x - 12) - (y - 24)) < 2 || Math.Abs((36 - x) - (y - 24)) < 2))
                        c = new Color32(185, 108, 77, 255);
                    cores[y * 48 + x] = c;
                }

            tex.SetPixels32(cores);
            tex.Apply(false, true);
            recursosRoteiro.Add(tex);
            placaRoteiro = Sprite.Create(tex, new Rect(0, 0, 48, 48), new Vector2(.5f, 0), 42);
            recursosRoteiro.Add(placaRoteiro);
            return placaRoteiro;
        }

        private void CopiarObjetoRoteiro(Transform pai, string modelo, Vector2 offset)
        {
            SpriteRenderer origem = null;
            if (!string.IsNullOrEmpty(modelo))
                foreach (var r in regioes)
                {
                    if (r == null)
                        continue;
                    foreach (var sr in r.GetComponentsInChildren<SpriteRenderer>(true))
                        if (sr.sprite != null && sr.gameObject.name != "Sombra" && sr.transform.parent != pai && (sr.name.IndexOf(modelo, StringComparison.OrdinalIgnoreCase) >= 0 || sr.transform.parent != null && sr.transform.parent.name.IndexOf(modelo, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            origem = sr;
                            break;
                        }

                    if (origem != null)
                        break;
                }

            var go = new GameObject("Objeto do roteiro");
            go.transform.SetParent(pai, false);
            go.transform.localPosition = offset;
            var desenho = go.AddComponent<SpriteRenderer>();
            desenho.sprite = origem == null ? PlacaRoteiro() : origem.sprite;
            desenho.spriteSortPoint = SpriteSortPoint.Pivot;
            if (origem != null)
            {
                desenho.sharedMaterial = origem.sharedMaterial;
                float tamanho = Mathf.Max(.01f, origem.sprite.bounds.size.y);
                float escala = Mathf.Min(origem.transform.lossyScale.x, 1.6f / tamanho);
                go.transform.localScale = Vector3.one * escala;
            }

            go.AddComponent<MTMOrdemY>();
        }

        private void CriarPontosRoteiro()
        {
            foreach (var def in tramaTecida.pontosNovos)
            {
                var r = RegiaoTecido(def.regiao);
                if (r == null)
                    throw new InvalidOperationException("Região ausente: " + def.regiao);
                if (PontoTecido(def.regiao, def.id) != null)
                    continue;
                var go = new GameObject("Roteiro_" + def.id);
                go.transform.SetParent(r.transform, false);
                go.transform.localPosition = new Vector3(def.x, def.y, 0);
                var p = go.AddComponent<MTMPonto>();
                p.dados = new MTMPontoDados
                {
                    id = def.id,
                    nome = def.nome,
                    tipo = "roteiro",
                    x = def.x,
                    y = def.y,
                    fonte = FonteRoteiro,
                    falas = new[]
                    {
                        def.nome
                    }
                };
                p.alcance = def.visual == "silhueta" ? 8f : 1.8f;
                if (def.andar)
                {
                    var andar = go.AddComponent<MTMCaminhanteV22>();
                    andar.vida = GetComponent<MTMVidaV22>();
                    andar.pausa = 1.4f;
                    andar.velocidade = .45f;
                    andar.pontos = Array.ConvertAll(def.patrulha, v => new Vector2(v.x, v.y));
                }

                if (def.visual == "pessoa" || def.visual == "procissao")
                {
                    var modelo = ModeloAtorRoteiro(def.modeloRegiao, def.modeloPonto);
                    CopiarPessoaRoteiro(go.transform, modelo, def.nome, Vector2.zero);
                    var fonte = PontoTecido(def.modeloRegiao, def.modeloPonto);
                    if (fonte != null)
                    {
                        p.retrato = fonte.retrato;
                        p.recorteRosto = fonte.recorteRosto;
                    }

                    if (def.visual == "procissao")
                    {
                        CopiarPessoaRoteiro(go.transform, ModeloAtorRoteiro("vila", "npc_cida"), "Moradora da procissão", new Vector2(-1, -.5f));
                        CopiarPessoaRoteiro(go.transform, ModeloAtorRoteiro("rua", "npc_zeca"), "Morador da procissão", new Vector2(1, -.5f));
                    }
                }
                else if (def.visual == "reuniao")
                {
                    // Moradores transferidos para outra região também participam desta reunião.
                    var localFeliciana = MTMVinculosRegiao.RegiaoDoPonto(regioes, "npc_feliciana");
                    if (localFeliciana != null && localFeliciana != r)
                        CopiarPessoaRoteiro(go.transform, ModeloAtorRoteiro(localFeliciana.dados.id, "npc_feliciana"), "Dona Feliciana · reunião", new Vector2(-3.8f, 2.4f));
                    var localSimao = MTMVinculosRegiao.RegiaoDoPonto(regioes, "npc_simao");
                    if (localSimao != null && localSimao != r)
                        CopiarPessoaRoteiro(go.transform, ModeloAtorRoteiro(localSimao.dados.id, "npc_simao"), "Simão de Góis · reunião", new Vector2(3.8f, 2.4f));
                    CopiarPessoaRoteiro(go.transform, ModeloAtorRoteiro("fabrica", "pdf_artesao"), "Artesão", new Vector2(-2, 0));
                    CopiarPessoaRoteiro(go.transform, ModeloAtorRoteiro("mercado", "npc_feirante"), "Seu Bento", new Vector2(2, 0));
                }
                else if (def.visual == "silhueta" || def.visual == "sombra")
                {
                    var rio = RegiaoTecido("rio");
                    SpriteRenderer origem = null;
                    if (rio != null && rio.aparicoes != null)
                        foreach (var figura in rio.aparicoes)
                        {
                            if (figura != null)
                                origem = figura.GetComponentInChildren<SpriteRenderer>(true);
                            if (origem != null)
                                break;
                        }

                    if (origem != null)
                    {
                        var visual = new GameObject("Silhueta ficcional");
                        visual.transform.SetParent(go.transform, false);
                        var sr = visual.AddComponent<SpriteRenderer>();
                        sr.sprite = origem.sprite;
                        sr.color = new Color(.12f, .18f, .19f, 0);
                        visual.transform.localScale = Vector3.one * (2.6f / Mathf.Max(.1f, origem.sprite.bounds.size.y));
                        visual.AddComponent<MTMOrdemY>();
                    }
                    else
                        CopiarPessoaRoteiro(go.transform, ModeloAtorRoteiro("rua", "npc_zeca"), "Silhueta ficcional", Vector2.zero);
                }
                else
                    CopiarObjetoRoteiro(go.transform, def.modeloObjeto, Vector2.zero);
                var lista = new List<MTMPonto>(r.pontos ?? new MTMPonto[0]);
                lista.Add(p);
                r.pontos = lista.ToArray();
                pontosNovosRoteiro.Add(def.id, p);
            }

            var praca = RegiaoTecido("praca");
            festaRoteiro = new GameObject("Celebracao_da_praca");
            festaRoteiro.transform.SetParent(praca.transform, false);
            for (int i = 0; i < 6; i++)
                CopiarObjetoRoteiro(festaRoteiro.transform, i % 2 == 0 ? "arbusto_florido" : "poste_praca", new Vector2(-12 + i * 4, -14));
            CriarSonsRioRoteiro();
            AtualizarVisibilidadeRoteiro();
        }

        private void CriarSonsRioRoteiro()
        {
            var r = RegiaoTecido("rio");
            if (r == null)
                return;
            var go = new GameObject("Sons noturnos da margem");
            go.transform.SetParent(r.transform, false);
            go.transform.localPosition = new Vector3(10, -15, 0);
            sonsRioRoteiro = go.AddComponent<AudioSource>();
            sonsRioRoteiro.loop = true;
            sonsRioRoteiro.playOnAwake = false;
            sonsRioRoteiro.spatialBlend = .65f;
            sonsRioRoteiro.volume = .12f;
            sonsRioRoteiro.minDistance = 5;
            sonsRioRoteiro.maxDistance = 30;
            const int hz = 22050;
            var amostras = new float[hz * 4];
            var random = new System.Random(24);
            float filtrado = 0;
            for (int i = 0; i < amostras.Length; i++)
            {
                float t = i / (float)hz;
                filtrado = filtrado * .96f + (float)(random.NextDouble() * 2 - 1) * .04f;
                float borda = Mathf.Min(1f, Mathf.Min(t, 4f - t) * 4f);
                amostras[i] = borda * (filtrado * .55f + Mathf.Sin(t * 2 * Mathf.PI * 95f) * Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), 8) * .08f);
            }

            var clip = AudioClip.Create("Ambiente ficcional da margem", amostras.Length, 1, hz, false);
            clip.SetData(amostras, 0);
            sonsRioRoteiro.clip = clip;
            recursosRoteiro.Add(clip);
        }

        private void AtualizarVisibilidadeRoteiro()
        {
            if (tramaTecida == null)
                return;
            foreach (var def in tramaTecida.pontosNovos)
            {
                MTMPonto p;
                if (!pontosNovosRoteiro.TryGetValue(def.id, out p) || p == null)
                    continue;
                MTMMissaoTecido m;
                MTMEtapaTecido e;
                bool tem = EtapaNoPontoRoteiro(def.id, out m, out e);
                bool pessoa = def.visual == "pessoa";
                bool visivel = pessoa || tem && MTMMotorMissoes.HorarioCorreto(e, estado.relogio);
                if (!string.IsNullOrEmpty(def.habilitarEvento))
                    visivel = estado.Tem(def.habilitarEvento);
                if (def.visual == "sombra")
                {
                    var arco = MissaoTecido(def.regiao == "rio" ? "m18" : "m23");
                    visivel = visivel && Noite && DisponivelTecido(arco) && !MTMTrama.Concluida(estado, arco) && Mathf.Repeat(Time.unscaledTime, 12f) < 3f;
                }

                if (p.gameObject.activeSelf != visivel)
                    p.gameObject.SetActive(visivel);
                if (def.visual == "silhueta")
                    foreach (var sr in p.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        Color c = sr.color;
                        c.a = etapaAparicao != null ? Mathf.Clamp01((fimAparicao - Time.unscaledTime) / 1.2f) * .85f : 0;
                        sr.color = c;
                    }

                if (def.visual == "sombra")
                    foreach (var sr in p.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.color = new Color(.12f, .16f, .18f, Mathf.Clamp01(3f - Mathf.Repeat(Time.unscaledTime, 12f)) * .7f);
            }

            if (festaRoteiro != null)
                festaRoteiro.SetActive(estado.Tem(tramaTecida.eventoFesta));
            if (sonsRioRoteiro != null)
            {
                var m = MissaoTecido("m18");
                bool tocar = atual != null && atual.dados.id == "rio" && Noite && DisponivelTecido(m) && !MTMTrama.Concluida(estado, m);
                if (tocar && !sonsRioRoteiro.isPlaying)
                    sonsRioRoteiro.Play();
                else if (!tocar && sonsRioRoteiro.isPlaying)
                    sonsRioRoteiro.Stop();
            }
        }

        private void IniciarEscoltaRoteiro(MTMMissaoTecido m, MTMEtapaTecido e)
        {
            missaoEscolta = m;
            etapaEscolta = e;
            passoEscolta = 1;
            var p = PontoTecido(e.regiao, e.ponto);
            if (p != null && e.percurso != null && e.percurso.Length > 0)
                p.transform.localPosition = new Vector3(e.percurso[0].x, e.percurso[0].y, 0);
            Avisar("Caminhe junto à comunidade. O grupo espera se você se afastar.");
        }

        private void IniciarAparicaoRoteiro(MTMMissaoTecido m, MTMEtapaTecido e)
        {
            missaoAparicao = m;
            etapaAparicao = e;
            fimAparicao = Time.unscaledTime + 3f;
            painel = "observacao";
            Parar();
            AtualizarVisibilidadeRoteiro();
        }

        private void AtualizarRoteiro()
        {
            if (tramaTecida == null)
                return;
            if (Time.unscaledTime >= proximaRevisaoRoteiro)
            {
                proximaRevisaoRoteiro = Time.unscaledTime + .15f;
                AtualizarVisibilidadeRoteiro();
            }

            if (etapaAparicao != null)
            {
                if (painel != "observacao")
                {
                    etapaAparicao = null;
                    return;
                }

                if (Time.unscaledTime >= fimAparicao)
                {
                    var e = etapaAparicao;
                    var m = missaoAparicao;
                    etapaAparicao = null;
                    painel = "";
                    Abrir("Uma forma na noite", FonteRoteiro, e.falas, () => ConcluirEtapaRoteiro(m, e, null, false));
                    AtualizarVisibilidadeRoteiro();
                }

                return;
            }

            if (Bloqueado || atual == null)
                return;
            if (etapaEscolta != null && atual.dados.id == etapaEscolta.regiao)
            {
                var p = PontoTecido(etapaEscolta.regiao, etapaEscolta.ponto);
                if (p != null && etapaEscolta.percurso != null && Vector2.Distance(transform.position, p.transform.position) <= 5f)
                {
                    if (passoEscolta < etapaEscolta.percurso.Length)
                    {
                        var v = etapaEscolta.percurso[passoEscolta];
                        var alvo = new Vector3(v.x, v.y, 0);
                        p.transform.localPosition = Vector3.MoveTowards(p.transform.localPosition, alvo, 1.15f * Time.deltaTime);
                        if (Vector3.Distance(p.transform.localPosition, alvo) < .05f)
                            passoEscolta++;
                    }

                    if (passoEscolta >= etapaEscolta.percurso.Length)
                    {
                        var e = etapaEscolta;
                        var m = missaoEscolta;
                        etapaEscolta = null;
                        ConcluirEtapaRoteiro(m, e, null, true);
                    }
                }
            }

            foreach (var m in tramaTecida.missoes)
            {
                if (!DisponivelTecido(m))
                    continue;
                var e = EtapaAtualTecido(m);
                if (e == null || e.acao != "aproximar" || e.regiao != atual.dados.id || !MTMMotorMissoes.HorarioCorreto(e, estado.relogio))
                    continue;
                var p = PontoTecido(e.regiao, e.ponto);
                if (p != null && Vector2.Distance(transform.position, p.transform.position) < 2.2f)
                {
                    ConcluirEtapaRoteiro(m, e, null, false);
                    break;
                }
            }
        }

        private void LimparRecursosRoteiro()
        {
            foreach (var recurso in recursosRoteiro)
                if (recurso != null)
                    Destroy(recurso);
            recursosRoteiro.Clear();
        }
    }
}

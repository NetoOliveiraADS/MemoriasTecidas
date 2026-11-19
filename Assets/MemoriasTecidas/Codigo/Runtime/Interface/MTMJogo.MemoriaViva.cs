using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Memória viva: comparar uma foto antiga com a praça de hoje, ouvir uma pessoa com atenção
    // e escutar os sons da cidade. As regras ficam em MTMMemoriaViva.cs (testadas fora do Unity);
    // o conteúdo vem de Resources/MemoriasTecidas/memoria_viva.json.
    public sealed partial class MTMJogo
    {
        private const string FonteMemoriaViva = "Memória viva · Memórias Tecidas";
        private const string PrefixoPontoVivo = "mvp_";
        private sealed class SomVivoCena
        {
            public MTMSomVivo dados;
            public MTMPonto ancora;
            public Vector3 Posicao { get { return ancora.transform.position + new Vector3(dados.dx, dados.dy, 0f); } }
        }

        private MTMMemoriaVivaDados memoriaViva;
        private readonly List<MTMPonto> pontosVivos = new List<MTMPonto>();
        private readonly Dictionary<string, MTMFotoViva> fotosPorPonto = new Dictionary<string, MTMFotoViva>();
        private readonly Dictionary<string, MTMEntrevistaViva> entrevistasPorPonto = new Dictionary<string, MTMEntrevistaViva>();
        private readonly Dictionary<MTMPonto, SpriteRenderer> marcadoresVivos = new Dictionary<MTMPonto, SpriteRenderer>();
        private readonly List<SomVivoCena> sonsVivos = new List<SomVivoCena>();
        private readonly Dictionary<string, float> escutaAcumulada = new Dictionary<string, float>();
        private Texture2D anelVivo, marcadorVivo;
        private Sprite spriteMarcadorVivo;
        private bool escutandoViva;
        // Foto antiga
        private MTMFotoViva fotoAtual;
        private Texture2D fotoTextura;
        private readonly HashSet<int> fotoAchadas = new HashSet<int>();
        private float fotoOpacidade = .55f;
        private int fotoDestaque = -1;
        private string fotoMensagem = "";
        // Entrevista
        private MTMEntrevistaViva entrevistaAtual;
        private MTMSessaoEntrevista sessaoViva;
        private string[] transcricaoViva = new string[0];
        // Caderno
        private string achadoSelecionado = "";

        private bool EmMemoriaViva()
        {
            return painel == "foto_viva" || painel == "entrevista_viva";
        }

        private bool PontoVivo(MTMPonto p)
        {
            return p != null && p.dados != null && p.dados.id != null && p.dados.id.StartsWith(PrefixoPontoVivo, StringComparison.Ordinal);
        }

        // ---------- carregamento ----------

        private void PrepararMemoriaViva()
        {
            memoriaViva = null;
            try
            {
                var arquivo = Resources.Load<TextAsset>("MemoriasTecidas/memoria_viva");
                if (arquivo == null)
                    return;
                var dados = JsonUtility.FromJson<MTMMemoriaVivaDados>(arquivo.text);
                MTMMemoriaViva.Validar(dados);
                if (dados.oculto)
                    return;
                memoriaViva = dados;
            }
            catch (Exception erro)
            {
                Debug.LogWarning("A memória viva não foi carregada: " + erro.Message + " O resto do jogo continua.", this);
                return;
            }

            var eventosDaTrama = new HashSet<string>();
            if (tramaTecida != null && tramaTecida.missoes != null)
                foreach (var m in tramaTecida.missoes)
                    foreach (var e in m.etapas)
                        foreach (string ev in e.eventos)
                            eventosDaTrama.Add(ev);
            if (memoriaViva.fotos != null)
                foreach (var f in memoriaViva.fotos)
                {
                    if (!RequerConhecido(f.id, f.requer, eventosDaTrama))
                        continue;
                    var ponto = CriarPontoVivo(f.regiao, f.ancora, f.dx, f.dy, PrefixoPontoVivo + f.id, f.nome);
                    if (ponto != null)
                        fotosPorPonto[ponto.dados.id] = f;
                }

            if (memoriaViva.entrevistas != null)
                foreach (var e in memoriaViva.entrevistas)
                {
                    if (!RequerConhecido(e.id, e.requer, eventosDaTrama))
                        continue;
                    var ponto = CriarPontoVivo(e.regiao, e.npc, e.dx, e.dy, PrefixoPontoVivo + e.id, e.titulo);
                    if (ponto != null)
                        entrevistasPorPonto[ponto.dados.id] = e;
                }

            if (memoriaViva.sons != null)
                foreach (var s in memoriaViva.sons)
                {
                    if (!RequerConhecido(s.id, s.requer, eventosDaTrama))
                        continue;
                    var ancora = PontoTecido(s.regiao, s.ancora);
                    if (ancora == null)
                    {
                        Debug.LogWarning("Som '" + s.id + "': a âncora '" + s.ancora + "' não existe na região '" + s.regiao + "'. O som foi ignorado.", this);
                        continue;
                    }

                    sonsVivos.Add(new SomVivoCena { dados = s, ancora = ancora });
                }
        }

        private bool RequerConhecido(string id, string requer, ISet<string> eventosDaTrama)
        {
            if (string.IsNullOrEmpty(requer) || eventosDaTrama.Contains(requer))
                return true;
            Debug.LogWarning("Memória viva '" + id + "' exige o evento '" + requer + "', que não existe na campanha. O item foi ignorado.", this);
            return false;
        }

        private MTMPonto CriarPontoVivo(string regiaoId, string ancoraId, float dx, float dy, string idPonto, string nome)
        {
            var regiao = RegiaoTecido(regiaoId);
            var ancora = PontoTecido(regiaoId, ancoraId);
            if (regiao == null || ancora == null)
            {
                Debug.LogWarning("Memória viva '" + idPonto + "': região '" + regiaoId + "' ou âncora '" + ancoraId + "' não existe na cena. O item foi ignorado.", this);
                return null;
            }

            var go = new GameObject("Memória viva · " + idPonto) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(regiao.transform, false);
            go.transform.position = ancora.transform.position + new Vector3(dx, dy, 0f);
            var ponto = go.AddComponent<MTMPonto>();
            ponto.dados = new MTMPontoDados { id = idPonto, nome = nome, tipo = "memoria", falas = new string[0] };
            ponto.alcance = 1.7f;
            var marcador = new GameObject("Marcador") { hideFlags = HideFlags.DontSave };
            marcador.transform.SetParent(go.transform, false);
            marcador.transform.localPosition = new Vector3(0f, .9f, 0f);
            var desenho = marcador.AddComponent<SpriteRenderer>();
            desenho.sprite = SpriteMarcadorVivo();
            marcador.AddComponent<MTMOrdemY>();
            marcadoresVivos[ponto] = desenho;
            var lista = new List<MTMPonto>(regiao.pontos ?? new MTMPonto[0]);
            lista.Add(ponto);
            regiao.pontos = lista.ToArray();
            pontosVivos.Add(ponto);
            return ponto;
        }

        private Sprite SpriteMarcadorVivo()
        {
            if (spriteMarcadorVivo != null)
                return spriteMarcadorVivo;
            // Losango bordado em tons de cinza: a cor de cada marcador (dourado, vermelho) vem do tingimento.
            spriteMarcadorVivo = Resources.Load<Sprite>(PastaArteGuia + "marcador_ponto");
            if (spriteMarcadorVivo != null)
                return spriteMarcadorVivo;
            const int n = 48;
            marcadorVivo = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Marcador_memoria_viva", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Abs(x - 23.5f) + Mathf.Abs(y - 23.5f);
                    float miolo = Mathf.Clamp01(15f - d);
                    float brilho = Mathf.Clamp01(24f - d) * .35f;
                    pixels[y * n + x] = new Color(1f, 1f, 1f, Mathf.Max(miolo, brilho));
                }

            marcadorVivo.SetPixels(pixels);
            marcadorVivo.Apply(false, true);
            spriteMarcadorVivo = Sprite.Create(marcadorVivo, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n);
            spriteMarcadorVivo.hideFlags = HideFlags.DontSave;
            return spriteMarcadorVivo;
        }

        private Texture2D TexturaAnelVivo()
        {
            if (anelVivo != null)
                return anelVivo;
            const int n = 64;
            anelVivo = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Anel_memoria_viva", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f));
                    pixels[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.Abs(r - 28f) / 2.5f));
                }

            anelVivo.SetPixels(pixels);
            anelVivo.Apply(false, true);
            return anelVivo;
        }

        // ---------- disponibilidade e interação ----------

        private string EventoDoPontoVivo(MTMPonto p)
        {
            MTMFotoViva f;
            MTMEntrevistaViva e;
            if (fotosPorPonto.TryGetValue(p.dados.id, out f))
                return f.evento;
            return entrevistasPorPonto.TryGetValue(p.dados.id, out e) ? e.evento : "";
        }

        private bool DisponivelMemoriaViva(MTMPonto p)
        {
            MTMFotoViva f;
            MTMEntrevistaViva e;
            string requer = fotosPorPonto.TryGetValue(p.dados.id, out f) ? f.requer : entrevistasPorPonto.TryGetValue(p.dados.id, out e) ? e.requer : "";
            return string.IsNullOrEmpty(requer) || estado.Tem(requer);
        }

        private string AcaoMemoriaViva(MTMPonto p)
        {
            bool feito = estado.Tem(EventoDoPontoVivo(p));
            if (fotosPorPonto.ContainsKey(p.dados.id))
                return feito ? "Rever a foto antiga" : "Comparar com a foto antiga";
            return feito ? "Relembrar a conversa" : "Ouvir com atenção";
        }

        private bool InteragirMemoriaViva(MTMPonto ponto)
        {
            if (!PontoVivo(ponto))
                return false;
            MTMFotoViva foto;
            MTMEntrevistaViva entrevista;
            if (fotosPorPonto.TryGetValue(ponto.dados.id, out foto))
                AbrirFotoViva(foto);
            else if (entrevistasPorPonto.TryGetValue(ponto.dados.id, out entrevista))
                AbrirEntrevistaViva(entrevista);
            return true;
        }

        private void FecharMemoriaViva()
        {
            painel = "";
            fotoAtual = null;
            fotoTextura = null;
            entrevistaAtual = null;
            sessaoViva = null;
            Parar();
            LimparFocoUI();
            quadroDaAcao = Time.frameCount;
        }

        private void TeclasMemoriaViva()
        {
            Parar();
            if (quadroDaAcao == Time.frameCount)
                return;
            if (painel == "foto_viva")
            {
                float x = MTMEntrada.Movimento().x;
                if (Mathf.Abs(x) > .1f)
                    fotoOpacidade = Mathf.Clamp(fotoOpacidade + x * Time.unscaledDeltaTime * .6f, .08f, 1f);
                if (MTMEntrada.Apertou('h'))
                {
                    quadroDaAcao = Time.frameCount;
                    RevelarFotoViva();
                }
            }
            else if (painel == "entrevista_viva" && sessaoViva != null)
            {
                for (int i = 0; i < 4; i++)
                    if (MTMEntrada.Apertou((char)('1' + i)))
                    {
                        EscolherEntrevistaViva(i);
                        break;
                    }
            }
        }

        // ---------- foto antiga contra o presente ----------

        private string EventoDiferenca(MTMFotoViva f, int i)
        {
            return f.evento + "_d" + i;
        }

        private void AbrirFotoViva(MTMFotoViva f)
        {
            fotoAtual = f;
            fotoTextura = string.IsNullOrEmpty(f.imagem) ? null : Resources.Load<Texture2D>(f.imagem);
            fotoAchadas.Clear();
            for (int i = 0; i < f.diferencas.Length; i++)
                if (estado.Tem(EventoDiferenca(f, i)))
                    fotoAchadas.Add(i);
            fotoDestaque = -1;
            fotoMensagem = "";
            fotoOpacidade = .55f;
            painel = "foto_viva";
            quadroDaAcao = Time.frameCount;
            Parar();
            LimparFocoUI();
        }

        private void AcharDiferencaViva(int i)
        {
            var f = fotoAtual;
            if (f == null || i < 0 || i >= f.diferencas.Length || !fotoAchadas.Add(i))
                return;
            estado.Concluir(EventoDiferenca(f, i));
            fotoDestaque = i;
            fotoMensagem = f.diferencas[i].titulo + ": " + f.diferencas[i].texto;
            Salvar();
            if (fotoAchadas.Count >= f.minimo && !estado.Tem(f.evento))
            {
                estado.Concluir(f.evento);
                Salvar();
                var conclusao = f.conclusao;
                string nome = f.nome;
                FecharMemoriaViva();
                Abrir(nome, FonteMemoriaViva, conclusao, () => Avisar("Foto registrada. Veja em J → Achados."));
            }
        }

        private void RevelarFotoViva()
        {
            if (fotoAtual == null)
                return;
            int i = MTMMemoriaViva.ProximaNaoAchada(fotoAtual, fotoAchadas);
            if (i < 0)
            {
                fotoMensagem = "Você já encontrou todas as diferenças desta foto.";
                return;
            }

            AcharDiferencaViva(i);
        }

        private void FotoVivaUI()
        {
            var f = fotoAtual;
            if (f == null)
            {
                painel = "";
                return;
            }

            Color sepia = new Color(1f, .9f, .74f);
            Rect area = new Rect(40, 96, 1200, 486);
            float aspecto = fotoTextura != null ? fotoTextura.width / (float)fotoTextura.height : area.width / area.height;
            Rect img = area;
            if (area.width / area.height > aspecto)
            {
                float w = area.height * aspecto;
                img = new Rect(area.x + (area.width - w) * .5f, area.y, w, area.height);
            }
            else
            {
                float h = area.width / aspecto;
                img = new Rect(area.x, area.y + (area.height - h) * .5f, area.width, h);
            }

            Color anterior = GUI.color;
            GUI.color = new Color(sepia.r, sepia.g, sepia.b, fotoOpacidade);
            GUI.DrawTexture(img, fotoTextura != null ? fotoTextura : Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = anterior;
            BordaTecido(img, AlfaTecido(ouro, .9f), 3f);
            if (fotoTextura == null)
                TextoTecido(new Rect(img.x + 20, img.y + 16, img.width - 40, 50), "Foto de exemplo: coloque a imagem em Resources/" + f.imagem + " (PNG ou JPG).", 15, AlfaTecido(textoSuperficieUI, .9f), true);
            var anel = TexturaAnelVivo();
            for (int i = 0; i < f.diferencas.Length; i++)
            {
                if (!fotoAchadas.Contains(i))
                    continue;
                var d = f.diferencas[i];
                float lado = Mathf.Max(30f, d.raio * 2f * img.width);
                GUI.color = i == fotoDestaque ? Color.white : ouro;
                GUI.DrawTexture(new Rect(img.x + d.x * img.width - lado * .5f, img.y + d.y * img.height - lado * .5f, lado, lado), anel);
            }

            GUI.color = anterior;
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && img.Contains(e.mousePosition) && quadroDaAcao != Time.frameCount)
            {
                float u = (e.mousePosition.x - img.x) / img.width, v = (e.mousePosition.y - img.y) / img.height;
                int achada = MTMMemoriaViva.DiferencaNoPonto(f, u, v, aspecto, fotoAchadas);
                if (achada >= 0)
                    AcharDiferencaViva(achada);
                else
                    fotoMensagem = "Nada diferente aqui. Compare a foto com o que você vê atrás dela.";
                e.Use();
            }

            Color barra = AlfaTecido(profundoUI, .95f);
            Fundo(new Rect(0, 0, 1280, 88), barra);
            Fundo(new Rect(0, 88, 1280, 3), ouro);
            TextoTecido(new Rect(40, 10, 1200, 30), ResumoHUDClaroUI(f.nome, 1200, 30, 22, true), 22, textoSuperficieUI, true);
            TextoTecido(new Rect(40, 42, 1200, 20), ResumoHUDClaroUI(f.legenda, 1200, 20, 14), 14, textoSuperficieUI);
            TextoTecido(new Rect(40, 64, 1200, 18), ResumoHUDClaroUI("Fonte: " + f.fonte, 1200, 18, 12), 12, AlfaTecido(textoSuperficieUI, .8f));
            Fundo(new Rect(0, 590, 1280, 130), barra);
            Fundo(new Rect(0, 590, 1280, 3), ouro);
            string mensagem = string.IsNullOrEmpty(fotoMensagem) ? f.instrucao : fotoMensagem;
            TextoTecido(new Rect(40, 600, 1200, 46), ResumoHUDClaroUI(mensagem, 1200, 46, 16), 16, textoSuperficieUI);
            TextoTecido(new Rect(40, 654, 330, 36), "Diferenças: " + fotoAchadas.Count + " de " + f.diferencas.Length + " (basta " + f.minimo + " para registrar)", 13, textoSuperficieUI);
            if (BotaoTecido(new Rect(380, 652, 150, 40), "← Presente"))
                fotoOpacidade = Mathf.Clamp(fotoOpacidade - .15f, .08f, 1f);
            if (BotaoTecido(new Rect(540, 652, 150, 40), "Foto →"))
                fotoOpacidade = Mathf.Clamp(fotoOpacidade + .15f, .08f, 1f);
            TextoTecido(new Rect(700, 660, 150, 24), "Foto: " + Mathf.RoundToInt(fotoOpacidade * 100f) + "%", 13, textoSuperficieUI);
            if (BotaoTecido(new Rect(850, 652, 180, 40), "Dica  " + AtalhoUI("H"), false, fotoAchadas.Count < f.diferencas.Length) && quadroDaAcao != Time.frameCount)
            {
                quadroDaAcao = Time.frameCount;
                RevelarFotoViva();
            }

            if (BotaoTecido(new Rect(1040, 652, 200, 40), "Sair  " + AtalhoUI("Esc"), true) && quadroDaAcao != Time.frameCount)
                FecharMemoriaViva();
        }

        // ---------- entrevista de história oral ----------

        private void AbrirEntrevistaViva(MTMEntrevistaViva e)
        {
            // Já concluída: relembra o fechamento guardado em vez de refazer a conversa.
            if (estado.Tem(e.evento))
            {
                for (int i = 0; i < e.fechamentos.Length; i++)
                    if (estado.Tem(e.evento + "_f" + i))
                    {
                        Abrir(e.titulo, FonteMemoriaViva, e.fechamentos[i].linhas, null);
                        return;
                    }
            }

            Abrir(e.titulo, FonteMemoriaViva, e.abertura, () =>
            {
                entrevistaAtual = e;
                sessaoViva = new MTMSessaoEntrevista(e);
                transcricaoViva = new string[0];
                painel = "entrevista_viva";
                quadroDaAcao = Time.frameCount;
                Parar();
                LimparFocoUI();
            });
        }

        // Até quatro opções numeradas: o silêncio, quando existe, ocupa a última.
        private List<MTMPerguntaViva> OpcoesVivas()
        {
            var lista = sessaoViva.Opcoes();
            bool silencio = sessaoViva.SilencioDisponivel;
            int maximo = silencio ? 3 : 4;
            if (lista.Count > maximo)
                lista = lista.GetRange(0, maximo);
            if (silencio)
                lista.Add(entrevistaAtual.silencio);
            return lista;
        }

        private void EscolherEntrevistaViva(int indice)
        {
            if (sessaoViva == null || sessaoViva.Encerrada)
                return;
            var opcoes = OpcoesVivas();
            if (indice < 0 || indice >= opcoes.Count)
                return;
            var p = opcoes[indice];
            bool silencio = p == entrevistaAtual.silencio;
            if (!(silencio ? sessaoViva.FicarEmSilencio() : sessaoViva.Perguntar(p.id)))
                return;
            var linhas = new List<string>();
            linhas.Add(silencio ? "Você fica em silêncio por um momento." : "Você: " + p.texto);
            linhas.AddRange(p.resposta);
            transcricaoViva = linhas.ToArray();
            quadroDaAcao = Time.frameCount;
        }

        private void ConcluirEntrevistaViva()
        {
            if (sessaoViva == null || entrevistaAtual == null)
                return;
            var e = entrevistaAtual;
            int indice = sessaoViva.IndiceFechamento();
            var fechamento = e.fechamentos[indice];
            FecharMemoriaViva();
            // Guarda antes do fechamento: sair dele com Esc não perde a entrevista.
            estado.Concluir(e.evento);
            estado.Concluir(e.evento + "_f" + indice);
            Salvar();
            Abrir(e.titulo, FonteMemoriaViva, fechamento.linhas, () => Avisar("Um novo fio foi guardado. Veja em J → Achados."));
        }

        private void EntrevistaVivaUI()
        {
            var e = entrevistaAtual;
            var s = sessaoViva;
            if (e == null || s == null)
            {
                painel = "";
                return;
            }

            Fundo(new Rect(0, 0, 1280, 720), new Color(.16f, .12f, .09f, .60f));
            PainelTecido(new Rect(28, 32, 1224, 656), algodaoUI);
            Fundo(new Rect(28, 32, 1224, 120), tinta);
            TextoTecido(new Rect(58, 50, 1000, 22), "MEMÓRIA VIVA  /  ENTREVISTA", 13, acentoTextoUI, true);
            TextoTecido(new Rect(58, 80, 1050, 40), ResumoHUDClaroUI(e.titulo, 1050, 40, 28), 28, textoSuperficieUI, true);
            TextoTecido(new Rect(58, 122, 1050, 22), ResumoHUDClaroUI("Fonte: " + e.fonte, 1050, 22, 13), 13, AlfaTecido(textoSuperficieUI, .85f));
            TramaFaixa(new Rect(28, 152, 1224, 8));
            if (BotaoTecido(new Rect(1094, 54, 130, 38), "Sair") && quadroDaAcao != Time.frameCount)
            {
                FecharMemoriaViva();
                return;
            }

            Fundo(new Rect(58, 178, 700, 334), papelUI);
            string texto = transcricaoViva.Length == 0 ? "Escolha o que perguntar. Cada resposta muda a confiança entre vocês." : string.Join("\n\n", transcricaoViva);
            float altura = AlturaTecido(texto, 660, 18);
            rolagemDetalhe = IniciarRolagemUI(new Rect(78, 194, 668, 302), rolagemDetalhe, new Rect(0, 0, 660, Mathf.Max(302, altura)));
            TextoTecido(new Rect(0, 0, 660, altura), texto, 18, textoPapelUI);
            TerminarRolagemUI();
            Fundo(new Rect(782, 178, 436, 334), papelUI);
            TextoTecido(new Rect(806, 194, 390, 26), "Confiança", 20, textoPapelUI, true);
            for (int i = 0; i < MTMSessaoEntrevista.ConfiancaMaxima; i++)
            {
                Rect pip = new Rect(806 + i * 52, 230, 44, 18);
                Fundo(pip, i < s.Confianca ? ouro : AlfaTecido(textoPapelUI, .18f));
            }

            TextoTecido(new Rect(806, 262, 390, 26), "Perguntas restantes: " + s.Restantes, 16, textoPapelUI, true);
            string dica = "Algumas perguntas só surgem quando a pessoa confia em você. Escutar em silêncio também conta.";
            if (s.PedemMaisConfianca() > 0)
                dica = "Ainda há algo que parece não ser o momento de perguntar. Mais confiança pode abrir essa conversa.";
            float alturaDica = Mathf.Max(80, AlturaTecido(dica, 390, 15));
            TextoTecido(new Rect(806, 300, 390, alturaDica), dica, 15, textoPapelUI);
            if (s.Encerrada)
            {
                TextoTecido(new Rect(58, 524, 1160, 26), "A conversa chegou ao fim.", 18, textoPapelUI, true);
                if (BotaoTecido(new Rect(58, 560, 1160, 56), "Encerrar a conversa  →", true) && quadroDaAcao != Time.frameCount)
                {
                    quadroDaAcao = Time.frameCount;
                    ConcluirEntrevistaViva();
                }
            }
            else
            {
                var opcoes = OpcoesVivas();
                for (int i = 0; i < opcoes.Count; i++)
                {
                    Rect r = new Rect(58 + i % 2 * 592, 530 + i / 2 * 62, 576, 54);
                    if (BotaoTecido(r, (i + 1) + " · " + opcoes[i].texto) && quadroDaAcao != Time.frameCount)
                    {
                        EscolherEntrevistaViva(i);
                        break;
                    }
                }
            }

            TextoTecido(new Rect(58, 662, 1160, 20), "Teclas 1 a 4 escolhem. Esc sai sem concluir; nada é perdido, você pode recomeçar.", 12, AlfaTecido(textoPapelUI, .8f));
        }

        // ---------- modo escuta ----------

        private bool SomVivoLiberado(MTMSomVivo s)
        {
            return string.IsNullOrEmpty(s.requer) || estado.Tem(s.requer);
        }

        private void AtualizarMarcadoresVivos()
        {
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            float pulso = reduzir ? 1f : 1f + .12f * Mathf.Sin(Time.unscaledTime * 3.2f);
            foreach (var par in marcadoresVivos)
            {
                var ponto = par.Key;
                var desenho = par.Value;
                if (ponto == null || desenho == null)
                    continue;
                bool ativo = ponto.gameObject.activeInHierarchy && DisponivelMemoriaViva(ponto);
                desenho.enabled = ativo;
                if (!ativo)
                    continue;
                bool feito = estado.Tem(EventoDoPontoVivo(ponto));
                desenho.color = feito ? new Color(.97f, .92f, .8f, .45f) : new Color(.91f, .70f, .43f, .95f);
                desenho.transform.localScale = Vector3.one * pulso * (feito ? .8f : 1f);
            }
        }

        // Chamado em Update quando o jogo está livre. Segure L para escutar.
        private void AtualizarEscutaViva()
        {
            if (memoriaViva == null || atual == null)
            {
                escutandoViva = false;
                return;
            }

            AtualizarMarcadoresVivos();
            escutandoViva = MTMEntrada.Escutando() && sonsVivos.Count > 0;
            if (!escutandoViva)
                return;
            multiplicador = .55f;
            foreach (var s in sonsVivos)
            {
                if (s.dados.regiao != atual.dados.id || !SomVivoLiberado(s.dados))
                    continue;
                float distancia = Vector2.Distance(corpo.position, s.Posicao);
                float intensidade = MTMMemoriaViva.Intensidade(distancia, s.dados.raio);
                float acumulado;
                escutaAcumulada.TryGetValue(s.dados.id, out acumulado);
                if (estado.Tem(s.dados.evento))
                    continue;
                acumulado = MTMMemoriaViva.Acumular(acumulado, Time.deltaTime, intensidade);
                escutaAcumulada[s.dados.id] = acumulado;
                if (acumulado >= s.dados.tempo)
                {
                    CapturarSomVivo(s.dados);
                    return;
                }
            }
        }

        private void CapturarSomVivo(MTMSomVivo s)
        {
            escutaAcumulada.Remove(s.id);
            escutandoViva = false;
            estado.Concluir(s.evento);
            Salvar();
            string[] linhas = s.memoria != null && s.memoria.Length > 0 ? s.memoria : new[] { s.legenda };
            Abrir("Escuta · " + s.nome, FonteMemoriaViva, linhas, () => Avisar("Som guardado. Veja em J → Achados."));
        }

        private void SetaBordaViva(Vector2 pos, float alfa, Color cor)
        {
            PrepararSetaGuia();
            Rect area = new Rect(70, 150, 1140, 430);
            Vector2 centro = area.center;
            Vector2 d = pos - centro;
            float sx = Mathf.Abs(d.x) < .01f ? float.MaxValue : area.width * .5f / Mathf.Abs(d.x);
            float sy = Mathf.Abs(d.y) < .01f ? float.MaxValue : area.height * .5f / Mathf.Abs(d.y);
            Vector2 p = centro + d * Mathf.Min(1f, Mathf.Min(sx, sy));
            Matrix4x4 matriz = GUI.matrix;
            Color antiga = GUI.color;
            GirarEmVolta(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, p);
            GUI.color = new Color(cor.r, cor.g, cor.b, alfa);
            GUI.DrawTexture(new Rect(p.x - 20, p.y - 20, 40, 40), setaGuia);
            GUI.color = antiga;
            GUI.matrix = matriz;
        }

        private void EscutaHUD(Color fundo, Color claro, Color dourado)
        {
            if (memoriaViva == null || atual == null || sonsVivos.Count == 0)
                return;
            bool algum = false;
            foreach (var s in sonsVivos)
                if (s.dados.regiao == atual.dados.id && SomVivoLiberado(s.dados))
                {
                    algum = true;
                    break;
                }

            if (!algum)
                return;
            if (!escutandoViva)
            {
                if (!estado.Tem(EventoPrimeiraConversa))
                    return;
                // Junto dos atalhos, como mais uma tecla; recua quando o Neto anda.
                float a = alfaAtalhosHUD;
                float largura = 24f + 8f + LarguraTextoHUD("Escutar", 13, true);
                float x = 1260 - 3 * 68f + 10f - 16f - largura;
                TeclaHUD(x, 659, "L", a, 12, 22f);
                TextoSombraHUD(new Rect(x + 30, 659, largura - 26, 22), "Escutar", 13, AlfaTecido(claro, a), true, TextAnchor.MiddleLeft);
                return;
            }

            Fundo(new Rect(0, 0, 1280, 720), new Color(.05f, .04f, .03f, .30f));
            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            var anel = TexturaAnelVivo();
            SomVivoCena forte = null;
            float maior = 0f;
            Color antiga = GUI.color;
            foreach (var s in sonsVivos)
            {
                if (s.dados.regiao != atual.dados.id || !SomVivoLiberado(s.dados))
                    continue;
                Vector3 mundo = s.Posicao;
                float intensidade = MTMMemoriaViva.Intensidade(Vector2.Distance(corpo.position, mundo), s.dados.raio);
                if (intensidade <= 0f)
                    continue;
                if (intensidade > maior)
                {
                    maior = intensidade;
                    forte = s;
                }

                Vector3 proj = cameraDoMapa.WorldToScreenPoint(mundo);
                if (proj.z <= 0f)
                    continue;
                Vector2 pos = CoordenadaHUDCompacta(proj);
                if (new Rect(0, 0, 1280, 720).Contains(pos))
                    for (int k = 0; k < 3; k++)
                    {
                        float fase = reduzir ? .5f : Mathf.Repeat(Time.unscaledTime * .7f + k / 3f, 1f);
                        float lado = 30f + fase * 220f * intensidade;
                        GUI.color = new Color(dourado.r, dourado.g, dourado.b, (1f - fase) * intensidade);
                        GUI.DrawTexture(new Rect(pos.x - lado * .5f, pos.y - lado * .5f, lado, lado), anel);
                    }
                else
                    SetaBordaViva(pos, .35f + .65f * intensidade, dourado);
            }

            GUI.color = antiga;
            Rect cartao = new Rect(340, 560, 600, forte != null ? 74 : 44);
            Fundo(cartao, fundo);
            if (forte == null)
            {
                TextoTecido(cartao, "Escutando… nada por perto. Tente outro lugar.", 15, claro, false, TextAnchor.MiddleCenter);
                return;
            }

            bool feito = estado.Tem(forte.dados.evento);
            TextoTecido(new Rect(cartao.x + 16, cartao.y + 8, cartao.width - 32, 26), ResumoHUDClaroUI("♪  " + forte.dados.legenda, 568, 26, 16, true), 16, claro, true, TextAnchor.MiddleCenter);
            float acumulado;
            escutaAcumulada.TryGetValue(forte.dados.id, out acumulado);
            if (feito)
                TextoTecido(new Rect(cartao.x + 16, cartao.y + 40, cartao.width - 32, 24), "Já guardado em J → Achados.", 13, claro, false, TextAnchor.MiddleCenter);
            else
            {
                Rect trilho = new Rect(cartao.x + 40, cartao.y + 50, cartao.width - 80, 8);
                Fundo(trilho, AlfaTecido(claro, .2f));
                Fundo(new Rect(trilho.x, trilho.y, trilho.width * Mathf.Clamp01(acumulado / forte.dados.tempo), trilho.height), dourado);
            }
        }

        // ---------- Caderno: aba "Achados" ----------

        private sealed class AchadoViva
        {
            public string chave, titulo, tipo, texto, fio;
        }

        private List<AchadoViva> AchadosVivos()
        {
            var lista = new List<AchadoViva>();
            AdicionarAchadosConfronto(lista);
            if (memoriaViva == null)
                return lista;
            if (memoriaViva.fotos != null)
                foreach (var f in memoriaViva.fotos)
                {
                    var linhas = new List<string>();
                    for (int i = 0; i < f.diferencas.Length; i++)
                        if (estado.Tem(EventoDiferenca(f, i)))
                            linhas.Add("• " + f.diferencas[i].titulo + ": " + f.diferencas[i].texto);
                    if (linhas.Count == 0)
                        continue;
                    string texto = string.Join("\n\n", linhas.ToArray());
                    if (estado.Tem(f.evento))
                        texto += "\n\n" + string.Join("\n\n", f.conclusao);
                    lista.Add(new AchadoViva { chave = f.id, titulo = f.nome, tipo = "FOTO ANTIGA", texto = texto + "\n\nFonte: " + f.fonte });
                }

            if (memoriaViva.entrevistas != null)
                foreach (var e in memoriaViva.entrevistas)
                    for (int i = 0; i < e.fechamentos.Length; i++)
                        if (estado.Tem(e.evento + "_f" + i))
                            lista.Add(new AchadoViva { chave = e.id, titulo = e.titulo, tipo = "ENTREVISTA", texto = e.fechamentos[i].resumo + "\n\nFonte: " + e.fonte, fio = e.fechamentos[i].fio });
            if (memoriaViva.sons != null)
                foreach (var s in memoriaViva.sons)
                    if (estado.Tem(s.evento))
                        lista.Add(new AchadoViva { chave = s.id, titulo = s.nome, tipo = "SOM DA CIDADE", texto = s.legenda + (s.memoria != null && s.memoria.Length > 0 ? "\n\n" + string.Join("\n\n", s.memoria) : "") });
            return lista;
        }

        private void AchadosVivosUI()
        {
            var lista = AchadosVivos();
            if (lista.Count == 0)
            {
                TextoTecido(new Rect(200, 330, 880, 160), memoriaViva == null ? "Ainda não há achados. As lendas que você enfrentar ficam guardadas aqui." : "Ainda não há achados. Compare uma foto antiga com a praça, ouça uma pessoa com atenção ou segure L para escutar os sons da cidade.", 22, textoPapelUI, false, TextAnchor.MiddleCenter);
                return;
            }

            if (!lista.Exists(a => a.chave == achadoSelecionado))
                achadoSelecionado = lista[0].chave;
            rolagemLista = IniciarRolagemUI(new Rect(58, 252, 350, 378), rolagemLista, new Rect(0, 0, 326, Mathf.Max(378, lista.Count * 74)));
            for (int i = 0; i < lista.Count; i++)
            {
                var a = lista[i];
                Rect r = new Rect(0, i * 74, 324, 64);
                bool sel = a.chave == achadoSelecionado;
                Fundo(r, sel ? tinta : papelUI);
                TextoTecido(new Rect(14, r.y + 8, 296, 36), ResumoHUDClaroUI(a.titulo, 296, 36, 15, true), 15, sel ? textoSuperficieUI : textoPapelUI, true);
                TextoTecido(new Rect(14, r.y + 44, 296, 15), a.tipo, 10, sel ? acentoTextoUI : terracotaUI, true);
                if (AcaoTecido(r, "achado:" + a.chave))
                {
                    achadoSelecionado = a.chave;
                    rolagemDetalhe = Vector2.zero;
                }
            }

            TerminarRolagemUI();
            var escolhido = lista.Find(a => a.chave == achadoSelecionado);
            Fundo(new Rect(434, 252, 784, 378), papelUI);
            TextoTecido(new Rect(460, 272, 640, 40), ResumoHUDClaroUI(escolhido.titulo, 640, 40, 25, true), 25, textoPapelUI, true);
            TextoTecido(new Rect(460, 318, 640, 22), escolhido.tipo, 13, terracotaUI, true);
            if (!string.IsNullOrEmpty(escolhido.fio))
                Fundo(new Rect(1130, 278, 60, 18), CorTecido(MTMMemoriaViva.CorDoFio(escolhido.fio), ouro));
            float altura = AlturaTecido(escolhido.texto, 700, 18);
            rolagemDetalhe = IniciarRolagemUI(new Rect(460, 352, 730, 258), rolagemDetalhe, new Rect(0, 0, 704, Mathf.Max(258, altura)));
            TextoTecido(new Rect(0, 0, 700, altura), escolhido.texto, 18, textoPapelUI);
            TerminarRolagemUI();
        }
    }
}

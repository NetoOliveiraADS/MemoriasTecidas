using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Som do jogo: música (exploração e tensão), ambiente de vento, passos e efeitos.
    // Os arquivos ficam em Resources/MemoriasTecidas/Sons; variações terminam em _1, _2...
    // Música e ambiente trocam com fade cruzado; os efeitos variam levemente o tom a cada toque.
    public sealed partial class MTMJogo
    {
        private const string PastaSons = "MemoriasTecidas/Sons/";
        private const string ChaveSom = "MemoriasTecidas.Som.";

        private readonly Dictionary<string, AudioClip[]> sons = new Dictionary<string, AudioClip[]>();
        private AudioSource efeitosSom, galopeSom;
        private readonly AudioSource[] musicaSom = new AudioSource[2], ambienteSom = new AudioSource[2];
        private int musicaAtiva, ambienteAtivo;
        private string musicaTocando, ambienteTocando, painelAnteriorSom = "";
        private float volumeMusica = .7f, volumeEfeitos = 1f;
        private float passoAcumulado, proximoAssobio;
        private int ultimoPasso = -1;
        private MTMEstadoInimigo estadoAnteriorSom;
        private bool somPronto;

        private void PrepararSom()
        {
            volumeMusica = Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveSom + "Musica", .7f));
            volumeEfeitos = Mathf.Clamp01(PlayerPrefs.GetFloat(ChaveSom + "Efeitos", 1f));
            var raiz = new GameObject("Som") { hideFlags = HideFlags.DontSave };
            raiz.transform.SetParent(transform, false);
            efeitosSom = NovaFonteSom(raiz, false);
            // PlayOneShot multiplica pelo volume da fonte: ela fica cheia e cada toque leva o seu volume.
            efeitosSom.volume = 1f;
            galopeSom = NovaFonteSom(raiz, true);
            for (int i = 0; i < 2; i++)
            {
                musicaSom[i] = NovaFonteSom(raiz, true);
                ambienteSom[i] = NovaFonteSom(raiz, true);
            }

            somPronto = true;
        }

        private static AudioSource NovaFonteSom(GameObject raiz, bool repetir)
        {
            var fonte = raiz.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.loop = repetir;
            fonte.spatialBlend = 0f;
            fonte.volume = 0f;
            return fonte;
        }

        private AudioClip[] SonsDe(string nome)
        {
            AudioClip[] lista;
            if (sons.TryGetValue(nome, out lista))
                return lista;
            var achados = new List<AudioClip>();
            var unico = Resources.Load<AudioClip>(PastaSons + nome);
            if (unico != null)
                achados.Add(unico);
            for (int i = 1; i <= 12; i++)
            {
                var variacao = Resources.Load<AudioClip>(PastaSons + nome + "_" + i);
                if (variacao == null)
                    break;
                achados.Add(variacao);
            }

            if (achados.Count == 0)
                Debug.Log("Som: '" + nome + "' não está em Resources/" + PastaSons, this);
            lista = achados.ToArray();
            sons[nome] = lista;
            return lista;
        }

        // Efeito de interface ou de jogo; "variacao" muda o tom em até ± esse valor.
        private void TocarSom(string nome, float volume = 1f, float variacao = .05f)
        {
            if (!somPronto || volumeEfeitos <= 0f)
                return;
            var lista = SonsDe(nome);
            if (lista.Length == 0)
                return;
            int i = Random.Range(0, lista.Length);
            // Na tela inicial os efeitos ficam baixos, para a música de abertura aparecer.
            if (inicioAbertoUI)
                volume *= .45f;
            if (lista.Length > 1 && i == ultimoPasso && nome.StartsWith("passo"))
                i = (i + 1) % lista.Length;
            if (nome.StartsWith("passo"))
                ultimoPasso = i;
            efeitosSom.pitch = 1f + Random.Range(-variacao, variacao);
            efeitosSom.PlayOneShot(lista[i], volume * volumeEfeitos);
        }

        private void AtualizarSom()
        {
            if (!somPronto)
                return;
            float dt = Time.unscaledDeltaTime;
            bool menu = inicioAbertoUI || !string.IsNullOrEmpty(painel);
            // No aviso de fim da luta a música já volta para a exploração.
            bool lutando = EmConfronto && confrontoAtual != null && !encerrandoConfronto;

            // Música: na tela inicial em meio volume; no jogo desce devagar até ficar abaixo dos efeitos.
            // Tensão nas lutas, exploração no resto.
            // Na abertura de cinema a música sobe e o vento acompanha a bandeira.
            bool abertura = painel == "prologo";
            string musica = lutando ? "musica_tensao" : "musica_exploracao";
            float alvoMusica = volumeMusica * (abertura ? .55f : inicioAbertoUI ? .7f : lutando ? .34f : menu || falas != null ? .2f : .16f);
            TrocarLoop(musicaSom, ref musicaAtiva, ref musicaTocando, musica, alvoMusica, abertura || inicioAbertoUI || lutando ? 1.5f : 6f, dt);

            // Ambiente: por enquanto só o vento, em todo lugar. Quase nada na tela inicial.
            // (amb_cidade, amb_dia, amb_noite e amb_rio continuam na pasta para quando voltarem.)
            const string ambiente = "amb_vento";
            TrocarLoop(ambienteSom, ref ambienteAtivo, ref ambienteTocando, ambiente, volumeEfeitos * (abertura ? .4f : inicioAbertoUI ? .12f : menu ? .3f : .6f), 3f, dt);

            // Painéis: um toque ao abrir.
            if (painel != painelAnteriorSom)
            {
                if (!string.IsNullOrEmpty(painel) && string.IsNullOrEmpty(painelAnteriorSom))
                    TocarSom("ui_abrir", .7f, .03f);
                painelAnteriorSom = painel ?? "";
            }

            AtualizarPassosSom(menu, dt);
            AtualizarTearSom();
            AtualizarConfrontoSom(lutando, dt);
        }

        // Tear: batida de madeira ao trocar o pedal, pano ao trocar o fio.
        // Compara com o quadro anterior, então vale para mouse, teclado e controle.
        private MTMSessaoTear sessaoTearSom;
        private int nivelTearSom = -1, pedalTearSom = -1, corTearSom = -1;

        private void AtualizarTearSom()
        {
            var s = sessaoTear;
            if (s == null)
            {
                sessaoTearSom = null;
                return;
            }

            // Sessão nova, nível novo ou passagem em andamento: só acompanha, sem tocar.
            bool tocar = s == sessaoTearSom && s.Nivel == nivelTearSom && !passandoTear;
            if (tocar && s.Pedal != pedalTearSom && s.Pedal >= 0 && !s.PedalAutomatico)
                TocarSom("tear_pedal", .8f, .06f);
            if (tocar && s.Cor != corTearSom && s.Cor >= 0)
                TocarSom("tear_fio", .75f, .08f);
            sessaoTearSom = s;
            nivelTearSom = s.Nivel;
            pedalTearSom = s.Pedal;
            corTearSom = s.Cor;
        }

        private void TrocarLoop(AudioSource[] par, ref int ativa, ref string tocando, string nome, float alvo, float fade, float dt)
        {
            if (nome != tocando)
            {
                var lista = SonsDe(nome);
                if (lista.Length > 0)
                {
                    ativa = 1 - ativa;
                    par[ativa].clip = lista[0];
                    par[ativa].volume = 0f;
                    par[ativa].Play();
                }

                tocando = nome;
            }

            float passo = dt / Mathf.Max(.05f, fade);
            for (int i = 0; i < 2; i++)
            {
                var f = par[i];
                if (f == null)
                    continue;
                float meta = i == ativa ? alvo : 0f;
                f.volume = Mathf.MoveTowards(f.volume, meta, passo * Mathf.Max(.2f, Mathf.Max(alvo, f.volume)));
                if (i != ativa && f.isPlaying && f.volume <= .001f)
                    f.Stop();
            }
        }

        private void AtualizarPassosSom(bool menu, float dt)
        {
            float rapidez = corpo != null && !menu && falas == null && !NetoAtordoado ? corpo.linearVelocity.magnitude : 0f;
            if (rapidez < .4f)
            {
                passoAcumulado = .45f; // o primeiro passo soa logo que começa a andar
                return;
            }

            passoAcumulado += rapidez * dt;
            float passada = rapidez > 4.2f ? .95f : .72f;
            if (passoAcumulado < passada)
                return;
            passoAcumulado -= passada;
            // Um passo só em todo lugar (o da cidade); o de terra soava estranho.
            TocarSom("passo_pedra", rapidez > 4.2f ? .75f : .6f, .08f);
        }

        private void AtualizarConfrontoSom(bool lutando, float dt)
        {
            var s = sessaoConfronto;
            // Na contagem antes da luta ela está parada: sem galope.
            if (!lutando || s == null || s.Encerrado || contandoConfronto)
            {
                if (galopeSom.isPlaying)
                {
                    galopeSom.volume = Mathf.MoveTowards(galopeSom.volume, 0f, dt * 1.5f);
                    if (galopeSom.volume <= .001f)
                        galopeSom.Stop();
                }

                return;
            }

            bool luta = confrontoAtual.tipo == "luta";
            if (s.Estado != estadoAnteriorSom)
            {
                if (s.Estado == MTMEstadoInimigo.Preparando)
                    TocarSom(luta ? "empina" : "assobio", luta ? .9f : .6f, .03f);
                estadoAnteriorSom = s.Estado;
            }

            if (luta)
            {
                // Galope enquanto ela se move: mais alto e rápido na investida, perto do jogador.
                var lista = SonsDe("galope");
                if (lista.Length > 0 && galopeSom.clip != lista[0])
                    galopeSom.clip = lista[0];
                bool correndo = s.Estado == MTMEstadoInimigo.Rondando || s.Estado == MTMEstadoInimigo.Investindo;
                float distancia = inimigoConfronto != null ? Vector2.Distance(inimigoConfronto.transform.position, transform.position) : 6f;
                float perto = Mathf.Lerp(1f, .35f, Mathf.InverseLerp(1.5f, 9f, distancia));
                float alvo = correndo ? volumeEfeitos * perto * (s.Estado == MTMEstadoInimigo.Investindo ? .9f : .45f) : 0f;
                galopeSom.pitch = Mathf.MoveTowards(galopeSom.pitch, s.Estado == MTMEstadoInimigo.Investindo ? 1.45f : 1f, dt * 3f);
                if (alvo > 0f && !galopeSom.isPlaying && galopeSom.clip != null)
                    galopeSom.Play();
                galopeSom.volume = Mathf.MoveTowards(galopeSom.volume, alvo, dt * 3f);
            }
            else if (s.Estado == MTMEstadoInimigo.Rondando && Time.unscaledTime >= proximoAssobio)
            {
                // O Curupira assobia de vez em quando enquanto corre em volta.
                if (proximoAssobio > 0f)
                    TocarSom("assobio", .35f, .1f);
                proximoAssobio = Time.unscaledTime + Random.Range(4f, 7.5f);
            }
        }

        private void SalvarPreferenciasSom()
        {
            PlayerPrefs.SetFloat(ChaveSom + "Musica", volumeMusica);
            PlayerPrefs.SetFloat(ChaveSom + "Efeitos", volumeEfeitos);
        }

        private static float ProximoVolume(float atual)
        {
            // 0 → 25 → 50 → 75 → 100 → 0
            float proximo = Mathf.Round(atual * 4f + 1f) / 4f;
            return proximo > 1.001f ? 0f : proximo;
        }

        private static string RotuloVolume(float valor)
        {
            return valor <= 0f ? "desligada" : Mathf.RoundToInt(valor * 100f) + "%";
        }
    }
}

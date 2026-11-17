using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [Serializable]
    public sealed class MTMQuadroPrologo
    {
        // Caminho em Resources, sem extensão. "ajuste": "preencher" (corta), "conter" (mostra inteira) ou "direita" (só o lado direito).
        public string imagem, titulo, texto, ajuste;
    }

    [Serializable]
    public sealed class MTMPrologo
    {
        public int versao;
        public MTMQuadroPrologo[] quadros;
    }

    // Abertura em quadros ilustrados e o primeiro enquadramento de Dona Feliciana.
    public sealed partial class MTMJogo
    {
        private const string ChavePrologoVisto = ChaveInterface + "PrologoVisto";
        private MTMPrologo prologo;
        private Texture2D[] artesPrologo;
        private int quadroPrologo;
        private float inicioQuadroPrologo;
        private Transform focoFeliciana, focoLetreiro;
        private float focoFelicianaAte, travaFelicianaAte;

        private void PrepararPrologo()
        {
            prologo = null;
            artesPrologo = null;
            try
            {
                var arquivo = Resources.Load<TextAsset>("MemoriasTecidas/prologo");
                if (arquivo == null)
                    return;
                var dados = JsonUtility.FromJson<MTMPrologo>(arquivo.text);
                if (dados == null || dados.quadros == null || dados.quadros.Length == 0)
                    throw new InvalidOperationException("prologo.json sem quadros.");
                foreach (var q in dados.quadros)
                    if (q == null || string.IsNullOrWhiteSpace(q.texto))
                        throw new InvalidOperationException("Todo quadro do prólogo precisa de texto.");
                artesPrologo = new Texture2D[dados.quadros.Length];
                for (int i = 0; i < dados.quadros.Length; i++)
                    artesPrologo[i] = string.IsNullOrEmpty(dados.quadros[i].imagem) ? null : Resources.Load<Texture2D>(dados.quadros[i].imagem);
                prologo = dados;
            }
            catch (Exception erro)
            {
                prologo = null;
                Debug.LogWarning("O prólogo não foi carregado: " + erro.Message + " O jogo começa sem a abertura.", this);
            }
        }

        // Chamado quando o jogador entra na jornada. "explicita" = escolheu Nova jornada.
        private void ComecoDaJornada(bool explicita)
        {
            // JornadaVazia() não serve aqui: visitar a Entrada já grava um marco.
            if (estado.Tem(EventoPrimeiraConversa))
                return;
            bool jaViu = PlayerPrefs.GetInt(ChavePrologoVisto, 0) == 1;
            if (prologo != null && (explicita || !jaViu))
            {
                IniciarPrologo();
                return;
            }

            IniciarFocoFeliciana();
        }

        private void IniciarPrologo()
        {
            quadroPrologo = 0;
            inicioQuadroPrologo = Time.unscaledTime;
            painel = "prologo";
            quadroDaAcao = Time.frameCount;
            Parar();
            LimparFocoUI();
        }

        private void AvancarPrologo()
        {
            if (prologo != null && quadroPrologo + 1 < prologo.quadros.Length)
            {
                TocarSom("ui_abrir", .6f, .02f);
                quadroPrologo++;
                inicioQuadroPrologo = Time.unscaledTime;
                return;
            }

            EncerrarPrologo();
        }

        private void EncerrarPrologo()
        {
            PlayerPrefs.SetInt(ChavePrologoVisto, 1);
            PlayerPrefs.Save();
            painel = "";
            quadroDaAcao = Time.frameCount;
            Parar();
            LimparFocoUI();
            IniciarFocoFeliciana();
        }

        private void TeclasPrologo()
        {
            Parar();
            if (quadroDaAcao == Time.frameCount)
                return;
            if (MTMEntrada.Apertou('e') || MTMEntrada.ConfirmouMenu())
            {
                quadroDaAcao = Time.frameCount;
                AvancarPrologo();
            }
        }

        private void PrologoUI()
        {
            if (prologo == null || prologo.quadros == null || prologo.quadros.Length == 0)
            {
                EncerrarPrologo();
                return;
            }

            int total = prologo.quadros.Length;
            int i = Mathf.Clamp(quadroPrologo, 0, total - 1);
            var q = prologo.quadros[i];
            float alfa = apresentacao != null && apresentacao.reduzirMovimento ? 1f : Mathf.Clamp01((Time.unscaledTime - inicioQuadroPrologo) / .6f);
            Fundo(new Rect(0, 0, 1280, 720), profundoUI);
            Rect moldura = new Rect(140, 36, 1000, 440);
            Fundo(new Rect(moldura.x - 2, moldura.y - 2, moldura.width + 4, moldura.height + 4), AlfaTecido(ouro, .6f));
            Fundo(moldura, tinta);
            var arte = artesPrologo != null && i < artesPrologo.Length ? artesPrologo[i] : null;
            if (arte != null)
            {
                Color antiga = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alfa);
                if (q.ajuste == "direita")
                {
                    // Mostra só o lado direito da arte (a esquerda dela é a área vazia do menu inicial).
                    float largura = .62f, altura = Mathf.Clamp01(largura * arte.width / Mathf.Max(1f, arte.height) * moldura.height / moldura.width);
                    GUI.DrawTextureWithTexCoords(moldura, arte, new Rect(1f - largura, Mathf.Clamp(.56f - altura * .5f, 0f, 1f - altura), largura, altura));
                }
                else
                    GUI.DrawTexture(moldura, arte, q.ajuste == "conter" ? ScaleMode.ScaleToFit : ScaleMode.ScaleAndCrop, true);
                GUI.color = antiga;
            }
            else
                TituloEditorial(moldura, q.titulo ?? "", 40, AlfaTecido(textoSuperficieUI, alfa));

            Rect legenda = new Rect(140, 496, 1000, 116);
            Fundo(legenda, AlfaTecido(tinta, .96f));
            Fundo(new Rect(legenda.x, legenda.y, 4, legenda.height), ouro);
            if (!string.IsNullOrEmpty(q.titulo))
                TextoTecido(new Rect(legenda.x + 26, legenda.y + 10, 940, 20), q.titulo.ToUpperInvariant(), 13, AlfaTecido(acentoTextoUI, alfa), true);
            TextoTecido(new Rect(legenda.x + 26, legenda.y + 32, 948, 80),
                ResumoHUDClaroUI(q.texto, 948, 80, 20), 20, AlfaTecido(textoSuperficieUI, alfa));
            for (int j = 0; j < total; j++)
                Fundo(new Rect(640 - total * 14 + j * 28 + 9, 626, 10, 10), j == i ? ouro : AlfaTecido(textoSuperficieUI, .4f));
            if (BotaoTecido(new Rect(140, 650, 190, 40), "Pular  " + AtalhoUI("Esc")) && quadroDaAcao != Time.frameCount)
                EncerrarPrologo();
            else if (BotaoTecido(new Rect(950, 650, 190, 40), i + 1 < total ? "Continuar  →" : "Começar  →", true) && quadroDaAcao != Time.frameCount)
            {
                quadroDaAcao = Time.frameCount;
                AvancarPrologo();
            }
        }

        // Mostra Dona Feliciana por alguns segundos e devolve a câmera ao jogador.
        private void IniciarFocoFeliciana()
        {
            Avisar("Ande com WASD ou setas e siga a seta dourada até Dona Feliciana.");
            // O banner da primeira chegada expirou atrás da abertura; mostra de novo agora.
            if (atual != null)
                MostrarBannerRegiao(atual.dados.id, true);
            var local = LocalPrimeiroFio();
            var pessoa = FelicianaPrimeiroFio();
            if (pessoa == null || local == null || local != atual || (apresentacao != null && apresentacao.reduzirMovimento) || !GuiaPrimeiroFioAtivo())
                return;
            focoFeliciana = pessoa.transform;
            focoLetreiro = null;
            foreach (var t in local.GetComponentsInChildren<Transform>(true))
                if (t.name.IndexOf("Letreiro", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    focoLetreiro = t;
                    break;
                }

            focoFelicianaAte = Time.unscaledTime + 2.2f;
            travaFelicianaAte = focoFelicianaAte + .9f;
        }

        private void PularFocoFeliciana()
        {
            focoFelicianaAte = travaFelicianaAte = 0f;
        }
    }
}

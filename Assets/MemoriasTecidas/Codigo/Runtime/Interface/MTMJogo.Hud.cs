using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private Texture2D losangoCompacta;
        private Vector2 CoordenadaHUDCompacta(Vector3 p)
        {
            Rect px = cameraDoMapa.pixelRect;
            float s = Mathf.Min(px.width / 1280f, px.height / 720f);
            return new Vector2((p.x - px.x - (px.width - 1280 * s) * .5f) / s, (px.yMax - p.y - (px.height - 720 * s) * .5f) / s);
        }

        private void LosangoCompacta(Rect r, Color cor)
        {
            if (losangoCompacta == null)
            {
                const int n = 64;
                losangoCompacta = new Texture2D(n, n, TextureFormat.RGBA32, false)
                {
                    name = "No_de_memoria",
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear
                };
                var pixels = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        pixels[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(32 - Mathf.Abs(x - 31.5f) - Mathf.Abs(y - 31.5f)));
                losangoCompacta.SetPixels(pixels);
                losangoCompacta.Apply(false, true);
            }

            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTexture(r, losangoCompacta);
            GUI.color = antiga;
        }

        private string AcaoProximaHUD()
        {
            if (perto == null || perto.dados == null) return "";
            var d = perto.dados;
            if (PontoVivo(perto)) return AcaoMemoriaViva(perto);
            if (PontoConfronto(perto)) return AcaoConfronto(perto);
            if (d.tipo == "saida")
                return RegiaoLiberadaRoteiro(d.destino) ? "Ir para " + NomeMapaGuia(d.destino) : "Caminho fechado por enquanto";
            if (d.tipo == "descanso") return "Descansar";
            if (d.id == IdAmostraPrimeiroFio)
                return estado.Tem(MTMPrimeiroFio.Teceu) ? "Rever amostra" : "Observar tecido";
            bool pessoa = tramaTecida != null && tramaTecida.personagens != null &&
                System.Array.Exists(tramaTecida.personagens, x => x != null && x.id == d.id);
            return pessoa ? "Conversar" : d.tipo == "coleta" ? "Recolher" : "Examinar";
        }

        private void HUDCompacta()
        {
            Color fundo = TintaAtualHUD;
            Color claro = CremeHUD, dourado = OuroHUD;
            bool primeiro = GuiaPrimeiroFioAtivo();
            var missao = MissaoAtualTecido();
            var etapaAtual = EtapaAtualTecido(missao);
            bool ehSaida;
            MTMPonto alvoGuia = AlvoGuiaAtual(out ehSaida);
            // Durante um confronto, só a luta importa: sem objetivo da missão, seta ou rota.
            bool lutando = EmConfronto;
            if (lutando)
                alvoGuia = null;
            alvoGuiaHUD = alvoGuia;
            cartaoObjetivoHUD = new Rect(0, 0, 0, 0);
            // Atalhos e dicas recuam enquanto o Neto anda e voltam quando ele para.
            bool andando = movimento.sqrMagnitude > .01f;
            alfaAtalhosHUD = Mathf.MoveTowards(alfaAtalhosHUD, andando ? .35f : 1f, Time.unscaledDeltaTime * (andando ? 1.5f : 4f));

            // Durante o foco de abertura na Feliciana, a tela fica limpa para o letreiro e para ela.
            bool focoAbertura = focoFeliciana != null && Time.unscaledTime < focoFelicianaAte;
            if (mostrarObjetivoTecido && !lutando && !focoAbertura)
                ObjetivoHUD(primeiro, missao, etapaAtual);
            RelogioHUD();
            // O nome do lugar fica numa linha discreta sob o relógio.
            if (atual != null && atual.dados != null)
                TextoSombraHUD(new Rect(900, 94, 358, 20), ResumoHUDClaroUI(atual.dados.nome, 358, 20, 13, true), 13, claro, true, TextAnchor.UpperRight);

            if (!lutando)
                AtalhosHUD(primeiro);
            GuiaMundoHUD(alvoGuia, ehSaida, dourado, fundo, claro);
            BannerRegiaoHUD(fundo, claro, dourado);
            if (!lutando)
                EscutaHUD(fundo, claro, dourado);
            ConfrontoHUD(fundo, claro, dourado);
            AvisoHUD(lutando);
            if (!lutando && perto != null && Disponivel(perto))
                InteracaoHUD(alvoGuia);
        }

        private void ObjetivoHUD(bool primeiro, MTMMissaoTecido missao, MTMEtapaTecido etapaAtual)
        {
            string objetivo = primeiro ? ObjetivoPrimeiroFio() : ObjetivoTecido();
            // Uma ou duas linhas de objetivo: o cartão cresce em vez de apertar o destino.
            bool duasLinhas = AlturaTecido(objetivo, 352, 16) > 24f;
            Rect cartao = new Rect(20, 20, 384, duasLinhas ? 116 : 96);
            // Objetivo novo: o cartão brilha em vez de o aviso repetir o texto.
            if (Time.unscaledTime < objetivoBrilhoAte)
            {
                float t = Mathf.Clamp01((objetivoBrilhoAte - Time.unscaledTime) / 2.4f);
                float pisca = apresentacao != null && apresentacao.reduzirMovimento ? 1f : .5f + .5f * Mathf.Sin(Time.unscaledTime * 8f);
                ArredondadoHUD(new Rect(cartao.x - 6, cartao.y - 6, cartao.width + 12, cartao.height + 12), AlfaTecido(OuroHUD, (.25f + .35f * pisca) * t));
            }

            PainelHUD(cartao, 1f, Time.unscaledTime < objetivoBrilhoAte);
            cartaoObjetivoHUD = cartao;
            TramaHUD(new Rect(cartao.x + 1, cartao.y + 9, 4, cartao.height - 18));
            string titulo = primeiro ? aberturaMemorias.titulo : missao == null ? "Sua jornada" : missao.titulo;
            LosangoCompacta(new Rect(cartao.x + 19, cartao.y + 15, 12, 12), OuroHUD);
            int feitos, total;
            PassosObjetivoHUD(primeiro, missao, etapaAtual, out feitos, out total);
            float larguraPassos = total > 0 ? total * 15f + 4f : 0f;
            TextoTecido(new Rect(cartao.x + 38, cartao.y + 13, 324 - larguraPassos, 18),
                ResumoHUDClaroUI((titulo ?? "").ToUpperInvariant(), 320 - larguraPassos, 18, 12, true), 12, OuroHUD, true);
            for (int i = 0; i < total; i++)
            {
                Rect l = new Rect(cartao.xMax - 18 - (total - i) * 15f + 4f, cartao.y + 15, 11, 11);
                if (i < feitos)
                    LosangoCompacta(l, OuroHUD);
                else
                {
                    LosangoCompacta(l, i == feitos ? OuroHUD : AlfaTecido(OuroHUD, .55f));
                    LosangoCompacta(new Rect(l.x + 2, l.y + 2, l.width - 4, l.height - 4), TintaAtualHUD);
                }
            }

            TextoTecido(new Rect(cartao.x + 18, cartao.y + 36, 352, 42),
                ResumoHUDClaroUI(objetivo, 352, 42, 16), 16, CremeHUD);
            string onde = LinhaOndeHUD(primeiro, missao, etapaAtual, false);
            if (!string.IsNullOrEmpty(onde))
            {
                float yOnde = cartao.yMax - 26f;
                LosangoCompacta(new Rect(cartao.x + 20, yOnde + 4, 9, 9), AlfaTecido(OuroHUD, .8f));
                TextoTecido(new Rect(cartao.x + 36, yOnde, 332, 18), ResumoHUDClaroUI(onde, 332, 18, 13), 13, CremeFracoHUD);
            }

            if (AcaoTecido(cartao, "hud:objetivo"))
            {
                if (primeiro) AbrirRedeMemorias();
                else
                {
                    abaCaderno = "missoes";
                    missaoSelecionadaTecido = missao == null ? "" : missao.id;
                    AlternarPainel("diario");
                }
            }
        }

        private void NovoObjetivoHUD()
        {
            objetivoBrilhoAte = Time.unscaledTime + 2.4f;
            Avisar("Novo objetivo");
            TocarSom("objetivo", .8f, 0f);
        }

        // Bolsa, mapa e caderno como teclas com ícone, no canto inferior direito.
        private void AtalhosHUD(bool primeiro)
        {
            float a = alfaAtalhosHUD;
            string[] ids = { "bolsa", "mapa", "diario" };
            string[] icones = { "bolsa", "mapa", "diario" };
            string[] teclas = { "B", "M", "J" };
            for (int i = 0; i < ids.Length; i++)
            {
                Rect r = new Rect(1260 - (ids.Length - i) * 68f + 10f, 642, 58, 58);
                PainelHUD(r, a);
                IconeTecido(icones[i], new Rect(r.x + 15, r.y + 13, 28, 28), AlfaTecido(CremeHUD, a));
                TeclaHUD(r.xMax - 18, r.yMax - 16, teclas[i], a, 11, 20f);
                if (AcaoTecido(r, "hud:atalho:" + ids[i])) AlternarPainel(ids[i]);
            }

            // Dica de controles só até a primeira conversa; depois o jogador já sabe andar.
            if (!estado.Tem(EventoPrimeiraConversa))
            {
                string[] t = { "WASD", "Shift", "E" };
                string[] rot = { "Andar", "Correr", "Conversar" };
                float largura = DicasHUD(0, 0, t, rot, true) + 36f;
                Rect dica = new Rect(640 - largura * .5f, 656, largura, 44);
                PainelHUD(dica);
                DicasHUD(dica.x + 18, dica.y + 9, t, rot, false);
            }

            // A receita só aparece depois de ser apresentada na campanha.
            if (!primeiro && estado.Tem("r24_m09_01") && !estado.rede)
            {
                Rect receita = new Rect(20, 656, 244, 44);
                PainelHUD(receita, Mathf.Max(a, .6f));
                IconeTecido("rede", new Rect(receita.x + 14, receita.y + 11, 22, 22), OuroHUD);
                TextoTecido(new Rect(receita.x + 44, receita.y, 192, receita.height), "Conferir materiais da rede", 13, CremeHUD, false, TextAnchor.MiddleLeft);
                if (AcaoTecido(receita, "hud:receita")) AlternarPainel("receita");
            }
        }

        // Balão preso ao ponto mais próximo: tecla E redonda e a ação.
        private void InteracaoHUD(MTMPonto alvoGuia)
        {
            Vector3 proj = cameraDoMapa.WorldToScreenPoint(perto.transform.position + Vector3.up * 1.6f);
            if (proj.z <= 0)
                return;
            Vector2 apoio = CoordenadaHUDCompacta(proj);
            string acao = AcaoProximaHUD();
            float texto = Mathf.Min(260f, LarguraTextoHUD(acao, 15, true));
            bool destaque = perto == alvoGuia;
            if (destaque)
            {
                // O ponto que faz a missão andar ganha um halo que pulsa.
                bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
                float pisca = reduzir ? 1f : .5f + .5f * Mathf.Sin(Time.unscaledTime * 5.5f);
                float largura = texto + 62f;
                Rect halo = new Rect(Mathf.Clamp(apoio.x - largura * .5f, 16, 1264 - largura) - 5f - 3f * pisca, Mathf.Clamp(apoio.y - 52f, 130, 600) - 5f - 3f * pisca,
                    largura + 10f + 6f * pisca, 52f + 6f * pisca);
                ArredondadoHUD(halo, AlfaTecido(OuroHUD, .18f + .25f * pisca));
            }

            Rect r = BalaoHUD(apoio, texto + 62f, 42f, destaque);
            TeclaRedondaHUD(new Rect(r.x + 7, r.y + 6, 30, 30), "E");
            TextoTecido(new Rect(r.x + 46, r.y, texto + 8, r.height), ResumoHUDClaroUI(acao, texto + 4, 22, 15, true), 15, CremeHUD, true, TextAnchor.MiddleLeft);
            if (AcaoTecido(r, "hud:interagir")) Interagir(perto);
        }

        // Aviso curto no alto, abaixo do título de lugar ou da barra da lenda.
        private void AvisoHUD(bool lutando)
        {
            if (avisoAte <= Time.unscaledTime || string.IsNullOrEmpty(aviso))
                return;
            float restante = avisoAte - Time.unscaledTime;
            float alfa = Mathf.Clamp01(restante / .4f);
            float largura = Mathf.Clamp(LarguraTextoHUD(aviso, 14, false) + 58f, 260f, 620f);
            float altura = AlturaTecido(aviso, largura - 58f, 14) > 22f ? 58f : 40f;
            Rect r = new Rect(640 - largura * .5f, lutando ? 138f : 130f, largura, altura);
            // Não encosta no cartão de objetivo: desce para baixo dele quando os dois se cruzam.
            if (!lutando && cartaoObjetivoHUD.width > 0f && r.x < cartaoObjetivoHUD.xMax + 8f && r.y < cartaoObjetivoHUD.yMax + 8f)
                r.y = cartaoObjetivoHUD.yMax + 12f;
            PainelHUD(r, alfa);
            LosangoCompacta(new Rect(r.x + 16, r.y + altura * .5f - 6f, 12, 12), AlfaTecido(OuroHUD, alfa));
            TextoTecido(new Rect(r.x + 40, r.y, largura - 54f, altura), ResumoHUDClaroUI(aviso, largura - 54f, altura - 8f, 14), 14,
                AlfaTecido(CremeHUD, alfa), false, TextAnchor.MiddleLeft);
        }
    }
}

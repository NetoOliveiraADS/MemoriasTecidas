using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Orientação do jogador: seta de guia na borda da tela, marcador sobre o alvo,
    // dica por inatividade e relógio parado até a primeira conversa.
    public sealed partial class MTMJogo
    {
        [Tooltip("Mantém o relógio parado até a primeira conversa com Dona Feliciana.")]
        public bool pausarRelogioAteConversaInicial = true;

        private const string EventoPrimeiraConversa = "r24_m01_01";
        private const float SegundosParaDicaGuia = 25f;
        private bool mostrarGuiaMundo = true;
        private Texture2D setaGuia;
        private int marcosVistosGuia = -1;
        private string regiaoVistaGuia = "";
        private float inicioSemProgressoGuia, ultimaChamadaGuia;
        private bool dicaGuiaMostrada;

        // O tempo só corre depois que o jogador aprendeu a andar e falou com Feliciana.
        private bool RelogioPausado()
        {
            return pausarRelogioAteConversaInicial && tramaTecida != null && !estado.Tem(EventoPrimeiraConversa);
        }

        // Alvo atual da missão nesta região: a saída que leva ao destino ou o ponto da etapa.
        private MTMPonto AlvoGuiaAtual(out bool ehSaida)
        {
            ehSaida = false;
            if (atual == null || tramaTecida == null)
                return null;
            bool primeiro = GuiaPrimeiroFioAtivo();
            var missao = MissaoAtualTecido();
            var local = primeiro ? LocalPrimeiroFio() : null;
            string destino = !string.IsNullOrEmpty(destinoGuiadoTecido) ? destinoGuiadoTecido :
                local != null ? local.dados.id : RegiaoDaEtapaTecido(EtapaAtualTecido(missao));
            MTMPonto alvo = ProximaSaidaTecido(destino);
            if (alvo == null && destino == atual.dados.id)
                alvo = primeiro && string.IsNullOrEmpty(destinoGuiadoTecido) ? AlvoPrimeiroFio() : AlvoDaEtapaTecido(EtapaAtualTecido(missao));
            if (alvo == null || alvo.dados == null || !alvo.gameObject.activeInHierarchy)
                return null;
            ehSaida = alvo.dados.tipo == "saida";
            return alvo;
        }

        // Terceira linha do cartão de objetivo: passo, lugar e distância em passagens.
        // Passos da etapa atual, para os losangos do cartão de objetivo.
        private void PassosObjetivoHUD(bool primeiro, MTMMissaoTecido missao, MTMEtapaTecido etapa, out int feitos, out int total)
        {
            feitos = total = 0;
            if (primeiro && LocalPrimeiroFio() != null)
            {
                total = 4;
                feitos = Mathf.Clamp(MTMPrimeiroFio.Etapa(estado), 0, 4);
                return;
            }

            if (missao == null || missao.etapas == null || missao.etapas.Length < 2)
                return;
            total = missao.etapas.Length;
            int i = etapa == null ? -1 : Array.IndexOf(missao.etapas, etapa);
            feitos = i < 0 ? total : i;
        }

        private string LinhaOndeHUD(bool primeiro, MTMMissaoTecido missao, MTMEtapaTecido etapa, bool comPasso = true)
        {
            string destino;
            string passo = "";
            if (primeiro && LocalPrimeiroFio() != null)
            {
                destino = LocalPrimeiroFio().dados.id;
                passo = "Passo " + Mathf.Min(4, MTMPrimeiroFio.Etapa(estado) + 1) + "/4 · ";
            }
            else
            {
                destino = RegiaoDaEtapaTecido(etapa);
                if (missao != null && etapa != null && missao.etapas != null && missao.etapas.Length > 1)
                {
                    int i = Array.IndexOf(missao.etapas, etapa);
                    if (i >= 0)
                        passo = "Passo " + (i + 1) + "/" + missao.etapas.Length + " · ";
                }
            }

            if (!comPasso)
                passo = "";
            if (string.IsNullOrEmpty(destino))
                return passo.TrimEnd(' ', '·');
            string nome = NomeCurtoTecido(destino);
            if (atual != null && destino == atual.dados.id)
                return passo + nome + " · você está aqui";
            int passagens = CaminhoTecido(destino).Count - 1;
            return passo + nome + (passagens > 0 ? " · " + passagens + (passagens == 1 ? " passagem" : " passagens") : "");
        }

        // GUIUtility.RotateAroundPivot recebe o pivô em pixels de tela, não nas coordenadas
        // virtuais 1280x720 do HUD. Converte antes; sem isso, em telas de outro tamanho,
        // setas e faixas giram em volta do ponto errado e aparecem deslocadas.
        private static void GirarEmVolta(float graus, Vector2 pivo)
        {
            GUIUtility.RotateAroundPivot(graus, GUI.matrix.MultiplyPoint3x4(new Vector3(pivo.x, pivo.y, 0f)));
        }

        // Altura do losango acima do alvo: sobre a cabeça de pessoas, rente ao chão em saídas.
        private float AlturaMarcadorGuia(MTMPonto alvo)
        {
            if (alvo == null || alvo.dados == null)
                return 1.6f;
            if (alvo.dados.tipo == "saida")
                return .7f;
            if (PontoConfronto(alvo) || PontoVivo(alvo))
                return 1.4f;
            bool pessoa = tramaTecida != null && tramaTecida.personagens != null &&
                Array.Exists(tramaTecida.personagens, x => x != null && x.id == alvo.dados.id);
            // Logo acima da cabeça: os personagens têm cerca de 1,7 de altura e o ponto fica 0,35 acima dos pés.
            // Mais alto, o losango parecia marcar o chão vazio atrás da pessoa.
            return pessoa ? 2.3f : 1.8f;
        }

        private void PrepararSetaGuia()
        {
            if (setaGuia != null)
                return;
            const int n = 64;
            setaGuia = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "Seta_de_guia",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[n * n];
            // Triângulo apontando para a direita: ponta em x=56, base em x=8.
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float meia = (56f - x) * .5f;
                    float alfa = x < 8 || x > 56 ? 0f : Mathf.Clamp01(meia - Mathf.Abs(y - 31.5f));
                    pixels[y * n + x] = new Color(1, 1, 1, alfa);
                }

            setaGuia.SetPixels(pixels);
            setaGuia.Apply(false, true);
        }

        private void AtualizarProgressoGuia()
        {
            float agora = Time.unscaledTime;
            // Painéis e conversas não desenham o HUD: o relógio de inatividade recomeça.
            if (agora - ultimaChamadaGuia > 1f)
                inicioSemProgressoGuia = agora;
            ultimaChamadaGuia = agora;
            int marcos = estado.marcos == null ? 0 : estado.marcos.Count;
            string regiao = atual == null ? "" : atual.dados.id;
            if (marcos != marcosVistosGuia || regiao != regiaoVistaGuia)
            {
                marcosVistosGuia = marcos;
                regiaoVistaGuia = regiao;
                inicioSemProgressoGuia = agora;
                dicaGuiaMostrada = false;
            }
        }

        // Desenha o marcador (alvo visível) ou a seta de borda (alvo fora da tela).
        private void GuiaMundoHUD(MTMPonto alvo, bool ehSaida, Color dourado, Color fundo, Color claro)
        {
            AtualizarProgressoGuia();
            if (!mostrarGuiaMundo || alvo == null || cameraDoMapa == null)
                return;
            bool parado = Time.unscaledTime - inicioSemProgressoGuia > SegundosParaDicaGuia;
            if (parado && !dicaGuiaMostrada)
            {
                dicaGuiaMostrada = true;
                Avisar("Dica: siga a seta dourada. Aperte M para ver o caminho no mapa.");
            }

            bool reduzir = apresentacao != null && apresentacao.reduzirMovimento;
            float pulso = reduzir ? 1f : 1f + (parado ? .25f : .1f) * Mathf.Sin(Time.unscaledTime * (parado ? 7f : 4f));
            Vector3 proj = cameraDoMapa.WorldToScreenPoint(alvo.transform.position + Vector3.up * AlturaMarcadorGuia(alvo));
            if (proj.z <= 0)
                return;
            Vector2 pos = CoordenadaHUDCompacta(proj);
            // Visível na tela = losango sobre o alvo; fora dela = seta na borda.
            Rect tela = new Rect(28, 28, 1224, 664);
            Rect area = new Rect(70, 150, 1140, 430);
            float distancia = corpo == null ? 0f : Vector2.Distance(corpo.position, alvo.transform.position);
            string rotulo = ehSaida ? NomeMapaGuia(alvo.dados.destino) : Mathf.Max(1, Mathf.RoundToInt(distancia)) + " m";
            // Pisca: mais rápido e mais forte quando o jogador está parado há um tempo.
            float pisca = reduzir ? 1f : .5f + .5f * Mathf.Sin(Time.unscaledTime * (parado ? 9f : 5.5f));
            if (distancia > alvo.alcance * 1.4f)
                RastroGuiaHUD(alvo, distancia, dourado, reduzir);
            if (tela.Contains(pos))
            {
                // Perto do alvo, o prompt "E" já orienta; o losango some para não poluir.
                if (distancia <= alvo.alcance * 1.4f)
                    return;
                // Onda no chão do alvo: marca o lugar exato, não só a direção.
                Vector3 chaoTela = cameraDoMapa.WorldToScreenPoint(alvo.transform.position);
                if (chaoTela.z > 0)
                    OndaGuiaHUD(CoordenadaHUDCompacta(chaoTela), dourado, reduzir);
                // Não deixa o losango se esconder atrás do cartão de objetivo ou do relógio.
                foreach (var bloqueio in new[] { cartaoObjetivoHUD, new Rect(1084, 20, 176, 96) })
                    if (bloqueio.width > 0f && new Rect(pos.x - 22f, pos.y - 22f, 44f, 44f).Overlaps(bloqueio))
                        pos.y = bloqueio.yMax + 26f;
                float oscila = reduzir ? 0f : Mathf.Sin(Time.unscaledTime * 3f) * 5f;
                float lado = 30f * pulso;
                // Halo que pisca atrás do losango.
                DiscoHUD(new Rect(pos.x - lado - 6f, pos.y - lado - 6f + oscila, lado * 2f + 12f, lado * 2f + 12f), AlfaTecido(dourado, .12f + .22f * pisca));
                // Contorno escuro: o dourado sozinho some sobre a terra clara do sertão.
                LosangoCompacta(new Rect(pos.x - lado * .5f - 4f, pos.y - lado * .5f + oscila - 4f, lado + 8f, lado + 8f), AlfaTecido(TintaAtualHUD, .9f));
                LosangoCompacta(new Rect(pos.x - lado * .5f, pos.y - lado * .5f + oscila, lado, lado), Color.Lerp(dourado, claro, .35f * pisca));
                LosangoCompacta(new Rect(pos.x - lado * .22f, pos.y - lado * .22f + oscila, lado * .44f, lado * .44f), claro);
                return;
            }

            PrepararSetaGuia();
            Vector2 centro = new Vector2(area.center.x, area.center.y);
            Vector2 d = pos - centro;
            float sx = Mathf.Abs(d.x) < .01f ? float.MaxValue : area.width * .5f / Mathf.Abs(d.x);
            float sy = Mathf.Abs(d.y) < .01f ? float.MaxValue : area.height * .5f / Mathf.Abs(d.y);
            Vector2 p = centro + d * Mathf.Min(1f, Mathf.Min(sx, sy));
            // A seta "empurra" para fora, na direção do alvo, e pisca.
            if (!reduzir)
                p += d.normalized * (4f + 4f * Mathf.Sin(Time.unscaledTime * 6f));
            // Disco escuro com borda dourada e a seta dentro: lê bem em qualquer chão.
            float disco = 50f * pulso;
            DiscoHUD(new Rect(p.x - disco * .5f - 12f, p.y - disco * .5f - 12f, disco + 24f, disco + 24f), AlfaTecido(dourado, .1f + .28f * pisca));
            DiscoHUD(new Rect(p.x - disco * .5f, p.y - disco * .5f + 3f, disco, disco), new Color(0, 0, 0, .35f));
            DiscoHUD(new Rect(p.x - disco * .5f - 2f, p.y - disco * .5f - 2f, disco + 4f, disco + 4f), dourado);
            DiscoHUD(new Rect(p.x - disco * .5f, p.y - disco * .5f, disco, disco), AlfaTecido(TintaAtualHUD, .95f));
            float tamanho = 30f * pulso;
            Matrix4x4 matriz = GUI.matrix;
            Color antiga = GUI.color;
            GirarEmVolta(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, p);
            GUI.color = Color.Lerp(dourado, claro, .45f * pisca);
            GUI.DrawTexture(new Rect(p.x - tamanho * .5f + 2f, p.y - tamanho * .5f, tamanho, tamanho), setaGuia);
            GUI.color = antiga;
            GUI.matrix = matriz;
            float largura = Mathf.Clamp(LarguraTextoHUD(rotulo, 13, true) + 26f, 90f, 240f);
            Rect r = new Rect(Mathf.Clamp(p.x - largura * .5f, 16, 1264 - largura), Mathf.Clamp(p.y + 32, 140, 600), largura, 28);
            PainelHUD(r);
            TextoTecido(r, ResumoHUDClaroUI(rotulo, largura - 16, 22, 13, true), 13, claro, true, TextAnchor.MiddleCenter);
        }

        // Onda que se abre no chão do alvo, duas por vez, defasadas.
        private void OndaGuiaHUD(Vector2 chao, Color dourado, bool reduzir)
        {
            var anel = TexturaAnelVivo();
            Color antiga = GUI.color;
            for (int k = 0; k < 2; k++)
            {
                float fase = reduzir ? .5f : Mathf.Repeat(Time.unscaledTime / 1.4f + k * .5f, 1f);
                float tamanho = Mathf.Lerp(18f, 78f, fase);
                GUI.color = AlfaTecido(dourado, (1f - fase) * .85f);
                // Achatado: o chão é visto de cima, em perspectiva leve.
                GUI.DrawTexture(new Rect(chao.x - tamanho * .5f, chao.y - tamanho * .25f, tamanho, tamanho * .5f), anel);
            }

            GUI.color = antiga;
        }

        // Rastro de pontos no chão, do Neto em direção ao alvo, correndo para a frente.
        private void RastroGuiaHUD(MTMPonto alvo, float distancia, Color dourado, bool reduzir)
        {
            if (corpo == null || distancia < 2.6f)
                return;
            Vector2 origem = corpo.position;
            Vector2 direcao = ((Vector2)alvo.transform.position - origem).normalized;
            float fase = reduzir ? 0f : Mathf.Repeat(Time.unscaledTime * 1.3f, 1f);
            const float espaco = .85f;
            for (int k = 0; k < 6; k++)
            {
                float d = 1.1f + (k + fase) * espaco;
                if (d > distancia - 1f)
                    break;
                Vector2 ponto = origem + direcao * d;
                Vector3 tela = cameraDoMapa.WorldToScreenPoint(new Vector3(ponto.x, ponto.y, 0f));
                if (tela.z <= 0)
                    continue;
                Vector2 p = CoordenadaHUDCompacta(tela);
                // Some na ponta e nasce devagar perto do Neto.
                float alfa = Mathf.Clamp01((k + fase) / 1.2f) * Mathf.Clamp01((6f - k - fase) / 2.5f) * .9f;
                LosangoCompacta(new Rect(p.x - 7f, p.y - 5f, 14f, 10f), AlfaTecido(TintaAtualHUD, alfa * .7f));
                LosangoCompacta(new Rect(p.x - 5f, p.y - 3.5f, 10f, 7f), AlfaTecido(dourado, alfa));
            }
        }

        private string bannerNome = "", bannerDescricao = "";
        private float bannerInicio = -100f, bannerDuracao;

        // Título de lugar ao entrar numa região; a descrição só aparece na primeira visita.
        private void MostrarBannerRegiao(string id, bool primeiraVisita)
        {
            var ficha = MTMAutoriaRoteiro.Lugar(tramaTecida, id);
            bannerNome = NomeMapaGuia(id);
            bannerDescricao = primeiraVisita && ficha != null ? ficha.descricao ?? "" : "";
            bannerInicio = Time.unscaledTime;
            bannerDuracao = bannerDescricao.Length > 0 ? 6f : 3.5f;
        }

        private void BannerRegiaoHUD(Color fundo, Color claro, Color dourado)
        {
            float t = Time.unscaledTime - bannerInicio;
            if (string.IsNullOrEmpty(bannerNome) || t < 0f || t > bannerDuracao)
                return;
            float alfa = Mathf.Clamp01(Mathf.Min(t / .4f, (bannerDuracao - t) / .6f));
            bool descricao = bannerDescricao.Length > 0;
            // Título de lugar como abertura de cordel: nome entre losangos e fios dourados.
            Rect r = new Rect(430, 22, 420, descricao ? 92 : 56);
            PainelHUD(r, alfa);
            Color texto = new Color(claro.r, claro.g, claro.b, alfa);
            float nome = Mathf.Min(LarguraTextoHUD(bannerNome, 21, true), r.width - 90f);
            float meio = r.center.x, linhaY = r.y + 26f;
            Fundo(new Rect(r.x + 18, linhaY, meio - nome * .5f - 22f - (r.x + 18), 1), AlfaTecido(dourado, alfa * .7f));
            Fundo(new Rect(meio + nome * .5f + 22f, linhaY, r.xMax - 18 - (meio + nome * .5f + 22f), 1), AlfaTecido(dourado, alfa * .7f));
            LosangoCompacta(new Rect(meio - nome * .5f - 17f, linhaY - 4f, 9, 9), AlfaTecido(dourado, alfa));
            LosangoCompacta(new Rect(meio + nome * .5f + 8f, linhaY - 4f, 9, 9), AlfaTecido(dourado, alfa));
            TextoTecido(new Rect(meio - nome * .5f, r.y + 12, nome + 2, 30),
                ResumoHUDClaroUI(bannerNome, nome + 2, 30, 21, true), 21, texto, true, TextAnchor.MiddleCenter);
            if (descricao)
                TextoTecido(new Rect(r.x + 22, r.y + 46, r.width - 44, 40),
                    ResumoHUDClaroUI(bannerDescricao, r.width - 44, 40, 13), 13, AlfaTecido(CremeFracoHUD, alfa), false, TextAnchor.MiddleCenter);
        }
    }
}

using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        public MTMConfiguracaoVisual apresentacao = new MTMConfiguracaoVisual();
        private Vector2 antecipacaoCamera;
        private float velocidadeZoom, zoomPreferido = 1f;
        private bool possuiFoco = true;
        private Vector2 ultimaPosicaoCamera;
        private bool primeiraMedicaoCamera = true;
        public MTMRegiao RegiaoAtualVisual
        {
            get
            {
                return atual;
            }
        }

        private const string ChaveVisual = "MemoriasTecidas.Visual.";
        private void PrepararApresentacao()
        {
            if (apresentacao == null)
                apresentacao = new MTMConfiguracaoVisual();
            MTMConfiguracaoVisual.Usar(apresentacao);
            MTMAmbienteV2.Qualidade = Mathf.Clamp(PlayerPrefs.GetInt(ChaveVisual + "Qualidade", 1), 0, 2);
            zoomPreferido = Mathf.Clamp(PlayerPrefs.GetFloat(ChaveVisual + "Zoom", 1f), .65f, 1.6f);
            apresentacao.reduzirMovimento = PlayerPrefs.GetInt(ChaveVisual + "Movimento", apresentacao.reduzirMovimento ? 1 : 0) == 1;
            apresentacao.exposicao = Mathf.Clamp(PlayerPrefs.GetFloat(ChaveVisual + "Brilho", apresentacao.exposicao), .7f, 1.4f);
            if (corpo != null)
            {
                corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
                corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void MoverComSuavidade()
        {
            if (corpo == null)
                return;
            if (Bloqueado || !pronto || !possuiFoco)
            {
                corpo.linearVelocity = Vector2.zero;
                return;
            }

            corpo.linearVelocity = MTMCalculosVisuais.AvancarVelocidade(corpo.linearVelocity, movimento, velocidade * multiplicador, apresentacao.aceleracao, apresentacao.frenagem, Time.fixedDeltaTime);
        }

        private Vector2 DimensoesCamera()
        {
            Vector3 s = atual.transform.lossyScale;
            return new Vector2(atual.dados.largura * Mathf.Abs(s.x), atual.dados.altura * Mathf.Abs(s.y));
        }

        private float TamanhoMaximoCamera()
        {
            Vector2 d = DimensoesCamera();
            return Mathf.Max(1f, Mathf.Min(d.y * .5f, d.x * .5f / Mathf.Max(.1f, cameraDoMapa.aspect)));
        }

        private Vector3 ConterCamera(Vector3 p)
        {
            return MTMCalculosVisuais.ConterCamera(p, atual.transform.position, DimensoesCamera(), cameraDoMapa.orthographicSize, cameraDoMapa.aspect);
        }

        private Vector3 AlvoCamera()
        {
            Vector3 alvo = transform.position + (Vector3)antecipacaoCamera;
            if (falas != null && perto != null && apresentacao.aproximarConversas && !apresentacao.reduzirMovimento)
                alvo = Vector3.Lerp(transform.position, perto.transform.position, .35f) + Vector3.up * .6f;
            alvo.z = -10f;
            return ConterCamera(alvo);
        }

        private void AtualizarCameraFluida()
        {
            if (!pronto || atual == null || cameraDoMapa == null)
                return;
            Vector2 posicaoVisual = transform.position;
            Vector2 deslocamento = primeiraMedicaoCamera ? Vector2.zero : posicaoVisual - ultimaPosicaoCamera;
            primeiraMedicaoCamera = false;
            ultimaPosicaoCamera = posicaoVisual;
            Vector2 velocidadeVista = MTMCalculosMovimento.MedirVelocidade(deslocamento, Time.deltaTime, velocidade * 1.8f);
            // Menus e perda de foco mantêm o enquadramento. Conversas conservam o foco suave.
            if (!possuiFoco || inicioAbertoUI || (painel.Length > 0 && falas == null))
                return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            if (dt <= 0f)
                return;
            if (!Bloqueado && possuiFoco)
                zoomPreferido = Mathf.Clamp(zoomPreferido - MTMEntrada.Zoom() * .08f, .65f, 1.6f);
            float fator = 1f;
            if (!apresentacao.reduzirMovimento)
            {
                float corrida = Mathf.InverseLerp(velocidade, velocidade * 1.55f, velocidadeVista.magnitude);
                fator += corrida * apresentacao.aberturaCorrida * .1f;
                if (falas != null && apresentacao.aproximarConversas)
                    fator *= .94f;
            }

            float tamanho = Mathf.Clamp(tamanhoCamera * zoomPreferido * fator, 2f, TamanhoMaximoCamera());
            cameraDoMapa.orthographicSize = Mathf.Min(TamanhoMaximoCamera(), Mathf.SmoothDamp(cameraDoMapa.orthographicSize, tamanho, ref velocidadeZoom, .24f, Mathf.Infinity, dt));
            Vector2 frente = Bloqueado || apresentacao.reduzirMovimento ? Vector2.zero : Vector2.ClampMagnitude(velocidadeVista * apresentacao.antecipacao, 1.4f);
            antecipacaoCamera = Vector2.Lerp(antecipacaoCamera, frente, 1f - Mathf.Exp(-8f * dt));
            Vector3 alvo = AlvoCamera(), pos = cameraDoMapa.transform.position;
            Vector2 zona = apresentacao.reduzirMovimento ? Vector2.zero : apresentacao.zonaMorta;
            alvo.x = ZonaMorta(pos.x, alvo.x, Mathf.Max(0f, zona.x));
            alvo.y = ZonaMorta(pos.y, alvo.y, Mathf.Max(0f, zona.y));
            pos = Vector3.SmoothDamp(pos, alvo, ref velocidadeCamera, Mathf.Max(.04f, apresentacao.suavidadeCamera), Mathf.Infinity, dt);
            Vector3 contida = ConterCamera(pos);
            MTMCalculosMovimento.LimitarInerciaCamera(pos, contida, ref velocidadeCamera);
            cameraDoMapa.transform.position = contida;
        }

        private static float ZonaMorta(float atual, float alvo, float margem)
        {
            float delta = alvo - atual;
            return Mathf.Abs(delta) <= margem ? atual : alvo - Mathf.Sign(delta) * margem;
        }

        private void CentralizarCamera()
        {
            velocidadeCamera = Vector3.zero;
            antecipacaoCamera = Vector2.zero;
            velocidadeZoom = 0f;
            primeiraMedicaoCamera = true;
            if (atual == null || cameraDoMapa == null)
                return;
            AjustarCamera();
            cameraDoMapa.orthographicSize = Mathf.Clamp(tamanhoCamera * zoomPreferido, 1f, TamanhoMaximoCamera());
            cameraDoMapa.transform.position = AlvoCamera();
        }

        private void OnApplicationFocus(bool foco)
        {
            possuiFoco = foco;
            if (!foco)
                Parar();
        }

        private void SalvarPreferenciasVisuais()
        {
            PlayerPrefs.SetInt(ChaveVisual + "Qualidade", MTMAmbienteV2.Qualidade);
            PlayerPrefs.SetFloat(ChaveVisual + "Zoom", zoomPreferido);
            PlayerPrefs.SetInt(ChaveVisual + "Movimento", apresentacao.reduzirMovimento ? 1 : 0);
            PlayerPrefs.SetFloat(ChaveVisual + "Brilho", apresentacao.exposicao);
            PlayerPrefs.Save();
        }

        private void OpcoesVisuaisTecido()
        {
            CabecalhoOpcoesUI("Do seu jeito.", "OPÇÕES DE JOGO");
            TextoTecido(new Rect(214, 212, 392, 28), "Imagem e câmera", 20, textoPapelUI, true);
            TextoTecido(new Rect(658, 212, 408, 28), "Leitura e conforto", 20, textoPapelUI, true);
            if (BotaoTecido(new Rect(214, 264, 392, 44), "Efeitos: " + MTMAmbienteV2.Nomes[MTMAmbienteV2.Qualidade]))
                MTMAmbienteV2.Qualidade = (MTMAmbienteV2.Qualidade + 1) % 3;
            if (BotaoTecido(new Rect(214, 318, 392, 44), "Acabamento natural: " + (acabamentoOriginal != null && acabamentoOriginal.Natural ? "ligado" : "desligado"), false, acabamentoOriginal != null))
                acabamentoOriginal.DefinirNatural(!acabamentoOriginal.Natural);
            TextoTecido(new Rect(214, 370, 392, 24), "Distância da câmera", 16, textoPapelUI, true);
            if (BotaoTecido(new Rect(214, 400, 124, 36), "Perto"))
                zoomPreferido = Mathf.Max(.65f, zoomPreferido - .1f);
            if (BotaoTecido(new Rect(348, 400, 124, 36), "Longe"))
                zoomPreferido = Mathf.Min(1.6f, zoomPreferido + .1f);
            if (BotaoTecido(new Rect(482, 400, 124, 36), "Padrão"))
                zoomPreferido = 1f;
            TextoTecido(new Rect(214, 446, 392, 28), "Brilho do cenário: " + Mathf.RoundToInt(apresentacao.exposicao * 100f) + "%", 16, textoPapelUI, true);
            if (BotaoTecido(new Rect(214, 486, 190, 42), "Diminuir brilho"))
                apresentacao.exposicao = Mathf.Max(.7f, apresentacao.exposicao - .05f);
            if (BotaoTecido(new Rect(416, 486, 190, 42), "Aumentar brilho"))
                apresentacao.exposicao = Mathf.Min(1.4f, apresentacao.exposicao + .05f);
            if (BotaoTecido(new Rect(658, 264, 408, 44), "Contraste reforçado: " + (contrasteUI ? "ligado" : "desligado")))
            {
                contrasteUI = !contrasteUI;
                tituloUI = null;
                estilosTecido.Clear();
            }

            if (BotaoTecido(new Rect(658, 326, 408, 44), "Texto das conversas: " + (tamanhoDialogoUI == 20 ? "padrão" : tamanhoDialogoUI == 24 ? "grande" : "maior")))
                tamanhoDialogoUI = tamanhoDialogoUI == 20 ? 24 : tamanhoDialogoUI == 24 ? 28 : 20;
            if (BotaoTecido(new Rect(658, 388, 408, 44), "Movimento reduzido: " + (apresentacao.reduzirMovimento ? "ligado" : "desligado")))
                apresentacao.reduzirMovimento = !apresentacao.reduzirMovimento;
            if (BotaoTecido(new Rect(658, 450, 408, 44), "Objetivo no HUD: " + (mostrarObjetivoTecido ? "visível" : "oculto")))
                mostrarObjetivoTecido = !mostrarObjetivoTecido;
            TextoTecido(new Rect(658, 512, 408, 56), "Movimento reduzido suaviza a exploração e desativa balanços e zoom automático.", 14, textoPapelUI);
            if (BotaoTecido(new Rect(214, 580, 852, 42), "Salvar e voltar", true))
                VoltarInterfaceUI();
        }
    }
}

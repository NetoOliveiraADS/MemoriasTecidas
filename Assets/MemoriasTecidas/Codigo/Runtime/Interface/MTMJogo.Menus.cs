using System;
using System.IO;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private bool inicioAbertoUI = true, jornadaIniciadaUI, possuiJornadaUI;
        private bool contrasteUI;
        private int tamanhoDialogoUI = 20;
        private string retornoOpcoesUI = "inicio", retornoColecaoUI = "", erroMenuUI = "";
        private Texture2D arteInicioUI;
        private Font fonteLeituraUI, fonteForteUI, fonteTituloUI;
        private const string ChaveInterface = "MemoriasTecidas.Interface.";
        private void PrepararMenusUI()
        {
            possuiJornadaUI = carregarProgresso && (File.Exists(CaminhoSave) || (importarProgressoAnterior && File.Exists(Path.Combine(Application.persistentDataPath, "memorias-tecidas-mundo-v1.json"))));
            contrasteUI = PlayerPrefs.GetInt(ChaveInterface + "Contraste", 0) == 1;
            tamanhoDialogoUI = Mathf.Clamp(PlayerPrefs.GetInt(ChaveInterface + "Dialogo", 20), 20, 28);
            mostrarObjetivoTecido = PlayerPrefs.GetInt(ChaveInterface + "Objetivo", 1) == 1;
            fonteLeituraUI = Resources.Load<Font>("MemoriasTecidas/Interface/DejaVuSans");
            fonteForteUI = Resources.Load<Font>("MemoriasTecidas/Interface/DejaVuSans-Bold");
            fonteTituloUI = Resources.Load<Font>("MemoriasTecidas/Interface/DejaVuSerif");
            arteInicioUI = Resources.Load<Texture2D>("MemoriasTecidas/Interface/InicioTecida");
            mapaJaguaruanaUI = Resources.Load<Texture2D>("MemoriasTecidas/Interface/MapaJaguaruana");
            PrepararMapaGuiaUI();
        }

        private void SalvarPreferenciasInterface()
        {
            PlayerPrefs.SetInt(ChaveInterface + "Contraste", contrasteUI ? 1 : 0);
            PlayerPrefs.SetInt(ChaveInterface + "Dialogo", tamanhoDialogoUI);
            PlayerPrefs.SetInt(ChaveInterface + "Objetivo", mostrarObjetivoTecido ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void TituloEditorial(Rect r, string texto, int tamanho, Color cor)
        {
            var estilo = new GUIStyle(EstiloTecido(tamanho, false, TextAnchor.UpperLeft));
            if (fonteTituloUI != null)
                estilo.font = fonteTituloUI;
            CorTextoUI(estilo, cor);
            GUI.Label(r, texto, estilo);
        }

        private void FundoInicioUI()
        {
            Fundo(new Rect(0, 0, 1280, 720), profundoUI);
            if (arteInicioUI != null)
                GUI.DrawTexture(new Rect(0, 0, 1280, 720), arteInicioUI, ScaleMode.ScaleAndCrop, false);
            // A área opaca garante leitura independente da imagem ou da luminosidade do cenário.
            Fundo(new Rect(0, 0, 496, 720), AlfaTecido(algodaoUI, contrasteUI ? 1f : .96f));
            TramaHUD(new Rect(496, 0, 5, 720));
        }

        private void InicioUI()
        {
            FundoInicioUI();
            TextoTecido(new Rect(52, 48, 392, 20), "MEMÓRIAS TECIDAS", 12, acentoTextoUI, true);
            // Marca em texto real: Jaguaruana Rede de Memórias • Entre Memórias e Lendas.
            TituloEditorial(new Rect(48, 92, 414, 72), "Jaguaruana", 54, textoSuperficieUI);
            TituloEditorial(new Rect(52, 170, 392, 38), "Rede de Memórias", 27, acentoTextoUI);
            TextoTecido(new Rect(52, 218, 392, 24), "Entre Memórias e Lendas", 15, textoSuperficieUI);
            RedeDecorativaTecido(new Rect(52, 258, 100, 32), ouro);
            Linha(new Vector2(172, 272), new Vector2(280, 272), AlfaTecido(ouro, .6f));
            if (confirmarNovo)
            {
                ConfirmacaoJornadaUI();
                return;
            }

            // Os botões vêm logo abaixo do título; a nota só aparece quando há algo a dizer
            // (jornada salva ou aviso), sem deixar espaço vazio no jogo novo.
            float y = 330f;
            if (BotaoTecido(new Rect(52, y, 392, 52), possuiJornadaUI ? "Continuar de onde parei  →" : "Começar a jornada  →", true))
                EntrarNaJornadaUI();
            y += 52f;
            string nota = !string.IsNullOrEmpty(erroMenuUI) ? erroMenuUI : possuiJornadaUI ? ResumoJornadaSalvaUI() : null;
            if (nota != null)
            {
                TextoTecido(new Rect(52, y + 8, 392, 40), nota, 13, textoSuperficieUI);
                y += 48f;
            }

            if (possuiJornadaUI)
            {
                if (BotaoTecido(new Rect(52, y + 6, 392, 42), "Nova jornada"))
                    confirmarNovo = true;
                y += 48f;
            }

            float atalhos = y + 14f;
            if (BotaoTecido(new Rect(52, atalhos, 190, 42), "Opções"))
                AbrirOpcoesUI("visual", "inicio");
            if (BotaoTecido(new Rect(254, atalhos, 190, 42), "Como jogar"))
                AbrirOpcoesUI("ajuda", "inicio");
            if (BotaoTecido(new Rect(52, atalhos + 56, 190, 40), "Sobre o jogo"))
                AbrirOpcoesUI("sobre", "inicio");
            if (BotaoTecido(new Rect(254, atalhos + 56, 190, 40), "Sair"))
                SairDoJogoUI();
        }

        private void EntrarNaJornadaUI()
        {
            inicioAbertoUI = false;
            jornadaIniciadaUI = true;
            possuiJornadaUI = true;
            painel = "";
            confirmarNovo = false;
            erroMenuUI = "";
            interfaceVisivel = true;
            quadroDaAcao = Time.frameCount;
            LimparFocoUI();
            Salvar();
            Parar();
            if (retomadaNoPontoSeguro)
            {
                Avisar("Jornada retomada no ponto seguro da região. Seu progresso foi mantido.");
                retomadaNoPontoSeguro = false;
            }

            ComecoDaJornada(false);
        }

        private void ConfirmacaoJornadaUI()
        {
            Fundo(new Rect(30, 360, 436, 330), algodaoUI);
            TextoTecido(new Rect(58, 380, 374, 32), "Começar uma nova história?", 20, textoSuperficieUI, true);
            TextoTecido(new Rect(58, 430, 374, 92), "A jornada atual será reiniciada. Antes disso, o jogo guardará uma cópia do progresso salvo neste computador.", 16, textoSuperficieUI);
            if (!string.IsNullOrEmpty(erroMenuUI))
                TextoTecido(new Rect(58, 524, 374, 52), erroMenuUI, 14, acentoTextoUI);
            if (BotaoTecido(new Rect(58, 598, 180, 44), "Manter jornada", true))
            {
                confirmarNovo = false;
                erroMenuUI = "";
            }

            if (BotaoTecido(new Rect(252, 598, 180, 44), "Recomeçar"))
                RecomecarComBackupUI();
        }

        private void RecomecarComBackupUI()
        {
            if (carregarProgresso && File.Exists(CaminhoSave))
            {
                try
                {
                    File.Copy(CaminhoSave, CaminhoSave + ".antes-nova-jornada-" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff") + ".json", false);
                }
                catch (Exception e)
                {
                    erroMenuUI = "Não foi possível guardar a cópia. Sua jornada foi mantida.";
                    Debug.LogWarning(erroMenuUI + " " + e.Message, this);
                    return;
                }
            }

            inicioAbertoUI = false;
            jornadaIniciadaUI = true;
            possuiJornadaUI = true;
            erroMenuUI = "";
            falas = null;
            aoConcluir = null;
            NovaJornada();
            LimparFocoUI();
            quadroDaAcao = Time.frameCount;
            ComecoDaJornada(true);
        }

        private void AbrirOpcoesUI(string paginaUI, string origem)
        {
            retornoOpcoesUI = origem;
            painel = paginaUI;
            Parar();
            LimparFocoUI();
        }

        private void VoltarInterfaceUI()
        {
            if (confirmarNovo)
            {
                confirmarNovo = false;
                erroMenuUI = "";
                LimparFocoUI();
                return;
            }

            if (painel == "prologo")
            {
                EncerrarPrologo();
                return;
            }

            if (EmMemoriaViva())
            {
                FecharMemoriaViva();
                return;
            }

            if (painel == "mapaCidade")
                painel = retornoMapaCidadeUI == "inicio" ? "" : "pausa";
            else if (painel == "arteLugar")
                painel = "mapa";
            else if (painel == "visual" || painel == "ajuda" || painel == "sobre")
            {
                SalvarPreferenciasVisuais();
                SalvarPreferenciasInterface();
                painel = retornoOpcoesUI == "inicio" ? "" : "pausa";
            }
            else if (inicioAbertoUI)
                return;
            else if (painel == "receita")
                painel = "bolsa";
            else
            {
                bool aberto = Bloqueado;
                LimparAtividadeRoteiro();
                LimparPrimeiroFio();
                falas = null;
                aoConcluir = null;
                painel = aberto ? retornoColecaoUI : "pausa";
                retornoColecaoUI = "";
            }

            buscaBolsaEmFoco = false;
            Parar();
            LimparFocoUI();
            quadroDaAcao = Time.frameCount;
        }

        private void PausaEditorialUI()
        {
            Fundo(new Rect(0, 0, 1280, 720), new Color(.05f, .04f, .03f, .78f));
            PainelTecido(new Rect(214, 90, 852, 540), algodaoUI);
            ArredondadoHUD(new Rect(215, 91, 100, 538), tinta);
            Fundo(new Rect(240, 91, 282, 538), tinta);
            TramaHUD(new Rect(522, 91, 4, 538));
            IconeTecido("rede", new Rect(270, 145, 196, 134), acentoTextoUI);
            TituloEditorial(new Rect(252, 320, 232, 92), "Um tempo\npara respirar", 28, textoSuperficieUI);
            TextoTecido(new Rect(252, 444, 232, 78), atual.dados.nome + "\nDia " + estado.dia, 16, textoSuperficieUI);
            TextoTecido(new Rect(566, 130, 450, 24), "JORNADA EM PAUSA", 12, terracotaUI, true);
            TituloEditorial(new Rect(564, 170, 450, 54), "O próximo fio espera.", 30, textoPapelUI);
            if (BotaoTecido(new Rect(566, 254, 452, 52), "Continuar jornada  →", true))
                VoltarInterfaceUI();
            if (BotaoTecido(new Rect(566, 328, 218, 44), "Caderno"))
            {
                retornoColecaoUI = "pausa";
                painel = "diario";
            }

            if (BotaoTecido(new Rect(800, 328, 218, 44), "Bolsa"))
            {
                retornoColecaoUI = "pausa";
                painel = "bolsa";
            }

            if (BotaoTecido(new Rect(566, 390, 218, 44), "Opções"))
                AbrirOpcoesUI("visual", "pausa");
            if (BotaoTecido(new Rect(800, 390, 218, 44), "Como jogar"))
                AbrirOpcoesUI("ajuda", "pausa");
            if (BotaoTecido(new Rect(566, 452, 452, 44), "Mapa de Jaguaruana"))
            {
                retornoColecaoUI = "pausa";
                painel = "mapa";
                LimparFocoUI();
            }

            if (BotaoTecido(new Rect(566, 510, 452, 44), "Voltar à tela inicial"))
            {
                Salvar();
                inicioAbertoUI = true;
                painel = "";
                retornoColecaoUI = "";
                LimparFocoUI();
            }

            if (carregarProgresso)
                TextoTecido(new Rect(566, 572, 452, 42), "O progresso é salvo sozinho a cada encontro.", 13, AlfaTecido(textoPapelUI, .7f));
        }

        private void CabecalhoOpcoesUI(string titulo, string subtitulo)
        {
            if (inicioAbertoUI)
                FundoInicioUI();
            Fundo(new Rect(0, 0, 1280, 720), new Color(.05f, .04f, .03f, .78f));
            PainelTecido(new Rect(170, 48, 940, 624), algodaoUI);
            LosangoCompacta(new Rect(214, 84, 12, 12), ouro);
            TextoTecido(new Rect(234, 80, 680, 22), subtitulo, 12, terracotaUI, true);
            TituloEditorial(new Rect(212, 118, 690, 48), titulo, 34, textoPapelUI);
            if (BotaoTecido(new Rect(958, 92, 108, 40), "Voltar"))
                VoltarInterfaceUI();
            TracoHUD(new Rect(214, 184, 852, 1), AlfaTecido(ouro, .45f), false);
        }

        private void AjudaUI()
        {
            CabecalhoOpcoesUI("Cada encontro é um começo.", "COMO JOGAR");
            TextoTecido(new Rect(214, 214, 824, 70), "Explore Jaguaruana, converse com seus moradores e acompanhe as missões no caderno. Sua rede reúne as lembranças desses encontros.", 19, textoPapelUI);
            string[] teclas = MTMEntrada.UsandoControle
                ? new[]
                {
                    "Analógico / direcional",
                    "RT ou L3",
                    "A",
                    "View · LB · Y",
                    "B ou Start",
                    "R3",
                    "Analógico direito",
                    "X · LT"
                }
                : new[]
                {
                    "WASD / setas",
                    "Shift",
                    "E / Enter",
                    "M · B/I · J",
                    "Esc",
                    "R",
                    "Roda / + / −",
                    "F1"
                };
            string[] acoes =
            {
                "Caminhar",
                "Correr",
                "Conversar ou interagir",
                "Mapa · bolsa · caderno",
                "Voltar ou pausar",
                "Voltar ao caminho seguro",
                "Ajustar a câmera",
                MTMEntrada.UsandoControle ? "Golpear · escutar (segure)" : "Mostrar ou ocultar o HUD"
            };
            for (int i = 0; i < 8; i++)
            {
                float x = 214 + (i / 4) * 438, y = 306 + (i % 4) * 60;
                Fundo(new Rect(x, y, 414, 46), papelUI);
                TextoTecido(new Rect(x + 12, y + 12, 146, 26), teclas[i], 14, textoPapelUI, true);
                TextoTecido(new Rect(x + 162, y + 8, 242, 34), acoes[i], 14, textoPapelUI, false, TextAnchor.MiddleLeft);
            }

            TextoTecido(new Rect(214, 570, 852, 46), "Procure Dona Feliciana para começar. Use J → Missões para escolher qual história acompanhar.", 16, textoPapelUI);
        }

        private void SobreUI()
        {
            CabecalhoOpcoesUI("Histórias que se entrelaçam.", "SOBRE O JOGO");
            IconeTecido("rede", new Rect(234, 230, 196, 138), terracotaUI);
            TextoTecido(new Rect(470, 222, 570, 112), "Memórias Tecidas aproxima lugares, personagens e saberes por meio da exploração e da criação de uma rede de lembranças.", 21, textoPapelUI);
            TextoTecido(new Rect(214, 404, 852, 104), "Os diálogos e atividades são adaptações ficcionais e didáticas. As fontes dos encontros aparecem no caderno; os textos não devem ser tratados como depoimentos históricos autênticos.", 18, textoPapelUI);
            TextoTecido(new Rect(214, 530, 852, 84), "Fontes DejaVu.\nSons: Kenney e Blanket (CC0 e domínio público), gezortenplotz/Freesound (CC BY 3.0) e 0 A.D. © Wildfire Games, wildfiregames.com (CC BY-SA 3.0).\nLista completa em Resources/MemoriasTecidas/Sons/CREDITOS.txt.", 13, textoPapelUI);
        }

        private void SairDoJogoUI()
        {
            SalvarPreferenciasVisuais();
            SalvarPreferenciasInterface();
            Salvar();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            erroMenuUI="Para sair, feche esta aba do navegador.";
            Avisar(erroMenuUI);
#else
            Application.Quit();
#endif
        }
    }
}

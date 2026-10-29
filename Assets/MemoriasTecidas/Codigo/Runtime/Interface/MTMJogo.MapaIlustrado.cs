using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private Texture2D mapaJaguaruanaUI;
        private string retornoMapaCidadeUI = "inicio";
        private float zoomMapaCidadeUI = 1f;
        private Vector2 deslocamentoMapaCidadeUI;
        private int controleArrastoMapaUI;
        private void AbrirMapaCidadeUI(string origem)
        {
            retornoMapaCidadeUI = origem;
            zoomMapaCidadeUI = 1f;
            deslocamentoMapaCidadeUI = Vector2.zero;
            painel = "mapaCidade";
            Parar();
            LimparFocoUI();
            quadroDaAcao = Time.frameCount;
        }

        private Vector2 TamanhoMapaCidadeUI(Rect janela)
        {
            float escala = Mathf.Min(janela.width / mapaJaguaruanaUI.width, janela.height / mapaJaguaruanaUI.height);
            return new Vector2(mapaJaguaruanaUI.width, mapaJaguaruanaUI.height) * escala;
        }

        private void LimitarMapaCidadeUI(Rect janela)
        {
            Vector2 tamanho = TamanhoMapaCidadeUI(janela) * zoomMapaCidadeUI;
            float x = Mathf.Max(0, (tamanho.x - janela.width) * .5f), y = Mathf.Max(0, (tamanho.y - janela.height) * .5f);
            deslocamentoMapaCidadeUI = new Vector2(Mathf.Clamp(deslocamentoMapaCidadeUI.x, -x, x), Mathf.Clamp(deslocamentoMapaCidadeUI.y, -y, y));
        }

        private void AmpliarMapaCidadeUI(Rect janela, float valor, Vector2 ancora)
        {
            float novo = Mathf.Clamp(valor, 1f, 3f), fator = novo / zoomMapaCidadeUI;
            deslocamentoMapaCidadeUI = deslocamentoMapaCidadeUI * fator + (ancora - janela.center) * (1f - fator);
            zoomMapaCidadeUI = novo;
            LimitarMapaCidadeUI(janela);
        }

        // Chamado somente em OnGUI; libera a captura também ao sair com Esc.
        private void SoltarArrastoMapaUI()
        {
            if (controleArrastoMapaUI != 0 && GUIUtility.hotControl == controleArrastoMapaUI)
                GUIUtility.hotControl = 0;
            controleArrastoMapaUI = 0;
        }

        private void EntradaMapaCidadeUI(Rect janela)
        {
            Event e = Event.current;
            int controle = GUIUtility.GetControlID(732401, FocusType.Passive);
            if (!possuiFoco)
                return;
            if (e.type == EventType.ScrollWheel && janela.Contains(e.mousePosition))
            {
                AmpliarMapaCidadeUI(janela, zoomMapaCidadeUI * Mathf.Pow(1.12f, -e.delta.y), e.mousePosition);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && janela.Contains(e.mousePosition) && GUIUtility.hotControl == 0)
            {
                GUIUtility.hotControl = controle;
                controleArrastoMapaUI = controle;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && controleArrastoMapaUI != 0 && GUIUtility.hotControl == controleArrastoMapaUI)
            {
                deslocamentoMapaCidadeUI += e.delta;
                LimitarMapaCidadeUI(janela);
                e.Use();
            }
            else if (e.rawType == EventType.MouseUp && e.button == 0 && controleArrastoMapaUI != 0)
            {
                SoltarArrastoMapaUI();
                if (e.type != EventType.Used)
                    e.Use();
            }
            else if (e.type == EventType.KeyDown)
            {
                Vector2 passo = Vector2.zero;
                switch (e.keyCode)
                {
                    case KeyCode.Plus:
                    case KeyCode.Equals:
                    case KeyCode.KeypadPlus:
                        AmpliarMapaCidadeUI(janela, zoomMapaCidadeUI + .25f, janela.center);
                        break;
                    case KeyCode.Minus:
                    case KeyCode.KeypadMinus:
                        AmpliarMapaCidadeUI(janela, zoomMapaCidadeUI - .25f, janela.center);
                        break;
                    case KeyCode.Home:
                        zoomMapaCidadeUI = 1f;
                        deslocamentoMapaCidadeUI = Vector2.zero;
                        break;
                    case KeyCode.LeftArrow:
                    case KeyCode.A:
                        passo.x = 48;
                        break;
                    case KeyCode.RightArrow:
                    case KeyCode.D:
                        passo.x = -48;
                        break;
                    case KeyCode.UpArrow:
                    case KeyCode.W:
                        passo.y = 48;
                        break;
                    case KeyCode.DownArrow:
                    case KeyCode.S:
                        passo.y = -48;
                        break;
                    default:
                        return;
                }

                deslocamentoMapaCidadeUI += passo;
                LimitarMapaCidadeUI(janela);
                e.Use();
            }
        }

        private void MapaCidadeUI()
        {
            Fundo(new Rect(0, 0, 1280, 720), profundoUI);
            TextoTecido(new Rect(28, 16, 540, 20), "MEMÓRIAS TECIDAS / MAPA", 12, acentoTextoUI, true);
            TituloEditorial(new Rect(26, 40, 600, 42), "Redes de Memórias", 28, textoSuperficieUI);
            if (BotaoTecido(new Rect(1144, 30, 108, 42), "Voltar"))
            {
                SoltarArrastoMapaUI();
                VoltarInterfaceUI();
                return;
            }

            Rect janela = new Rect(28, 94, 1224, 548);
            if (mapaJaguaruanaUI == null)
            {
                TextoTecido(janela, "O mapa ainda não está disponível.", 22, textoSuperficieUI, false, TextAnchor.MiddleCenter);
                return;
            }

            EntradaMapaCidadeUI(janela);
            if (BotaoTecido(new Rect(700, 30, 60, 42), "−", false, zoomMapaCidadeUI > 1.001f))
                AmpliarMapaCidadeUI(janela, zoomMapaCidadeUI - .25f, janela.center);
            TextoTecido(new Rect(766, 38, 94, 28), Mathf.RoundToInt(zoomMapaCidadeUI * 100) + "%", 16, textoSuperficieUI, true, TextAnchor.MiddleCenter);
            if (BotaoTecido(new Rect(866, 30, 60, 42), "+", false, zoomMapaCidadeUI < 2.999f))
                AmpliarMapaCidadeUI(janela, zoomMapaCidadeUI + .25f, janela.center);
            if (BotaoTecido(new Rect(942, 30, 180, 42), "Ver inteiro"))
            {
                zoomMapaCidadeUI = 1f;
                deslocamentoMapaCidadeUI = Vector2.zero;
            }

            Vector2 tamanho = TamanhoMapaCidadeUI(janela) * zoomMapaCidadeUI;
            Vector2 posicao = (janela.size - tamanho) * .5f + deslocamentoMapaCidadeUI;
            GUI.BeginGroup(janela);
            try
            {
                GUI.DrawTexture(new Rect(posicao, tamanho), mapaJaguaruanaUI, ScaleMode.StretchToFill, false);
            }
            finally
            {
                GUI.EndGroup();
            }

            BordaTecido(janela, AlfaTecido(ouro, .45f));
            TextoTecido(new Rect(28, 662, 1224, 22), "Roda / + / − · ampliar    Arrastar / WASD / setas · explorar    Home · ver inteiro    Esc · voltar", 13, textoSuperficieUI);
            TextoTecido(new Rect(28, 691, 1224, 18), "Tab · selecionar botões    Enter / espaço · confirmar", 12, acentoTextoUI);
        }
    }
}

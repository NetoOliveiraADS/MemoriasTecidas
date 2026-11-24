using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private sealed class AlvoFocoUI
        {
            public string chave, nomeCampo;
            public Rect retangulo, janela, conteudo;
            public Vector2 rolagem;
            public bool emLista;
        }

        private List<AlvoFocoUI> focosUI = new List<AlvoFocoUI>(), proximosFocosUI = new List<AlvoFocoUI>();
        private string focoUI = "", telaFocoUI = "", ativarFocoUI = "";
        private string pressionadoUI = "";
        private bool tecladoUI, emRolagemUI, rolagemPendenteUI;
        private int quadroAtivacaoUI = -1, quadroControleUI = -1;
        private Rect janelaRolagemUI, conteudoRolagemUI, janelaPendenteUI;
        private Vector2 posicaoRolagemUI, posicaoPendenteUI;
        private bool ConfirmacaoComFocoUI
        {
            get
            {
                return tecladoUI && !string.IsNullOrEmpty(focoUI) && MTMEntrada.ConfirmouMenu();
            }
        }

        private void LimparFocoUI()
        {
            focoUI = "";
            ativarFocoUI = "";
            pressionadoUI = "";
            tecladoUI = false;
            rolagemPendenteUI = false;
            // Pode ser chamado fora de OnGUI: não acessar GUI aqui.
            focosUI.Clear();
        }

        private void IniciarFocoUI()
        {
            string tela = (inicioAbertoUI ? "inicio/" : "") + (falas != null ? "dialogo" : painel) + "/" + abaCaderno + "/" + abaBolsa + "/" + confirmarNovo;
            if (tela != telaFocoUI)
            {
                LimparFocoUI();
                telaFocoUI = tela;
                GUI.FocusControl(null);
            }

            proximosFocosUI.Clear();
            emRolagemUI = false;
            ativarFocoUI = "";
            Event e = Event.current;
            if (e.rawType == EventType.MouseUp || !possuiFoco)
                pressionadoUI = "";
            bool campo = GUI.GetNameOfFocusedControl() == "MT_BuscaBolsa";
            bool seta = (inicioAbertoUI || painel == "pausa" || painel == "visual" || painel == "ajuda" || painel == "sobre" || painel == "creditos") && !campo && painel != "mapaCidade";
            // Controle: o direcional escolhe o botão mais próximo naquela direção e A confirma.
            // Só com algum painel ou conversa aberta; no mundo, o direcional anda.
            bool painelAberto = inicioAbertoUI || falas != null || !string.IsNullOrEmpty(painel);
            if (painelAberto && !campo && e.type == EventType.Layout && quadroControleUI != Time.frameCount)
            {
                quadroControleUI = Time.frameCount;
                // Nos menus (início, pausa, ajustes), o controle já chega com um botão destacado.
                if (seta && MTMEntrada.UsandoControle && !tecladoUI && focosUI.Count > 0)
                {
                    int primeiro = focosUI.FindIndex(f => f.nomeCampo == null);
                    if (primeiro >= 0)
                        FocarAlvoUI(focosUI[primeiro]);
                }

                var direcao = MTMEntrada.NavegacaoUI();
                if (direcao != Vector2Int.zero && focosUI.Count > 0)
                    MoverFocoControleUI(direcao);
                if (MTMEntrada.UsandoControle && tecladoUI && !string.IsNullOrEmpty(focoUI) && MTMEntrada.ConfirmouMenu())
                    ativarFocoUI = focoUI;
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Tab || (seta && (e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.UpArrow))))
            {
                if (focosUI.Count > 0)
                {
                    int atualF = focosUI.FindIndex(f => campo ? f.nomeCampo == "MT_BuscaBolsa" : f.chave == focoUI);
                    int passo = e.shift || e.keyCode == KeyCode.UpArrow ? -1 : 1;
                    int indice = atualF < 0 ? (passo > 0 ? 0 : focosUI.Count - 1) : (atualF + passo + focosUI.Count) % focosUI.Count;
                    FocarAlvoUI(focosUI[indice]);
                }

                e.Use();
            }
            else if (e.type == EventType.KeyDown && !campo && tecladoUI && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space))
            {
                ativarFocoUI = focoUI;
                e.Use();
            }
            else if (e.type == EventType.MouseDown)
                tecladoUI = false;
        }

        private void FocarAlvoUI(AlvoFocoUI alvo)
        {
            focoUI = alvo.chave;
            tecladoUI = true;
            GUI.FocusControl(alvo.nomeCampo);
            buscaBolsaEmFoco = alvo.nomeCampo == "MT_BuscaBolsa";
            if (alvo.emLista)
            {
                float y = alvo.rolagem.y;
                if (alvo.retangulo.y < y)
                    y = alvo.retangulo.y;
                if (alvo.retangulo.yMax > y + alvo.janela.height)
                    y = alvo.retangulo.yMax - alvo.janela.height;
                posicaoPendenteUI = new Vector2(alvo.rolagem.x, Mathf.Clamp(y, 0, Mathf.Max(0, alvo.conteudo.height - alvo.janela.height)));
                janelaPendenteUI = alvo.janela;
                rolagemPendenteUI = true;
            }
        }

        // Retângulo do alvo na tela (dentro de listas, desconta a rolagem).
        private static Rect TelaFocoUI(AlvoFocoUI f)
        {
            return f.emLista
                ? new Rect(f.janela.x + f.retangulo.x - f.rolagem.x, f.janela.y + f.retangulo.y - f.rolagem.y, f.retangulo.width, f.retangulo.height)
                : f.retangulo;
        }

        // Navegação espacial pelo controle: o alvo mais perto na direção pedida; sem nenhum, segue a ordem.
        private void MoverFocoControleUI(Vector2Int direcao)
        {
            int atual = focosUI.FindIndex(f => f.chave == focoUI && f.nomeCampo == null);
            if (atual < 0)
            {
                int primeiro = focosUI.FindIndex(f => f.nomeCampo == null);
                if (primeiro >= 0)
                    FocarAlvoUI(focosUI[primeiro]);
                return;
            }

            if (!tecladoUI)
            {
                // O primeiro toque só mostra onde o foco está.
                FocarAlvoUI(focosUI[atual]);
                return;
            }

            Vector2 centro = TelaFocoUI(focosUI[atual]).center, d = new Vector2(direcao.x, direcao.y);
            AlvoFocoUI melhor = null;
            float melhorNota = float.MaxValue;
            foreach (var f in focosUI)
            {
                if (f.chave == focoUI || f.nomeCampo != null)
                    continue;
                Vector2 v = TelaFocoUI(f).center - centro;
                float frente = Vector2.Dot(v, d);
                if (frente < 6f)
                    continue;
                float lado = Mathf.Abs(v.x * d.y - v.y * d.x);
                float nota = frente + lado * 2.2f;
                if (nota < melhorNota)
                {
                    melhorNota = nota;
                    melhor = f;
                }
            }

            if (melhor == null)
            {
                int passo = direcao.x + direcao.y > 0 ? 1 : -1;
                for (int i = 1; i <= focosUI.Count; i++)
                {
                    var f = focosUI[(atual + passo * i + focosUI.Count * 2) % focosUI.Count];
                    if (f.nomeCampo == null && f.chave != focoUI)
                    {
                        melhor = f;
                        break;
                    }
                }
            }

            if (melhor != null)
                FocarAlvoUI(melhor);
        }

        private void FinalizarFocoUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;
            var anterior = focosUI;
            focosUI = proximosFocosUI;
            proximosFocosUI = anterior;
        }

        private void RegistrarFocoUI(Rect r, string chave, string nomeCampo = null)
        {
            if (!GUI.enabled)
                return;
            proximosFocosUI.Add(new AlvoFocoUI { chave = chave, nomeCampo = nomeCampo, retangulo = r, emLista = emRolagemUI, janela = janelaRolagemUI, conteudo = conteudoRolagemUI, rolagem = posicaoRolagemUI });
            if (tecladoUI && focoUI == chave)
            {
                // Foco do teclado ou do controle: contorno dourado com um fio escuro por dentro.
                BordaTecido(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), ouro, 3);
                BordaTecido(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), new Color(0, 0, 0, .55f), 1);
            }
        }

        private int EstadoAcaoUI(Rect r, string chave, bool habilitado)
        {
            if (!habilitado)
                return 0;
            bool sobre = r.Contains(Event.current.mousePosition);
            if (sobre && pressionadoUI == chave && GUIUtility.hotControl != 0)
                return 2;
            return sobre || (tecladoUI && focoUI == chave) ? 1 : 0;
        }

        private bool AcaoTecido(Rect r, string chave)
        {
            RegistrarFocoUI(r, chave);
            if (GUI.enabled && Event.current.type == EventType.MouseDown && Event.current.button == 0 && r.Contains(Event.current.mousePosition))
                pressionadoUI = chave;
            bool clicou = GUI.Button(r, GUIContent.none, GUIStyle.none);
            bool ativou = GUI.enabled && tecladoUI && ativarFocoUI == chave && quadroAtivacaoUI != Time.frameCount;
            if (ativou)
            {
                ativarFocoUI = "";
                quadroAtivacaoUI = Time.frameCount;
            }

            if (clicou)
            {
                focoUI = chave;
                tecladoUI = false;
            }

            return clicou || ativou;
        }

        private Vector2 IniciarRolagemUI(Rect janela, Vector2 posicao, Rect conteudo)
        {
            if (rolagemPendenteUI && janela == janelaPendenteUI)
            {
                posicao = posicaoPendenteUI;
                rolagemPendenteUI = false;
            }

            posicao = GUI.BeginScrollView(janela, posicao, conteudo);
            emRolagemUI = true;
            janelaRolagemUI = janela;
            conteudoRolagemUI = conteudo;
            posicaoRolagemUI = posicao;
            return posicao;
        }

        private void TerminarRolagemUI()
        {
            GUI.EndScrollView();
            emRolagemUI = false;
        }
    }
}

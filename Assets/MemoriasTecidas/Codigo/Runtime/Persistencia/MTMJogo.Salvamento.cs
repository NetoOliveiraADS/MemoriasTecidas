using System;
using System.IO;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private string CaminhoSave
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "memorias-tecidas-mundo-v2.json");
            }
        }

        // A pasta do save leva o nome da empresa e do jogo (Player Settings). Até a versão 3.0 ela era
        // DefaultCompany/MemoriasTecidas: se o save ainda não existe na pasta nova, copia o de lá
        // (o arquivo antigo fica intacto). As preferências (volume, imagem) começam do padrão.
        private void TrazerSaveDaPastaAntiga()
        {
            try
            {
                string pastaAtual = Application.persistentDataPath;
                string raiz = Path.GetDirectoryName(Path.GetDirectoryName(pastaAtual));
                if (string.IsNullOrEmpty(raiz))
                    return;
                string pastaAntiga = Path.Combine(raiz, "DefaultCompany", "MemoriasTecidas");
                if (!Directory.Exists(pastaAntiga) || string.Equals(Path.GetFullPath(pastaAntiga).TrimEnd('\\', '/'),
                        Path.GetFullPath(pastaAtual).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                    return;
                foreach (var nome in new[] { "memorias-tecidas-mundo-v2.json", "memorias-tecidas-mundo-v1.json" })
                {
                    string de = Path.Combine(pastaAntiga, nome), para = Path.Combine(pastaAtual, nome);
                    if (!File.Exists(de) || File.Exists(para))
                        continue;
                    Directory.CreateDirectory(pastaAtual);
                    File.Copy(de, para, false);
                    Debug.Log("Progresso trazido da pasta anterior: " + de, this);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Não foi possível trazer o progresso da pasta anterior: " + e.Message, this);
            }
        }

        private void Salvar()
        {
            if (!pronto || !carregarProgresso || !jornadaIniciadaUI)
                return;
            try
            {
                RegistrarPontoDaJornada();
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporario = CaminhoSave + ".tmp";
                // No meio de uma luta a noite foi adiantada só para o encontro: o save guarda o horário de antes.
                float relogioReal = estado.relogio;
                if (noiteForcadaConfronto)
                    estado.relogio = relogioAntesConfronto;
                string json;
                try
                {
                    json = JsonUtility.ToJson(estado, true);
                }
                finally
                {
                    estado.relogio = relogioReal;
                }

                File.WriteAllText(temporario, json);
                File.Copy(temporario, CaminhoSave, true);
                File.Delete(temporario);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Não foi possível salvar Mundo V2: " + e.Message, this);
                Avisar("O progresso desta sessão não pôde ser salvo.");
            }
        }

        // Salva sozinho de tempos em tempos enquanto o jogador anda livre, para a jornada voltar perto
        // de onde parou mesmo se o jogo fechar de repente. Encontros e trocas de lugar já salvam na hora.
        private const float IntervaloAutosave = 30f;
        private float proximoAutosave;

        private void AutosalvarSeHora()
        {
            if (proximoAutosave <= 0f)
                proximoAutosave = Time.unscaledTime + IntervaloAutosave;
            if (Time.unscaledTime < proximoAutosave)
                return;
            proximoAutosave = Time.unscaledTime + IntervaloAutosave;
            if (!Bloqueado && !EmConfronto)
                Salvar();
        }

        private void Carregar()
        {
            string origem = CaminhoSave;
            if (!File.Exists(origem) && importarProgressoAnterior)
                origem = Path.Combine(Application.persistentDataPath, "memorias-tecidas-mundo-v1.json");
            if (!File.Exists(origem))
                return;
            try
            {
                MTMEstado s = JsonUtility.FromJson<MTMEstado>(File.ReadAllText(origem));
                if (s == null || s.versao != 1 || s.marcos == null || float.IsNaN(s.relogio) || float.IsInfinity(s.relogio))
                    throw new InvalidDataException("Formato de save inválido.");
                s.relogio = Mathf.Repeat(s.relogio, 1f);
                s.dia = Mathf.Max(1, s.dia);
                estado = s;
                if (s.versaoRoteiro < 2 && origem == CaminhoSave)
                {
                    string reserva = CaminhoSave + ".antes-roteiro24-" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff") + ".json";
                    try
                    {
                        File.Copy(CaminhoSave, reserva, false);
                        Debug.Log("Backup do progresso anterior criado: " + reserva, this);
                    }
                    catch (Exception backupErro)
                    {
                        migracaoBloqueadaRoteiro = true;
                        caminhoSaveComErro = origem;
                        erroFatalUI = "Não foi possível criar a cópia de segurança do seu progresso antes de atualizá-lo. Nada foi alterado.";
                        Debug.LogError("Não foi possível criar o backup do progresso. O jogo foi interrompido sem sobrescrever o save: " + backupErro.Message, this);
                    }
                }

                if (origem != CaminhoSave)
                    Debug.Log("Mundo V2: progresso anterior importado. O arquivo do Mundo V1 foi preservado.", this);
            }
            catch (Exception e)
            {
                migracaoBloqueadaRoteiro = true;
                caminhoSaveComErro = origem;
                erroFatalUI = "O progresso salvo não pôde ser aberto. O arquivo foi preservado sem alterações.";
                Debug.LogError("O save não pôde ser carregado e foi preservado sem alterações: " + e.Message, this);
            }
        }

        // ---------- tela de erro (save ou roteiro que não abriu) ----------

        private string erroFatalUI = "", caminhoSaveComErro = "";
        private GUIStyle estiloErroFatal;

        // Desenhada com a skin padrão: não depende do tema, que pode não ter sido preparado.
        private void ErroFatalUI()
        {
            Matrix4x4 matriz = GUI.matrix;
            Color cor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(.09f, .07f, .05f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float escala = Mathf.Max(.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280f * escala) * .5f, (Screen.height - 720f * escala) * .5f, 0f), Quaternion.identity, Vector3.one * escala);
            if (estiloErroFatal == null)
            {
                estiloErroFatal = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true, alignment = TextAnchor.MiddleCenter };
                estiloErroFatal.normal.textColor = new Color(.96f, .91f, .83f, 1f);
            }

            bool temSave = !string.IsNullOrEmpty(caminhoSaveComErro);
            string texto = erroFatalUI + (temSave
                ? "\n\nVocê pode começar uma nova jornada. O arquivo antigo será guardado com outro nome, na mesma pasta."
                : "\n\nO Console do Unity mostra o motivo.");
            GUI.Label(new Rect(190, 170, 900, 250), texto, estiloErroFatal);
            var botao = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            if (temSave && GUI.Button(new Rect(340, 460, 360, 54), "Começar nova jornada", botao))
                RecomecarAposErroDeSave();
            if (GUI.Button(new Rect(temSave ? 720 : 540, 460, 220, 54), "Sair", botao))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }

            GUI.matrix = matriz;
            GUI.color = cor;
        }

        private void RecomecarAposErroDeSave()
        {
            try
            {
                File.Move(caminhoSaveComErro, caminhoSaveComErro + ".com-erro-" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff") + ".json");
            }
            catch (Exception e)
            {
                erroFatalUI = "Não foi possível guardar o arquivo antigo, então nada foi alterado: " + e.Message;
                caminhoSaveComErro = "";
                return;
            }

            // Nada do jogo foi montado ainda: recarregar a cena recomeça do zero, sem o save.
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void NovaJornada()
        {
            retomadaNoPontoSeguro = false;
            regiaoConfrontoConferida = "";
            estado = new MTMEstado();
            confirmarNovo = false;
            painel = "";
            missaoFixadaTecido = "";
            missaoSelecionadaTecido = "";
            destinoMapa = "";
            destinoGuiadoTecido = "";
            estado.versaoRoteiro = 2;
            LimparAtividadeRoteiro();
            etapaEscolta = null;
            etapaAparicao = null;
            AtualizarVisibilidadeRoteiro();
            LimparPrimeiroFio();
            AtualizarAmostraPrimeiroFio();
            if (!IrParaPontoSeguro(RegiaoInicialPrimeiroFio()))
                IrParaPontoSeguro("praca");
            Avisar("Uma nova jornada começou. Converse com Dona Feliciana para guardar a primeira lembrança.");
        }
    }
}

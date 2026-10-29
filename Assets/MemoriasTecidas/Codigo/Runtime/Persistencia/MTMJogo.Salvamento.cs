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

        private void Salvar()
        {
            if (!pronto || !carregarProgresso || !jornadaIniciadaUI)
                return;
            try
            {
                RegistrarPontoDaJornada();
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporario = CaminhoSave + ".tmp";
                File.WriteAllText(temporario, JsonUtility.ToJson(estado, true));
                File.Copy(temporario, CaminhoSave, true);
                File.Delete(temporario);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Não foi possível salvar Mundo V2: " + e.Message, this);
                Avisar("O progresso desta sessão não pôde ser salvo.");
            }
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
                        Debug.LogError("Não foi possível criar o backup do progresso. O jogo foi interrompido sem sobrescrever o save: " + backupErro.Message, this);
                    }
                }

                if (origem != CaminhoSave)
                    Debug.Log("Mundo V2: progresso anterior importado. O arquivo do Mundo V1 foi preservado.", this);
            }
            catch (Exception e)
            {
                migracaoBloqueadaRoteiro = true;
                Debug.LogError("O save não pôde ser carregado e foi preservado sem alterações: " + e.Message, this);
            }
        }

        private void NovaJornada()
        {
            retomadaNoPontoSeguro = false;
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

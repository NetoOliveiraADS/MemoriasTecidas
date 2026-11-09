using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        /// <summary>Placas só funcionam durante exploração livre, sem painéis nem diálogos.</summary>
        public bool PodeAtivarPuzzle
        {
            get
            {
                return pronto && possuiFoco && !Bloqueado &&
                       !ConfirmacaoComFocoUI && atual != null && enabled;
            }
        }

        public void AvisarPuzzle(string mensagem)
        {
            if (PodeAtivarPuzzle)
                Avisar(mensagem);
        }

        public bool RegistrarPuzzleConcluido(string idPuzzle)
        {
            if (string.IsNullOrWhiteSpace(idPuzzle) || !PodeAtivarPuzzle || estado == null)
                return false;

            string marco = "exploracao_puzzle_" + idPuzzle;
            if (estado.Tem(marco)) return true;

            if (estado.marcos == null)
                estado.marcos = new System.Collections.Generic.List<string>();
            estado.marcos.Add(marco);
            Salvar();
            return true;
        }
    }
}

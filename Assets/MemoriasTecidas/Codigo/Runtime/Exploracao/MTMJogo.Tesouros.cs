using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private bool InteragirTesouro(MTMPonto ponto)
        {
            var bau = ponto == null ? null : ponto.GetComponent<MTMBau>();
            if (bau == null) return false;
            string id = "exploracao_bau_" + bau.idTesouro;
            if (string.IsNullOrWhiteSpace(bau.idTesouro) || estado == null)
            {
                Avisar("Este tesouro precisa de um identificador.");
                return true;
            }
            if (!string.IsNullOrEmpty(bau.idPuzzleNecessario) &&
                !estado.Tem("exploracao_puzzle_" + bau.idPuzzleNecessario))
            {
                Avisar("O selo permanece fechado. Ative as placas na ordem correta.");
                return true;
            }
            if (estado.Tem(id))
            {
                Avisar("Você já encontrou este tesouro.");
                bau.Sincronizar(estado);
                return true;
            }
            if (string.IsNullOrWhiteSpace(bau.idRecompensa))
            {
                Avisar("Este baú não possui recompensa configurada.");
                return true;
            }
            int antes = MTMTrama.Quantidade(estado, bau.idRecompensa);
            // Evita overflow e não altera a progressão de missões.
            if (antes > int.MaxValue - bau.quantidade)
            {
                Avisar("Não é possível guardar mais deste material.");
                return true;
            }
            estado.DefinirQuantidade(bau.idRecompensa, antes + bau.quantidade);
            if (estado.marcos == null)
                estado.marcos = new System.Collections.Generic.List<string>();
            estado.marcos.Add(id);
            bau.Sincronizar(estado);
            Salvar();
            Avisar("Tesouro encontrado! +" + bau.quantidade + " " + bau.idRecompensa + ".");
            return true;
        }

        private void SincronizarTesourosDaRegiao()
        {
            if (atual == null) return;
            foreach (var bau in atual.GetComponentsInChildren<MTMBau>(true))
                bau.Sincronizar(estado);
        }
    }
}

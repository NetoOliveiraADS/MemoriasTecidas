using System;

namespace MemoriasTecidas.MundoV2
{
    [Serializable]
    public sealed class MTMPerfilPersonagens
    {
        public bool ativo = true;
        public float saturacao = .94f, suavizacao = .12f, luzPreenchimento = .03f;

        public void Validar()
        {
            if (!NoIntervalo(saturacao, 0f, 1f) || !NoIntervalo(suavizacao, 0f, .35f) ||
                !NoIntervalo(luzPreenchimento, 0f, .18f))
                throw new InvalidOperationException("Perfil de personagens inválido: saturação 0–1, suavização 0–0,35 e luz 0–0,18.");
        }

        private static bool NoIntervalo(float valor, float minimo, float maximo)
        {
            return !float.IsNaN(valor) && !float.IsInfinity(valor) && valor >= minimo && valor <= maximo;
        }
    }
}

namespace MemoriasTecidas.Missao01
{
    public enum MTMemoriaEtapa { FalarComNeto, OuvirAndre, Concluida }
    public enum MTMemoriaEncontro { SemAvanco, ApresentacaoNeto, RelatoAndre }

    // Estado de uma sessão. Não escreve PlayerPrefs e não depende de MonoBehaviour.
    public sealed class MTMemoriaProgresso
    {
        public MTMemoriaEtapa Etapa { get; private set; } = MTMemoriaEtapa.FalarComNeto;

        public bool Concluir(MTMemoriaEncontro encontro)
        {
            if (Etapa == MTMemoriaEtapa.FalarComNeto && encontro == MTMemoriaEncontro.ApresentacaoNeto)
            {
                Etapa = MTMemoriaEtapa.OuvirAndre;
                return true;
            }
            if (Etapa == MTMemoriaEtapa.OuvirAndre && encontro == MTMemoriaEncontro.RelatoAndre)
            {
                Etapa = MTMemoriaEtapa.Concluida;
                return true;
            }
            return false;
        }
    }
}

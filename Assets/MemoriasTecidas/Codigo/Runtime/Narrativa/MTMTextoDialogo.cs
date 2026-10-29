using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MemoriasTecidas.MundoV2
{
    public static class MTMTextoDialogo
    {
        public const string Assinatura = "Rede de Memorias - Memorias Tecidas";
        private static readonly string[] Vozes =
        {
            "Neto",
            "Dona Feliciana",
            "Simão de Góis",
            "Padre Ducéu",
            "Seu Bento",
            "Lia",
            "Rosa",
            "Miguel",
            "Artesão",
            "Artesã",
            "Comerciante",
            "Guia",
            "Moradora",
            "Morador",
            "Visitante",
            "Voz misteriosa",
            "Narrador"
        };
        public static string NomeLimpo(string nome)
        {
            string texto = (nome ?? "").Trim();
            texto = Regex.Replace(texto, @"^mem[oó]ria\s+(?:de|da|do)\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            texto = Regex.Replace(texto, @"\s*[\(\[]\s*mem[oó]ria[^\)\]]*[\)\]]\s*$", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            foreach (string separador in new[]
            {
                " · ",
                " • ",
                " — ",
                " – "
            }

            )
            {
                int i = texto.IndexOf(separador, StringComparison.Ordinal);
                if (i > 0)
                    texto = texto.Substring(0, i);
            }

            texto = Regex.Replace(texto, @"\s+-\s+mem[oó]ria.*$", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return texto.Trim();
        }

        public static void SepararFala(string linha, string encontro, IEnumerable<string> participantes, out string nome, out string texto)
        {
            nome = NomeLimpo(encontro);
            texto = (linha ?? "").Replace("\r\n", "\n").Trim();
            int doisPontos = texto.IndexOf(':');
            if (doisPontos <= 0 || doisPontos > 60)
                return;
            string candidato = NomeLimpo(texto.Substring(0, doisPontos));
            bool conhecido = Array.Exists(Vozes, x => string.Equals(x, candidato, StringComparison.OrdinalIgnoreCase));
            if (!conhecido && participantes != null)
                foreach (string pessoa in participantes)
                    if (string.Equals(NomeLimpo(pessoa), candidato, StringComparison.OrdinalIgnoreCase))
                    {
                        conhecido = true;
                        break;
                    }

            if (!conhecido)
                return; // Dois-pontos em pistas e instruções não inventam um interlocutor.
            nome = candidato;
            texto = texto.Substring(doisPontos + 1).Trim();
        }
    }
}

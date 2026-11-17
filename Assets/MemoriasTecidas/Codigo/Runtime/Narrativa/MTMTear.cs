using System;
using System.Collections.Generic;

namespace MemoriasTecidas.MundoV2
{
    // Puzzle do tear de pedais (o primeiro fio). Regras independentes do Unity, testadas fora dele.
    //
    // A urdidura (fios em pé) já está montada, cada coluna com uma cor. Cada pedal levanta um
    // grupo de fios da urdidura. A lançadeira passa a trama (fio deitado) de uma cor: onde a
    // urdidura está levantada aparece a cor dela; onde ficou baixa, aparece a cor da trama.
    // O jogador escolhe, linha a linha, o pedal e a cor que reproduzem a amostra de referência.
    // A prévia da linha aparece antes de passar a lançadeira, com os pontos que ainda não batem.

    [Serializable]
    public sealed class MTMTearDados
    {
        public int versao;
        public string[] nomesCores, cores;
        public MTMTearNivel[] niveis;
    }

    [Serializable]
    public sealed class MTMTearNivel
    {
        public string id, nome, explicacao, conclusao;
        // Cor de cada fio da urdidura (índice em nomesCores).
        public int[] urdidura;
        // Um texto de 0 e 1 por pedal: 1 = levanta aquele fio da urdidura.
        public string[] pedais;
        // Solução usada para montar a amostra: "pedal,cor" por linha (pedal começa em 1).
        public string[] solucao;
        // Amostra de aprendizado: o tear troca os pedais sozinho e o jogador só escolhe a cor.
        public bool pedalAutomatico;
    }

    public enum MTMResultadoTear
    {
        SemEscolha,
        Errada,
        Certa,
        NivelCompleto,
        TudoCompleto
    }

    public static class MTMTear
    {
        // Prefixo que identifica o formato novo no salvamento.
        public const int MarcaSalvo = 1000;

        public static bool Levanta(MTMTearNivel nivel, int pedal, int coluna)
        {
            return nivel.pedais[pedal][coluna] == '1';
        }

        // Cores que aparecem numa linha tecida com este pedal e esta cor de trama.
        public static int[] Linha(MTMTearNivel nivel, int pedal, int cor)
        {
            var linha = new int[nivel.urdidura.Length];
            for (int i = 0; i < linha.Length; i++)
                linha[i] = Levanta(nivel, pedal, i) ? nivel.urdidura[i] : cor;
            return linha;
        }

        public static void Solucao(MTMTearNivel nivel, int linha, out int pedal, out int cor)
        {
            var partes = nivel.solucao[linha].Split(',');
            pedal = int.Parse(partes[0].Trim()) - 1;
            cor = int.Parse(partes[1].Trim());
        }

        public static int[] Alvo(MTMTearNivel nivel, int linha)
        {
            int pedal, cor;
            Solucao(nivel, linha, out pedal, out cor);
            return Linha(nivel, pedal, cor);
        }

        public static bool Iguais(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        // Quantos pares (pedal, cor) produzem a linha da amostra. 1 = só uma resposta.
        public static int Respostas(MTMTearNivel nivel, int linha, int totalCores)
        {
            var alvo = Alvo(nivel, linha);
            int n = 0;
            for (int p = 0; p < nivel.pedais.Length; p++)
                for (int c = 0; c < totalCores; c++)
                    if (Iguais(Linha(nivel, p, c), alvo))
                        n++;
            return n;
        }

        public static void Validar(MTMTearDados dados)
        {
            if (dados == null || dados.versao != 1)
                throw new InvalidOperationException("tear_primeiro_fio.json inválido ou de versão desconhecida.");
            if (dados.nomesCores == null || dados.cores == null || dados.nomesCores.Length < 2 || dados.nomesCores.Length != dados.cores.Length)
                throw new InvalidOperationException("O tear precisa de pelo menos duas cores, com nome e valor.");
            if (dados.niveis == null || dados.niveis.Length == 0)
                throw new InvalidOperationException("O tear precisa de pelo menos uma amostra.");
            var ids = new HashSet<string>();
            foreach (var n in dados.niveis)
            {
                if (n == null || string.IsNullOrEmpty(n.id) || !ids.Add(n.id) || string.IsNullOrEmpty(n.nome))
                    throw new InvalidOperationException("Amostra do tear sem id, repetida ou sem nome.");
                if (n.urdidura == null || n.urdidura.Length < 4 || n.urdidura.Length > 16)
                    throw new InvalidOperationException("Amostra " + n.id + ": a urdidura deve ter de 4 a 16 fios.");
                foreach (int c in n.urdidura)
                    if (c < 0 || c >= dados.cores.Length)
                        throw new InvalidOperationException("Amostra " + n.id + ": cor da urdidura fora da paleta.");
                if (n.pedais == null || n.pedais.Length < 2 || n.pedais.Length > 4)
                    throw new InvalidOperationException("Amostra " + n.id + ": use de 2 a 4 pedais.");
                foreach (string p in n.pedais)
                {
                    if (p == null || p.Length != n.urdidura.Length)
                        throw new InvalidOperationException("Amostra " + n.id + ": cada pedal precisa de um 0 ou 1 por fio da urdidura.");
                    foreach (char ch in p)
                        if (ch != '0' && ch != '1')
                            throw new InvalidOperationException("Amostra " + n.id + ": pedal com caractere diferente de 0 ou 1.");
                }

                if (n.solucao == null || n.solucao.Length == 0 || n.solucao.Length > 10)
                    throw new InvalidOperationException("Amostra " + n.id + ": a solução deve ter de 1 a 10 linhas.");
                for (int i = 0; i < n.solucao.Length; i++)
                {
                    int pedal, cor;
                    try
                    {
                        Solucao(n, i, out pedal, out cor);
                    }
                    catch (Exception)
                    {
                        throw new InvalidOperationException("Amostra " + n.id + ", linha " + (i + 1) + ": escreva \"pedal,cor\", por exemplo \"1,2\".");
                    }

                    if (pedal < 0 || pedal >= n.pedais.Length || cor < 0 || cor >= dados.cores.Length)
                        throw new InvalidOperationException("Amostra " + n.id + ", linha " + (i + 1) + ": pedal ou cor fora do intervalo.");
                }
            }
        }
    }

    public sealed class MTMSessaoTear
    {
        private readonly MTMTearDados dados;
        private readonly List<int> feitas = new List<int>();

        public int Nivel { get; private set; }
        public int Pedal { get; private set; }
        public int Cor { get; private set; }
        public int ErrosNaLinha { get; private set; }
        public int DicaNaLinha { get; private set; }
        public int[] UltimaErrada { get; private set; }
        public bool Terminou { get; private set; }

        public MTMSessaoTear(MTMTearDados dados)
        {
            if (dados == null)
                throw new ArgumentNullException("dados");
            this.dados = dados;
            Pedal = Cor = -1;
            AjustarPedal();
        }

        public bool PedalAutomatico
        {
            get { return NivelAtual.pedalAutomatico; }
        }

        // Nas amostras de pedal automático, o pedal acompanha a linha da vez.
        private void AjustarPedal()
        {
            if (!PedalAutomatico || Terminou || NivelPronto)
                return;
            int pedal, cor;
            MTMTear.Solucao(NivelAtual, feitas.Count, out pedal, out cor);
            Pedal = pedal;
        }

        private void SolucaoDaVez(out int pedal, out int cor)
        {
            MTMTear.Solucao(NivelAtual, Math.Min(LinhaAtual, NivelAtual.solucao.Length - 1), out pedal, out cor);
        }

        // Linha que sairia com a escolha atual; null enquanto falta pedal ou cor.
        public int[] Previa()
        {
            return Pedal < 0 || Cor < 0 || Terminou || NivelPronto ? null : MTMTear.Linha(NivelAtual, Pedal, Cor);
        }

        // Pontos da prévia diferentes da amostra; -1 enquanto falta escolha.
        public int Diferencas()
        {
            var previa = Previa();
            if (previa == null)
                return -1;
            var alvo = MTMTear.Alvo(NivelAtual, feitas.Count);
            int n = 0;
            for (int i = 0; i < previa.Length; i++)
                if (previa[i] != alvo[i])
                    n++;
            return n;
        }

        public bool Combina
        {
            get { return Diferencas() == 0; }
        }

        // O pedal escolhido levanta os mesmos fios que o da amostra?
        public bool PedalCombina()
        {
            if (Pedal < 0 || Terminou || NivelPronto)
                return false;
            int pedal, cor;
            SolucaoDaVez(out pedal, out cor);
            for (int c = 0; c < NivelAtual.urdidura.Length; c++)
                if (MTMTear.Levanta(NivelAtual, pedal, c) != MTMTear.Levanta(NivelAtual, Pedal, c))
                    return false;
            return true;
        }

        public MTMTearNivel NivelAtual
        {
            get { return dados.niveis[Math.Min(Nivel, dados.niveis.Length - 1)]; }
        }

        public int LinhasFeitas
        {
            get { return feitas.Count; }
        }

        public int LinhaAtual
        {
            get { return Terminou ? NivelAtual.solucao.Length : feitas.Count; }
        }

        // Linha já tecida: pedal e cor escolhidos.
        public void Feita(int linha, out int pedal, out int cor)
        {
            pedal = feitas[linha] / 10;
            cor = feitas[linha] % 10;
        }

        public void EscolherPedal(int pedal)
        {
            if (!Terminou && !PedalAutomatico && pedal >= 0 && pedal < NivelAtual.pedais.Length)
                Pedal = pedal;
        }

        public void EscolherCor(int cor)
        {
            if (!Terminou && cor >= 0 && cor < dados.cores.Length)
                Cor = cor;
        }

        public MTMResultadoTear Passar()
        {
            if (Terminou)
                return MTMResultadoTear.TudoCompleto;
            if (Pedal < 0 || Cor < 0)
                return MTMResultadoTear.SemEscolha;
            var nivel = NivelAtual;
            var linha = MTMTear.Linha(nivel, Pedal, Cor);
            if (!MTMTear.Iguais(linha, MTMTear.Alvo(nivel, feitas.Count)))
            {
                UltimaErrada = linha;
                ErrosNaLinha++;
                return MTMResultadoTear.Errada;
            }

            UltimaErrada = null;
            ErrosNaLinha = 0;
            DicaNaLinha = 0;
            feitas.Add(Pedal * 10 + Cor);
            // Pedal e cor continuam escolhidos: linhas repetidas saem com um toque só.
            if (feitas.Count < nivel.solucao.Length)
            {
                AjustarPedal();
                return MTMResultadoTear.Certa;
            }
            if (Nivel + 1 < dados.niveis.Length)
                return MTMResultadoTear.NivelCompleto;
            Terminou = true;
            return MTMResultadoTear.TudoCompleto;
        }

        // Depois de "NivelCompleto": a interface mostra a amostra pronta e então chama isto.
        public void ProximoNivel()
        {
            if (Terminou || feitas.Count < NivelAtual.solucao.Length || Nivel + 1 >= dados.niveis.Length)
                return;
            Nivel++;
            feitas.Clear();
            Pedal = Cor = -1;
            ErrosNaLinha = DicaNaLinha = 0;
            UltimaErrada = null;
            AjustarPedal();
        }

        public bool NivelPronto
        {
            get { return feitas.Count >= NivelAtual.solucao.Length; }
        }

        public bool Desfazer()
        {
            if (Terminou || feitas.Count == 0)
                return false;
            feitas.RemoveAt(feitas.Count - 1);
            ErrosNaLinha = DicaNaLinha = 0;
            UltimaErrada = null;
            AjustarPedal();
            return true;
        }

        public void Reiniciar()
        {
            Nivel = 0;
            feitas.Clear();
            Terminou = false;
            Pedal = Cor = -1;
            ErrosNaLinha = DicaNaLinha = 0;
            UltimaErrada = null;
            AjustarPedal();
        }

        public void LimparErro()
        {
            UltimaErrada = null;
        }

        // Dica: 1 aponta o pedal certo (ou a cor, quando o tear troca os pedais sozinho); 2 aponta os dois.
        public int PedirDica()
        {
            if (Terminou || NivelPronto)
                return DicaNaLinha;
            DicaNaLinha = Math.Min(PedalAutomatico ? 1 : 2, DicaNaLinha + 1);
            return DicaNaLinha;
        }

        public int CorDaDica()
        {
            int pedal, cor;
            SolucaoDaVez(out pedal, out cor);
            return cor;
        }

        public bool UrdiduraAparece(int coluna)
        {
            int pedal, cor;
            MTMTear.Solucao(NivelAtual, LinhaAtual, out pedal, out cor);
            return MTMTear.Levanta(NivelAtual, pedal, coluna);
        }

        public int PedalDaDica()
        {
            int pedal, cor;
            MTMTear.Solucao(NivelAtual, LinhaAtual, out pedal, out cor);
            return pedal;
        }

        // Salvamento: [MarcaSalvo + nível, linhas feitas...]. Formato antigo ou inválido recomeça.
        public List<int> Codificar()
        {
            var lista = new List<int> { MTMTear.MarcaSalvo + Nivel };
            lista.AddRange(feitas);
            return lista;
        }

        public void Restaurar(IList<int> salvo)
        {
            Reiniciar();
            if (salvo == null || salvo.Count == 0 || salvo[0] < MTMTear.MarcaSalvo)
                return;
            int nivel = salvo[0] - MTMTear.MarcaSalvo;
            if (nivel < 0 || nivel >= dados.niveis.Length)
                return;
            Nivel = nivel;
            Pedal = -1;
            var n = NivelAtual;
            for (int i = 1; i < salvo.Count && feitas.Count < n.solucao.Length; i++)
            {
                int pedal = salvo[i] / 10, cor = salvo[i] % 10;
                if (pedal < 0 || pedal >= n.pedais.Length || cor < 0 || cor >= dados.cores.Length ||
                    !MTMTear.Iguais(MTMTear.Linha(n, pedal, cor), MTMTear.Alvo(n, feitas.Count)))
                    break;
                feitas.Add(salvo[i]);
            }

            // Última amostra já completa: o tear volta terminado, pronto para concluir.
            if (Nivel + 1 >= dados.niveis.Length && feitas.Count >= n.solucao.Length)
                Terminou = true;
            AjustarPedal();
        }
    }
}

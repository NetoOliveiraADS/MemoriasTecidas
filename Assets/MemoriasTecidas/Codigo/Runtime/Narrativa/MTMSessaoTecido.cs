using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MemoriasTecidas.MundoV2
{
    // Regras da interação independentes de Unity, relógio, entrada e desenho.
    public sealed class MTMSessaoTecido
    {
        public const int QuantidadePassagens = 4;
        private readonly List<int> linhas = new List<int>();
        private readonly ReadOnlyCollection<int> leitura;
        public IList<int> Passagens { get { return leitura; } }
        public int CorSelecionada { get; private set; } = -1;
        public int LinhaEmEdicao { get; private set; } = -1;
        public int LinhaAtiva { get { return LinhaEmEdicao >= 0 ? LinhaEmEdicao : linhas.Count; } }
        public bool Passando { get; private set; }
        public float Progresso { get; private set; }
        public bool PodePassar { get { return !Passando && CorSelecionada >= 0 && LinhaAtiva < QuantidadePassagens; } }

        public MTMSessaoTecido() { leitura = linhas.AsReadOnly(); }

        public void Restaurar(IList<int> salvas)
        {
            Reiniciar();
            if (salvas == null || salvas.Count > QuantidadePassagens) return;
            foreach (int cor in salvas)
                if (cor < 0 || cor > 2) return;
            linhas.AddRange(salvas);
        }

        public void SelecionarCor(int cor)
        {
            if (!Passando && cor >= 0 && cor <= 2) CorSelecionada = cor;
        }

        public bool EditarLinha(int indice)
        {
            if (Passando || indice < 0 || indice >= linhas.Count) return false;
            LinhaEmEdicao = indice;
            CorSelecionada = linhas[indice];
            Progresso = 0f;
            return true;
        }

        public bool IniciarPassagem()
        {
            if (!PodePassar) return false;
            Passando = true;
            Progresso = 0f;
            return true;
        }

        public bool Percorrer(float progresso)
        {
            if (!Passando || float.IsNaN(progresso) || float.IsInfinity(progresso)) return false;
            Progresso = Math.Max(0f, Math.Min(1f, progresso));
            if (Progresso < 1f) return false;
            if (LinhaEmEdicao >= 0) linhas[LinhaEmEdicao] = CorSelecionada;
            else linhas.Add(CorSelecionada);
            Cancelar();
            LinhaEmEdicao = -1;
            CorSelecionada = -1;
            return true;
        }

        public void Cancelar() { Passando = false; Progresso = 0f; }

        public void Desfazer()
        {
            Cancelar();
            if (linhas.Count > 0) linhas.RemoveAt(linhas.Count - 1);
            LinhaEmEdicao = -1;
            CorSelecionada = -1;
        }

        public void Reiniciar()
        {
            Cancelar();
            linhas.Clear();
            LinhaEmEdicao = -1;
            CorSelecionada = -1;
        }

        public static float ProgressoDoGesto(float x, float esquerda, float largura, int linha)
        {
            if (largura <= 0f || float.IsNaN(largura) || float.IsInfinity(largura) ||
                float.IsNaN(esquerda) || float.IsInfinity(esquerda) ||
                float.IsNaN(x) || float.IsInfinity(x)) return 0f;
            float valor = (x - esquerda) / largura;
            return Math.Max(0f, Math.Min(1f, linha % 2 == 0 ? valor : 1f - valor));
        }
    }
}

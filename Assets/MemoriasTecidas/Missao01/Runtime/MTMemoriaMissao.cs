using UnityEngine;
using MemoriasTecidas.PracaV1;

namespace MemoriasTecidas.Missao01
{
    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    public sealed class MTMemoriaMissao : MonoBehaviour
    {
        public MTPracaJogador jogador;
        public MTMemoriaPainel painel;
        public MTMemoriaNPC neto, andre;
        public MTMemoriaNPC[] personagens;
        public MTMemoriaDialogo netoInicio, netoLembrete, netoFim, andreAntes, andreLenda;
        private MTMemoriaProgresso progresso;
        private MTMemoriaEncontro encontro;
        private int ultimoQuadro = -1;
        public MTMemoriaEtapa Etapa => progresso == null ? MTMemoriaEtapa.FalarComNeto : progresso.Etapa;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (jogador == null || painel == null || neto == null || andre == null
                || netoInicio == null || netoLembrete == null || netoFim == null
                || andreAntes == null || andreLenda == null)
            {
                Debug.LogError("Missão 01: referências incompletas. Execute o menu Validar cena aberta.", this);
                enabled = false;
                return;
            }
            // Não deixa o tutorial antigo disputar a mesma tecla E.
            if (jogador.demonstracao != null) jogador.demonstracao.enabled = false;
            jogador.demonstracao = null;
            progresso = new MTMemoriaProgresso();
            encontro = MTMemoriaEncontro.SemAvanco;
            ultimoQuadro = Time.frameCount;
            jogador.DefinirBloqueioExterno(false);
            painel.ReiniciarVisual();
            AtualizarObjetivo();
        }

        private void Update()
        {
            if (painel.Aberto)
            {
                jogador.DefinirBloqueioExterno(true);
                if (Time.frameCount == ultimoQuadro) return;
                if (MTPracaEntrada.Fechar())
                {
                    painel.Fechar();
                    encontro = MTMemoriaEncontro.SemAvanco;
                    ultimoQuadro = Time.frameCount;
                }
                else if (MTPracaEntrada.Conversar())
                {
                    ultimoQuadro = Time.frameCount;
                    if (painel.Avancar())
                    {
                        bool mudou = progresso.Concluir(encontro);
                        encontro = MTMemoriaEncontro.SemAvanco;
                        AtualizarObjetivo();
                        if (mudou) painel.Notificar(Etapa == MTMemoriaEtapa.Concluida
                            ? "Missão concluída • Primeiros fios de memória"
                            : "Novo objetivo • Encontre André Lúcio");
                    }
                }
                return;
            }

            bool mesmoQuadro = Time.frameCount == ultimoQuadro;
            jogador.DefinirBloqueioExterno(mesmoQuadro);
            if (mesmoQuadro) return;
            MTMemoriaNPC perto = MaisProximo();
            painel.MostrarDica(perto == null ? null : "E / Enter — Conversar com " + perto.nome);
            if (perto != null && MTPracaEntrada.Conversar())
            {
                MTMemoriaDialogo dialogo = Escolher(perto);
                if (painel.Abrir(dialogo))
                {
                    ultimoQuadro = Time.frameCount;
                    jogador.DefinirBloqueioExterno(true);
                }
                else Debug.LogWarning("Conversa sem falas: " + perto.nome, perto);
            }
        }

        private MTMemoriaNPC MaisProximo()
        {
            MTMemoriaNPC melhor = null;
            float menor = float.PositiveInfinity;
            if (personagens == null) return null;
            foreach (MTMemoriaNPC npc in personagens)
            {
                if (npc == null || !npc.isActiveAndEnabled) continue;
                float d = ((Vector2)(npc.transform.position - jogador.transform.position)).sqrMagnitude;
                if (d <= npc.alcance * npc.alcance && d < menor) { menor = d; melhor = npc; }
            }
            return melhor;
        }

        private MTMemoriaDialogo Escolher(MTMemoriaNPC npc)
        {
            encontro = MTMemoriaEncontro.SemAvanco;
            if (npc == neto)
            {
                if (Etapa == MTMemoriaEtapa.FalarComNeto)
                {
                    encontro = MTMemoriaEncontro.ApresentacaoNeto;
                    return netoInicio;
                }
                return Etapa == MTMemoriaEtapa.OuvirAndre ? netoLembrete : netoFim;
            }
            if (npc == andre)
            {
                if (Etapa == MTMemoriaEtapa.FalarComNeto) return andreAntes;
                if (Etapa == MTMemoriaEtapa.OuvirAndre) encontro = MTMemoriaEncontro.RelatoAndre;
                return andreLenda;
            }
            return npc.dialogoLivre;
        }

        private void AtualizarObjetivo()
        {
            switch (Etapa)
            {
                case MTMemoriaEtapa.FalarComNeto:
                    painel.MostrarObjetivo("Procure Neto e converse com ele.", "1 / 2 • Comece pela praça", neto);
                    break;
                case MTMemoriaEtapa.OuvirAndre:
                    painel.MostrarObjetivo("Encontre André Lúcio e ouça a lenda.", "2 / 2 • A história da Mulher-Cavalo", andre);
                    break;
                default:
                    painel.MostrarObjetivo("Você ouviu a história da Mulher-Cavalo.", "Concluída • Primeiros fios de memória", null);
                    break;
            }
        }

        private void OnDisable()
        {
            if (jogador != null) jogador.DefinirBloqueioExterno(false);
            if (painel != null && painel.caixaConversa != null) painel.Fechar();
        }
    }
}

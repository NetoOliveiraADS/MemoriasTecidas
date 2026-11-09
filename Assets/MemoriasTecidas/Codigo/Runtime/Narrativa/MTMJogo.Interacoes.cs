using System;
using System.IO;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private bool Disponivel(MTMPonto p)
        {
            if (p == null || p.dados == null || !p.gameObject.activeInHierarchy)
                return false;
            if (p.GetComponent<MTMBau>() != null || p.GetComponent<MTMPontoPesca>() != null)
                return true;
            if (p.dados.id == IdAmostraPrimeiroFio)
                return aberturaMemorias != null && MTMVinculosRegiao.RegiaoAncestral(p.transform) == LocalPrimeiroFio();
            if (tramaTecida != null)
                return DisponivelRoteiro(p);
            if (p.dados.noturno && (!Noite || !estado.Tem("andre")))
                return false;
            return p.dados.tipo != "coleta" || !estado.Tem(p.dados.evento);
        }

        private MTMPonto MaisProximo()
        {
            MTMPonto melhor = null;
            float distancia = float.PositiveInfinity;
            if (atual == null || atual.pontos == null)
                return null;
            foreach (MTMPonto p in atual.pontos)
            {
                if (!Disponivel(p))
                    continue;
                float d = Vector2.Distance(corpo.position, p.transform.position);
                if (d <= p.alcance && d < distancia)
                {
                    distancia = d;
                    melhor = p;
                }
            }

            return melhor;
        }

        private void Interagir(MTMPonto ponto)
        {
            MTMPontoDados d = ponto.dados;
            retratoAtual = ponto.retrato;
            recorteAtual = ponto.recorteRosto;
            Parar();
            quadroDaAcao = Time.frameCount;
            if (InteragirTesouro(ponto) || InteragirPontoPesca(ponto))
                return;
            if (d.tipo == "saida")
            {
                IrPara(d.destino, new Vector2(d.chegadaX, d.chegadaY));
                return;
            }

            if (InteragirPrimeiroFio(ponto))
                return;
            if (InteragirRoteiro(ponto))
                return;
            if (!string.IsNullOrEmpty(d.condicao) && !estado.Tem(d.condicao))
            {
                Abrir(d.nome, d.fonte, new[] { "Vamos continuar essa conversa depois desta etapa: " + ObjetivoTecido() }, null);
                return;
            }

            if (d.tipo == "descanso")
            {
                bool eraNoite = Noite;
                Abrir("Uma pausa na praça", "", new[] { eraNoite ? "Descansar até as 8h da manhã?" : "Descansar até as 20h para observar as histórias da noite?", "Confirme para descansar. Esc permite continuar explorando agora." }, () =>
                {
                    if (eraNoite && estado.relogio >= .75f)
                        estado.dia++;
                    estado.relogio = eraNoite ? 8f / 24f : 20f / 24f;
                    Salvar();
                });
                return;
            }

            if (d.evento == "neto" && estado.rede)
            {
                Abrir(d.nome, MTMTextoDialogo.Assinatura, tramaTecida != null && tramaTecida.encerramento != null ? tramaTecida.encerramento : new[] { "A rede reuniu materiais, encontros e trabalho." }, () => Registrar("entrega_final"));
                return;
            }

            if (d.evento == "mercado_entrega" && !estado.Tem("mercado_entrega"))
            {
                if (!estado.Tem("pacote1") || !estado.Tem("pacote2") || !estado.Tem("pacote3"))
                {
                    Abrir(d.nome, d.fonte, new[] { "Preciso conferir três encomendas. Visite cada uma das três bancas aqui em frente ao mercado e depois volte.", "Você recebe os punhos para a rede quando as três encomendas estiverem conferidas." }, null);
                    return;
                }
            }

            if (d.evento == "fiar" && !estado.Tem("fiar") && estado.algodao < 6)
            {
                Abrir(d.nome, d.fonte, new[] { "Vamos preparar três conjuntos de fios. Para esta atividade do jogo, traga seis algodões do sítio.", "Você trouxe " + estado.algodao + " de 6. O caminho do sítio sai do pátio da fábrica." }, null);
                return;
            }

            if (d.evento == "tecer" && !estado.rede && !estado.PodeTecelagem)
            {
                Abrir("Tear da oficina", "Receita simplificada para o jogo.", new[] { "Para começar: 3 fios, 1 corda, 1 pano, 1 conjunto de punhos e 1 enfeite.", "Fios: Rosa. Corda e pano: Cruzeiro. Punhos: mercado. Enfeite: coreto. Pressione I para conferir sua bolsa." }, null);
                return;
            }

            if (d.tipo == "coleta" && estado.Tem(d.evento))
                return;
            if ((d.evento == "fiar" || d.evento == "tecer" || d.evento == "mercado_entrega") && estado.Tem(d.evento))
            {
                Abrir(d.nome, d.fonte, new[] { "Essa etapa já foi concluída. " + ObjetivoTecido() }, null);
                return;
            }

            Abrir(d.nome, d.fonte, d.falas, () => Registrar(d.evento));
        }

        private void Registrar(string evento)
        {
            if (estado.Concluir(evento))
            {
                Salvar();
                Avisar("Registro atualizado. " + ObjetivoTecido());
            }
        }

        private void Abrir(string nome, string fonte, string[] linhas, Action concluir)
        {
            if (linhas == null || linhas.Length == 0)
                return;
            locutor = nome;
            contexto = MTMTextoDialogo.Assinatura;
            falas = linhas;
            pagina = 0;
            aoConcluir = concluir;
            linhaPreparadaDialogo = null;
            locutorPreparadoDialogo = null;
            quadroDaAcao = Time.frameCount;
            Parar();
        }

        private void Avancar()
        {
            if (falas == null || quadroDaAcao == Time.frameCount)
                return;
            quadroDaAcao = Time.frameCount;
            if (pagina + 1 < falas.Length)
            {
                pagina++;
                return;
            }

            Action acao = aoConcluir;
            falas = null;
            aoConcluir = null;
            acao?.Invoke();
        }
    }
}

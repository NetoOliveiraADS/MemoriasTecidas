using System;
using System.Collections.Generic;

namespace MemoriasTecidas.MundoV2
{
    // Regras de progressão sem dependência da cena ou do desenho da interface.
    public static class MTMMotorMissoes
    {
        private static readonly string[] Materiais =
        {
            "fio",
            "corda",
            "pano",
            "punho",
            "enfeite"
        };
        private static readonly int[] Necessarios =
        {
            3,
            1,
            1,
            1,
            1
        };
        public static MTMMissaoTecido Missao(MTMTrama trama, string id)
        {
            return trama == null || trama.missoes == null ? null : Array.Find(trama.missoes, m => m != null && m.id == id);
        }

        public static bool Disponivel(MTMEstado estado, MTMTrama trama, MTMMissaoTecido missao)
        {
            if (estado == null || missao == null)
                return false;
            if (missao.requerMissoes != null)
                foreach (string id in missao.requerMissoes)
                    if (!MTMTrama.Concluida(estado, Missao(trama, id)))
                        return false;
            var proxima = Proxima(estado, missao);
            if (proxima != null && proxima.exigeReceita && !estado.rede && !estado.PodeTecelagem)
                return false;
            return true;
        }

        public static MTMEtapaTecido Proxima(MTMEstado estado, MTMMissaoTecido missao)
        {
            if (missao != null && missao.etapas != null)
                foreach (var etapa in missao.etapas)
                    if (!MTMTrama.Concluida(estado, etapa))
                        return etapa;
            return null;
        }

        public static bool HorarioCorreto(MTMEtapaTecido etapa, float horario)
        {
            bool noite = horario < .25f || horario >= .75f;
            if (etapa == null)
                return false;
            if (etapa.horario == "noite")
                return noite;
            if (etapa.horario == "dia")
                return !noite;
            if (etapa.horario == "entardecer")
                return horario >= 17f / 24f || horario < .25f;
            return true;
        }

        public static bool TemPista(MTMEstado estado, MTMTrama trama, string id)
        {
            var p = trama == null || trama.pistas == null ? null : Array.Find(trama.pistas, x => x != null && x.id == id);
            return p != null && estado.Tem(p.evento);
        }

        public static bool RespostaCorreta(MTMEstado estado, MTMTrama trama, MTMPuzzleTecido jogo, IList<int> resposta)
        {
            if (jogo == null || jogo.resposta == null || resposta == null || jogo.resposta.Length != resposta.Count)
                return false;
            if (jogo.requerPistas != null)
                foreach (string id in jogo.requerPistas)
                    if (!TemPista(estado, trama, id))
                        return false;
            bool conjunto = jogo.tipo == "conjunto" || jogo.tipo == "grade";
            var vistos = new HashSet<int>();
            for (int i = 0; i < resposta.Count; i++)
            {
                int n = resposta[i];
                if (n < 0 || jogo.opcoes == null || n >= jogo.opcoes.Length)
                    return false;
                if (conjunto)
                {
                    if (!vistos.Add(n) || Array.IndexOf(jogo.resposta, n) < 0)
                        return false;
                }
                else if (n != jogo.resposta[i])
                    return false;
            }

            return true;
        }

        public static bool PodeConcluir(MTMEstado estado, MTMTrama trama, MTMMissaoTecido missao, MTMEtapaTecido etapa, out string motivo)
        {
            motivo = "";
            if (!Disponivel(estado, trama, missao) || etapa == null || Proxima(estado, missao) != etapa)
            {
                motivo = "Conclua a etapa anterior do caderno.";
                return false;
            }

            if (!HorarioCorreto(etapa, estado.relogio))
            {
                motivo = etapa.horario == "dia" ? "Volte durante o dia. Você pode descansar no banco da praça." : "Volte no entardecer ou à noite. Você pode descansar no banco da praça.";
                return false;
            }

            if (etapa.exigeReceita && !estado.rede && !estado.PodeTecelagem)
            {
                motivo = "Confira a receita: faltam materiais na bolsa.";
                return false;
            }

            if (etapa.exigir != null)
                foreach (var item in etapa.exigir)
                    if (item == null || item.quantidade < 0 || MTMTrama.Quantidade(estado, item.id) < item.quantidade)
                    {
                        motivo = "Você precisa do objeto indicado para executar esta ação.";
                        return false;
                    }

            if (etapa.consumir != null)
                foreach (var item in etapa.consumir)
                    if (item == null || item.quantidade < 0 || MTMTrama.Quantidade(estado, item.id) < item.quantidade)
                    {
                        motivo = "Ainda falta um objeto que precisa ser entregue nesta etapa.";
                        return false;
                    }

            return true;
        }

        public static bool Concluir(MTMEstado estado, MTMTrama trama, MTMMissaoTecido missao, MTMEtapaTecido etapa, IList<int> resposta, bool percursoConcluido, out string motivo)
        {
            if (!PodeConcluir(estado, trama, missao, etapa, out motivo))
                return false;
            if (etapa.acao == "atividade")
            {
                var jogo = trama.atividades == null ? null : Array.Find(trama.atividades, a => a.id == etapa.atividade);
                if (!RespostaCorreta(estado, trama, jogo, resposta))
                {
                    motivo = "A sequência ou comparação ainda não está correta. Reveja as referências e tente novamente.";
                    return false;
                }
            }

            if (etapa.acao == "escolta" && !percursoConcluido)
            {
                motivo = "Acompanhe a comunidade até o fim deste trecho.";
                return false;
            }

            // Todas as condições são conferidas antes de consumir materiais ou conceder recompensas.
            if (etapa.fabricarRede && !estado.rede)
            {
                if (!estado.PodeTecelagem)
                {
                    motivo = "Ainda faltam materiais para finalizar a rede.";
                    return false;
                }

                for (int i = 0; i < Materiais.Length; i++)
                    estado.DefinirQuantidade(Materiais[i], MTMTrama.Quantidade(estado, Materiais[i]) - Necessarios[i]);
                estado.rede = true;
                if (!estado.Tem("tecer"))
                    estado.marcos.Add("tecer");
            }

            if (etapa.consumir != null)
                foreach (var item in etapa.consumir)
                    estado.DefinirQuantidade(item.id, MTMTrama.Quantidade(estado, item.id) - item.quantidade);
            if (etapa.completarMateriais && !estado.rede)
                for (int i = 0; i < Materiais.Length; i++)
                    estado.DefinirQuantidade(Materiais[i], Math.Max(MTMTrama.Quantidade(estado, Materiais[i]), Necessarios[i]));
            if (etapa.conceder != null)
                foreach (var item in etapa.conceder)
                {
                    // Saves que já possuem a rede não recebem de novo os seus materiais consumidos.
                    if (estado.rede && Array.IndexOf(Materiais, item.id) >= 0)
                        continue;
                    int atual = MTMTrama.Quantidade(estado, item.id);
                    estado.DefinirQuantidade(item.id, item.minimo ? Math.Max(atual, item.quantidade) : atual + item.quantidade);
                }

            foreach (string id in etapa.eventos)
                if (!estado.Tem(id))
                    estado.marcos.Add(id);
            string legado = etapa.ponto == "npc_feliciana" ? "feliciana" : etapa.ponto == "npc_simao" ? "simao" : etapa.ponto == "visita_cruzeiro" ? "cruzeiro" : etapa.ponto == "npc_guia" ? "guia_museu" : "";
            if (legado.Length > 0 && !estado.Tem(legado))
                estado.marcos.Add(legado);
            return true;
        }
    }
}

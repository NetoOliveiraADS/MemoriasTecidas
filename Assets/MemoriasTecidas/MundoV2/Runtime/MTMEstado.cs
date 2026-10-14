using System;
using System.Collections.Generic;

namespace MemoriasTecidas.MundoV2
{
    // Estado serializável. As recompensas só entram ao confirmar a última fala.
    [Serializable] public sealed class MTMEstado
    {
        public int versao = 1, dia = 1;
        public string area = "praca";
        public float relogio = 8f / 24f;
        public int algodao, fio, corda, pano, punho, enfeite, cera;
        public bool rede;
        public List<string> marcos = new List<string>();

        public bool Tem(string id) { return !string.IsNullOrEmpty(id) && marcos != null && marcos.Contains(id); }
        public bool PodeTecelagem { get { return !rede && fio >= 3 && corda >= 1 && pano >= 1 && punho >= 1 && enfeite >= 1; } }

        public bool Concluir(string id)
        {
            if (string.IsNullOrEmpty(id) || Tem(id)) return false;
            if (marcos == null) marcos = new List<string>();
            if (id == "fiar")
            {
                if (algodao < 6) return false;
                algodao -= 6; fio += 3;
            }
            else if (id == "tecer")
            {
                if (!PodeTecelagem) return false;
                fio -= 3; corda--; pano--; punho--; enfeite--; rede = true;
            }
            else if (id == "mercado_entrega")
            {
                if (!Tem("pacote1") || !Tem("pacote2") || !Tem("pacote3")) return false;
                punho++;
            }
            else if (id == "cruzeiro") { corda++; pano++; }
            else if (id == "enfeite") enfeite++;
            else if (id == "museu") cera++;
            else if (id.StartsWith("algodao_", StringComparison.Ordinal)) algodao++;
            else if (id == "entrega_final" && !rede) return false;
            marcos.Add(id);
            return true;
        }

        public string Objetivo()
        {
            if (!Tem("neto")) return "Converse com Neto na Praça Matriz.";
            if (!Tem("feliciana")) return "Encontre a memória de Dona Feliciana na praça.";
            if (!Tem("simao")) return "Converse com a memória de Simão de Góis.";
            if (!Tem("cruzeiro")) return "Siga pelo Sítio do Algodão até o Cruzeiro da Serra Dantas.";
            if (!Tem("andre")) return "Volte à praça e ouça André Lúcio.";
            if (!Tem("aparicao")) return "Descanse no banco da praça; visite o rio à noite e observe a lenda.";
            if (!Tem("museu")) return "Visite o Memorial Carnaúba e examine a exposição.";
            if (!Tem("mercado_entrega")) return "Confira as três encomendas do mercado e fale com Seu Bento.";
            if (!Tem("enfeite")) return "Recolha o enfeite ao lado do coreto na praça.";
            if (!Tem("fiar")) return algodao < 6 ? "Colha 6 algodões no sítio; depois procure Rosa na oficina." : "Leve os 6 algodões a Rosa, dentro da oficina.";
            if (!rede) return "Use o tear da oficina para montar sua rede.";
            if (!Tem("entrega_final")) return "Mostre a rede a Neto na Praça Matriz.";
            return "Rede concluída! Explore os encontros na Casa das Memórias e na cidade.";
        }
    }
}

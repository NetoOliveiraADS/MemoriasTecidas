using System;
using System.IO;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private bool IrParaPontoSeguro(string id)
        {
            MTMRegiao alvo = Array.Find(regioes, r => r != null && r.dados.id == id);
            return alvo != null && IrPara(id, new Vector2(alvo.dados.seguroX, alvo.dados.seguroY));
        }

        private bool IrPara(string id, Vector2 chegada)
        {
            if (!RegiaoLiberadaRoteiro(id))
            {
                var b = Array.Find(tramaTecida.bloqueios, x => x.regiao == id);
                Avisar(b == null ? "Continue as missões para abrir este lugar." : b.mensagem);
                return false;
            }

            MTMRegiao alvo = Array.Find(regioes, r => r != null && r.dados.id == id);
            if (alvo == null)
            {
                Avisar("Essa ligação não tem região de destino. Confira mundo.json.");
                return false;
            }

            foreach (MTMRegiao r in regioes)
                if (r != null)
                    r.gameObject.SetActive(r == alvo);
            atual = alvo;
            estado.area = id;
            perto = null;
            painel = "";
            bool chegouDestinoGuiado = destinoGuiadoTecido == id;
            if (chegouDestinoGuiado)
                destinoGuiadoTecido = "";
            MTMContatoCenario.Preparar(atual);
            PrepararAmostraPrimeiroFio();
            Reposicionar(chegada);
            bool primeiraVisita = !estado.Tem("visitou_" + id);
            estado.Concluir("visitou_" + id);
            MostrarBannerRegiao(id, primeiraVisita);
            Salvar();
            if (chegouDestinoGuiado)
                Avisar("Você chegou a " + NomeMapaGuia(id) + ".");
            return true;
        }

        private void Reposicionar(Vector2 p)
        {
            Parar();
            corpo.position = p;
            transform.position = new Vector3(p.x, p.y, 0f);
            Physics2D.SyncTransforms();
            CentralizarCamera();
            // Quem chega (ou volta ao centro) em cima de um anel não é levado embora na mesma hora.
            saidaPisada = AnelDeSaidaEm(p);
        }

        // ---------- passagens sem apertar E ----------

        // A passagem em cujo anel o Neto já estava: só volta a funcionar depois que ele sai do anel.
        private MTMPonto saidaPisada;

        // Entrar no anel do chão de uma passagem já leva ao outro lugar. Caminho fechado avisa uma vez por entrada.
        private bool SaidaAutomatica()
        {
            if (corpo == null)
                return false;
            MTMPonto dentro = AnelDeSaidaEm(corpo.position);
            if (dentro == null)
            {
                saidaPisada = null;
                return false;
            }

            if (dentro == saidaPisada || quadroDaAcao == Time.frameCount)
                return false;
            saidaPisada = dentro;
            Interagir(dentro);
            return true;
        }

        private MTMPonto AnelDeSaidaEm(Vector2 posicao)
        {
            if (atual == null || atual.pontos == null)
                return null;
            float rx = MTMMarcadorSaidaV221.DiametroAnel * .5f, ry = rx * .5f;
            foreach (MTMPonto p in atual.pontos)
            {
                if (p == null || p.dados == null || p.dados.tipo != "saida" || !p.gameObject.activeInHierarchy || !Disponivel(p))
                    continue;
                Vector2 d = posicao - (Vector2)p.transform.position;
                if ((d.x / rx) * (d.x / rx) + (d.y / ry) * (d.y / ry) <= 1f)
                    return p;
            }

            return null;
        }
    }
}

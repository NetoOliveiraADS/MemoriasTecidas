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
            estado.Concluir("visitou_" + id);
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
        }
    }
}

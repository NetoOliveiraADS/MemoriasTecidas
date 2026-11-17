using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        // O tear novo não usa arrasto (MTMJogo.TearUI.cs); mantido para a chamada do OnGUI.
        private void SoltarArrastoPrimeiroFioSeInativo()
        {
        }

        private void RedeMemoriasUI()
        {
            if (aberturaMemorias == null)
            {
                TextoTecido(new Rect(80, 300, 1120, 120), "A rede de memórias não foi carregada. Confira abertura_memorias.json. As missões e o caderno continuam disponíveis.", 22, textoPapelUI);
                return;
            }

            var motivos = aberturaMemorias.motivos;
            if (!Array.Exists(motivos, m => m.id == motivoSelecionadoRede))
                motivoSelecionadoRede = motivos[0].id;
            PainelTecido(new Rect(58, 252, 736, 378), tinta);
            TextoTecido(new Rect(84, 270, 682, 24), "SUA REDE  ·  " + MTMPrimeiroFio.Quantidade(estado, aberturaMemorias) + " / " + motivos.Length + " MOTIVOS REVELADOS", 13, acentoTextoUI, true);
            // Linhas de urdidura fixas; as cores dos motivos registram a progressão.
            for (int i = 0; i < 30; i++)
                Fundo(new Rect(88 + i * 23, 302, 1, 272), AlfaTecido(algodaoUI, .13f));
            Fundo(new Rect(80, 300, 692, 4), terracotaUI);
            Fundo(new Rect(80, 574, 692, 4), terracotaUI);
            for (int i = 0; i < motivos.Length; i++)
            {
                var motivo = motivos[i];
                bool revelado = MTMPrimeiroFio.Revelado(estado, motivo), selecionado = motivo.id == motivoSelecionadoRede;
                Rect r = new Rect(88 + i % 3 * 230, 312 + i / 3 * 88, 214, 76);
                Color cor = CorTecido(motivo.cor, ouro);
                Fundo(r, revelado ? Color.Lerp(tinta, cor, .16f) : profundoUI);
                if (revelado)
                {
                    PanoTecido(r, AlfaTecido(cor, .48f));
                    for (int fio = 0; fio < 5; fio++)
                        Fundo(new Rect(r.x + 7, r.yMax - 14 + fio * 2, r.width - 14, 1), AlfaTecido(cor, .6f));
                    IconeTecido(motivo.icone, new Rect(r.x + 12, r.y + 15, 38, 38), cor);
                }
                else
                {
                    NoTecido(new Vector2(r.x + 30, r.y + 34), AlfaTecido(algodaoUI, .35f), 6);
                    NoTecido(new Vector2(r.x + 30, r.y + 34), profundoUI, 3);
                }

                TextoTecido(new Rect(r.x + 60, r.y + 12, 142, 44), revelado ? motivo.titulo : "Ainda por descobrir", 14, revelado ? textoSuperficieUI : AlfaTecido(textoSuperficieUI, .60f), revelado);
                BordaTecido(r, selecionado ? ouro : AlfaTecido(algodaoUI, .16f), selecionado ? 2 : 1);
                if (AcaoTecido(r, "motivo:" + motivo.id))
                {
                    motivoSelecionadoRede = motivo.id;
                    rolagemDetalhe = Vector2.zero;
                }
            }

            bool terminou = estado.Tem(MTMPrimeiroFio.Memoria);
            if (BotaoTecido(new Rect(84, 590, 332, 28), terminou ? "Revisitar Feliciana no mapa" : "Acompanhar o primeiro fio"))
            {
                if (terminou)
                {
                    var local = LocalPrimeiroFio();
                    if (local != null)
                        AbrirMapaTecido(local.dados.id);
                }
                else
                    AcompanharPrimeiroFio();
            }

            if (BotaoTecido(new Rect(432, 590, 336, 28), "Continuar pela campanha", true))
            {
                AdiarPrimeiroFio();
                painel = "";
                Avisar(ObjetivoTecido());
            }

            var escolhido = Array.Find(motivos, m => m.id == motivoSelecionadoRede);
            bool aberto = MTMPrimeiroFio.Revelado(estado, escolhido);
            Fundo(new Rect(818, 252, 400, 378), papelUI);
            TextoTecido(new Rect(844, 272, 348, 20), aberto ? "UMA HISTÓRIA NA TRAMA" : "UM ESPAÇO PARA NOVOS ENCONTROS", 11, terracotaUI, true);
            TextoTecido(new Rect(844, 310, 348, 72), aberto ? escolhido.titulo : "A rede cresce com a jornada", 25, textoPapelUI, true);
            string resumo = aberto ? escolhido.resumo : "Converse, observe e participe das atividades da cidade. Os encontros importantes vão preencher estes espaços, sem precisar trocar materiais por pontos.";
            float altura = AlturaTecido(resumo, 320, 18);
            rolagemDetalhe = IniciarRolagemUI(new Rect(844, 394, 348, 132), rolagemDetalhe, new Rect(0, 0, 320, Mathf.Max(132, altura)));
            TextoTecido(new Rect(0, 0, 320, altura), resumo, 18, textoPapelUI);
            TerminarRolagemUI();
            if (BotaoTecido(new Rect(844, 574, 348, 36), aberto ? "Ler o registro desse encontro" : "Ver minhas missões", true))
            {
                if (aberto)
                {
                    abaCaderno = "memorias";
                    registroSelecionadoRoteiro = escolhido.registro;
                }
                else
                    abaCaderno = "missoes";
                rolagemLista = rolagemDetalhe = Vector2.zero;
            }
        }

        private void RedeMemoriasHUD()
        {
            if (aberturaMemorias == null)
                return;
            Rect r = new Rect(1090, 90, 166, 32);
            PainelTecido(r, FundoHUDClaroUI, false);
            TextoTecido(new Rect(1098, 94, 150, 24), "Rede · " + MTMPrimeiroFio.Quantidade(estado, aberturaMemorias) + " / " + aberturaMemorias.motivos.Length, 13, textoSuperficieUI, true, TextAnchor.MiddleCenter);
            if (AcaoTecido(r, "hud:rede"))
                AbrirRedeMemorias();
        }
    }
}

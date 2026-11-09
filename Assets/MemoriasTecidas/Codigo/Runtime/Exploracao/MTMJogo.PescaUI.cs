using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private void PescaUI()
        {
            Fundo(new Rect(0, 0, 1280, 720), new Color(.07f, .15f, .18f, .78f));
            PainelTecido(new Rect(210, 82, 860, 556), algodaoUI);
            Fundo(new Rect(210, 82, 860, 118), tinta);
            TextoTecido(new Rect(252, 104, 760, 25),
                "MEMÓRIAS TECIDAS / ATIVIDADES DO RIO", 13, acentoTextoUI, true);
            TextoTecido(new Rect(252, 137, 760, 43),
                pescaResultado == 1 ? "Pesca concluída!" :
                pescaResultado == -1 ? "O peixe escapou" : "Pescaria no Jaguaribe",
                28, textoSuperficieUI, true);

            if (pescaResultado != 0)
            {
                TextoTecido(new Rect(275, 243, 725, 68),
                    pescaResultado == 1
                        ? "Você capturou: " + PescaNomes[pescaTipo] + "!"
                        : "A tentativa terminou. Você pode lançar a linha novamente.",
                    25, textoPapelUI, true, TextAnchor.MiddleCenter);
                if (pescaResultado == 1)
                {
                    IconeTecido("peixe", new Rect(578, 333, 124, 124), terracotaUI);
                    TextoTecido(new Rect(276, 474, 725, 30),
                        "O peixe foi adicionado à sua bolsa e salvo na jornada.",
                        17, textoPapelUI, false, TextAnchor.MiddleCenter);
                }
                if (BotaoTecido(new Rect(294, 554, 305, 54), "Pescar novamente", true))
                    ReiniciarPesca();
                if (BotaoTecido(new Rect(625, 554, 346, 54), "Voltar para o rio"))
                    FecharPesca();
                return;
            }

            TextoTecido(new Rect(252, 217, 760, 54),
                "Aguarde o marcador atravessar a faixa verde e pressione ESPAÇO ou E.\nTrês acertos capturam o peixe; três erros encerram a tentativa.",
                18, textoPapelUI);
            float margem = PescaMargens[pescaTipo];
            Rect trilho = new Rect(290, 314, 700, 54);
            Fundo(trilho, papelUI);
            BordaTecido(trilho, AlfaTecido(tinta, .5f), 2f);
            float ini = trilho.x + (.5f - margem) * trilho.width;
            Fundo(new Rect(ini, trilho.y + 4, margem * 2f * trilho.width, 46), folhaUI);

            float marcadorX = trilho.x + PosicaoPeixePesca() * trilho.width;
            Fundo(new Rect(marcadorX - 5f, trilho.y - 12, 10f, trilho.height + 24), ouro);
            Fundo(new Rect(marcadorX - 9f, trilho.y - 17, 18f, 6f), terracotaUI);
            TextoTecido(new Rect(290, 382, 700, 24),
                "FAIXA VERDE = CAPTURA PRECISA", 13, textoPapelUI, true, TextAnchor.MiddleCenter);

            TextoTecido(new Rect(290, 422, 700, 30),
                "Acertos: " + pescaAcertos + "/3    •    Erros: " + pescaErros +
                "/3    •    Tempo: " + Mathf.CeilToInt(Mathf.Max(0f, PescaDuracao - pescaTempo)) + "s",
                18, textoPapelUI, true, TextAnchor.MiddleCenter);
            BarraTecido(new Rect(294, 470, 692, 13),
                pescaTempo / PescaDuracao, terracotaUI);

            if (BotaoTecido(new Rect(294, 541, 410, 55),
                    "Puxar linha  [ESPAÇO / E]", true, pescaTempoPressao <= 0f))
                TentarCapturaPesca();
            if (BotaoTecido(new Rect(719, 541, 266, 55), "Sair  [Esc]"))
                FecharPesca();
        }
    }
}

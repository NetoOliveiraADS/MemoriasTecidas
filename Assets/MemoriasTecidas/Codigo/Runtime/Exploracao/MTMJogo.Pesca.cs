using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private MTMPontoPesca pescaPontoAtual;
        private int pescaTipo, pescaAcertos, pescaErros;
        // 0 = pescando, 1 = captura, -1 = falha/cancelamento por limite de tempo.
        private int pescaResultado;
        private float pescaTempo, pescaTempoPressao, pescaVelocidade, pescaFaseInicial;
        private const float PescaDuracao = 25f;
        private static readonly string[] PescaIds =
        {
            "peixe_rio_miudo", "peixe_rio_prateado", "peixe_rio_especial"
        };
        private static readonly string[] PescaNomes =
        {
            "Peixe miúdo", "Peixe prateado", "Peixe especial"
        };
        private static readonly float[] PescaMargens = { .145f, .112f, .083f };
        private static readonly float[] PescaVelocidades = { .56f, .76f, 1.04f };

        private void RegistrarItensPesca()
        {
            if (tramaTecida == null) return;
            var lista = new List<MTMItemTecido>(tramaTecida.itens ?? Array.Empty<MTMItemTecido>());
            for (int i = 0; i < PescaIds.Length; i++)
            {
                if (lista.Exists(item => item != null && item.id == PescaIds[i]))
                    continue;
                lista.Add(new MTMItemTecido
                {
                    id = PescaIds[i],
                    nome = PescaNomes[i],
                    categoria = "material",
                    descricao = "Uma descoberta da pescaria no Rio Jaguaribe. Item colecionável desta atividade.",
                    origem = "Pontos de pesca na região do Rio Jaguaribe.",
                    regiao = "rio",
                    dica = "Acerte três vezes a zona de captura para completar a pescaria.",
                    icone = "peixe",
                    ocultoAteObter = true
                });
            }
            tramaTecida.itens = lista.ToArray();
        }

        private bool InteragirPontoPesca(MTMPonto ponto)
        {
            var pontoPesca = ponto == null ? null : ponto.GetComponent<MTMPontoPesca>();
            if (pontoPesca == null) return false;
            if (tramaTecida == null || estado == null)
                return true;
            pescaPontoAtual = pontoPesca;
            painel = "pesca";
            ReiniciarPesca();
            Parar();
            return true;
        }

        private void ReiniciarPesca()
        {
            pescaResultado = 0;
            pescaAcertos = 0;
            pescaErros = 0;
            pescaTempo = 0f;
            pescaTempoPressao = .35f;
            pescaFaseInicial = UnityEngine.Random.Range(0.12f, .88f);
            float sorteio = UnityEngine.Random.value;
            pescaTipo = sorteio < .55f ? 0 : sorteio < .88f ? 1 : 2;
            pescaVelocidade = PescaVelocidades[pescaTipo];
            quadroDaAcao = Time.frameCount;
        }

        private float PosicaoPeixePesca()
        {
            return Mathf.PingPong(pescaFaseInicial + pescaTempo * pescaVelocidade, 1f);
        }

        private void AtualizarPesca()
        {
            if (painel != "pesca" || pescaResultado != 0) return;
            pescaTempo += Mathf.Min(.15f, Time.unscaledDeltaTime);
            pescaTempoPressao = Mathf.Max(0f, pescaTempoPressao - Time.unscaledDeltaTime);
            if (pescaTempo >= PescaDuracao)
            {
                pescaResultado = -1;
                return;
            }
            if (MTMEntrada.ApertouPesca())
                TentarCapturaPesca();
        }

        private void TentarCapturaPesca()
        {
            if (painel != "pesca" || pescaResultado != 0 ||
                pescaTempoPressao > 0f || quadroDaAcao == Time.frameCount)
                return;
            quadroDaAcao = Time.frameCount;
            pescaTempoPressao = .28f;
            if (Mathf.Abs(PosicaoPeixePesca() - .5f) <= PescaMargens[pescaTipo])
            {
                pescaAcertos++;
                if (pescaAcertos >= 3)
                {
                    string id = PescaIds[pescaTipo];
                    int anterior = MTMTrama.Quantidade(estado, id);
                    if (anterior < int.MaxValue)
                    {
                        estado.DefinirQuantidade(id, anterior + 1);
                        pescaResultado = 1;
                        Salvar();
                    }
                    else
                        pescaResultado = -1;
                }
            }
            else
            {
                pescaErros++;
                if (pescaErros >= 3)
                    pescaResultado = -1;
            }
        }

        private void FecharPesca()
        {
            pescaPontoAtual = null;
            pescaResultado = 0;
            painel = "";
            Parar();
            quadroDaAcao = Time.frameCount;
        }
    }
}

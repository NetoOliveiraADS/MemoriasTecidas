using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        public const string IdAmostraPrimeiroFio = "abertura_amostra";
        private MTMAberturaMemorias aberturaMemorias;
        private MTMPonto amostraPrimeiroFio;
        private MTMAmostraTecida visualAmostraPrimeiroFio;
        private bool amostraComFeliciana;
        private readonly List<int> passagensPrimeiroFio = new List<int>();
        private string retornoPrimeiroFio = "", motivoSelecionadoRede = "";
        private void PrepararPrimeiroFio()
        {
            try
            {
                var arquivo = Resources.Load<TextAsset>("MemoriasTecidas/abertura_memorias");
                if (arquivo == null)
                    throw new InvalidOperationException("abertura_memorias.json ausente.");
                var dados = JsonUtility.FromJson<MTMAberturaMemorias>(arquivo.text);
                MTMPrimeiroFio.Validar(dados, tramaTecida);
                if (MTMVinculosRegiao.RegiaoDoPonto(regioes, dados.npc) == null)
                    throw new InvalidOperationException("Personagem da abertura ausente na cena.");
                aberturaMemorias = dados;
            }
            catch (Exception erro)
            {
                aberturaMemorias = null;
                Debug.LogWarning("A abertura adicional não foi carregada: " + erro.Message + " A campanha continua disponível.", this);
            }
        }

        private MTMRegiao LocalPrimeiroFio()
        {
            return aberturaMemorias == null ? null : MTMVinculosRegiao.RegiaoDoPonto(regioes, aberturaMemorias.npc);
        }

        private MTMPonto FelicianaPrimeiroFio()
        {
            var local = LocalPrimeiroFio();
            return local == null ? null : PontoTecido(local.dados.id, aberturaMemorias.npc);
        }

        private string RegiaoInicialPrimeiroFio()
        {
            var local = LocalPrimeiroFio();
            return MTMPrimeiroFio.JornadaVazia(estado) && local != null && RegiaoLiberadaRoteiro(local.dados.id) ? local.dados.id : estado.area;
        }

        private void PrepararAmostraPrimeiroFio()
        {
            var local = LocalPrimeiroFio();
            var pessoa = FelicianaPrimeiroFio();
            if (local == null || local != atual || pessoa == null)
                return;
            if (amostraPrimeiroFio == null && !amostraComFeliciana)
            {
                var autoral = MTMVinculosRegiao.RegiaoDoPonto(regioes, IdAmostraPrimeiroFio);
                if (autoral != null)
                {
                    if (autoral != local)
                    {
                        amostraComFeliciana = true;
                        Debug.LogWarning("A amostra está em outra região. Nesta sessão Feliciana apresenta a atividade. Mova o ponto da amostra para a região dela.", this);
                        return;
                    }

                    amostraPrimeiroFio = PontoTecido(local.dados.id, IdAmostraPrimeiroFio);
                }
                else
                {
                    Vector3 posicao;
                    if (!EncontrarEspacoAmostra(pessoa, out posicao))
                    {
                        amostraComFeliciana = true;
                        return;
                    }

                    var go = new GameObject("Amostra · primeiro fio")
                    {
                        hideFlags = HideFlags.DontSave
                    };
                    go.transform.SetParent(pessoa.transform, false);
                    go.transform.position = posicao;
                    amostraPrimeiroFio = go.AddComponent<MTMPonto>();
                    amostraPrimeiroFio.dados = new MTMPontoDados
                    {
                        id = IdAmostraPrimeiroFio,
                        nome = "Observar a amostra de tecido",
                        tipo = "memoria",
                        falas = new string[0]
                    };
                    amostraPrimeiroFio.alcance = 1.8f;
                    var pontos = new List<MTMPonto>(local.pontos ?? new MTMPonto[0]);
                    pontos.Add(amostraPrimeiroFio);
                    local.pontos = pontos.ToArray();
                }

                // Uma arte autoral colocada no ponto tem preferência sobre o expositor padrão.
                if (amostraPrimeiroFio.GetComponentInChildren<Renderer>(true) == null)
                {
                    var desenho = new GameObject("Tecido da lembrança")
                    {
                        hideFlags = HideFlags.DontSave
                    };
                    desenho.transform.SetParent(amostraPrimeiroFio.transform, false);
                    visualAmostraPrimeiroFio = desenho.AddComponent<MTMAmostraTecida>();
                }
            }

            AtualizarAmostraPrimeiroFio();
        }

        private bool EncontrarEspacoAmostra(MTMPonto pessoa, out Vector3 posicao)
        {
            var local = LocalPrimeiroFio();
            posicao = pessoa.transform.position;
            Physics2D.SyncTransforms();
            Vector2[] offsets =
            {
                new Vector2(3.3f, -.4f),
                new Vector2(-3.3f, -.4f),
                new Vector2(2.6f, -2.5f),
                new Vector2(-2.6f, -2.5f),
                new Vector2(0, -3.4f)
            };
            foreach (var offset in offsets)
            {
                Vector3 candidato = pessoa.transform.position + (Vector3)offset;
                Vector3 relativo = local.transform.InverseTransformPoint(candidato);
                if (Mathf.Abs(relativo.x) > local.dados.largura * .5f - 1.4f || Mathf.Abs(relativo.y) > local.dados.altura * .5f - 1.8f)
                    continue;
                bool livre = true;
                foreach (var col in Physics2D.OverlapCircleAll((Vector2)candidato, .95f))
                    if (ObstaculoAmostra(col, pessoa))
                    {
                        livre = false;
                        break;
                    }

                if (!livre)
                    continue;
                foreach (var hit in Physics2D.LinecastAll(pessoa.transform.position, candidato))
                    if (ObstaculoAmostra(hit.collider, pessoa))
                    {
                        livre = false;
                        break;
                    }

                if (livre)
                {
                    posicao = candidato;
                    return true;
                }
            }

            return false;
        }

        private bool ObstaculoAmostra(Collider2D col, MTMPonto pessoa)
        {
            return col != null && !col.isTrigger && !col.transform.IsChildOf(transform) && !col.transform.IsChildOf(pessoa.transform);
        }

        private void AtualizarAmostraPrimeiroFio()
        {
            if (visualAmostraPrimeiroFio != null)
                visualAmostraPrimeiroFio.Mostrar(estado.Tem(MTMPrimeiroFio.Teceu), aberturaMemorias);
        }

        private bool GuiaPrimeiroFioAtivo()
        {
            if (!MTMPrimeiroFio.Acompanhar(estado, aberturaMemorias) || LocalPrimeiroFio() == null)
                return false;
            // O primeiro diálogo conserva o horário da missão original.
            if (!estado.Tem(aberturaMemorias.eventoEncontro))
            {
                var primeira = MissaoTecido("m01");
                var etapa = EtapaAtualTecido(primeira);
                if (etapa != null && !MTMMotorMissoes.HorarioCorreto(etapa, estado.relogio))
                    return false;
            }

            return true;
        }

        private string ObjetivoPrimeiroFio()
        {
            int etapa = MTMPrimeiroFio.Etapa(estado);
            if (etapa == 0)
                return "Converse com Dona Feliciana para guardar uma primeira lembrança.";
            if (etapa == 1)
                return amostraComFeliciana ? "Converse com Feliciana para observar a amostra." : "Observe a amostra de tecido perto de Dona Feliciana.";
            if (etapa == 2)
                return amostraComFeliciana ? "Converse com Feliciana para experimentar as quatro passagens." : "Use a amostra e repita as quatro passagens do modelo.";
            return "Volte a Dona Feliciana para guardar o primeiro motivo na rede.";
        }

        private MTMPonto AlvoPrimeiroFio()
        {
            int e = MTMPrimeiroFio.Etapa(estado);
            return (e == 1 || e == 2) && amostraPrimeiroFio != null ? amostraPrimeiroFio : FelicianaPrimeiroFio();
        }

        private void AdiarPrimeiroFio()
        {
            if (aberturaMemorias != null && !estado.Tem(MTMPrimeiroFio.Memoria))
            {
                estado.Concluir(MTMPrimeiroFio.Adiou);
                Salvar();
            }
        }

        private void AcompanharPrimeiroFio()
        {
            estado.marcos.Remove(MTMPrimeiroFio.Adiou);
            missaoFixadaTecido = "";
            destinoGuiadoTecido = "";
            // Saves avançados precisam aceitar explicitamente o encontro adicional.
            if (estado.Tem(aberturaMemorias.eventoEncontro))
                MTMPrimeiroFio.Avancar(estado, aberturaMemorias, MTMPrimeiroFio.Convite);
            Salvar();
            painel = "";
            Avisar(ObjetivoPrimeiroFio());
        }

        private void AoConcluirEtapaPrimeiroFio(MTMEtapaTecido etapa, int motivosAntes)
        {
            if (aberturaMemorias == null)
                return;
            if (Array.IndexOf(etapa.eventos, aberturaMemorias.eventoEncontro) >= 0 && !estado.Tem(MTMPrimeiroFio.Convite) && !estado.Tem(MTMPrimeiroFio.Adiou))
            {
                Abrir("Dona Feliciana", aberturaMemorias.fonte, aberturaMemorias.convite, () =>
                {
                    MTMPrimeiroFio.Avancar(estado, aberturaMemorias, MTMPrimeiroFio.Convite);
                    Salvar();
                    Avisar(ObjetivoPrimeiroFio());
                });
                return;
            }

            if (MTMPrimeiroFio.Quantidade(estado, aberturaMemorias) > motivosAntes)
                Avisar("Um novo motivo foi tecido na sua rede. Abra J → Rede para revisitar a memória.");
        }

        private bool InteragirPrimeiroFio(MTMPonto ponto)
        {
            if (aberturaMemorias == null)
                return false;
            bool pessoa = ponto.dados.id == aberturaMemorias.npc, amostra = ponto.dados.id == IdAmostraPrimeiroFio;
            if (!pessoa && !amostra)
                return false;
            if (pessoa)
            {
                MTMMissaoTecido missao;
                MTMEtapaTecido etapa;
                // O próximo diálogo da campanha tem prioridade em todas as visitas.
                if (EtapaNoPontoRoteiro(ponto.dados.id, out missao, out etapa))
                    return false;
            }

            if (!estado.Tem(aberturaMemorias.eventoEncontro))
            {
                if (pessoa)
                    return false;
                Abrir("Amostra de tecido", aberturaMemorias.fonte, new[] { "Dona Feliciana pode apresentar esta lembrança. Converse com ela antes de experimentar." }, null);
                return true;
            }

            int passo = MTMPrimeiroFio.Etapa(estado);
            if (pessoa && passo == 4)
            {
                Abrir("Dona Feliciana", aberturaMemorias.fonte, new[] { "O motivo do nosso encontro já está na sua rede. Que outras histórias você encontrou pelo caminho?", ObjetivoTecido() }, null);
                return true;
            }

            if (pessoa && passo == 3)
            {
                Abrir("Dona Feliciana", aberturaMemorias.fonte, aberturaMemorias.reencontro, () =>
                {
                    if (MTMPrimeiroFio.Avancar(estado, aberturaMemorias, MTMPrimeiroFio.Memoria))
                        Salvar();
                    AbrirRedeMemorias("chegada");
                });
                return true;
            }

            if (passo == 0)
            {
                Abrir(pessoa ? "Dona Feliciana" : "Amostra de tecido", aberturaMemorias.fonte, aberturaMemorias.convite, () =>
                {
                    MTMPrimeiroFio.Avancar(estado, aberturaMemorias, MTMPrimeiroFio.Convite);
                    estado.marcos.Remove(MTMPrimeiroFio.Adiou);
                    Salvar();
                    Avisar(ObjetivoPrimeiroFio());
                });
                return true;
            }

            if (pessoa && !amostraComFeliciana)
            {
                Abrir("Dona Feliciana", aberturaMemorias.fonte, new[] { ObjetivoPrimeiroFio(), "Observe com calma. Depois podemos conversar sobre a sua amostra." }, null);
                return true;
            }

            if (passo == 1)
            {
                Abrir("Observar o tecido", aberturaMemorias.fonte, aberturaMemorias.observacao, () =>
                {
                    MTMPrimeiroFio.Avancar(estado, aberturaMemorias, MTMPrimeiroFio.Observou);
                    Salvar();
                    AbrirPrimeiroFio();
                });
                return true;
            }

            if (passo == 2)
            {
                AbrirPrimeiroFio();
                return true;
            }

            AbrirRedeMemorias("chegada");
            return true;
        }

        private void AbrirPrimeiroFio()
        {
            passagensPrimeiroFio.Clear();
            retornoPrimeiroFio = "";
            painel = "primeiro_fio";
            quadroDaAcao = Time.frameCount;
            Parar();
        }

        private void LimparPrimeiroFio()
        {
            passagensPrimeiroFio.Clear();
            retornoPrimeiroFio = "";
        }

        private void EscolherPassagemPrimeiroFio(int cor)
        {
            if (cor >= 0 && cor < 3 && passagensPrimeiroFio.Count < 4)
            {
                passagensPrimeiroFio.Add(cor);
                retornoPrimeiroFio = "";
            }
        }

        private void DesfazerPrimeiroFio()
        {
            if (passagensPrimeiroFio.Count > 0)
                passagensPrimeiroFio.RemoveAt(passagensPrimeiroFio.Count - 1);
            retornoPrimeiroFio = "";
        }

        private void TeclasPrimeiroFio()
        {
            Parar();
            if (quadroDaAcao == Time.frameCount)
                return;
            if (MTMEntrada.Apertou('1'))
                EscolherPassagemPrimeiroFio(0);
            else if (MTMEntrada.Apertou('2'))
                EscolherPassagemPrimeiroFio(1);
            else if (MTMEntrada.Apertou('3'))
                EscolherPassagemPrimeiroFio(2);
            else if (MTMEntrada.Apertou('u'))
                DesfazerPrimeiroFio();
            else if (MTMEntrada.Apertou('e'))
                ConferirPrimeiroFio();
        }

        private void ConferirPrimeiroFio()
        {
            if (quadroDaAcao == Time.frameCount || aberturaMemorias == null)
                return;
            quadroDaAcao = Time.frameCount;
            if (!MTMPrimeiroFio.RespostaCorreta(aberturaMemorias, passagensPrimeiroFio))
            {
                if (passagensPrimeiroFio.Count < 4)
                    retornoPrimeiroFio = "Complete as quatro passagens antes de conferir.";
                else
                {
                    int diferente = 0;
                    while (diferente < 4 && passagensPrimeiroFio[diferente] == aberturaMemorias.padrao[diferente])
                        diferente++;
                    retornoPrimeiroFio = "Observe a passagem " + (diferente + 1) + ": no modelo ela é " + aberturaMemorias.nomes[aberturaMemorias.padrao[diferente]] + ". Use Desfazer para corrigir.";
                }

                return;
            }

            if (!MTMPrimeiroFio.Avancar(estado, aberturaMemorias, MTMPrimeiroFio.Teceu, passagensPrimeiroFio))
                return;
            Salvar();
            AtualizarAmostraPrimeiroFio();
            LimparPrimeiroFio();
            painel = "";
            Abrir("O primeiro fio", aberturaMemorias.fonte, aberturaMemorias.conclusao, null);
        }

        private void AbrirRedeMemorias(string motivo = "")
        {
            painel = "diario";
            abaCaderno = "rede";
            motivoSelecionadoRede = motivo;
            rolagemDetalhe = Vector2.zero;
            Parar();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Teste automático do jogo, ligado só com "-testeAutomatico <pasta>" na linha de comando:
    // percorre as regiões e fotografa cada uma, abre as leituras da Casa das Memórias, confere se os
    // moradores andam e freiam perto do jogador e se os móveis novos bloqueiam a passagem, e anota
    // qualquer erro. Não carrega nem grava o progresso e não mexe nas preferências. No fim fecha o
    // jogo (código 1 se algo falhou) e deixa relatorio.txt e as fotos na pasta pedida.
    public sealed partial class MTMJogo
    {
        private string pastaTeste;

        private void PrepararTesteAutomatico()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-testeAutomatico")
                    pastaTeste = args[i + 1];
            if (string.IsNullOrEmpty(pastaTeste))
                return;
            carregarProgresso = false;
            Directory.CreateDirectory(pastaTeste);
            StartCoroutine(RodarTesteAutomatico());
        }

        private IEnumerator RodarTesteAutomatico()
        {
            var relatorio = new StringBuilder();
            var erros = new List<string>();
            int falhas = 0;
            Application.logMessageReceived += (texto, pilha, tipo) =>
            {
                if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
                    erros.Add(tipo + ": " + texto + (string.IsNullOrEmpty(pilha) ? "" : "\n" + pilha.Trim()));
            };

            void Anotar(bool ok, string texto)
            {
                if (!ok)
                    falhas++;
                relatorio.AppendLine((ok ? "OK     " : "FALHA  ") + texto);
            }

            relatorio.AppendLine("Teste automático · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " · " + SystemInfo.graphicsDeviceName);
            float limite = Time.realtimeSinceStartup + 90f;
            while (!pronto && Time.realtimeSinceStartup < limite)
                yield return null;
            Anotar(pronto, "o jogo ficou pronto (história, kit HD2D e regiões carregados)");
            if (pronto)
            {
                StartCoroutine(ManterFocoNoTeste());
                // Vai direto para a jornada, sem abrir a abertura nem tocar nas preferências.
                inicioAbertoUI = false;
                jornadaIniciadaUI = true;
                possuiJornadaUI = true;
                LimparPainelTeste();
                yield return new WaitForSecondsRealtime(1f);

                foreach (var r in regioes)
                {
                    if (r == null || r.dados == null)
                        continue;
                    string id = r.dados.id;
                    int antes = erros.Count;
                    estado.Concluir("visitou_" + id); // libera a região só nesta execução (nada é salvo)
                    bool entrou = IrParaPontoSeguro(id);
                    yield return new WaitForSecondsRealtime(3f);
                    LimparPainelTeste();
                    float inicio = Time.realtimeSinceStartup;
                    int quadro = Time.frameCount;
                    yield return new WaitForSecondsRealtime(2f);
                    float ms = (Time.realtimeSinceStartup - inicio) * 1000f / Mathf.Max(1, Time.frameCount - quadro);
                    FotografarTeste(id);
                    Anotar(entrou && atual == r && erros.Count == antes, string.Format(CultureInfo.InvariantCulture,
                        "{0}: entrou e foi fotografada · {1:0.0} ms por quadro · {2} erro(s) novo(s)", id, ms, erros.Count - antes));
                }

                yield return TestarCasaDasMemorias(Anotar);
                yield return TestarCaminhantes(Anotar);
            }

            relatorio.AppendLine();
            relatorio.AppendLine("Falhas: " + falhas + " · erros e exceções no log: " + erros.Count);
            foreach (var e in erros)
                relatorio.AppendLine("  " + e.Replace("\n", "\n    "));
            File.WriteAllText(Path.Combine(pastaTeste, "relatorio.txt"), relatorio.ToString(), new UTF8Encoding(true));
            MTMEntrada.MovimentoDeTeste = null;
            Application.Quit(falhas > 0 || erros.Count > 0 ? 1 : 0);
        }

        private IEnumerator TestarCasaDasMemorias(Action<bool, string> anotar)
        {
            estado.Concluir("visitou_memorias");
            if (!IrParaPontoSeguro("memorias"))
            {
                anotar(false, "Casa das Memórias: não foi possível entrar");
                yield break;
            }

            yield return new WaitForSecondsRealtime(2f);
            LimparPainelTeste();
            foreach (var id in new[] { "memorias_galeria", "memorias_moldura" })
            {
                MTMPonto p = atual.pontos == null ? null : Array.Find(atual.pontos, x => x != null && x.dados != null && x.dados.id == id);
                anotar(p != null, "Casa das Memórias: a leitura " + id + " existe");
                if (p == null)
                    continue;
                Reposicionar((Vector2)p.transform.position + Vector2.down * .6f);
                yield return new WaitForSecondsRealtime(.6f);
                var maisPerto = MaisProximo();
                anotar(maisPerto == p, "  chegando perto, " + id + " é o que o [E] pega (pegou: " + (maisPerto != null ? maisPerto.dados.id : "nenhum") + ")");
                Interagir(p);
                anotar(falas != null && falas.Length > 0, "  " + id + " abre a leitura (" + (falas != null ? falas.Length : 0) + " falas)");
                if (falas != null)
                {
                    PrepararFalaLegivelUI();
                    anotar(retratoExibidoDialogo != null, "  " + id + " mostra imagem na conversa (" + (retratoExibidoDialogo != null ? retratoExibidoDialogo.name : "nenhuma") + ")");
                }

                LimparPainelTeste();
                yield return null;
            }

            // Os móveis novos bloqueiam a passagem: empurra o Neto para cima, contra a mesa de redação.
            Transform mesa = null;
            foreach (var t in atual.GetComponentsInChildren<Transform>(true))
                if (t.name == "Mesa de redação")
                    mesa = t;
            anotar(mesa != null, "Casa das Memórias: a mesa de redação está na sala");
            if (mesa == null)
                yield break;
            Vector2 partida = (Vector2)mesa.position + new Vector2(-.6f, -.9f);
            Reposicionar(partida);
            yield return new WaitForSecondsRealtime(.3f);
            MTMEntrada.MovimentoDeTeste = Vector2.up;
            yield return new WaitForSecondsRealtime(1.5f);
            MTMEntrada.MovimentoDeTeste = null;
            float subiu = corpo.position.y - partida.y;
            anotar(subiu > .1f && corpo.position.y < mesa.position.y + .05f, string.Format(CultureInfo.InvariantCulture,
                "  a mesa barra o Neto (andou {0:0.00} para cima e parou em y={1:0.00}; a mesa começa em y={2:0.00})", subiu, corpo.position.y, mesa.position.y));
            FotografarTeste("memorias_mesa");
        }

        private IEnumerator TestarCaminhantes(Action<bool, string> anotar)
        {
            estado.Concluir("visitou_rua");
            if (!IrParaPontoSeguro("rua"))
            {
                anotar(false, "Rua do Comércio: não foi possível entrar");
                yield break;
            }

            yield return new WaitForSecondsRealtime(2f);
            LimparPainelTeste();
            foreach (var c in atual.GetComponentsInChildren<MTMCaminhanteV22>())
            {
                if (c.animal)
                    continue;
                string nome = c.transform.parent != null ? c.transform.parent.name + "/" + c.name : c.name;
                // Longe do Neto, o morador anda o seu caminho.
                if (((Vector2)c.transform.position - corpo.position).magnitude < 4f)
                    Reposicionar(corpo.position + new Vector2(0f, -4f));
                float percorrido = 0f;
                Vector3 anterior = c.transform.position;
                float fim = Time.realtimeSinceStartup + 9f;
                while (Time.realtimeSinceStartup < fim)
                {
                    percorrido += Vector3.Distance(c.transform.position, anterior);
                    anterior = c.transform.position;
                    yield return null;
                }

                anotar(percorrido > .3f, string.Format(CultureInfo.InvariantCulture, "Rua do Comércio: {0} andou {1:0.00} unidades em 9 s", nome, percorrido));
                // Com o Neto ao lado, ele freia e fica parado.
                Reposicionar((Vector2)c.transform.position + new Vector2(1.3f, -.4f));
                yield return new WaitForSecondsRealtime(1.5f);
                Vector3 parado = c.transform.position;
                yield return new WaitForSecondsRealtime(1.5f);
                float mexeu = Vector3.Distance(parado, c.transform.position);
                anotar(mexeu < .02f, string.Format(CultureInfo.InvariantCulture, "  {0} para com o Neto por perto (mexeu {1:0.000})", nome, mexeu));
                FotografarTeste("rua_caminhante");
            }
        }

        private IEnumerator ManterFocoNoTeste()
        {
            // Sem janela o jogo nunca "recebe foco": o teste mantém o foco ligado para tudo andar.
            while (true)
            {
                possuiFoco = true;
                yield return null;
            }
        }

        private void LimparPainelTeste()
        {
            falas = null;
            aoConcluir = null;
            painel = "";
        }

        private void FotografarTeste(string nome)
        {
            if (cameraDoMapa == null)
                return;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 2 };
            var alvoAnterior = cameraDoMapa.targetTexture;
            cameraDoMapa.targetTexture = rt;
            cameraDoMapa.Render();
            cameraDoMapa.targetTexture = alvoAnterior;
            var ativo = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = ativo;
            File.WriteAllBytes(Path.Combine(pastaTeste, nome + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
            Destroy(rt);
        }
    }
}

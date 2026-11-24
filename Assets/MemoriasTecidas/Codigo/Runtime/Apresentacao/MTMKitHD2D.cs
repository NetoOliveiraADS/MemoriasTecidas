using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [Serializable]
    public sealed class MTMKitHD2DDados
    {
        public int versao;
        public bool ativo;
        public bool preservarCenarioOriginal;
        public MTMQuadroHD2D[] neto, objetos;
        public MTMTrocaHD2D[] trocas;
        public MTMSuperficieHD2D[] superficies;
        public MTMPisoHD2D[] pisos;
        public MTMCartaoHD2D[] cartoes;
    }

    [Serializable]
    public sealed class MTMQuadroHD2D
    {
        public string id, folha;
        public int x, y, largura, altura, folhaLargura, folhaAltura;
        public float pivotX, pivotY, alturaReferencia;
    }

    [Serializable]
    public sealed class MTMTrocaHD2D
    {
        public string[] origens, regioes;
        public string destino;
        public int lado;
        public bool ajustarAltura;
    }

    [Serializable]
    public class MTMSuperficieHD2D
    {
        public string[] origens;
        public string textura;
    }

    [Serializable]
    public sealed class MTMPisoHD2D : MTMSuperficieHD2D
    {
        public float mistura = .28f, repeticao = 6f;
    }

    [Serializable]
    public sealed class MTMCartaoHD2D
    {
        public string regiao, textura;
    }

    [Serializable]
    internal sealed class MTMArtePersonagensHD2D
    {
        public MTMQuadroHD2D[] neto, feliciana;
        // Opcional: as mesmas 32 poses do Neto, correndo (mesma ordem de "neto").
        public MTMQuadroHD2D[] netoCorrida;
        public MTMMoradorHD2D[] moradores;
        public MTMRetratoHD2D[] retratos;
        // Opcional: caminhada de perfil dos moradores que andam pela cidade.
        public MTMCaminhadaHD2D[] caminhadas;
    }

    // Caminhada de perfil (virada para a direita) de quem usa a folha de frente "frente".
    // Andando, o morador troca para estes quadros (o jogo espelha para a esquerda); parado ou
    // conversando, volta à arte de frente.
    [Serializable]
    internal sealed class MTMCaminhadaHD2D
    {
        public string frente;
        public MTMQuadroHD2D[] quadros;
    }

    // Retrato revisado dos diálogos e do caderno: vale para o ponto "ponto" e para qualquer
    // ponto cujo retrato antigo se chame "origem" (as cópias que o roteiro cria).
    [Serializable]
    internal sealed class MTMRetratoHD2D
    {
        public string ponto, origem, textura;
    }

    // Pose revisada de um personagem: troca o sprite antigo com o nome "origem"
    // (e da textura "texturaOrigem", se preenchida) pelo recorte "quadro".
    // Com "ponto", vale só para a pessoa que o roteiro cria nesse ponto, que copia a arte de
    // outro personagem (ex.: pdf_visitante copia o André Lúcio e ganha o desenho próprio).
    [Serializable]
    internal sealed class MTMMoradorHD2D
    {
        public string origem, texturaOrigem, ponto;
        public MTMQuadroHD2D quadro;
    }

    // Instâncias apenas durante o Play. Não grava cenas, prefabs nem dados de missões.
    public sealed class MTMKitHD2D : IDisposable
    {
        public const string Pasta = "MemoriasTecidas/HD2D/";
        private const string PastaPersonagens = "PersonagensRevisados/";
        private readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, MTMQuadroHD2D> objetos = new Dictionary<string, MTMQuadroHD2D>();
        private readonly Dictionary<string, Texture2D> cartoes = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, MTMMoradorHD2D> moradores = new Dictionary<string, MTMMoradorHD2D>(StringComparer.Ordinal);
        // Folha de frente ("PersonagensRevisados/Moradores/Zeca") -> quadros da caminhada de perfil.
        private readonly Dictionary<string, MTMQuadroHD2D[]> caminhadas = new Dictionary<string, MTMQuadroHD2D[]>(StringComparer.Ordinal);
        private readonly Dictionary<string, Texture2D> retratosPorPonto = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly Dictionary<string, Texture2D> retratosPorOrigem = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly List<Action> restaurar = new List<Action>();
        private Material materialPiso;
        private MTMKitHD2DDados dados;
        private MTMArtePersonagensHD2D artePersonagens;
        private MTMQuadroHD2D[] netoCorrida;
        private Texture2D retratoNeto;
        public Texture2D RetratoNeto => aplicado ? retratoNeto : null;
        private bool aplicado;
        public int Personagens { get; private set; }
        public int Objetos { get; private set; }
        public int Superficies { get; private set; }
        public int Pisos { get; private set; }
        public int Retratos { get; private set; }
        public int Moradores { get; private set; }

        private Texture2D Textura(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                throw new InvalidOperationException("Nome de textura vazio.");
            Texture2D t;
            if (texturas.TryGetValue(nome, out t))
                return t;
            t = Resources.Load<Texture2D>(Pasta + nome);
            if (t == null)
                throw new InvalidOperationException("Recurso ausente: " + Pasta + nome);
            texturas.Add(nome, t);
            return t;
        }

        public void ValidarRecursos()
        {
            var json = Resources.Load<TextAsset>(Pasta + "kit_hd2d");
            if (json == null)
                throw new InvalidOperationException("kit_hd2d.json ausente.");
            dados = JsonUtility.FromJson<MTMKitHD2DDados>(json.text);
            if (dados == null || dados.versao != 1 || dados.neto == null || dados.neto.Length != 32 || dados.objetos == null || dados.trocas == null || dados.superficies == null || dados.pisos == null || dados.cartoes == null)
                throw new InvalidOperationException("Configuração HD2D incompleta ou versão incompatível.");
            var ids = new HashSet<string>();
            string[] direcoes =
            {
                "S",
                "SW",
                "W",
                "NW",
                "N",
                "NE",
                "E",
                "SE"
            };
            for (int i = 0; i < dados.neto.Length; i++)
            {
                ValidarQuadro(dados.neto[i], ids);
                if (dados.neto[i].id != "Neto_" + direcoes[i / 4] + "_" + (i % 4).ToString("00"))
                    throw new InvalidOperationException("Ordem das oito direções do Neto incorreta.");
            }

            objetos.Clear();
            if (!dados.preservarCenarioOriginal)
            {
                foreach (var q in dados.objetos)
                {
                    ValidarQuadro(q, ids);
                    objetos.Add(q.id, q);
                }

                foreach (var t in dados.trocas)
                    if (t == null || t.origens == null || t.origens.Length == 0 || t.regioes == null || string.IsNullOrEmpty(t.destino) || !objetos.ContainsKey(t.destino) || t.lado < -1 || t.lado > 1)
                        throw new InvalidOperationException("Regra de objeto HD2D inválida.");
                foreach (var s in dados.superficies)
                    ValidarSuperficie(s);
                foreach (var p in dados.pisos)
                {
                    ValidarSuperficie(p);
                    if (!Finito(p.mistura) || p.mistura < 0 || p.mistura > .5f || !Finito(p.repeticao) || p.repeticao <= 0)
                        throw new InvalidOperationException("Mistura do piso deve preservar ao menos metade da arte original.");
                }

                cartoes.Clear();
                foreach (var c in dados.cartoes)
                {
                    if (c == null || string.IsNullOrEmpty(c.regiao) || cartoes.ContainsKey(c.regiao))
                        throw new InvalidOperationException("Região de ilustração ausente ou repetida.");
                    cartoes.Add(c.regiao, Textura(c.textura));
                }
            }
            else
                cartoes.Clear();
            Textura("FelicianaRetrato");
            ValidarArtePersonagens();
            if (!dados.preservarCenarioOriginal && Resources.Load<Shader>(Pasta + "TerrenoDetalhe") == null)
                throw new InvalidOperationException("Shader TerrenoDetalhe ausente.");
        }

        private void ValidarArtePersonagens()
        {
            artePersonagens = null;
            retratoNeto = null;
            moradores.Clear();
            caminhadas.Clear();
            retratosPorPonto.Clear();
            retratosPorOrigem.Clear();
            var json = Resources.Load<TextAsset>(Pasta + PastaPersonagens + "quadros");
            if (json == null)
                return;

            MTMArtePersonagensHD2D arte = null;
            try
            {
                arte = JsonUtility.FromJson<MTMArtePersonagensHD2D>(json.text);
                if (arte == null || arte.neto == null || arte.neto.Length != dados.neto.Length ||
                    arte.feliciana == null || arte.feliciana.Length != 3)
                    throw new InvalidOperationException("Folhas de personagens incompletas.");

                var ids = new HashSet<string>();
                for (int i = 0; i < arte.neto.Length; i++)
                {
                    ValidarQuadro(arte.neto[i], ids);
                    if (arte.neto[i].id != dados.neto[i].id)
                        throw new InvalidOperationException("Ordem das poses do Neto incorreta.");
                }

                for (int i = 0; i < arte.feliciana.Length; i++)
                {
                    ValidarQuadro(arte.feliciana[i], ids);
                    if (arte.feliciana[i].id != "Feliciana_" + i.ToString("00"))
                        throw new InvalidOperationException("Ordem das poses da Feliciana incorreta.");
                }

                retratoNeto = Textura(PastaPersonagens + "NetoRetrato");
                artePersonagens = arte;
            }
            catch (Exception e)
            {
                Debug.LogWarning("HD2D: o kit anterior de personagens foi mantido. " + e.Message);
            }

            if (arte != null)
            {
                ValidarMoradores(arte.moradores);
                ValidarCaminhadas(arte.caminhadas);
                ValidarRetratos(arte.retratos);
                if (artePersonagens != null)
                    ValidarNetoCorrida(arte.netoCorrida);
            }
        }

        // Sem as poses de corrida (ou com problema nelas), o Neto corre com as da caminhada.
        // Uma pose com "folha" vazia também usa a da caminhada: assim dá para ligar a corrida
        // direção por direção, à medida que as imagens ficam prontas.
        private static bool SemPoseCorrida(MTMQuadroHD2D q)
        {
            return q == null || string.IsNullOrEmpty(q.folha);
        }

        private void ValidarNetoCorrida(MTMQuadroHD2D[] lista)
        {
            netoCorrida = null;
            if (lista == null || lista.Length == 0)
                return;
            try
            {
                if (lista.Length != dados.neto.Length)
                    throw new InvalidOperationException("são necessárias " + dados.neto.Length + " poses, na mesma ordem de \"neto\".");
                var ids = new HashSet<string>();
                int poses = 0;
                foreach (var q in lista)
                    if (!SemPoseCorrida(q))
                    {
                        ValidarQuadro(q, ids);
                        poses++;
                    }
                if (poses > 0)
                    netoCorrida = lista;
            }
            catch (Exception e)
            {
                Debug.LogWarning("HD2D: poses de corrida do Neto ignoradas. " + e.Message);
            }
        }

        private void ValidarRetratos(MTMRetratoHD2D[] lista)
        {
            if (lista == null)
                return;
            foreach (var r in lista)
                try
                {
                    if (r == null || string.IsNullOrEmpty(r.textura) || (string.IsNullOrEmpty(r.ponto) && string.IsNullOrEmpty(r.origem)))
                        throw new InvalidOperationException("retrato sem imagem, ponto ou origem.");
                    var t = Textura(r.textura);
                    if (!string.IsNullOrEmpty(r.ponto))
                        retratosPorPonto[r.ponto] = t;
                    if (!string.IsNullOrEmpty(r.origem))
                        retratosPorOrigem[r.origem] = t;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("HD2D: retrato revisado ignorado (" + (r == null ? "vazio" : r.textura) + "): " + e.Message);
                }
        }

        // Uma pose com problema é ignorada sem desligar as outras nem as de Neto e Feliciana.
        private void ValidarMoradores(MTMMoradorHD2D[] lista)
        {
            if (lista == null)
                return;
            var ids = new HashSet<string>();
            foreach (var m in lista)
                try
                {
                    if (m == null || string.IsNullOrEmpty(m.origem) || moradores.ContainsKey(ChaveMorador(m.ponto, m.origem)))
                        throw new InvalidOperationException("pose sem origem ou com origem repetida.");
                    ValidarQuadro(m.quadro, ids);
                    moradores.Add(ChaveMorador(m.ponto, m.origem), m);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("HD2D: pose revisada ignorada (" + (m == null ? "vazia" : m.origem) + "): " + e.Message);
                }
        }

        // Uma caminhada com problema é ignorada: o morador continua andando com a arte de frente.
        private void ValidarCaminhadas(MTMCaminhadaHD2D[] lista)
        {
            if (lista == null)
                return;
            foreach (var c in lista)
                try
                {
                    if (c == null || string.IsNullOrEmpty(c.frente) || c.quadros == null || c.quadros.Length < 2 || caminhadas.ContainsKey(c.frente))
                        throw new InvalidOperationException("caminhada sem folha de frente, com menos de 2 quadros ou repetida.");
                    var ids = new HashSet<string>();
                    foreach (var q in c.quadros)
                        ValidarQuadro(q, ids);
                    caminhadas.Add(c.frente, c.quadros);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("HD2D: caminhada de perfil ignorada (" + (c == null ? "vazia" : c.frente) + "): " + e.Message);
                }
        }

        private void ValidarSuperficie(MTMSuperficieHD2D s)
        {
            if (s == null || s.origens == null || s.origens.Length == 0)
                throw new InvalidOperationException("Regra de superfície vazia.");
            Textura(s.textura);
        }

        private void ValidarQuadro(MTMQuadroHD2D q, HashSet<string> ids)
        {
            if (q == null || string.IsNullOrEmpty(q.id) || !ids.Add(q.id))
                throw new InvalidOperationException("Quadro ausente ou repetido.");
            var t = Textura(q.folha);
            if (t.width != q.folhaLargura || t.height != q.folhaAltura || q.x < 0 || q.y < 0 || q.largura <= 0 || q.altura <= 0 || (long)q.x + q.largura > t.width || (long)q.y + q.altura > t.height || !Finito(q.pivotX) || !Finito(q.pivotY) || q.pivotX < 0 || q.pivotX > 1 || q.pivotY < 0 || q.pivotY > 1 || !Finito(q.alturaReferencia) || q.alturaReferencia <= 0)
                throw new InvalidOperationException("Recorte ou importação inválida: " + q.id + ". Use tamanho 2048 e Non Power of 2 = None.");
        }

        private static bool Finito(float n)
        {
            return !float.IsNaN(n) && !float.IsInfinity(n);
        }

        private Sprite CriarSprite(MTMQuadroHD2D q, float ppu, string chave)
        {
            Sprite s;
            if (sprites.TryGetValue(chave, out s))
                return s;
            s = Sprite.Create(Textura(q.folha), new Rect(q.x, q.y, q.largura, q.altura), new Vector2(q.pivotX, q.pivotY), Mathf.Max(1f, ppu), 0, SpriteMeshType.FullRect);
            s.name = "HD2D_" + q.id;
            s.hideFlags = HideFlags.DontSave;
            sprites.Add(chave, s);
            return s;
        }

        public void Aplicar(MTMJogo jogo)
        {
            if (aplicado)
                return;
            ValidarRecursos(); // Verifica tudo antes de substituir qualquer referência.
            if (!dados.ativo)
                return;
            try
            {
                foreach (var ator in jogo.GetComponentsInChildren<MTMAtorV22>(true))
                    if (ator.tipo == MTMAtorV22.Tipo.Jogador)
                        AplicarNeto(ator);
                var vistos = new HashSet<int>();
                var pontos = new HashSet<MTMPonto>();
                if (jogo.regioes != null)
                    foreach (var regiao in jogo.regioes)
                    {
                        if (regiao == null || regiao.dados == null)
                            continue;
                        if (!dados.preservarCenarioOriginal)
                        {
                            foreach (var sr in regiao.GetComponentsInChildren<SpriteRenderer>(true))
                            {
                                if (!vistos.Add(sr.GetInstanceID()) || sr.sprite == null || Sombra(sr))
                                    continue;
                                AplicarObjeto(sr, regiao);
                                AplicarPiso(sr);
                            }

                            foreach (var sup in regiao.GetComponentsInChildren<MTMSuperficieV23>(true))
                                AplicarSuperficie(sup);
                        }

                        foreach (var ponto in regiao.GetComponentsInChildren<MTMPonto>(true))
                            pontos.Add(ponto);
                        if (regiao.pontos != null)
                            foreach (var ponto in regiao.pontos)
                                if (ponto != null)
                                    pontos.Add(ponto);
                    }

                foreach (var ponto in pontos)
                {
                    if (ponto == null || ponto.dados == null)
                        continue;
                    if (ponto.dados.id != "npc_feliciana")
                    {
                        AplicarRetratoMorador(ponto);
                        continue;
                    }

                    AplicarRetrato(ponto);
                    if (artePersonagens != null)
                        AplicarFeliciana(jogo, ponto);
                }

                // Os outros personagens, inclusive as cópias que o roteiro cria antes do kit (reunião, festa).
                if (moradores.Count > 0 && jogo.regioes != null)
                    foreach (var regiao in jogo.regioes)
                        if (regiao != null)
                            foreach (var ator in regiao.GetComponentsInChildren<MTMAtorV22>(true))
                                if (ator.tipo == MTMAtorV22.Tipo.Morador)
                                    AplicarMorador(ator);
                AvisarPontosSemArte(jogo);
                aplicado = true;
                if (Personagens == 0)
                    Debug.LogWarning("HD2D: não foi encontrado o animador MTMAtorV22 do Neto. A aparência anterior foi mantida.", jogo);
                if (dados.preservarCenarioOriginal)
                    Debug.Log("Cenário original preservado: sprites, texturas, materiais e ilustrações dos lugares mantidos. " + Personagens + " protagonista(s), " + Moradores + " morador(es) e " + Retratos + " retrato(s) atualizados.", jogo);
                else
                    Debug.Log("HD2D integrado: " + Personagens + " protagonista(s), " + Moradores + " outros personagens, " + Objetos + " objetos, " + Superficies + " superfícies, " + Pisos + " pisos e " + Retratos + " retrato(s).", jogo);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void AplicarNeto(MTMAtorV22 ator)
        {
            var sr = ator.GetComponent<SpriteRenderer>();
            float altura = sr != null && sr.sprite != null ? Mathf.Max(.1f, sr.sprite.rect.height / sr.sprite.pixelsPerUnit) : 0f;
            if (!AplicarQuadros(ator, artePersonagens?.neto ?? dados.neto))
                return;
            Personagens++;
            if (netoCorrida == null || altura <= 0f)
                return;
            // Mesma escala das poses de caminhada (mesma altura de referência).
            var corrida = new Sprite[netoCorrida.Length];
            for (int i = 0; i < corrida.Length; i++)
                corrida[i] = SemPoseCorrida(netoCorrida[i]) ? null :
                    CriarSprite(netoCorrida[i], netoCorrida[i].alturaReferencia / altura, ator.GetInstanceID() + ":corrida:" + netoCorrida[i].id);
            var anteriores = ator.quadrosCorrida;
            restaurar.Add(() =>
            {
                if (ator != null)
                    ator.quadrosCorrida = anteriores;
            });
            ator.quadrosCorrida = corrida;
        }

        private void AplicarFeliciana(MTMJogo jogo, MTMPonto ponto)
        {
            var regiao = MTMVinculosRegiao.RegiaoAncestral(ponto.transform) ??
                MTMVinculosRegiao.RegiaoDoPonto(jogo.regioes, ponto.dados.id);
            if (regiao == null)
                return;

            foreach (var ator in regiao.GetComponentsInChildren<MTMAtorV22>(true))
                if (ator.tipo == MTMAtorV22.Tipo.Morador && ator.nomeDaConversa == ponto.dados.nome)
                {
                    if (AplicarQuadros(ator, artePersonagens.feliciana))
                        ProporcoesDoDesenho(ator);
                    return;
                }
        }

        // A arte revisada já vem com as proporções certas. O ajuste da malha feito para a arte antiga
        // (corpo 12% mais comprido e cabeça menor, puxada para o pivô) deixava os moradores finos e tortos.
        private void ProporcoesDoDesenho(MTMAtorV22 ator)
        {
            var malha = ator.GetComponent<MTMDeformacaoV22>();
            if (malha == null || malha.tipo != MTMDeformacaoV22.Tipo.Pessoa || (malha.escalaCabeca == 1f && malha.alongamentoCorpo == 1f))
                return;
            float cabeca = malha.escalaCabeca, corpo = malha.alongamentoCorpo;
            restaurar.Add(() =>
            {
                if (malha == null)
                    return;
                malha.escalaCabeca = cabeca;
                malha.alongamentoCorpo = corpo;
            });
            malha.escalaCabeca = 1f;
            malha.alongamentoCorpo = 1f;
        }

        private bool AplicarQuadros(MTMAtorV22 ator, MTMQuadroHD2D[] quadros)
        {
            var sr = ator.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
                return false;
            Sprite anterior = sr.sprite;
            Sprite[] anteriores = ator.quadros;
            float altura = Mathf.Max(.1f, anterior.rect.height / anterior.pixelsPerUnit);
            var novos = new Sprite[quadros.Length];
            for (int i = 0; i < novos.Length; i++)
                novos[i] = CriarSprite(quadros[i], quadros[i].alturaReferencia / altura, ator.GetInstanceID() + ":" + quadros[i].id);
            restaurar.Add(() =>
            {
                if (ator != null)
                    ator.quadros = anteriores;
                if (sr != null)
                    sr.sprite = anterior;
            });
            ator.quadros = novos;
            sr.sprite = novos[0];
            return true;
        }

        private void AplicarMorador(MTMAtorV22 ator)
        {
            var sr = ator.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;
            Sprite anterior = sr.sprite;
            Sprite[] anteriores = ator.quadros;
            // Pessoa criada pelo roteiro: fica dentro do próprio ponto de conversa. As regiões que o Neto não está
            // visitando ficam desligadas: sem "true" o ponto não seria achado e a pessoa ganharia a arte do modelo
            // (o visitante da Serra aparecia como André Lúcio).
            var dono = ator.GetComponentInParent<MTMPonto>(true);
            string ponto = dono != null && dono.dados != null ? dono.dados.id : null;
            Sprite atual = Revisado(anterior, ponto);
            bool trocou = atual != anterior;
            Sprite[] novos = anteriores;
            if (anteriores != null)
            {
                novos = new Sprite[anteriores.Length];
                for (int i = 0; i < novos.Length; i++)
                {
                    novos[i] = Revisado(anteriores[i], ponto);
                    trocou |= novos[i] != anteriores[i];
                }
            }

            if (!trocou)
                return;
            if (!string.IsNullOrEmpty(ponto) && (TemArteDoPonto(ponto, anterior) || (anteriores != null && Array.Exists(anteriores, s => TemArteDoPonto(ponto, s)))))
                pontosTrocados.Add(ponto);
            restaurar.Add(() =>
            {
                if (ator != null)
                    ator.quadros = anteriores;
                if (sr != null)
                    sr.sprite = anterior;
            });
            ator.quadros = novos;
            sr.sprite = atual;
            ProporcoesDoDesenho(ator);
            CaminhadaDeLado(ator, anterior, ponto, atual);
            Moradores++;
        }

        // Pessoas com arte própria pelo ponto (como o visitante da Serra, criado a partir de outro morador).
        private readonly HashSet<string> pontosTrocados = new HashSet<string>(StringComparer.Ordinal);

        private bool TemArteDoPonto(string ponto, Sprite s)
        {
            return s != null && moradores.ContainsKey(ChaveMorador(ponto, s.name));
        }

        // Avisa quando uma dessas pessoas ficou com a arte do modelo (ela apareceria como outro personagem).
        private void AvisarPontosSemArte(MTMJogo jogo)
        {
            var avisados = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chave in moradores.Keys)
            {
                int barra = chave.IndexOf('|');
                if (barra <= 0)
                    continue;
                string ponto = chave.Substring(0, barra);
                if (avisados.Add(ponto) && !pontosTrocados.Contains(ponto))
                    Debug.LogWarning("HD2D: o personagem do ponto '" + ponto + "' tem arte própria, mas ficou com a arte do modelo.", jogo);
            }
        }

        // A pose revisada ocupa no mundo a mesma altura da pose antiga.
        private Sprite Revisado(Sprite antigo, string ponto)
        {
            var m = MoradorRevisado(antigo, ponto);
            if (m == null)
                return antigo;
            float ppu = antigo.pixelsPerUnit * m.quadro.alturaReferencia / Mathf.Max(1f, antigo.rect.height);
            return CriarSprite(m.quadro, ppu, "morador:" + antigo.GetInstanceID() + ":" + m.quadro.id);
        }

        private MTMMoradorHD2D MoradorRevisado(Sprite antigo, string ponto)
        {
            MTMMoradorHD2D m;
            if (antigo == null)
                return null;
            if ((string.IsNullOrEmpty(ponto) || !moradores.TryGetValue(ChaveMorador(ponto, antigo.name), out m)) &&
                !moradores.TryGetValue(ChaveMorador(null, antigo.name), out m))
                return null;
            if (!string.IsNullOrEmpty(m.texturaOrigem) && (antigo.texture == null || antigo.texture.name != m.texturaOrigem))
                return null;
            return m;
        }

        // Quem tem caminhada de perfil ganha os quadros com a mesma altura no mundo da arte de frente revisada
        // (a folha de perfil fica na resolução em que foi desenhada; só a escala muda).
        private void CaminhadaDeLado(MTMAtorV22 ator, Sprite antigo, string ponto, Sprite frente)
        {
            var m = MoradorRevisado(antigo, ponto);
            MTMQuadroHD2D[] lado;
            if (m == null || frente == null || !caminhadas.TryGetValue(m.quadro.folha, out lado))
                return;
            var sprites = new Sprite[lado.Length];
            for (int i = 0; i < lado.Length; i++)
            {
                float ppu = frente.pixelsPerUnit * lado[i].alturaReferencia / Mathf.Max(1f, m.quadro.alturaReferencia);
                sprites[i] = CriarSprite(lado[i], ppu, "lado:" + antigo.GetInstanceID() + ":" + lado[i].id);
            }
            var anteriores = ator.quadrosLado;
            restaurar.Add(() =>
            {
                if (ator != null)
                    ator.quadrosLado = anteriores;
            });
            ator.quadrosLado = sprites;
        }

        private static string ChaveMorador(string ponto, string origem)
        {
            return (ponto ?? "") + "|" + origem;
        }

        private static bool Sombra(SpriteRenderer sr)
        {
            if (sr.name.IndexOf("sombra", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            // "true": também nas regiões desligadas (as que o Neto não está visitando).
            var visual = sr.GetComponentInParent<MTMVisualV2>(true);
            return visual != null && visual.sombra == sr;
        }

        private static bool Nome(string[] nomes, string nome)
        {
            if (nomes == null || string.IsNullOrEmpty(nome))
                return false;
            foreach (var n in nomes)
                if (string.Equals(n, nome, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static bool Corresponde(string[] nomes, Sprite s)
        {
            return s != null && (Nome(nomes, s.name) || (s.texture != null && Nome(nomes, s.texture.name)));
        }

        private void AplicarObjeto(SpriteRenderer sr, MTMRegiao regiao)
        {
            foreach (var t in dados.trocas)
            {
                if (!Corresponde(t.origens, sr.sprite) || (t.regioes.Length > 0 && !Nome(t.regioes, regiao.dados.id)))
                    continue;
                float x = regiao.transform.InverseTransformPoint(sr.transform.position).x;
                if ((t.lado < 0 && x >= 0) || (t.lado > 0 && x < 0))
                    continue;
                Sprite anterior = sr.sprite;
                var q = objetos[t.destino];
                float tamanho = t.ajustarAltura ? anterior.rect.height / anterior.pixelsPerUnit : anterior.rect.width / anterior.pixelsPerUnit;
                float ppu = (t.ajustarAltura ? q.altura : q.largura) / Mathf.Max(.01f, tamanho);
                var novo = CriarSprite(q, ppu, "objeto:" + anterior.GetInstanceID() + ":" + q.id + ":" + t.ajustarAltura);
                restaurar.Add(() =>
                {
                    if (sr != null)
                        sr.sprite = anterior;
                });
                sr.sprite = novo;
                Objetos++;
                return;
            }
        }

        private void AplicarSuperficie(MTMSuperficieV23 sup)
        {
            if (sup.textura == null)
                return;
            foreach (var regra in dados.superficies)
            {
                if (!Nome(regra.origens, sup.textura.name))
                    continue;
                var renderer = sup.GetComponent<MeshRenderer>();
                var anterior = sup.textura;
                var blocoAnterior = new MaterialPropertyBlock();
                if (renderer != null)
                    renderer.GetPropertyBlock(blocoAnterior);
                restaurar.Add(() =>
                {
                    if (sup != null)
                        sup.textura = anterior;
                    if (renderer != null)
                        renderer.SetPropertyBlock(blocoAnterior);
                });
                sup.textura = Textura(regra.textura);
                if (renderer != null)
                {
                    var bloco = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(bloco);
                    bloco.SetTexture("_MainTex", sup.textura);
                    renderer.SetPropertyBlock(bloco);
                }

                // Contorno, material, água animada, colisor e ordenação permanecem no componente original.
                Superficies++;
                return;
            }
        }

        private void AplicarPiso(SpriteRenderer sr)
        {
            foreach (var regra in dados.pisos)
            {
                if (!Corresponde(regra.origens, sr.sprite))
                    continue;
                if (materialPiso == null)
                {
                    materialPiso = new Material(Resources.Load<Shader>(Pasta + "TerrenoDetalhe"));
                    materialPiso.name = "HD2D_PisoInstancia";
                    materialPiso.hideFlags = HideFlags.DontSave;
                }

                Material anterior = sr.sharedMaterial;
                var blocoAnterior = new MaterialPropertyBlock();
                sr.GetPropertyBlock(blocoAnterior);
                restaurar.Add(() =>
                {
                    if (sr != null)
                    {
                        sr.sharedMaterial = anterior;
                        sr.SetPropertyBlock(blocoAnterior);
                    }
                });
                sr.sharedMaterial = materialPiso;
                var bloco = new MaterialPropertyBlock();
                sr.GetPropertyBlock(bloco);
                bloco.SetTexture("_DetailTex", Textura(regra.textura));
                bloco.SetFloat("_Mistura", regra.mistura);
                bloco.SetFloat("_Repeticao", regra.repeticao);
                sr.SetPropertyBlock(bloco);
                Pisos++;
                return;
            }
        }

        private void AplicarRetrato(MTMPonto ponto)
        {
            var anterior = ponto.retrato;
            var recorte = ponto.recorteRosto;
            restaurar.Add(() =>
            {
                if (ponto != null)
                {
                    ponto.retrato = anterior;
                    ponto.recorteRosto = recorte;
                }
            });
            ponto.retrato = Textura("FelicianaRetrato");
            ponto.recorteRosto = new Rect(0, 0, 1, 1);
            Retratos++;
        }

        private void AplicarRetratoMorador(MTMPonto ponto)
        {
            Texture2D novo;
            if (!retratosPorPonto.TryGetValue(ponto.dados.id, out novo) &&
                (ponto.retrato == null || !retratosPorOrigem.TryGetValue(ponto.retrato.name, out novo)))
                return;
            var anterior = ponto.retrato;
            var recorte = ponto.recorteRosto;
            restaurar.Add(() =>
            {
                if (ponto != null)
                {
                    ponto.retrato = anterior;
                    ponto.recorteRosto = recorte;
                }
            });
            ponto.retrato = novo;
            ponto.recorteRosto = new Rect(0, 0, 1, 1);
            Retratos++;
        }

        public Texture2D Imagem(string regiao)
        {
            Texture2D t;
            return aplicado && regiao != null && cartoes.TryGetValue(regiao, out t) ? t : null;
        }

        public void Dispose()
        {
            for (int i = restaurar.Count - 1; i >= 0; i--)
                restaurar[i]();
            restaurar.Clear();
            foreach (var s in sprites.Values)
                Destruir(s);
            sprites.Clear();
            Destruir(materialPiso);
            materialPiso = null;
            texturas.Clear();
            objetos.Clear();
            cartoes.Clear();
            retratoNeto = null;
            artePersonagens = null;
            netoCorrida = null;
            moradores.Clear();
            caminhadas.Clear();
            retratosPorPonto.Clear();
            retratosPorOrigem.Clear();
            Personagens = Objetos = Superficies = Pisos = Retratos = Moradores = 0;
            aplicado = false;
        }

        private static void Destruir(UnityEngine.Object objeto)
        {
            if (objeto == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(objeto);
            else
                UnityEngine.Object.DestroyImmediate(objeto);
        }
    }
}

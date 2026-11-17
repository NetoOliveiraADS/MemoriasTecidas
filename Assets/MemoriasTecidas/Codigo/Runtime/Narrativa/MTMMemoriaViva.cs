using System;
using System.Collections.Generic;

namespace MemoriasTecidas.MundoV2
{
    // Dados das três mecânicas de "memória viva": foto antiga, entrevista e escuta.
    // Tudo vem de Resources/MemoriasTecidas/memoria_viva.json, para a equipe editar sem código.
    [Serializable]
    public sealed class MTMMemoriaVivaDados
    {
        public int versao;
        // Verdadeiro enquanto o conteúdo é só de exemplo: nada de memória viva aparece no jogo.
        public bool oculto;
        public MTMFotoViva[] fotos;
        public MTMEntrevistaViva[] entrevistas;
        public MTMSomVivo[] sons;
    }

    [Serializable]
    public sealed class MTMFotoViva
    {
        // "ancora" é o id de um ponto já existente na região; dx/dy deslocam o ponto de vista.
        public string id, regiao, ancora, nome, requer, imagem, fonte, legenda, instrucao, evento;
        public float dx, dy;
        public int minimo = 1;
        public string[] conclusao;
        public MTMDiferencaFoto[] diferencas;
    }

    [Serializable]
    public sealed class MTMDiferencaFoto
    {
        // x e y vão de 0 a 1 (y a partir do topo); raio é uma fração da largura da imagem.
        public string titulo, texto;
        public float x, y, raio = .06f;
    }

    [Serializable]
    public sealed class MTMEntrevistaViva
    {
        public string id, regiao, npc, requer, titulo, fonte, evento;
        public float dx, dy;
        public string[] abertura;
        public int limite = 3;
        public MTMPerguntaViva[] perguntas;
        public MTMPerguntaViva silencio;
        public MTMFechamentoViva[] fechamentos;
    }

    [Serializable]
    public sealed class MTMPerguntaViva
    {
        public string id, texto;
        public string[] resposta;
        // Quanto a resposta aumenta a confiança (0 a 3) e o que precisa ter acontecido antes.
        public int confianca, exigeConfianca;
        public string[] exige;
    }

    [Serializable]
    public sealed class MTMFechamentoViva
    {
        // Vale o primeiro fechamento cujas exigências foram cumpridas; o último é o padrão.
        public string[] exige, linhas;
        public int confiancaMinima;
        public string fio, resumo;
    }

    [Serializable]
    public sealed class MTMSomVivo
    {
        public string id, regiao, ancora, nome, legenda, requer, evento;
        public float dx, dy, raio = 12f, tempo = 2.5f;
        public string[] memoria;
    }

    // Uma conversa em andamento. Só vira marco salvo quando o jogador conclui.
    public sealed class MTMSessaoEntrevista
    {
        public const string IdSilencio = "silencio";
        public const int ConfiancaMaxima = 3;
        private readonly MTMEntrevistaViva dados;
        private readonly List<string> feitas = new List<string>();
        public int Confianca { get; private set; }
        public int Restantes { get; private set; }
        public IList<string> Feitas { get { return feitas; } }
        public MTMSessaoEntrevista(MTMEntrevistaViva entrevista)
        {
            if (entrevista == null)
                throw new ArgumentNullException("entrevista");
            dados = entrevista;
            Restantes = Math.Max(1, entrevista.limite);
        }

        public bool Perguntada(string id)
        {
            return feitas.Contains(id);
        }

        public bool Liberada(MTMPerguntaViva p)
        {
            if (p == null || Perguntada(p.id) || p.exigeConfianca > Confianca)
                return false;
            if (p.exige != null)
                foreach (string anterior in p.exige)
                    if (!Perguntada(anterior))
                        return false;
            return true;
        }

        public List<MTMPerguntaViva> Opcoes()
        {
            var lista = new List<MTMPerguntaViva>();
            if (Restantes <= 0 || dados.perguntas == null)
                return lista;
            foreach (var p in dados.perguntas)
                if (Liberada(p))
                    lista.Add(p);
            return lista;
        }

        // JsonUtility nunca deixa campos de classe nulos: um silêncio ausente chega como objeto vazio.
        public static bool TemSilencio(MTMEntrevistaViva e)
        {
            return e != null && e.silencio != null && !string.IsNullOrEmpty(e.silencio.texto);
        }

        public bool SilencioDisponivel
        {
            get
            {
                return Restantes > 0 && TemSilencio(dados) && !Perguntada(IdSilencio);
            }
        }

        // Quantas perguntas continuam bloqueadas só por falta de confiança.
        public int PedemMaisConfianca()
        {
            int total = 0;
            if (Restantes <= 0 || dados.perguntas == null)
                return 0;
            foreach (var p in dados.perguntas)
                if (!Perguntada(p.id) && p.exigeConfianca > Confianca && ExigenciasCumpridas(p))
                    total++;
            return total;
        }

        private bool ExigenciasCumpridas(MTMPerguntaViva p)
        {
            if (p.exige != null)
                foreach (string anterior in p.exige)
                    if (!Perguntada(anterior))
                        return false;
            return true;
        }

        public bool Encerrada
        {
            get
            {
                return Restantes <= 0 || (Opcoes().Count == 0 && !SilencioDisponivel);
            }
        }

        public bool Perguntar(string id)
        {
            var p = dados.perguntas == null ? null : Array.Find(dados.perguntas, x => x != null && x.id == id);
            if (p == null || !Liberada(p) || Restantes <= 0)
                return false;
            Registrar(p.id, p.confianca);
            return true;
        }

        public bool FicarEmSilencio()
        {
            if (!SilencioDisponivel)
                return false;
            Registrar(IdSilencio, dados.silencio.confianca);
            return true;
        }

        private void Registrar(string id, int ganho)
        {
            feitas.Add(id);
            Restantes--;
            Confianca = Math.Max(0, Math.Min(ConfiancaMaxima, Confianca + ganho));
        }

        public int IndiceFechamento()
        {
            var lista = dados.fechamentos;
            if (lista == null || lista.Length == 0)
                return -1;
            for (int i = 0; i < lista.Length; i++)
            {
                var f = lista[i];
                if (f == null || Confianca < f.confiancaMinima)
                    continue;
                bool ok = true;
                if (f.exige != null)
                    foreach (string id in f.exige)
                        if (!Perguntada(id))
                        {
                            ok = false;
                            break;
                        }

                if (ok)
                    return i;
            }

            return lista.Length - 1;
        }
    }

    public static class MTMMemoriaViva
    {
        public const string PrefixoEvento = "mv_";
        // Mesmas cores do primeiro fio. Devolve null para um nome desconhecido.
        public static string CorDoFio(string fio)
        {
            switch (fio)
            {
                case "ouro":
                    return "#D9AF68";
                case "creme":
                    return "#F4E9D5";
                case "terracota":
                    return "#B96C4D";
            }

            return null;
        }

        // Índice da diferença mais próxima do clique que ainda não foi achada; -1 se nenhuma.
        public static int DiferencaNoPonto(MTMFotoViva foto, float u, float v, float aspecto, ICollection<int> achadas)
        {
            if (foto == null || foto.diferencas == null || aspecto <= 0f)
                return -1;
            int melhor = -1;
            float menor = float.MaxValue;
            for (int i = 0; i < foto.diferencas.Length; i++)
            {
                var d = foto.diferencas[i];
                if (d == null || (achadas != null && achadas.Contains(i)))
                    continue;
                float dx = u - d.x, dy = (v - d.y) / aspecto;
                float distancia = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distancia <= d.raio && distancia < menor)
                {
                    menor = distancia;
                    melhor = i;
                }
            }

            return melhor;
        }

        // Próxima diferença ainda não achada, para a dica.
        public static int ProximaNaoAchada(MTMFotoViva foto, ICollection<int> achadas)
        {
            if (foto == null || foto.diferencas == null)
                return -1;
            for (int i = 0; i < foto.diferencas.Length; i++)
                if (achadas == null || !achadas.Contains(i))
                    return i;
            return -1;
        }

        // 0 fora do alcance, 1 colado na fonte.
        public static float Intensidade(float distancia, float raio)
        {
            if (raio <= 0f || distancia >= raio)
                return 0f;
            float t = 1f - Math.Max(0f, distancia) / raio;
            return t * t * (3f - 2f * t);
        }

        // Dentro do alcance o tempo corre pelo menos na metade da velocidade.
        public static float Acumular(float acumulado, float dt, float intensidade)
        {
            if (intensidade <= 0f || dt <= 0f)
                return acumulado;
            return acumulado + dt * (.5f + .5f * Math.Min(1f, intensidade));
        }

        public static void Validar(MTMMemoriaVivaDados dados)
        {
            if (dados == null || dados.versao != 1)
                throw new InvalidOperationException("memoria_viva.json inválido ou de outra versão.");
            var ids = new HashSet<string>();
            var eventos = new HashSet<string>();
            if (dados.fotos != null)
                foreach (var f in dados.fotos)
                {
                    Basico(f == null ? null : f.id, f == null ? null : f.evento, "foto", ids, eventos);
                    if (string.IsNullOrEmpty(f.regiao) || string.IsNullOrEmpty(f.ancora) || string.IsNullOrEmpty(f.imagem))
                        throw new InvalidOperationException("Foto " + f.id + " precisa de região, âncora e imagem.");
                    if (f.diferencas == null || f.diferencas.Length == 0)
                        throw new InvalidOperationException("Foto " + f.id + " sem diferenças.");
                    if (f.minimo < 1 || f.minimo > f.diferencas.Length)
                        throw new InvalidOperationException("Foto " + f.id + ": mínimo fora do intervalo.");
                    foreach (var d in f.diferencas)
                        if (d == null || d.x < 0f || d.x > 1f || d.y < 0f || d.y > 1f || d.raio <= 0f || string.IsNullOrEmpty(d.titulo) || string.IsNullOrEmpty(d.texto))
                            throw new InvalidOperationException("Foto " + f.id + ": diferença incompleta ou fora da imagem.");
                    if (f.conclusao == null || f.conclusao.Length == 0)
                        throw new InvalidOperationException("Foto " + f.id + " sem texto de conclusão.");
                }

            if (dados.entrevistas != null)
                foreach (var e in dados.entrevistas)
                {
                    Basico(e == null ? null : e.id, e == null ? null : e.evento, "entrevista", ids, eventos);
                    if (string.IsNullOrEmpty(e.regiao) || string.IsNullOrEmpty(e.npc) || e.abertura == null || e.abertura.Length == 0)
                        throw new InvalidOperationException("Entrevista " + e.id + " precisa de região, personagem e abertura.");
                    if (e.perguntas == null || e.perguntas.Length == 0 || e.limite < 1)
                        throw new InvalidOperationException("Entrevista " + e.id + " sem perguntas ou limite.");
                    var perguntas = new HashSet<string> { MTMSessaoEntrevista.IdSilencio };
                    foreach (var p in e.perguntas)
                    {
                        if (p == null || string.IsNullOrEmpty(p.id) || string.IsNullOrEmpty(p.texto) || p.resposta == null || p.resposta.Length == 0 || !perguntas.Add(p.id))
                            throw new InvalidOperationException("Entrevista " + e.id + ": pergunta incompleta ou repetida.");
                    }

                    foreach (var p in e.perguntas)
                        if (p.exige != null)
                            foreach (string anterior in p.exige)
                                if (anterior == p.id || !perguntas.Contains(anterior))
                                    throw new InvalidOperationException("Entrevista " + e.id + ": exigência desconhecida " + anterior + ".");
                    bool algumSilencio = e.silencio != null && (!string.IsNullOrEmpty(e.silencio.texto) || (e.silencio.resposta != null && e.silencio.resposta.Length > 0));
                    if (algumSilencio && (!MTMSessaoEntrevista.TemSilencio(e) || e.silencio.resposta == null || e.silencio.resposta.Length == 0))
                        throw new InvalidOperationException("Entrevista " + e.id + ": silêncio incompleto.");
                    if (e.fechamentos == null || e.fechamentos.Length == 0)
                        throw new InvalidOperationException("Entrevista " + e.id + " sem fechamentos.");
                    foreach (var f in e.fechamentos)
                        if (f == null || f.linhas == null || f.linhas.Length == 0 || string.IsNullOrEmpty(f.fio) || string.IsNullOrEmpty(f.resumo))
                            throw new InvalidOperationException("Entrevista " + e.id + ": fechamento incompleto.");
                        else if (CorDoFio(f.fio) == null)
                            throw new InvalidOperationException("Entrevista " + e.id + ": fio desconhecido '" + f.fio + "' (use ouro, creme ou terracota).");
                    var ultimo = e.fechamentos[e.fechamentos.Length - 1];
                    if ((ultimo.exige != null && ultimo.exige.Length > 0) || ultimo.confiancaMinima > 0)
                        throw new InvalidOperationException("Entrevista " + e.id + ": o último fechamento deve ser o padrão, sem exigências.");
                }

            if (dados.sons != null)
                foreach (var s in dados.sons)
                {
                    Basico(s == null ? null : s.id, s == null ? null : s.evento, "som", ids, eventos);
                    if (string.IsNullOrEmpty(s.regiao) || string.IsNullOrEmpty(s.ancora) || string.IsNullOrEmpty(s.nome) || string.IsNullOrEmpty(s.legenda))
                        throw new InvalidOperationException("Som " + s.id + " precisa de região, âncora, nome e legenda.");
                    if (s.raio <= 0f || s.tempo <= 0f)
                        throw new InvalidOperationException("Som " + s.id + ": raio e tempo devem ser positivos.");
                }
        }

        // Eventos próprios começam com mv_ para nunca atrapalhar a campanha (r24_...).
        private static void Basico(string id, string evento, string tipo, ISet<string> ids, ISet<string> eventos)
        {
            if (string.IsNullOrEmpty(id) || !ids.Add(id))
                throw new InvalidOperationException("Id de " + tipo + " vazio ou repetido: " + id);
            if (string.IsNullOrEmpty(evento) || !evento.StartsWith(PrefixoEvento, StringComparison.Ordinal) || !eventos.Add(evento))
                throw new InvalidOperationException("Evento de " + tipo + " " + id + " deve começar com mv_ e ser único.");
        }
    }
}

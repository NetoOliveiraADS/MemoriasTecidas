using System;
using System.Collections.Generic;

namespace MemoriasTecidas.MundoV2
{
    // Regras dos confrontos com as lendas. Sem dependência do Unity, para poder testar fora dele.
    // Posições são relativas ao centro da arena (0, 0).

    [Serializable]
    public sealed class MTMConfrontosDados
    {
        public int versao;
        public MTMConfrontoDados[] confrontos;
    }

    [Serializable]
    public sealed class MTMPontoArena
    {
        public float x, y;
    }

    [Serializable]
    public sealed class MTMConfrontoDados
    {
        // tipo: "luta" (vida contra vida) ou "encanto" (esquiva, cerco e escuta).
        public string id, tipo, regiao, nome, requer, evento, imagem, chamada, resumo, fio, derrota;
        // modelo "aparicao": usa o personagem que a região já tem para as aparições da noite.
        public string modelo;
        public float escala = 1f;
        // Folha de animação em "imagem": colunas = quadros da corrida; linhas = 1 (sem direção)
        // ou 8 direções na ordem baixo, baixo-esquerda, esquerda, cima-esquerda, cima,
        // cima-direita, direita, baixo-direita. alturaQuadro = altura de um quadro no mundo.
        public int colunas = 1, linhas = 1;
        public float fps = 12f, alturaQuadro;
        // Opcionais: pose da lenda parada (escuta) e busto para o diálogo, em Resources.
        public string imagemParado, retrato;
        // Folhas extras, na mesma grade de "imagem": investida (correndo para atacar) e
        // preparo (o aviso antes da investida, tocado uma vez em sincronia com a faixa vermelha).
        public string imagemInvestida, imagemPreparo;
        // Só nas lutas: arte da arma de Neto em Resources e o nome dela para o HUD.
        public string arma, nomeArma;
        // Opcional: JSON (Resources) da folha de Neto já segurando a arma, nas mesmas 32 poses.
        public string netoArmado;
        // Falas mostradas uma vez, quando o confronto fica disponível, para chamar o jogador.
        public string[] convite;
        // Centro da arena na região e raio em que o confronto acontece.
        public float x, y, raio = 7f;
        public bool noite = true;
        public string[] abertura, vitoria;
        public int vidaJogador = 3, vidaInimigo = 8;
        public float velocidade = 2.6f, velocidadeInvestida = 11f, aviso = .9f, cansaco = 1.6f, alcanceGolpe = 1.9f;
        // Só para "encanto".
        public int esquivas = 4;
        public MTMPontoArena[] fogueiras;
        public bool ordemInversa;
        public float escuta = 3f;
        public string avisoCerco, avisoEscuta;

        public MTMConfrontoDados Copia()
        {
            return (MTMConfrontoDados)MemberwiseClone();
        }
    }

    public enum MTMFaseConfronto
    {
        Luta,
        Esquiva,
        Cerco,
        Escuta,
        Vitoria,
        Derrota
    }

    public enum MTMEstadoInimigo
    {
        Rondando,
        Preparando,
        Investindo,
        Cansado,
        Parado
    }

    [Flags]
    public enum MTMSinalConfronto
    {
        Nenhum = 0,
        Preparou = 1,
        Investiu = 2,
        JogadorAtingido = 4,
        Esquivou = 8,
        Cansou = 16,
        FaseMudou = 32,
        Perdeu = 64
    }

    public enum MTMResultadoFogueira
    {
        Nada,
        Certa,
        Errada,
        Completa
    }

    public sealed class MTMSessaoConfronto
    {
        public const float RaioAcerto = 1.05f;
        public const float AlcanceInvestida = 9f;
        public const float DistanciaRonda = 4.6f;
        public const float AlcanceEscuta = 4.5f;
        public const float Invulnerabilidade = 1.2f;
        public const float RecargaGolpe = .45f;
        // Até aqui o inimigo ainda mira; depois a direção fica travada e dá para sair da frente.
        public const float FracaoMira = .65f;

        private readonly MTMConfrontoDados dados;
        private readonly List<int> acesas = new List<int>();
        private float tempoEstado, percorrido, invulneravel, recarga, avisoAtual;
        private bool atingiuNestaInvestida, emendou;

        public MTMFaseConfronto Fase { get; private set; }
        public MTMEstadoInimigo Estado { get; private set; }
        public float Ix { get; private set; }
        public float Iy { get; private set; }
        public float DirX { get; private set; }
        public float DirY { get; private set; }
        public int VidaJogador { get; private set; }
        public int VidaInimigo { get; private set; }
        public int EsquivasFeitas { get; private set; }
        public float EscutaAcumulada { get; private set; }
        public IList<int> Acesas { get { return acesas; } }
        public bool Encerrado { get { return Fase == MTMFaseConfronto.Vitoria || Fase == MTMFaseConfronto.Derrota; } }
        public bool JogadorInvulneravel { get { return invulneravel > 0f; } }
        public bool Enfurecido { get { return Fase == MTMFaseConfronto.Luta && VidaInimigo * 2 <= dados.vidaInimigo; } }
        // 0 a 1: quanto do aviso da investida já passou.
        public float ProgressoAviso { get { return Estado == MTMEstadoInimigo.Preparando && avisoAtual > 0f ? Math.Min(1f, tempoEstado / avisoAtual) : 0f; } }
        public bool MiraTravada { get { return Estado == MTMEstadoInimigo.Preparando && ProgressoAviso >= FracaoMira; } }

        public MTMSessaoConfronto(MTMConfrontoDados dados)
        {
            if (dados == null)
                throw new ArgumentNullException("dados");
            this.dados = dados;
            Fase = dados.tipo == "luta" ? MTMFaseConfronto.Luta : MTMFaseConfronto.Esquiva;
            Estado = MTMEstadoInimigo.Rondando;
            VidaJogador = Math.Max(1, dados.vidaJogador);
            VidaInimigo = Math.Max(1, dados.vidaInimigo);
            Ix = 0f;
            Iy = Math.Min(dados.raio - 1f, 3.5f);
            DirY = -1f;
        }

        private float IntervaloRonda
        {
            get
            {
                if (Fase == MTMFaseConfronto.Cerco)
                    return 2.6f;
                return Enfurecido ? 1.0f : 1.7f;
            }
        }

        public MTMSinalConfronto Avancar(float dt, float px, float py)
        {
            if (Encerrado || dt <= 0f)
                return MTMSinalConfronto.Nenhum;
            var sinais = MTMSinalConfronto.Nenhum;
            invulneravel = Math.Max(0f, invulneravel - dt);
            recarga = Math.Max(0f, recarga - dt);
            tempoEstado += dt;
            switch (Estado)
            {
                case MTMEstadoInimigo.Rondando:
                    Rondar(dt, px, py);
                    if (tempoEstado >= IntervaloRonda)
                    {
                        Preparar(px, py, dados.aviso);
                        sinais |= MTMSinalConfronto.Preparou;
                    }

                    break;
                case MTMEstadoInimigo.Preparando:
                    if (tempoEstado < avisoAtual * FracaoMira)
                        Mirar(px, py);
                    if (tempoEstado >= avisoAtual)
                    {
                        Estado = MTMEstadoInimigo.Investindo;
                        tempoEstado = 0f;
                        percorrido = 0f;
                        atingiuNestaInvestida = false;
                        sinais |= MTMSinalConfronto.Investiu;
                    }

                    break;
                case MTMEstadoInimigo.Investindo:
                    sinais |= Investir(dt, px, py);
                    break;
                case MTMEstadoInimigo.Cansado:
                    if (tempoEstado >= dados.cansaco)
                    {
                        Estado = MTMEstadoInimigo.Rondando;
                        tempoEstado = 0f;
                    }

                    break;
            }

            return sinais;
        }

        private void Preparar(float px, float py, float aviso)
        {
            Estado = MTMEstadoInimigo.Preparando;
            tempoEstado = 0f;
            avisoAtual = Math.Max(.2f, aviso);
            Mirar(px, py);
        }

        private void Mirar(float px, float py)
        {
            float dx = px - Ix, dy = py - Iy;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < .001f)
                return;
            DirX = dx / d;
            DirY = dy / d;
        }

        // Circula o jogador mantendo distância, para não ficar parado ao alcance do golpe.
        private void Rondar(float dt, float px, float py)
        {
            float dx = Ix - px, dy = Iy - py;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < .001f)
            {
                dx = 0f;
                dy = 1f;
                d = 1f;
            }

            dx /= d;
            dy /= d;
            // Gira um pouco em torno do jogador e procura a distância de ronda.
            const float giro = .9f;
            float cos = (float)Math.Cos(giro * dt), sen = (float)Math.Sin(giro * dt);
            float rx = dx * cos - dy * sen, ry = dx * sen + dy * cos;
            float alvoX = px + rx * DistanciaRonda, alvoY = py + ry * DistanciaRonda;
            float mx = alvoX - Ix, my = alvoY - Iy;
            float m = (float)Math.Sqrt(mx * mx + my * my);
            float passo = dados.velocidade * dt;
            if (m > passo)
            {
                mx = mx / m * passo;
                my = my / m * passo;
            }

            Ix += mx;
            Iy += my;
            ConterNaArena();
        }

        private bool ConterNaArena()
        {
            float d = (float)Math.Sqrt(Ix * Ix + Iy * Iy);
            if (d <= dados.raio)
                return false;
            Ix = Ix / d * dados.raio;
            Iy = Iy / d * dados.raio;
            return true;
        }

        private MTMSinalConfronto Investir(float dt, float px, float py)
        {
            var sinais = MTMSinalConfronto.Nenhum;
            float passo = dados.velocidadeInvestida * dt;
            // Passos curtos para a investida não atravessar o jogador entre dois quadros.
            int partes = Math.Max(1, (int)Math.Ceiling(passo / .4f));
            bool bateuNaBorda = false;
            for (int i = 0; i < partes && !bateuNaBorda; i++)
            {
                Ix += DirX * passo / partes;
                Iy += DirY * passo / partes;
                percorrido += passo / partes;
                bateuNaBorda = ConterNaArena();
                float dx = px - Ix, dy = py - Iy;
                if (!atingiuNestaInvestida && invulneravel <= 0f && dx * dx + dy * dy <= RaioAcerto * RaioAcerto)
                {
                    atingiuNestaInvestida = true;
                    invulneravel = Invulnerabilidade;
                    VidaJogador--;
                    sinais |= MTMSinalConfronto.JogadorAtingido;
                    if (VidaJogador <= 0)
                    {
                        Fase = MTMFaseConfronto.Derrota;
                        Estado = MTMEstadoInimigo.Parado;
                        return sinais | MTMSinalConfronto.Perdeu;
                    }
                }
            }

            if (percorrido < AlcanceInvestida && !bateuNaBorda)
                return sinais;
            if (!atingiuNestaInvestida)
            {
                sinais |= MTMSinalConfronto.Esquivou;
                if (Fase == MTMFaseConfronto.Esquiva)
                {
                    EsquivasFeitas++;
                    if (EsquivasFeitas >= dados.esquivas)
                    {
                        sinais |= MTMSinalConfronto.FaseMudou;
                        if (dados.fogueiras != null && dados.fogueiras.Length > 0)
                            Fase = MTMFaseConfronto.Cerco;
                        else
                        {
                            EntrarNaEscuta();
                            return sinais;
                        }
                    }
                }
            }

            // Enfurecida, ela emenda uma segunda investida com aviso mais curto.
            if (Enfurecido && !emendou)
            {
                emendou = true;
                Preparar(px, py, dados.aviso * .6f);
                return sinais | MTMSinalConfronto.Preparou;
            }

            emendou = false;
            Estado = MTMEstadoInimigo.Cansado;
            tempoEstado = 0f;
            return sinais | MTMSinalConfronto.Cansou;
        }

        private void EntrarNaEscuta()
        {
            Fase = MTMFaseConfronto.Escuta;
            Estado = MTMEstadoInimigo.Parado;
            tempoEstado = 0f;
            Ix = 0f;
            Iy = 0f;
        }

        // 1 acertou; 0 errou (longe, de costas ou em recarga); -1 ela aparou, porque não estava cansada.
        // Só o cansaço depois da investida abre a guarda: apertar o botão sem esquivar não vence a luta.
        public int Golpear(float px, float py, float frenteX, float frenteY)
        {
            if (Fase != MTMFaseConfronto.Luta || recarga > 0f)
                return 0;
            recarga = RecargaGolpe;
            if (Estado == MTMEstadoInimigo.Investindo)
                return 0;
            float dx = Ix - px, dy = Iy - py;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d > dados.alcanceGolpe)
                return 0;
            float f = (float)Math.Sqrt(frenteX * frenteX + frenteY * frenteY);
            if (d > .3f && f > .001f && (dx * frenteX + dy * frenteY) / (d * f) < .2f)
                return 0;
            if (Estado != MTMEstadoInimigo.Cansado)
            {
                // Aparado: ela responde com uma investida de aviso mais curto.
                if (Estado == MTMEstadoInimigo.Rondando)
                    Preparar(px, py, dados.aviso * .75f);
                return -1;
            }

            VidaInimigo--;
            if (VidaInimigo <= 0)
            {
                VidaInimigo = 0;
                Fase = MTMFaseConfronto.Vitoria;
                Estado = MTMEstadoInimigo.Parado;
            }

            return 1;
        }

        public bool PodeGolpear { get { return Fase == MTMFaseConfronto.Luta && recarga <= 0f; } }

        // Qual fogueira deve ser acesa agora, ou -1 fora do cerco.
        public int ProximaFogueira
        {
            get
            {
                if (Fase != MTMFaseConfronto.Cerco || dados.fogueiras == null)
                    return -1;
                return dados.ordemInversa ? dados.fogueiras.Length - 1 - acesas.Count : acesas.Count;
            }
        }

        public MTMResultadoFogueira AcenderFogueira(int indice)
        {
            if (Fase != MTMFaseConfronto.Cerco || dados.fogueiras == null || indice < 0 || indice >= dados.fogueiras.Length || acesas.Contains(indice))
                return MTMResultadoFogueira.Nada;
            if (indice != ProximaFogueira)
            {
                acesas.Clear();
                return MTMResultadoFogueira.Errada;
            }

            acesas.Add(indice);
            if (acesas.Count < dados.fogueiras.Length)
                return MTMResultadoFogueira.Certa;
            EntrarNaEscuta();
            return MTMResultadoFogueira.Completa;
        }

        // Verdadeiro quando a escuta se completa e o confronto termina.
        public bool Escutar(float dt, float px, float py)
        {
            if (Fase != MTMFaseConfronto.Escuta || dt <= 0f)
                return false;
            float dx = px - Ix, dy = py - Iy;
            if (dx * dx + dy * dy > AlcanceEscuta * AlcanceEscuta)
                return false;
            EscutaAcumulada += dt;
            if (EscutaAcumulada < dados.escuta)
                return false;
            Fase = MTMFaseConfronto.Vitoria;
            return true;
        }
    }

    public static class MTMConfrontos
    {
        public const string PrefixoEvento = "cf_";
        public const string PrefixoPonto = "cfp_";

        // Linha da folha de 8 direções para um movimento (dx, dy), com y para cima.
        public static int LinhaDaDirecao(float dx, float dy)
        {
            if (dx * dx + dy * dy < 1e-8f)
                return 0;
            double angulo = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            int linha = (int)Math.Round((270.0 - angulo) / 45.0);
            return ((linha % 8) + 8) % 8;
        }

        // Cada derrota deixa a próxima tentativa um pouco mais leve, para ninguém ficar preso.
        public static MTMConfrontoDados ComAjuda(MTMConfrontoDados dados, int derrotas)
        {
            var c = dados.Copia();
            int n = Math.Max(0, Math.Min(derrotas, 3));
            c.vidaJogador = dados.vidaJogador + Math.Min(n, 2);
            c.vidaInimigo = Math.Max((dados.vidaInimigo + 1) / 2, dados.vidaInimigo - n);
            c.aviso = dados.aviso + .1f * n;
            if (c.tipo == "encanto")
                c.esquivas = Math.Max(2, dados.esquivas - n);
            return c;
        }

        public static void Validar(MTMConfrontosDados dados)
        {
            if (dados == null || dados.confrontos == null || dados.confrontos.Length == 0)
                throw new InvalidOperationException("confrontos.json sem confrontos.");
            var ids = new HashSet<string>();
            var eventos = new HashSet<string>();
            foreach (var c in dados.confrontos)
            {
                if (c == null || string.IsNullOrEmpty(c.id) || !ids.Add(c.id))
                    throw new InvalidOperationException("Id de confronto vazio ou repetido: " + (c == null ? "" : c.id));
                if (string.IsNullOrEmpty(c.evento) || !c.evento.StartsWith(PrefixoEvento, StringComparison.Ordinal) || !eventos.Add(c.evento))
                    throw new InvalidOperationException("Evento do confronto " + c.id + " deve começar com cf_ e ser único.");
                if (c.tipo != "luta" && c.tipo != "encanto")
                    throw new InvalidOperationException("Confronto " + c.id + ": tipo desconhecido '" + c.tipo + "' (use luta ou encanto).");
                if (string.IsNullOrEmpty(c.regiao) || string.IsNullOrEmpty(c.nome))
                    throw new InvalidOperationException("Confronto " + c.id + " precisa de região e nome.");
                if (c.abertura == null || c.abertura.Length == 0 || c.vitoria == null || c.vitoria.Length == 0)
                    throw new InvalidOperationException("Confronto " + c.id + " precisa de falas de abertura e de vitória.");
                if (c.raio < 4f)
                    throw new InvalidOperationException("Confronto " + c.id + ": a arena precisa de raio 4 ou maior.");
                if (c.vidaJogador < 1 || c.vidaInimigo < 1)
                    throw new InvalidOperationException("Confronto " + c.id + ": as vidas devem ser 1 ou mais.");
                if (c.velocidade <= 0f || c.velocidadeInvestida <= 0f || c.aviso < .3f || c.cansaco <= 0f || c.alcanceGolpe <= 0f)
                    throw new InvalidOperationException("Confronto " + c.id + ": velocidades, aviso (mínimo 0,3 s), cansaço e alcance devem ser positivos.");
                if (c.colunas < 1 || (c.linhas != 1 && c.linhas != 8) || c.fps <= 0f || c.alturaQuadro < 0f)
                    throw new InvalidOperationException("Confronto " + c.id + ": folha de animação com colunas 1 ou mais, linhas 1 ou 8, fps positivo e alturaQuadro não negativa.");
                if (!string.IsNullOrEmpty(c.fio) && c.fio != "ouro" && c.fio != "creme" && c.fio != "terracota")
                    throw new InvalidOperationException("Confronto " + c.id + ": fio desconhecido '" + c.fio + "' (use ouro, creme ou terracota).");
                if (c.tipo != "encanto")
                    continue;
                if (c.esquivas < 1 || c.escuta <= 0f)
                    throw new InvalidOperationException("Confronto " + c.id + ": esquivas e tempo de escuta devem ser positivos.");
                if (c.fogueiras == null)
                    continue;
                if (c.fogueiras.Length == 1)
                    throw new InvalidOperationException("Confronto " + c.id + ": use nenhuma fogueira ou pelo menos duas.");
                foreach (var f in c.fogueiras)
                    if (f == null || f.x * f.x + f.y * f.y > c.raio * c.raio)
                        throw new InvalidOperationException("Confronto " + c.id + ": fogueira fora da arena.");
            }
        }
    }
}

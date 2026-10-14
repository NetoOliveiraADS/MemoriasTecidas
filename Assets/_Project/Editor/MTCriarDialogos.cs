#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MTCriarDialogos
{
    [MenuItem("Memorias Tecidas/Criar dialogos de exemplo")]
    public static void CreateExamples()
    {
        const string folder = "Assets/_Project/Data/Dialogues";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        Create(folder, "AL01_AndreLucio", "lenda_mulher_cavalo", "André Lúcio", "Lenda | roteiro adaptado para o jogo; não é transcrição de André.", "Pesquisas e ideia de jogo.pdf, pp. 5 e 8. Revisão local pendente.", new string[] { "Você já ouviu o causo da Mulher Cavalo? É uma das histórias que circulam nas conversas sobre a nossa região.", "Numa versão desse causo, uma figura com cabeça de cavalo aparece nos caminhos à noite. Os detalhes mudam conforme quem conta.", "Guarde também a origem do relato. Ouvir uma lenda ajuda a conhecer a memória de um lugar, sem transformar o causo em prova do que aconteceu." });
        Create(folder, "H01_Gerardinho", "mestre_gerardinho", "Mestre Gerardinho", "Cena de memória | narração adaptada em terceira pessoa.", "Jaguaruanenses ilustres - Memórias de Jaguaruana.pdf, entrevista de José Wilson Freitas.", new string[] { "Gerardo Correia Lima é lembrado como Mestre Gerardinho. No depoimento de José Wilson Freitas, aparecem as aulas realizadas na casa do professor. Este encontro convida a pensar nos lugares em que a educação acontecia e nas pessoas que mantiveram essa memória." });
        Create(folder, "H02_PadreDuceu", "padre_duceu", "Padre Ducéu", "Cena de memória | síntese da pesquisa a revisar.", "Pesquisas e ideia de jogo.pdf, p. 3.", new string[] { "Raimundo Sales Façanha, conhecido como Padre Ducéu, aparece na pesquisa como uma referência religiosa e comunitária de Jaguaruana. Este ponto da mostra reúne memórias sobre sua presença na vida da cidade. Os episódios específicos precisam ser conferidos com suas fontes antes da publicação." });
        Create(folder, "H03_IvoneteMaia", "ivonete_maia", "Ivonete Maia", "Cena de memória | texto biográfico adaptado.", "Jaguaruanenses ilustres - Memórias de Jaguaruana.pdf; Pesquisas e ideia de jogo.pdf, pp. 3 e 4.", new string[] { "Ivonete Maia é apresentada no acervo como jornalista, escritora e educadora de Jaguaruana. Sua presença na mostra aproxima o jogador da comunicação e do registro de histórias. O jornalismo também nos convida a perguntar de onde vem uma informação e quem pode ajudar a confirmá-la." });
        Create(folder, "H04_RaimundoJaguaribe", "raimundo_jaguaribe", "Raimundo Francisco Freitas Jaguaribe", "Cena de memória | relato familiar identificado.", "Jaguaruanenses ilustres - Memórias de Jaguaruana.pdf, texto de Vicência Jaguaribe.", new string[] { "No texto de Vicência Jaguaribe, Raimundo Francisco Freitas Jaguaribe é lembrado por sua relação com livros, música e educação. A autora associa a ele iniciativas culturais da cidade. Esta apresentação identifica a origem familiar do relato e convida a conhecer também os registros dessas iniciativas." });
        Create(folder, "H05_EmiliaFreitas", "emilia_freitas", "Emília Freitas", "Cena de memória | síntese literária a revisar.", "Histórias e Lendas de Jaguaruana.pdf, p. 1; Pesquisas e ideia de jogo.pdf, p. 6.", new string[] { "Emília Freitas é autora de A Rainha do Ignoto. A pesquisa reúne sua ligação com a região e a presença de mulheres que agem e lideram em sua narrativa. Este encontro aproxima literatura e imaginação, sem usar o título impreciso de primeiro escritor do Brasil." });
        Create(folder, "R01_Origem", "praca_origem", "Memória da praça", "Registro de origem | síntese da pesquisa a revisar.", "Pesquisas e ideia de jogo.pdf, p. 1.", new string[] { "A pesquisa liga a formação do povoado à doação de terras de Dona Feliciana Soares da Costa, viúva de Simão de Góis, para uma capela dedicada a Sant Ana. Os dois nomes entram nesta memória de outra época. Datas e detalhes da escritura devem acompanhar a fonte usada pela equipe." });
        Create(folder, "R02_Trilha", "trilha_serra", "Memória da trilha", "Visita representada | cenário adaptado.", "Ideias para o jogo.pdf, pp. 1 e 2; pesquisa local da equipe.", new string[] { "Esta trilha foi inspirada na Serra Dantas. O mapa aproxima lugares para criar um percurso curto e não reproduz as distâncias reais. A apresentação final do cruzeiro e das práticas de visita deve trazer a origem das informações e a revisão de pessoas que conhecem o local." });
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Diálogos criados. Arquivos existentes foram preservados em Assets/_Project/Data/Dialogues.");
    }
    private static void Create(string folder, string fileName, string id, string name, string context, string source, string[] lines)
    {
        string path = folder + "/" + fileName + ".asset";
        if (File.Exists(path)) return;
        MTDialogueAsset data = ScriptableObject.CreateInstance<MTDialogueAsset>();
        data.id = id; data.characterName = name; data.context = context;
        data.source = source; data.lines = lines;
        AssetDatabase.CreateAsset(data, path);
    }
}
#endif

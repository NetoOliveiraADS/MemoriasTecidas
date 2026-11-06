using System.Text.Json;
using MemoriasTecidas.MundoV2;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Executar na raiz do repositório. Usa as regras de produção, sem cópias ou mocks.
var raiz = Path.GetFullPath(args.Length > 0 ? args[0] : Directory.GetCurrentDirectory());
var recursos = Path.Combine(raiz, "Assets/MemoriasTecidas/Resources/MemoriasTecidas");
var json = new JsonSerializerOptions { IncludeFields = true };
var abertura = JsonSerializer.Deserialize<MTMAberturaMemorias>(File.ReadAllText(Path.Combine(recursos, "abertura_memorias.json")), json);
var trama = JsonSerializer.Deserialize<MTMTrama>(File.ReadAllText(Path.Combine(recursos, "trama_01.json")), json);
int aprovados = 0, falhas = 0;

void Exigir(bool valor, string mensagem)
{
    if (!valor) throw new InvalidOperationException(mensagem);
}
void Teste(string nome, Action verificar)
{
    try { verificar(); aprovados++; Console.WriteLine("PASS " + nome); }
    catch (Exception erro) { falhas++; Console.Error.WriteLine("FAIL " + nome + ": " + erro.Message); }
}
void Passar(MTMSessaoTecido tear, int cor)
{
    tear.SelecionarCor(cor);
    Exigir(tear.IniciarPassagem(), "A passagem não iniciou.");
    Exigir(tear.Percorrer(1f), "A passagem não concluiu.");
}

Teste("Selecionar uma cor prepara o fio sem preencher a amostra", () =>
{
    var tear = new MTMSessaoTecido();
    Exigir(!tear.IniciarPassagem(), "Não deve tecer sem fio.");
    tear.SelecionarCor(-1);
    tear.SelecionarCor(3);
    Exigir(!tear.PodePassar, "Índice inválido aceito.");
    tear.SelecionarCor(1);
    Exigir(tear.Passagens.Count == 0 && tear.PodePassar, "Selecionar concluiu a linha.");
});
Teste("Cancelar um gesto preserva as passagens completas", () =>
{
    var tear = new MTMSessaoTecido();
    Passar(tear, 0);
    tear.SelecionarCor(2);
    tear.IniciarPassagem();
    Exigir(!tear.Percorrer(.6f), "Gesto parcial concluiu.");
    tear.Cancelar();
    Exigir(tear.Passagens.SequenceEqual(new[] { 0 }) && !tear.Passando, "Cancelar alterou a amostra.");
});
Teste("Uma passagem conclui uma vez e não muda de cor durante o gesto", () =>
{
    var tear = new MTMSessaoTecido();
    tear.SelecionarCor(1);
    tear.IniciarPassagem();
    tear.SelecionarCor(2);
    Exigir(!tear.IniciarPassagem(), "Gesto duplicado.");
    Exigir(!tear.Percorrer(float.NaN) && !tear.Percorrer(float.PositiveInfinity), "Progresso inválido.");
    Exigir(tear.Percorrer(1f) && !tear.Percorrer(1f), "Conclusão duplicada.");
    Exigir(tear.Passagens.SequenceEqual(new[] { 1 }), "Cor mudou durante o gesto.");
});
Teste("O sentido alterna e aceita chegar além da borda", () =>
{
    Exigir(MTMSessaoTecido.ProgressoDoGesto(100, 100, 500, 0) == 0, "Início da primeira linha.");
    Exigir(MTMSessaoTecido.ProgressoDoGesto(600, 100, 500, 0) == 1, "Final da primeira linha.");
    Exigir(MTMSessaoTecido.ProgressoDoGesto(600, 100, 500, 1) == 0, "Início da segunda linha.");
    Exigir(MTMSessaoTecido.ProgressoDoGesto(100, 100, 500, 1) == 1, "Final da segunda linha.");
    Exigir(MTMSessaoTecido.ProgressoDoGesto(-200, 100, 500, 0) == 0, "Arrasto contrário.");
    Exigir(MTMSessaoTecido.ProgressoDoGesto(900, 100, 500, 0) == 1, "Arrasto além da borda.");
    Exigir(MTMSessaoTecido.ProgressoDoGesto(100, 0, 0, 1) == 0, "Largura zero.");
    Exigir(MTMSessaoTecido.ProgressoDoGesto(100, 0, float.NaN, 1) == 0, "Largura inválida.");
});
Teste("Corrigir substitui somente a linha escolhida", () =>
{
    var tear = new MTMSessaoTecido();
    tear.Restaurar(new[] { 0, 2, 0, 2 });
    Exigir(tear.EditarLinha(1), "Linha existente não editável.");
    tear.SelecionarCor(1);
    tear.IniciarPassagem();
    tear.Percorrer(.5f);
    tear.Cancelar();
    Exigir(tear.Passagens.SequenceEqual(new[] { 0, 2, 0, 2 }), "Edição interrompida perdeu linha original.");
    Passar(tear, 1);
    Exigir(tear.Passagens.SequenceEqual(abertura.padrao), "Correção alterou outras linhas.");
    Exigir(!tear.PodePassar && !tear.EditarLinha(4), "Quinta linha foi permitida.");
    Exigir(MTMPrimeiroFio.RespostaCorreta(abertura, tear.Passagens), "Amostra corrigida não confere.");
});
Teste("Desfazer cancela o gesto e permite refazer a última linha", () =>
{
    var tear = new MTMSessaoTecido();
    tear.Restaurar(new[] { 0, 1 });
    tear.EditarLinha(0);
    tear.IniciarPassagem();
    tear.Percorrer(.5f);
    tear.Desfazer();
    Exigir(tear.Passagens.SequenceEqual(new[] { 0 }) && !tear.Passando && tear.LinhaEmEdicao == -1, "Estado pendente após desfazer.");
    Passar(tear, 1);
    Exigir(tear.Passagens.SequenceEqual(new[] { 0, 1 }), "Refazer não funcionou.");
});
Teste("Retomar copia o save e protege a lista interna", () =>
{
    var salvas = new List<int> { 0, 1 };
    var tear = new MTMSessaoTecido();
    tear.Restaurar(salvas);
    salvas[0] = 2;
    Exigir(tear.Passagens.SequenceEqual(new[] { 0, 1 }), "Sessão compartilha lista do save.");
    bool recusou = false;
    try { tear.Passagens.Add(2); } catch (NotSupportedException) { recusou = true; }
    Exigir(recusou, "Mutação externa permitida.");
    Passar(tear, 0);
    Passar(tear, 2);
    Exigir(MTMPrimeiroFio.RespostaCorreta(abertura, tear.Passagens), "Retomada não concluiu padrão.");
});
Teste("Dados ausentes ou inválidos reabrem vazios; recomeçar limpa", () =>
{
    var tear = new MTMSessaoTecido();
    foreach (IList<int> salvas in new IList<int>[] { null, new[] { 0, 8 }, new[] { 0, 1, 0, 2, 0 } })
    {
        tear.Restaurar(salvas);
        Exigir(tear.Passagens.Count == 0 && !tear.Passando, "Dados inválidos parcialmente aceitos.");
    }
    tear.Restaurar(abertura.padrao);
    tear.Reiniciar();
    Exigir(tear.Passagens.Count == 0 && tear.CorSelecionada == -1, "Recomeçar não limpou.");
});
Teste("Dados reais mantêm as referências e o diálogo integral", () =>
{
    MTMPrimeiroFio.Validar(abertura, trama);
    Exigir(trama.missoes.Length == 24 && abertura.motivos.Length == 9, "Campanha perdeu conteúdo.");
    var encontro = trama.missoes.SelectMany(m => m.etapas).Single(e => e.eventos.Contains(abertura.eventoEncontro));
    Exigir(encontro.falas.Length >= 7, "Diálogo integral não está disponível ao caderno.");
});
Teste("Tecelagem exige a sequência, não repete prêmio nem gasta materiais", () =>
{
    var estado = new MTMEstado { algodao = 6, fio = 3, corda = 1, pano = 1, punho = 1, enfeite = 1, cera = 1 };
    Exigir(!MTMPrimeiroFio.Avancar(estado, abertura, MTMPrimeiroFio.Teceu, abertura.padrao), "Saltou o encontro.");
    estado.Concluir(abertura.eventoEncontro);
    Exigir(MTMPrimeiroFio.Avancar(estado, abertura, MTMPrimeiroFio.Convite), "Convite.");
    Exigir(MTMPrimeiroFio.Avancar(estado, abertura, MTMPrimeiroFio.Observou), "Observação.");
    Exigir(!MTMPrimeiroFio.Avancar(estado, abertura, MTMPrimeiroFio.Teceu, new[] { 2, 2, 2, 2 }), "Padrão errado.");
    Exigir(MTMPrimeiroFio.Avancar(estado, abertura, MTMPrimeiroFio.Teceu, abertura.padrao), "Tecelagem.");
    Exigir(!MTMPrimeiroFio.Avancar(estado, abertura, MTMPrimeiroFio.Teceu, abertura.padrao), "Repetiu tecelagem.");
    Exigir(MTMPrimeiroFio.Avancar(estado, abertura, MTMPrimeiroFio.Memoria), "Reencontro.");
    Exigir(!MTMPrimeiroFio.Avancar(estado, abertura, MTMPrimeiroFio.Memoria), "Repetiu memória.");
    Exigir(estado.algodao == 6 && estado.fio == 3 && estado.corda == 1 && estado.pano == 1 &&
        estado.punho == 1 && estado.enfeite == 1 && estado.cera == 1 && !estado.rede, "Materiais ou rede foram alterados.");
});
Teste("Modelo do save aceita campo ausente e mantém progresso ao retomar", () =>
{
    // Teste portátil do modelo; JsonUtility precisa de conferência no Play.
    const string antigo = "{\"versao\":1,\"marcos\":[\"r24_m03_01\"],\"fio\":3}";
    var estado = JsonSerializer.Deserialize<MTMEstado>(antigo, json);
    var tear = new MTMSessaoTecido();
    tear.Restaurar(estado.amostraPrimeiroFio);
    Exigir(tear.Passagens.Count == 0 && estado.fio == 3 && estado.Tem("r24_m03_01"), "Save antigo perdeu dados.");
    Exigir(!MTMPrimeiroFio.Acompanhar(estado, abertura), "Campanha avançada foi forçada à introdução.");
    estado.amostraPrimeiroFio = new List<int> { 0, 1, 0 };
    var recarregado = JsonSerializer.Deserialize<MTMEstado>(JsonSerializer.Serialize(estado, json), json);
    tear.Restaurar(recarregado.amostraPrimeiroFio);
    Exigir(tear.Passagens.SequenceEqual(new[] { 0, 1, 0 }) && recarregado.fio == 3, "Passagens não persistiram.");
});
Teste("Perfil de personagens carrega a configuração real e permite desativar", () =>
{
    using var arquivo = JsonDocument.Parse(File.ReadAllText(Path.Combine(recursos, "acabamento_original.json")));
    var perfil = JsonSerializer.Deserialize<MTMPerfilPersonagens>(arquivo.RootElement.GetProperty("personagens").GetRawText(), json);
    perfil.Validar();
    Exigir(perfil.ativo && perfil.saturacao > 0f, "Perfil de produção não está ativo.");
    perfil.ativo = false;
    perfil.Validar();
    Exigir(!perfil.ativo, "Não foi possível manter o perfil desativado.");
    new MTMPerfilPersonagens().Validar();
});
Teste("Perfil rejeita números inválidos antes de enviar parâmetros aos shaders", () =>
{
    var alteracoes = new Action<MTMPerfilPersonagens>[]
    {
        p => p.saturacao = float.NaN, p => p.saturacao = 1.01f,
        p => p.suavizacao = -.01f, p => p.suavizacao = .36f,
        p => p.luzPreenchimento = float.PositiveInfinity, p => p.luzPreenchimento = .19f
    };
    foreach (var alterar in alteracoes)
    {
        var perfil = new MTMPerfilPersonagens();
        alterar(perfil);
        bool recusou = false;
        try { perfil.Validar(); } catch (InvalidOperationException) { recusou = true; }
        Exigir(recusou, "Parâmetro inválido foi aceito.");
    }
});
Teste("Sintaxe de todos os scripts nas variantes de entrada e Editor", () =>
{
    var variantes = new[]
    {
        Array.Empty<string>(), new[] { "ENABLE_INPUT_SYSTEM" }, new[] { "ENABLE_LEGACY_INPUT_MANAGER" },
        new[] { "UNITY_EDITOR", "ENABLE_INPUT_SYSTEM", "ENABLE_LEGACY_INPUT_MANAGER" }
    };
    var arquivos = Directory.GetFiles(Path.Combine(raiz, "Assets/MemoriasTecidas"), "*.cs", SearchOption.AllDirectories);
    var erros = new List<string>();
    foreach (var simbolos in variantes)
    {
        var opcoes = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: simbolos);
        foreach (var arquivo in arquivos)
        {
            var arvore = CSharpSyntaxTree.ParseText(File.ReadAllText(arquivo), opcoes, arquivo);
            erros.AddRange(arvore.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()));
        }
    }
    Exigir(erros.Count == 0, string.Join(Environment.NewLine, erros.Distinct()));
    Console.WriteLine("  " + arquivos.Length + " scripts; 4 variantes. Sintaxe apenas, sem compilar APIs Unity.");
});
Console.WriteLine(aprovados + " verificações aprovadas; " + falhas + " falhas.");
Environment.ExitCode = falhas == 0 ? 0 : 1;

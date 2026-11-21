using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Build do jogo para Windows (64 bits): aplica o nome, a empresa, a versão e o ícone (aparecem no executável,
    // na janela e na barra de tarefas), gera o jogo e escreve um LEIA-ME ao lado do executável.
    // Pelo menu: Memorias Tecidas > Gerar build do Windows (sai em Builds/Windows).
    // Pela linha de comando (Unity CLI), com o Unity fechado:
    //   unity build <projeto> --target StandaloneWindows64 --output-path Builds/Windows/MemoriasTecidas.exe
    //     --execute-method MemoriasTecidas.MundoV2.MTMConstrucaoJogo.GerarWindows --allow-dirty-build
    public static class MTMConstrucaoJogo
    {
        public const string Nome = "Memórias Tecidas";
        public const string Empresa = "Equipe Memórias Tecidas";
        public const string Versao = "3.0";
        private const string Icone = "Assets/MemoriasTecidas/Arte/Icone/IconeMemoriasTecidas.png";
        private const string SaidaPadrao = "Builds/Windows/MemoriasTecidas.exe";
        private const string CreditosSons = "Assets/MemoriasTecidas/Resources/MemoriasTecidas/Sons/CREDITOS.txt";

        [MenuItem("Memorias Tecidas/Gerar build do Windows", false, 40)]
        private static void GerarPeloMenu()
        {
            if (Gerar(SaidaPadrao))
                EditorUtility.RevealInFinder(Path.GetFullPath(SaidaPadrao));
        }

        // Chamado pela linha de comando (-executeMethod); usa o -buildOutput que a CLI passa.
        public static void GerarWindows()
        {
            if (!Gerar(SaidaDaLinhaDeComando() ?? SaidaPadrao))
                EditorApplication.Exit(1);
        }

        public static void AplicarIdentidade()
        {
            PlayerSettings.productName = Nome;
            PlayerSettings.companyName = Empresa;
            PlayerSettings.bundleVersion = Versao;
            var icone = AssetDatabase.LoadAssetAtPath<Texture2D>(Icone);
            if (icone != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icone }, IconKind.Any);
            else
                Debug.LogWarning("Build: o ícone não foi encontrado em " + Icone + ".");
            AssetDatabase.SaveAssets();
        }

        private static bool Gerar(string saida)
        {
            AplicarIdentidade();
            string[] cenas = EditorBuildSettings.scenes.Where(c => c.enabled).Select(c => c.path).ToArray();
            if (cenas.Length == 0)
            {
                Debug.LogError("Build: nenhuma cena marcada para a build (File > Build Profiles).");
                return false;
            }

            string caminho = Path.GetFullPath(saida);
            string pasta = Path.GetDirectoryName(caminho);
            Directory.CreateDirectory(pasta);
            var opcoes = new BuildPlayerOptions
            {
                scenes = cenas,
                locationPathName = caminho,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                // Os dados do jogo vão comprimidos (LZ4HC, sem perda): a pasta fica bem menor e carrega rápido.
                options = BuildOptions.CompressWithLz4HC
            };
            BuildReport relatorio = BuildPipeline.BuildPlayer(opcoes);
            var resumo = relatorio.summary;
            if (resumo.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build: não foi concluída (" + resumo.result + ", " + resumo.totalErrors + " erros).");
                return false;
            }

            GravarDadosDoExecutavel(caminho);
            EscreverLeiaMe(pasta, Path.GetFileName(caminho));
            if (File.Exists(CreditosSons))
                File.Copy(CreditosSons, Path.Combine(pasta, "Creditos dos sons.txt"), true);
            Debug.Log(string.Format("Build pronta: {0} ({1:0.0} MB, {2:mm\\:ss}).", caminho, resumo.totalSize / 1048576.0, resumo.totalTime));
            return true;
        }

        // ---- Dados do executável (Propriedades > Detalhes e Gerenciador de Tarefas) ----
        // O Unity copia o executável dele (sem assinatura, já com o nosso ícone) e deixa nos detalhes os dados do
        // próprio Unity. Aqui o recurso de versão do Windows é trocado pelo do jogo: descrição, produto, empresa,
        // versão e direitos. Só no Windows; se algo falhar, a build continua válida e só avisa no console.
        private static void GravarDadosDoExecutavel(string exe)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                return;
            try
            {
                var idiomas = new System.Collections.Generic.List<ushort>();
                IntPtr modulo = LoadLibraryEx(exe, IntPtr.Zero, 0x2 | 0x20);
                if (modulo != IntPtr.Zero)
                {
                    EnumResLangProc coletar = (m, tipo, nome, idioma, p) =>
                    {
                        idiomas.Add(idioma);
                        return true;
                    };
                    EnumResourceLanguages(modulo, (IntPtr)16, (IntPtr)1, coletar, IntPtr.Zero);
                    GC.KeepAlive(coletar);
                    FreeLibrary(modulo);
                }

                const ushort portugues = 0x0416;
                byte[] versao = RecursoDeVersao(portugues);
                IntPtr atualizacao = BeginUpdateResource(exe, false);
                if (atualizacao == IntPtr.Zero)
                    throw new InvalidOperationException("o executável não pôde ser aberto para edição.");
                foreach (var idioma in idiomas)
                    UpdateResource(atualizacao, (IntPtr)16, (IntPtr)1, idioma, null, 0);
                bool ok = UpdateResource(atualizacao, (IntPtr)16, (IntPtr)1, portugues, versao, (uint)versao.Length);
                if (!EndUpdateResource(atualizacao, !ok) || !ok)
                    throw new InvalidOperationException("o Windows recusou a gravação (erro " + Marshal.GetLastWin32Error() + ").");
            }
            catch (Exception erro)
            {
                Debug.LogWarning("Build: os dados do executável não foram gravados (" + erro.Message + "). O jogo funciona normalmente.");
            }
        }

        // Monta o VS_VERSIONINFO (cabeçalho fixo + textos em português + tradução).
        private static byte[] RecursoDeVersao(ushort idioma)
        {
            string[] partes = Versao.Split('.');
            int maior = partes.Length > 0 ? int.Parse(partes[0]) : 1, menor = partes.Length > 1 ? int.Parse(partes[1]) : 0;
            var fixo = new MemoryStream();
            using (var w = new BinaryWriter(fixo))
            {
                w.Write(0xFEEF04BDu);          // assinatura
                w.Write(0x00010000u);          // versão da estrutura
                w.Write((uint)((maior << 16) | menor));
                w.Write(0u);
                w.Write((uint)((maior << 16) | menor));
                w.Write(0u);
                w.Write(0x3Fu);                // máscara de flags
                w.Write(0u);                   // flags
                w.Write(0x40004u);             // VOS_NT_WINDOWS32
                w.Write(1u);                   // VFT_APP
                w.Write(0u);
                w.Write(0u);
                w.Write(0u);
            }

            string numero = maior + "." + menor + ".0.0";
            var textos = new[]
            {
                Texto("CompanyName", Empresa),
                Texto("FileDescription", Nome),
                Texto("FileVersion", numero),
                Texto("InternalName", "MemoriasTecidas"),
                Texto("LegalCopyright", "© " + DateTime.Now.Year + " " + Empresa),
                Texto("OriginalFilename", "MemoriasTecidas.exe"),
                Texto("ProductName", Nome),
                Texto("ProductVersion", Versao),
                Texto("Comments", "Jaguaruana · Rede de Memórias · Entre Memórias e Lendas")
            };
            byte[] tabela = Bloco(idioma.ToString("X4") + "04B0", null, 0, 1, textos);
            byte[] arquivo = Bloco("StringFileInfo", null, 0, 1, new[] { tabela });
            byte[] traducao = BitConverter.GetBytes((uint)(idioma | (1200 << 16)));
            byte[] variaveis = Bloco("VarFileInfo", null, 0, 1, new[] { Bloco("Translation", traducao, 4, 0, null) });
            byte[] valorFixo = fixo.ToArray();
            return Bloco("VS_VERSION_INFO", valorFixo, valorFixo.Length, 0, new[] { arquivo, variaveis });
        }

        private static byte[] Texto(string chave, string valor)
        {
            byte[] v = Encoding.Unicode.GetBytes(valor + "\0");
            return Bloco(chave, v, valor.Length + 1, 1, null);
        }

        // Um nó do recurso de versão: tamanho, tamanho do valor, tipo, chave (UTF-16 com 0), valor e filhos,
        // tudo alinhado de 4 em 4 bytes.
        private static byte[] Bloco(string chave, byte[] valor, int tamanhoValor, ushort tipo, byte[][] filhos)
        {
            var m = new MemoryStream();
            var w = new BinaryWriter(m);
            w.Write((ushort)0);
            w.Write((ushort)tamanhoValor);
            w.Write(tipo);
            w.Write(Encoding.Unicode.GetBytes(chave + "\0"));
            Alinhar(w);
            if (valor != null)
                w.Write(valor);
            if (filhos != null)
                foreach (var filho in filhos)
                {
                    Alinhar(w);
                    w.Write(filho);
                }

            w.Flush();
            byte[] r = m.ToArray();
            r[0] = (byte)(r.Length & 0xFF);
            r[1] = (byte)(r.Length >> 8);
            return r;
        }

        private static void Alinhar(BinaryWriter w)
        {
            w.Flush();
            while (w.BaseStream.Position % 4 != 0)
                w.Write((byte)0);
        }

        private delegate bool EnumResLangProc(IntPtr modulo, IntPtr tipo, IntPtr nome, ushort idioma, IntPtr parametro);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr LoadLibraryEx(string arquivo, IntPtr reservado, uint flags);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr modulo);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool EnumResourceLanguages(IntPtr modulo, IntPtr tipo, IntPtr nome, EnumResLangProc chamada, IntPtr parametro);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr BeginUpdateResource(string arquivo, bool apagarTudo);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UpdateResource(IntPtr atualizacao, IntPtr tipo, IntPtr nome, ushort idioma, byte[] dados, uint tamanho);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool EndUpdateResource(IntPtr atualizacao, bool descartar);

        private static string SaidaDaLinhaDeComando()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-buildOutput" && !string.IsNullOrEmpty(args[i + 1]))
                    return args[i + 1];
            return null;
        }

        private static void EscreverLeiaMe(string pasta, string executavel)
        {
            string dados = Path.GetFileNameWithoutExtension(executavel) + "_Data";
            var t = new StringBuilder();
            t.AppendLine("MEMÓRIAS TECIDAS · Versão " + Versao);
            t.AppendLine("Jaguaruana · Rede de Memórias · Entre Memórias e Lendas");
            t.AppendLine();
            t.AppendLine("SOBRE O JOGO");
            t.AppendLine("Memórias Tecidas é um jogo de exploração em Jaguaruana, no Ceará, a Terra da Rede. Neto chega à");
            t.AppendLine("cidade para ouvir as histórias dos moradores e guardar cada uma como um fio na sua rede de memórias.");
            t.AppendLine("Pelo caminho ele conhece lugares, pessoas e saberes, aprende a tecer e, quando a noite cai, enfrenta");
            t.AppendLine("as lendas da cidade.");
            t.AppendLine();
            t.AppendLine("COMO ABRIR");
            t.AppendLine("Abra " + executavel + ". Deixe a pasta " + dados + " e os outros arquivos desta pasta junto do");
            t.AppendLine("executável. Requer Windows 10 ou 11 (64 bits).");
            t.AppendLine();
            t.AppendLine("COMO JOGAR (teclado · controle)");
            t.AppendLine("Caminhar ............................ WASD ou setas · analógico ou direcional");
            t.AppendLine("Correr .............................. Shift · RT ou L3");
            t.AppendLine("Conversar ou interagir .............. E ou Enter · A");
            t.AppendLine("Mapa · bolsa · caderno .............. M · B ou I · J  ·  View · LB · Y");
            t.AppendLine("Voltar ou pausar .................... Esc · B ou Start");
            t.AppendLine("Voltar ao caminho seguro ............ R · R3");
            t.AppendLine("Ajustar a câmera .................... roda do mouse, + e − · analógico direito");
            t.AppendLine("Golpear com o pau, na luta .......... clique do mouse (ou Espaço) · X");
            t.AppendLine("Escutar (segure) .................... L · LT");
            t.AppendLine("Lanterna à noite .................... F ou o atalho da lanterna na tela · RB");
            t.AppendLine("Mostrar ou ocultar a interface ...... F1");
            t.AppendLine();
            t.AppendLine("O progresso é salvo sozinho nos encontros, ao trocar de lugar e a cada 30 segundos.");
            t.AppendLine("Para rever a abertura, use \"Sobre o jogo\" no menu inicial.");
            t.AppendLine();
            t.AppendLine("CRÉDITOS");
            t.AppendLine("- Fontes DejaVu.");
            t.AppendLine("- Sons: Kenney e Blanket (CC0 e domínio público), gezortenplotz/Freesound (CC BY 3.0) e 0 A.D.");
            t.AppendLine("  © Wildfire Games, wildfiregames.com (CC BY-SA 3.0). Lista completa em \"Creditos dos sons.txt\".");
            t.AppendLine("- Bandeira de Jaguaruana: símbolo oficial do município, redesenhado para a abertura.");
            t.AppendLine("- Foto da Matriz de Sant'Ana (abertura e Casa das Memórias): @fotosdoceara.");
            t.AppendLine("- Fotos de Raimundo Francisco Freitas Jaguaribe, Ivonete Maia e Padre Ducéu (Casa das Memórias):");
            t.AppendLine("  blog Memórias de Jaguaruana, post \"Jaguaruanenses ilustres\" (2017).");
            t.AppendLine("- Os diálogos e atividades são adaptações ficcionais e didáticas. As fontes dos encontros");
            t.AppendLine("  aparecem no caderno do jogo.");
            File.WriteAllText(Path.Combine(pasta, "LEIA-ME.txt"), t.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(true));
        }
    }
}

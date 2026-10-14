using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoriasTecidas.MundoV2.Editor
{
    // Correcao localizada do LightMode. Execute uma vez, fora do Play.
    // Mantem os GUIDs e as personalizacoes dos materiais, prefabs e cenas.
    public static class MTMCorrecaoShaderBuiltIn
    {
        private const string Menu = "Memorias Tecidas/Mundo V2/Reparo visual/5 - Corrigir shader do Built-in";
        private const string Fonte = "Assets/MemoriasTecidas/MundoV2/Editor/MTMMateriais.cs";
        private const string ShaderPath = "Assets/MemoriasTecidas/MundoV2/Gerados/Shaders/PintadoV2.shader";
        private const string FonteAntes = "Tags { \"\"LightMode\"\"=\"\"SRPDefaultUnlit\"\" }";
        private const string FonteDepois = "Tags { \"\"LightMode\"\"=\"\"\" + (urp ? \"SRPDefaultUnlit\" : \"Always\") + @\"\"\" }";
        private const string ShaderAntes = "Tags { \"LightMode\"=\"SRPDefaultUnlit\" }";
        private const string ShaderDepois = "Tags { \"LightMode\"=\"Always\" }";
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        [MenuItem(Menu)]
        public static void Corrigir()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Pare o Play e execute novamente a correcao do shader.");
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogWarning("Aguarde a Unity terminar a importacao/compilacao.");
                return;
            }

            string backup = null;
            try
            {
                if (GraphicsSettings.currentRenderPipeline != null ||
                    QualitySettings.renderPipeline != null || GraphicsSettings.defaultRenderPipeline != null)
                    throw new InvalidOperationException(
                        "Este reparo corresponde ao Built-in dos relatorios enviados. " +
                        "O projeto esta configurado com outro pipeline; arquivos preservados.");

                string raiz = Path.GetDirectoryName(Application.dataPath);
                string fontePath = Path.Combine(raiz, Fonte);
                string shaderPath = Path.Combine(raiz, ShaderPath);
                if (!File.Exists(fontePath) || !File.Exists(shaderPath))
                    throw new FileNotFoundException("Nao localizei MTMMateriais.cs e PintadoV2.shader " +
                        "nos caminhos esperados de Assets/MemoriasTecidas/MundoV2. Arquivos preservados.");

                byte[] fonteBytes = File.ReadAllBytes(fontePath);
                byte[] shaderBytes = File.ReadAllBytes(shaderPath);
                // GetString/GetBytes conserva inclusive o BOM original, se existir.
                string fonte = Utf8.GetString(fonteBytes);
                string shader = Utf8.GetString(shaderBytes);
                if (!fonte.Contains("private static string Codigo(bool urp)") ||
                    !fonte.Contains("MemoriasTecidas/MundoV2/Pintado"))
                    throw new InvalidOperationException("Gerador diferente do padrao V2.1; " +
                        "arquivos preservados para revisao.");
                if (!shader.Contains("Shader \"MemoriasTecidas/MundoV2/Pintado\"") ||
                    !shader.Contains("CGPROGRAM") || !shader.Contains("UnityCG.cginc") ||
                    shader.Contains("HLSLPROGRAM") || shader.Contains("UniversalPipeline"))
                    throw new InvalidOperationException("O shader nao corresponde a variante Built-in " +
                        "dos relatorios. Arquivos preservados para revisao.");

                // Prepara as duas alteracoes antes de escrever qualquer arquivo.
                string fonteNova = TrocarUmaVez(fonte, FonteAntes, FonteDepois, "MTMMateriais.cs");
                string shaderNovo = TrocarUmaVez(shader, ShaderAntes, ShaderDepois, "PintadoV2.shader");
                bool mudaFonte = fonteNova != fonte;
                bool mudaShader = shaderNovo != shader;
                if (!mudaFonte && !mudaShader)
                {
                    Debug.Log("Shader e gerador ja estao corrigidos para o Built-in. " +
                        "Use Play normalmente, sem o teste de material padrao.");
                    return;
                }

                backup = Path.Combine(raiz, "Backups_MemoriasTecidas", "Shader_BuiltIn_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" +
                    Guid.NewGuid().ToString("N").Substring(0, 6));
                Directory.CreateDirectory(backup);
                CopiarComMeta(fontePath, Path.Combine(backup, "MTMMateriais.cs"));
                CopiarComMeta(shaderPath, Path.Combine(backup, "PintadoV2.shader"));

                AssetDatabase.StartAssetEditing();
                try
                {
                    try
                    {
                        if (mudaFonte) File.WriteAllBytes(fontePath, Utf8.GetBytes(fonteNova));
                        if (mudaShader) File.WriteAllBytes(shaderPath, Utf8.GetBytes(shaderNovo));
                    }
                    catch
                    {
                        // Restaura ambos se houver falha de escrita parcial.
                        if (mudaFonte) File.WriteAllBytes(fontePath, fonteBytes);
                        if (mudaShader) File.WriteAllBytes(shaderPath, shaderBytes);
                        throw;
                    }
                }
                finally { AssetDatabase.StopAssetEditing(); }

                // Reimportar o mesmo asset atualiza todos os materiais que o referenciam.
                AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate |
                    ImportAssetOptions.ForceSynchronousImport);
                Shader importado = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                bool shaderOk = importado != null && importado.isSupported &&
                    !ShaderUtil.ShaderHasError(importado);
                string resultado = "LightMode do Built-in: Always.\n" +
                    "Gerador: Always no Built-in; SRPDefaultUnlit na variante URP.\n" +
                    "Shader alterado nesta execucao: " + mudaShader + "\n" +
                    "Gerador alterado nesta execucao: " + mudaFonte + "\n" +
                    "Shader importado, suportado e sem erro reportado: " + shaderOk + "\n" +
                    "Backup: " + backup + "\n\n" +
                    "Aguarde a compilacao. Abra a cena existente e use Play normalmente.\n" +
                    "Confira o cenario sem usar o teste de material padrao.\n" +
                    "A verificacao acima nao substitui a confirmacao visual na Game.\n";
                File.WriteAllText(Path.Combine(backup, "RESULTADO.txt"), resultado, Utf8);
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
                if (shaderOk) Debug.Log("Shader corrigido (Built-in).\n" + resultado, importado);
                else Debug.LogError("Correcao gravada, mas o shader ainda requer verificacao. " +
                    "Confira os erros da Console.\n" + resultado);

                // Importa por ultimo porque alterar um script pode recarregar o dominio.
                if (mudaFonte) AssetDatabase.ImportAsset(Fonte, ImportAssetOptions.ForceUpdate);
            }
            catch (Exception e)
            {
                Debug.LogError("Correcao do shader nao concluida. " + e.Message +
                    (backup == null ? "" : "\nBackup disponivel: " + backup));
            }
        }

        private static string TrocarUmaVez(string texto, string antes, string depois, string arquivo)
        {
            int antigos = Contar(texto, antes);
            int novos = Contar(texto, depois);
            if (antigos == 0 && novos == 1) return texto;
            if (antigos != 1 || novos != 0)
                throw new InvalidOperationException("Trecho LightMode inesperado em " + arquivo +
                    "; nenhuma alteracao preparada foi gravada. Envie esse arquivo para revisao.");
            return texto.Replace(antes, depois);
        }

        private static int Contar(string texto, string trecho)
        {
            int total = 0;
            int indice = 0;
            while ((indice = texto.IndexOf(trecho, indice, StringComparison.Ordinal)) >= 0)
            {
                total++;
                indice += trecho.Length;
            }
            return total;
        }

        private static void CopiarComMeta(string origem, string destino)
        {
            File.Copy(origem, destino, false);
            if (File.Exists(origem + ".meta")) File.Copy(origem + ".meta", destino + ".meta", false);
        }
    }
}

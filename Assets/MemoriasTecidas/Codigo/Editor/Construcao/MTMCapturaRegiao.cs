using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Fotografa uma região da cena sem rodar o jogo (para conferir o cenário): abre a cena, liga só a região,
    // ajusta uma câmera ortográfica sobre ela e grava um PNG. A cena não é salva.
    // Linha de comando (Unity fechado):
    //   unity run <projeto> -- -executeMethod MemoriasTecidas.MundoV2.MTMCapturaRegiao.Capturar
    //     -regiao memorias -saida Builds/Capturas/memorias.png [-zoom 2.2 -foco 0,0] [-medir caminho.txt]
    public static class MTMCapturaRegiao
    {
        private const string Cena = "Assets/MemoriasTecidas/Cenas/Jaguaruana.unity";

        public static void Capturar()
        {
            try
            {
                string regiaoId = Argumento("-regiao") ?? "memorias";
                string saida = Argumento("-saida") ?? Path.Combine("Builds", "Capturas", regiaoId + ".png");
                float zoom = float.TryParse(Argumento("-zoom"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z) ? z : 1f;
                Vector2 foco = Vector2.zero;
                var textoFoco = Argumento("-foco");
                if (!string.IsNullOrEmpty(textoFoco))
                {
                    var partes = textoFoco.Split(',');
                    foco = new Vector2(float.Parse(partes[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(partes[1], System.Globalization.CultureInfo.InvariantCulture));
                }

                EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
                MTMRegiao alvo = null;
                foreach (var r in UnityEngine.Object.FindObjectsByType<MTMRegiao>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    bool eh = r.dados != null && r.dados.id == regiaoId;
                    r.gameObject.SetActive(eh);
                    if (eh)
                        alvo = r;
                }

                if (alvo == null)
                    throw new InvalidOperationException("região não encontrada: " + regiaoId);

                // Tamanho da região: os dados dela (largura x altura), com uma pequena margem.
                float largura = Mathf.Max(4f, alvo.dados.largura), altura = Mathf.Max(4f, alvo.dados.altura);
                const int w = 2400, h = 1350;
                float aspecto = w / (float)h;
                float tamanho = Mathf.Max(altura * .5f, largura * .5f / aspecto) * 1.04f / Mathf.Max(.1f, zoom);
                var go = new GameObject("Captura");
                var cam = go.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = tamanho;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(.12f, .11f, .1f);
                cam.transform.position = alvo.transform.position + new Vector3(foco.x, foco.y, -10f);
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(saida)));
                File.WriteAllBytes(saida, tex.EncodeToPNG());
                Debug.Log("Captura: " + Path.GetFullPath(saida) + " (região " + regiaoId + ", " + largura + " x " + altura + ")");

                var medir = Argumento("-medir");
                if (!string.IsNullOrEmpty(medir))
                    MedirPrefabs(medir);
            }
            catch (Exception erro)
            {
                Debug.LogError("Captura: " + erro.Message);
                EditorApplication.Exit(1);
            }
        }

        // Tamanho no mundo (largura x altura, em unidades) e pivô de cada prefab de cenário, para planejar a montagem.
        private static void MedirPrefabs(string arquivo)
        {
            var texto = new StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/MemoriasTecidas/Prefabs" }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
                if (prefab == null)
                    continue;
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instancia.transform.position = Vector3.zero;
                bool tem = false;
                var caixa = new Bounds();
                foreach (var sr in instancia.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite == null || sr.name.IndexOf("sombra", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (!tem)
                    {
                        caixa = sr.bounds;
                        tem = true;
                    }
                    else
                        caixa.Encapsulate(sr.bounds);
                }

                texto.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\t{1:0.00}\t{2:0.00}\t{3:0.00}\t{4:0.00}\t{5}",
                    Path.GetFileNameWithoutExtension(caminho), caixa.size.x, caixa.size.y, caixa.min.y, caixa.center.x, caminho));
                UnityEngine.Object.DestroyImmediate(instancia);
            }

            File.WriteAllText(arquivo, texto.ToString());
            Debug.Log("Medidas dos prefabs: " + Path.GetFullPath(arquivo));
        }

        private static string Argumento(string nome)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == nome)
                    return args[i + 1];
            return null;
        }
    }
}

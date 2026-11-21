using UnityEditor;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Coloca MTMFlutuanteV25 nos prefabs que ficam na água (canoa e aguapés); as instâncias da cena
    // herdam do prefab. Pode rodar de novo: só ajusta os valores. Linha de comando (Unity fechado):
    //   unity run <projeto> -- -executeMethod MemoriasTecidas.MundoV2.MTMAnimacaoCenario.InstalarFlutuantes
    public static class MTMAnimacaoCenario
    {
        private static readonly (string caminho, float altura, float inclinacao, float ritmo)[] Flutuantes =
        {
            ("Assets/MemoriasTecidas/Prefabs/Mundo/canoa.prefab", .035f, 1.2f, 1f),
            ("Assets/MemoriasTecidas/Prefabs/Locais/plantas_aquaticas.prefab", .018f, .6f, .8f),
        };

        [MenuItem("Memorias Tecidas/Fazer canoas e aguapés boiarem")]
        public static void InstalarFlutuantes()
        {
            foreach (var f in Flutuantes)
            {
                var raiz = PrefabUtility.LoadPrefabContents(f.caminho);
                try
                {
                    var desenho = raiz.transform.Find("Desenho");
                    if (desenho == null)
                    {
                        Debug.LogError("Flutuação: \"Desenho\" não encontrado em " + f.caminho);
                        continue;
                    }

                    var boia = raiz.GetComponent<MTMFlutuanteV25>();
                    if (boia == null)
                        boia = raiz.AddComponent<MTMFlutuanteV25>();
                    boia.desenho = desenho;
                    boia.altura = f.altura;
                    boia.inclinacao = f.inclinacao;
                    boia.ritmo = f.ritmo;
                    PrefabUtility.SaveAsPrefabAsset(raiz, f.caminho);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Flutuação instalada em " + Flutuantes.Length + " prefabs.");
        }

        // Instala e já grava os quadros (aceita os argumentos de MTMCapturaAnimacao).
        public static void InstalarECapturar()
        {
            InstalarFlutuantes();
            MTMCapturaAnimacao.Capturar();
        }
    }
}

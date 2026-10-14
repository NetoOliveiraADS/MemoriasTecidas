using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed class MTMRegiao : MonoBehaviour
    {
        public MTMAreaDados dados;
        public MTMPonto[] pontos;
        public GameObject[] aparicoes;
        private SpriteRenderer[] desenhos;
        private Color[] cores;

        public void Ambiente(bool noite, bool lendaOuvida)
        {
            if (desenhos == null)
            {
                desenhos = GetComponentsInChildren<SpriteRenderer>(true);
                cores = new Color[desenhos.Length];
                for (int i = 0; i < cores.Length; i++) cores[i] = desenhos[i].color;
            }
            // A cor do horário e as luzes locais são calculadas por MTMAmbienteV2 no shader.
            if (aparicoes != null) foreach (GameObject go in aparicoes)
                if (go != null) go.SetActive(noite && lendaOuvida);
        }
    }
}

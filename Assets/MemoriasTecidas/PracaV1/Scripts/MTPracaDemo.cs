using UnityEngine;

namespace MemoriasTecidas.PracaV1
{
    // Interface leve de verificação. O sistema de diálogo principal continua separado.
    [DefaultExecutionOrder(-100)]
    public sealed class MTPracaDemo : MonoBehaviour
    {
        public Transform jogador;
        public Transform neto;
        [Min(0.2f)] public float distanciaConversa = 1.6f;
        [TextArea(2, 5)] public string[] falas = {
            "Bem-vindo à praça! Eu sou Neto. Este é nosso primeiro espaço para explorar Jaguaruana no jogo.",
            "Caminhe entre os bancos e as árvores. Observe como você passa à frente e atrás dos objetos.",
            "A igreja é uma interpretação artística baseada nas referências locais. As histórias e missões serão integradas nas próximas etapas."
        };
        private int fala = -1;
        private int quadroAlterado = -1;
        private bool mostrar = true;
        private GUIStyle titulo, texto, pequeno;
        public bool BloqueiaMovimento => fala >= 0 || quadroAlterado == Time.frameCount;
        private bool Perto => jogador != null && neto != null
            && Vector2.Distance(jogador.position, neto.position) <= distanciaConversa;

        private void Update()
        {
            if (MTPracaEntrada.AlternarInterface()) mostrar = !mostrar;
            if (fala >= 0)
            {
                if (MTPracaEntrada.Fechar()) { fala = -1; quadroAlterado = Time.frameCount; }
                else if (MTPracaEntrada.Conversar() && quadroAlterado != Time.frameCount)
                {
                    fala++;
                    if (falas == null || fala >= falas.Length) fala = -1;
                    quadroAlterado = Time.frameCount;
                }
            }
            else if (Perto && MTPracaEntrada.Conversar() && falas != null && falas.Length > 0)
            {
                fala = 0; quadroAlterado = Time.frameCount;
            }
        }
        private static void Fundo(Rect r)
        {
            Color anterior = GUI.color;
            GUI.color = new Color(0.055f, 0.12f, 0.14f, 0.94f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = anterior;
        }
        private void OnGUI()
        {
            if (!mostrar && fala < 0) return;
            if (titulo == null)
            {
                titulo = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, wordWrap = true };
                texto = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
                pequeno = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                titulo.normal.textColor = new Color(1f, 0.87f, 0.59f);
                texto.normal.textColor = Color.white;
                pequeno.normal.textColor = new Color(0.86f, 0.92f, 0.89f);
            }
            Matrix4x4 anterior = GUI.matrix;
            float escala = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Vector3 margem = new Vector3((Screen.width - 1280f * escala) * 0.5f,
                (Screen.height - 720f * escala) * 0.5f, 0f);
            GUI.matrix = Matrix4x4.TRS(margem, Quaternion.identity, new Vector3(escala, escala, 1f));
            if (mostrar)
            {
                Fundo(new Rect(18, 18, 540, 90));
                GUI.Label(new Rect(34, 26, 510, 36), "Memórias Tecidas · Praça", titulo);
                GUI.Label(new Rect(34, 66, 510, 30), "Cena de teste visual · versão 1", pequeno);
            }
            if (fala >= 0 && falas != null && fala < falas.Length)
            {
                Fundo(new Rect(18, 478, 1244, 224));
                GUI.Label(new Rect(38, 492, 1180, 36), "Neto", titulo);
                GUI.Label(new Rect(38, 536, 1180, 94), falas[fala], texto);
                GUI.Label(new Rect(38, 646, 1180, 38), (fala + 1) + "/" + falas.Length
                    + "   E / Enter: avançar   ·   Esc: fechar   ·   Fala fictícia de tutorial", pequeno);
            }
            else if (mostrar)
            {
                Fundo(new Rect(18, 630, 1244, 72));
                string instrucao = Perto ? "Pressione E ou Enter para conversar com Neto."
                    : "Caminhe com WASD ou setas. Procure Neto e pressione E.";
                GUI.Label(new Rect(36, 638, 1200, 30), instrucao, texto);
                GUI.Label(new Rect(36, 670, 1200, 26), "F1: ocultar interface   ·   R: voltar ao início   ·   Clique na aba Game para jogar", pequeno);
            }
            GUI.matrix = anterior;
        }
    }
}

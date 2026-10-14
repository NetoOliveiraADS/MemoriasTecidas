using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MemoriasTecidas.ExpansaoV1
{
    // Somente a cena de demonstração usa este controle. Não adicionar ao jogador da Missão 01.
    [RequireComponent(typeof(Rigidbody2D)), DisallowMultipleComponent]
    public sealed class MTEXDemo : MonoBehaviour
    {
        public Camera cameraDoMapa;
        public MTEXAmbiente ambiente;
        public float velocidade = 3.5f;
        public Vector2[] inicios = { new Vector2(0f, -5.5f), new Vector2(42f, -5.5f), new Vector2(84f, -5.5f) };
        private Rigidbody2D corpo;
        private Vector2 direcao;
        private int area;
        private bool interfaceVisivel = true;
        private GUIStyle titulo, texto;
        private static readonly string[] Nomes = { "Entrada de Jaguaruana", "Centro e prefeitura", "Rio e carnaubal" };

        private void Awake() { corpo = GetComponent<Rigidbody2D>(); }
        private void Start() { IrPara(0); }
        private void Update()
        {
            direcao = Movimento();
            int destino = AreaPressionada();
            if (destino >= 0) { IrPara(destino); return; }
            if (Reiniciar()) { IrPara(area); return; }
            if (Noite() && ambiente != null) ambiente.noite = !ambiente.noite;
            if (Variacao() && ambiente != null) ambiente.usarCentaura = !ambiente.usarCentaura;
            if (Esconder()) interfaceVisivel = !interfaceVisivel;
            AjustarCamera();
        }
        private void FixedUpdate() { corpo.linearVelocity = direcao * velocidade; }
        private void OnDisable() { if (corpo != null) corpo.linearVelocity = Vector2.zero; }

        private void IrPara(int numero)
        {
            if (inicios == null || numero < 0 || numero >= inicios.Length) return;
            area = numero;
            direcao = Vector2.zero;
            corpo.position = inicios[area]; corpo.linearVelocity = Vector2.zero;
            if (cameraDoMapa != null) cameraDoMapa.transform.position = new Vector3(inicios[area].x, 0f, -10f);
            AjustarCamera();
        }

        private void AjustarCamera()
        {
            if (cameraDoMapa == null || Screen.height <= 0) return;
            float escala = (Screen.width / (float)Screen.height) / (16f / 9f);
            cameraDoMapa.rect = escala < 1f ? new Rect(0f, (1f - escala) * .5f, 1f, escala)
                : new Rect((1f - 1f / escala) * .5f, 0f, 1f / escala, 1f);
        }

        private void OnGUI()
        {
            if (!interfaceVisivel || cameraDoMapa == null) return;
            if (titulo == null)
            {
                titulo = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
                titulo.normal.textColor = new Color(.97f, .80f, .47f);
                texto = new GUIStyle(GUI.skin.label) { fontSize = 17 };
                texto.normal.textColor = new Color(.96f, .97f, .92f);
            }
            Matrix4x4 antiga = GUI.matrix;
            Color cor = GUI.color;
            Rect px = cameraDoMapa.pixelRect;
            float escala = px.width / 1280f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(px.x, Screen.height - px.yMax, 0), Quaternion.identity, Vector3.one * escala);
            GUI.color = new Color(.04f, .10f, .12f, .94f);
            GUI.DrawTexture(new Rect(24, 20, 1232, 108), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(42, 28, 1190, 36), "MEMÓRIAS TECIDAS  •  " + Nomes[Mathf.Clamp(area, 0, 2)], titulo);
            GUI.Label(new Rect(42, 70, 1190, 26), "WASD/setas: andar   |   1 / 2 / 3: visitar áreas   |   N: dia/noite   |   V: variação da lenda   |   R: voltar   |   H: ocultar", texto);
            string variante = ambiente != null && ambiente.usarCentaura ? "centaura da referência visual" : "Mulher-Cavalo do relato";
            GUI.Label(new Rect(42, 97, 1190, 26), "Demonstração visual • mapa adaptado • aparição estática à noite: " + variante, texto);
            GUI.matrix = antiga; GUI.color = cor;
        }

        private static Vector2 Movimento()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return v;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x--;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x++;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y--;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y++;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x--;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x++;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y--;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y++;
#endif
            return Vector2.ClampMagnitude(v, 1f);
        }
        private static int AreaPressionada()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return -1;
            if (k.digit1Key.wasPressedThisFrame) return 0;
            if (k.digit2Key.wasPressedThisFrame) return 1;
            if (k.digit3Key.wasPressedThisFrame) return 2;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
#endif
            return -1;
        }
        private static bool Noite()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.N);
#else
            return false;
#endif
        }
        private static bool Variacao()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.V);
#else
            return false;
#endif
        }
        private static bool Reiniciar()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
        private static bool Esconder()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.H);
#else
            return false;
#endif
        }
    }
}

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MemoriasTecidas.PracaV1
{
    // Controlador da praça. Use somente um controlador de movimento por jogador.
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
    public sealed class MTPracaJogador : MonoBehaviour
    {
        [Min(0.1f)] public float velocidade = 3.5f;
        public MTPracaDemo demonstracao;
        public Vector2 inicio = new Vector2(0f, -4.5f);
        private Rigidbody2D corpo;
        private Vector2 direcao;
        public bool BloqueioExterno { get; private set; }
        private bool Bloqueado => BloqueioExterno ||
            (demonstracao != null && demonstracao.isActiveAndEnabled && demonstracao.BloqueiaMovimento);

        public void DefinirBloqueioExterno(bool bloquear)
        {
            BloqueioExterno = bloquear;
            if (!bloquear) return;
            direcao = Vector2.zero;
            if (corpo != null) corpo.linearVelocity = Vector2.zero;
        }

        private void Awake() { corpo = GetComponent<Rigidbody2D>(); }
        private void Update()
        {
            bool bloqueado = Bloqueado;
            direcao = bloqueado ? Vector2.zero : MTPracaEntrada.Movimento();
            if (!bloqueado && MTPracaEntrada.Reiniciar())
            {
                corpo.position = inicio;
                corpo.linearVelocity = Vector2.zero;
                direcao = Vector2.zero;
            }
        }
        private void FixedUpdate()
        {
            bool bloqueado = Bloqueado;
            corpo.linearVelocity = bloqueado ? Vector2.zero : direcao * velocidade;
        }
        private void OnDisable()
        {
            direcao = Vector2.zero;
            if (corpo != null) corpo.linearVelocity = Vector2.zero;
        }
    }

    // Compila com o Input System ou com o gerenciador clássico.
    // Quando Both está ativo, lê somente o Input System para evitar teclas duplicadas.
    internal static class MTPracaEntrada
    {
        public static Vector2 Movimento()
        {
            float x = 0f, y = 0f;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return Vector2.zero;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
#endif
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
        public static bool Conversar()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
#else
            return false;
#endif
        }
        public static bool Fechar()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
        public static bool Reiniciar()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
        public static bool AlternarInterface()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F1);
#else
            return false;
#endif
        }
    }
}

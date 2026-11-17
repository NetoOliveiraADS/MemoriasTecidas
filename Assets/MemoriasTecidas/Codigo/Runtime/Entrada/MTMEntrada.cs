using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

#endif
namespace MemoriasTecidas.MundoV2
{
    // Teclado e controle (gamepad) no mesmo lugar. Mapa do controle (nomes do Xbox; no PlayStation,
    // A = ✕, B = ○, X = □, Y = △, View = Share/Create):
    //   analógico esquerdo ou direcional: andar (nos painéis, o direcional escolhe o botão)
    //   A: interagir, conversar, confirmar     B ou Start: voltar, pausar
    //   X: golpear (no tear, trocar o fio)      Y: caderno (no tear e na memória viva, dica)
    //   LB: bolsa (no tear, pedal anterior)     RB: no tear, próximo pedal
    //   View: mapa (no tear, desfazer)          LT segurado: escutar
    //   RT segurado ou L3: correr               analógico direito: zoom     R3: voltar ao caminho
    public static class MTMEntrada
    {
        private const float ZonaMorta = .22f;

        // Verdadeiro quando o último comando veio do controle: o HUD mostra os botões dele.
        public static bool UsandoControle { get; private set; }

        private static int quadroAtualizado = -1;
        private static Vector2Int navegacaoUI, direcaoSegurada;
        private static float proximaRepeticao, fimVibracao;

        // Uma vez por quadro, antes de ler os comandos.
        public static void Atualizar()
        {
            if (quadroAtualizado == Time.frameCount)
                return;
            quadroAtualizado = Time.frameCount;
            navegacaoUI = Vector2Int.zero;
#if ENABLE_INPUT_SYSTEM
            var g = Gamepad.current;
            var k = Keyboard.current;
            var mouse = Mouse.current;
            if (g != null && (g.wasUpdatedThisFrame && (AlgumBotao(g) || g.leftStick.ReadValue().sqrMagnitude > .25f || g.rightStick.ReadValue().sqrMagnitude > .25f)))
                UsandoControle = true;
            else if ((k != null && k.anyKey.wasPressedThisFrame) ||
                     (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 9f || mouse.leftButton.wasPressedThisFrame)))
                UsandoControle = false;

            if (g != null)
            {
                // Direcional (ou analógico) com repetição ao segurar, para andar pelos botões.
                Vector2 bruto = g.dpad.ReadValue();
                Vector2 alavanca = g.leftStick.ReadValue();
                if (bruto.sqrMagnitude < .25f && alavanca.sqrMagnitude > .5f)
                    bruto = alavanca;
                Vector2Int dir = Vector2Int.zero;
                if (Mathf.Abs(bruto.x) > Mathf.Abs(bruto.y) && Mathf.Abs(bruto.x) > .5f)
                    dir.x = bruto.x > 0 ? 1 : -1;
                else if (Mathf.Abs(bruto.y) > .5f)
                    dir.y = bruto.y > 0 ? -1 : 1; // na tela, y cresce para baixo
                if (dir != direcaoSegurada)
                {
                    direcaoSegurada = dir;
                    navegacaoUI = dir;
                    proximaRepeticao = Time.unscaledTime + .42f;
                }
                else if (dir != Vector2Int.zero && Time.unscaledTime >= proximaRepeticao)
                {
                    navegacaoUI = dir;
                    proximaRepeticao = Time.unscaledTime + .13f;
                }

                if (fimVibracao > 0f && Time.unscaledTime >= fimVibracao)
                {
                    g.SetMotorSpeeds(0f, 0f);
                    fimVibracao = 0f;
                }
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool AlgumBotao(Gamepad g)
        {
            return g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame ||
                   g.buttonNorth.wasPressedThisFrame || g.leftShoulder.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame ||
                   g.startButton.wasPressedThisFrame || g.selectButton.wasPressedThisFrame || g.dpad.ReadValue().sqrMagnitude > .25f ||
                   g.leftTrigger.wasPressedThisFrame || g.rightTrigger.wasPressedThisFrame ||
                   g.leftStickButton.wasPressedThisFrame || g.rightStickButton.wasPressedThisFrame;
        }

        private static Gamepad Controle
        {
            get
            {
                Atualizar();
                return Gamepad.current;
            }
        }
#endif

        // Direção escolhida nos painéis neste quadro (direcional do controle); zero se nada.
        public static Vector2Int NavegacaoUI()
        {
            Atualizar();
            return navegacaoUI;
        }

        // Tremida curta do controle (golpe, dano). Força de 0 a 1.
        public static void Vibrar(float fraca, float forte, float duracao)
        {
#if ENABLE_INPUT_SYSTEM
            var g = Gamepad.current;
            if (g == null || !UsandoControle)
                return;
            g.SetMotorSpeeds(Mathf.Clamp01(fraca), Mathf.Clamp01(forte));
            fimVibracao = Time.unscaledTime + Mathf.Max(.02f, duracao);
#endif
        }

        public static void PararVibracao()
        {
#if ENABLE_INPUT_SYSTEM
            var g = Gamepad.current;
            if (g != null)
                g.SetMotorSpeeds(0f, 0f);
            fimVibracao = 0f;
#endif
        }

        // No tear: LB volta um pedal, RB avança. Zero se nenhum.
        public static int CicloPedal()
        {
#if ENABLE_INPUT_SYSTEM
            var g = Controle;
            if (g == null)
                return 0;
            if (g.leftShoulder.wasPressedThisFrame)
                return -1;
            if (g.rightShoulder.wasPressedThisFrame)
                return 1;
#endif
            return 0;
        }

        // No tear: X troca o fio da lançadeira.
        public static bool TrocouFio()
        {
#if ENABLE_INPUT_SYSTEM
            var g = Controle;
            return g != null && g.buttonWest.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static Vector2 Movimento()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.aKey.isPressed || k.leftArrowKey.isPressed)
                    v.x--;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed)
                    v.x++;
                if (k.sKey.isPressed || k.downArrowKey.isPressed)
                    v.y--;
                if (k.wKey.isPressed || k.upArrowKey.isPressed)
                    v.y++;
            }

            var g = Controle;
            if (g != null)
            {
                v += g.dpad.ReadValue();
                // Analógico com zona morta radial: inclinar pouco anda devagar.
                Vector2 alavanca = g.leftStick.ReadValue();
                float m = alavanca.magnitude;
                if (m > ZonaMorta)
                    v += alavanca / m * Mathf.InverseLerp(ZonaMorta, .95f, m);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x--;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x++;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y--;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y++;
#endif
            return Vector2.ClampMagnitude(v, 1f);
        }

        public static bool Apertou(char tecla)
        {
#if ENABLE_INPUT_SYSTEM
            var g = Controle;
            if (g != null)
            {
                bool botao = false;
                switch (tecla)
                {
                    case 'e':
                        botao = g.buttonSouth.wasPressedThisFrame;
                        break;
                    case 'x':
                        botao = g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame;
                        break;
                    case 'm':
                    case 'u':
                        botao = g.selectButton.wasPressedThisFrame;
                        break;
                    case 'j':
                    case 'h':
                        botao = g.buttonNorth.wasPressedThisFrame;
                        break;
                    case 'i':
                        botao = g.leftShoulder.wasPressedThisFrame;
                        break;
                    case 'r':
                        botao = g.rightStickButton.wasPressedThisFrame;
                        break;
                }

                if (botao)
                    return true;
            }

            Keyboard k = Keyboard.current;
            if (k == null)
                return false;
            switch (tecla)
            {
                case 'e':
                    return k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame;
                case 'x':
                    return k.escapeKey.wasPressedThisFrame;
                case 'm':
                    return k.mKey.wasPressedThisFrame;
                case 'j':
                    return k.jKey.wasPressedThisFrame;
                case 'i':
                    return k.iKey.wasPressedThisFrame;
                case 'b':
                    return k.bKey.wasPressedThisFrame;
                case 'r':
                    return k.rKey.wasPressedThisFrame;
                case 'q':
                    return k.qKey.wasPressedThisFrame;
                case '1':
                    return k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame;
                case '2':
                    return k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame;
                case '3':
                    return k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame;
                case '4':
                    return k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame;
                case 'h':
                    return k.hKey.wasPressedThisFrame;
                case 'a':
                    return k.aKey.wasPressedThisFrame;
                case 's':
                    return k.sKey.wasPressedThisFrame;
                case 'd':
                    return k.dKey.wasPressedThisFrame;
                case 'u':
                    return k.uKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame;
            }

#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (tecla)
            {
                case 'e': return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
                case 'x': return Input.GetKeyDown(KeyCode.Escape);
                case 'm': return Input.GetKeyDown(KeyCode.M);
                case 'j': return Input.GetKeyDown(KeyCode.J);
                case 'i': return Input.GetKeyDown(KeyCode.I);
                case 'b': return Input.GetKeyDown(KeyCode.B);
                case 'r': return Input.GetKeyDown(KeyCode.R);
                case 'q': return Input.GetKeyDown(KeyCode.Q);
                case '1': return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
                case '2': return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
                case '3': return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
                case '4': return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
                case 'h': return Input.GetKeyDown(KeyCode.H);
                case 'a': return Input.GetKeyDown(KeyCode.A);
                case 's': return Input.GetKeyDown(KeyCode.S);
                case 'd': return Input.GetKeyDown(KeyCode.D);
                case 'u': return Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.Backspace);
            }
#endif
            return false;
        }

        public static bool ApertouF1()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && k.f1Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
    return Input.GetKeyDown(KeyCode.F1);
#else
            return false;
#endif
        }

        public static bool ConfirmouMenu()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var g = Controle;
            return (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                   (g != null && g.buttonSouth.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)||Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        public static float Zoom()
        {
#if ENABLE_INPUT_SYSTEM
            float valor = 0f;
            var mouse = Mouse.current;
            var k = Keyboard.current;
            if (mouse != null)
            {
                float roda = mouse.scroll.ReadValue().y;
                valor = roda == 0f ? 0f : Mathf.Sign(roda);
            }

            if (k != null)
            {
                if (k.equalsKey.wasPressedThisFrame || k.numpadPlusKey.wasPressedThisFrame)
                    valor++;
                if (k.minusKey.wasPressedThisFrame || k.numpadMinusKey.wasPressedThisFrame)
                    valor--;
            }

            var g = Controle;
            if (g != null)
            {
                if (g.rightStick.up.wasPressedThisFrame)
                    valor++;
                if (g.rightStick.down.wasPressedThisFrame)
                    valor--;
            }

            return Mathf.Clamp(valor, -1f, 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
            float valor=Input.mouseScrollDelta.y;
            if(Input.GetKeyDown(KeyCode.Equals)||Input.GetKeyDown(KeyCode.KeypadPlus))valor++;
            if(Input.GetKeyDown(KeyCode.Minus)||Input.GetKeyDown(KeyCode.KeypadMinus))valor--;
            return Mathf.Clamp(valor,-1f,1f);
#else
            return 0f;
#endif
        }

        // Segurar L para escutar os sons da cidade.
        public static bool Escutando()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var g = Controle;
            return (k != null && k.lKey.isPressed) || (g != null && g.leftTrigger.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.L);
#else
            return false;
#endif
        }

        // Golpe nos confrontos: Espaço ou K.
        public static bool Golpeou()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var g = Controle;
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.kKey.wasPressedThisFrame)) || (g != null && g.buttonWest.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.K);
#else
            return false;
#endif
        }

        public static bool Correndo()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var g = Controle;
            return (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)) ||
                   (g != null && (g.rightTrigger.isPressed || g.leftStickButton.isPressed));
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }
    }
}

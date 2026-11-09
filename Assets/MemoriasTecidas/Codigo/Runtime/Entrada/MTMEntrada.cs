using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

#endif
namespace MemoriasTecidas.MundoV2
{
    public static class MTMEntrada
    {
        public static Vector2 Movimento()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null)
                return v;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed)
                v.x--;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed)
                v.x++;
            if (k.sKey.isPressed || k.downArrowKey.isPressed)
                v.y--;
            if (k.wKey.isPressed || k.upArrowKey.isPressed)
                v.y++;
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
                case 'u': return Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.Backspace);
            }
#endif
            return false;
        }

        public static bool ApertouPesca()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null &&
                (k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
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
            return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
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

        public static bool Correndo()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }
    }
}

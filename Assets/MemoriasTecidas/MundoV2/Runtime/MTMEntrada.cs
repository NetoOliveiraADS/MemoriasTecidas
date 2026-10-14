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
            Keyboard k = Keyboard.current; if (k == null) return v;
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
        public static bool Apertou(char tecla)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current; if (k == null) return false;
            switch (tecla)
            {
                case 'e': return k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame;
                case 'x': return k.escapeKey.wasPressedThisFrame;
                case 'm': return k.mKey.wasPressedThisFrame;
                case 'j': return k.jKey.wasPressedThisFrame;
                case 'i': return k.iKey.wasPressedThisFrame;
                case 'r': return k.rKey.wasPressedThisFrame;
                case 'q': return k.qKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (tecla)
            {
                case 'e': return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
                case 'x': return Input.GetKeyDown(KeyCode.Escape);
                case 'm': return Input.GetKeyDown(KeyCode.M);
                case 'j': return Input.GetKeyDown(KeyCode.J);
                case 'i': return Input.GetKeyDown(KeyCode.I);
                case 'r': return Input.GetKeyDown(KeyCode.R);
                case 'q': return Input.GetKeyDown(KeyCode.Q);
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

        public static bool Correndo()
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;return k!=null&&(k.leftShiftKey.isPressed||k.rightShiftKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }
    }
}

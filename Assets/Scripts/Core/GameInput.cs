using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ToxicWasteland
{
    /// <summary>
    /// Единая точка ввода (клавиатура + мышь).
    /// Работает и с новым Input System, и со старым Input Manager.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard K => Keyboard.current;
        static Mouse M => Mouse.current;

        static bool Down(params Key[] keys)
        {
            var k = K; if (k == null) return false;
            foreach (var key in keys) if (k[key].wasPressedThisFrame) return true;
            return false;
        }

        static bool Held(params Key[] keys)
        {
            var k = K; if (k == null) return false;
            foreach (var key in keys) if (k[key].isPressed) return true;
            return false;
        }

        public static float MoveX
        {
            get
            {
                float x = 0;
                if (Held(Key.A, Key.LeftArrow)) x -= 1;
                if (Held(Key.D, Key.RightArrow)) x += 1;
                return x;
            }
        }

        public static bool JumpPressed => Down(Key.Space, Key.W, Key.UpArrow);
        public static bool JumpHeld => Held(Key.Space, Key.W, Key.UpArrow);
        public static bool AttackPressed => Down(Key.J) || (M != null && M.leftButton.wasPressedThisFrame);
        public static bool DeviceHeld => Held(Key.K) || (M != null && M.rightButton.isPressed);
        public static bool SwitchModePressed => Down(Key.Q) || (M != null && M.middleButton.wasPressedThisFrame);
        public static bool MedkitPressed => Down(Key.H);
        public static bool InteractPressed => Down(Key.E);
        public static bool UpPressed => Down(Key.W, Key.UpArrow);
        public static bool DownPressed => Down(Key.S, Key.DownArrow);
        public static bool ConfirmPressed => Down(Key.Enter, Key.NumpadEnter, Key.Space, Key.E) || (M != null && M.leftButton.wasPressedThisFrame);
        public static bool CancelPressed => Down(Key.Escape);
#else
        static bool Down(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKeyDown(k)) return true;
            return false;
        }

        static bool Held(params KeyCode[] keys)
        {
            foreach (var k in keys) if (Input.GetKey(k)) return true;
            return false;
        }

        public static float MoveX
        {
            get
            {
                float x = 0;
                if (Held(KeyCode.A, KeyCode.LeftArrow)) x -= 1;
                if (Held(KeyCode.D, KeyCode.RightArrow)) x += 1;
                return x;
            }
        }

        public static bool JumpPressed => Down(KeyCode.Space, KeyCode.W, KeyCode.UpArrow);
        public static bool JumpHeld => Held(KeyCode.Space, KeyCode.W, KeyCode.UpArrow);
        public static bool AttackPressed => Down(KeyCode.J) || Input.GetMouseButtonDown(0);
        public static bool DeviceHeld => Held(KeyCode.K) || Input.GetMouseButton(1);
        public static bool SwitchModePressed => Down(KeyCode.Q) || Input.GetMouseButtonDown(2);
        public static bool MedkitPressed => Down(KeyCode.H);
        public static bool InteractPressed => Down(KeyCode.E);
        public static bool UpPressed => Down(KeyCode.W, KeyCode.UpArrow);
        public static bool DownPressed => Down(KeyCode.S, KeyCode.DownArrow);
        public static bool ConfirmPressed => Down(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space, KeyCode.E) || Input.GetMouseButtonDown(0);
        public static bool CancelPressed => Down(KeyCode.Escape);
#endif
    }
}

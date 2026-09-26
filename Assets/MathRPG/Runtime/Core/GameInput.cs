using UnityEngine.InputSystem;

namespace MathRPG
{
    public static class GameInput
    {
        public static bool Pressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        public static bool Held(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
        public static float Horizontal => (Held(Key.D) || Held(Key.RightArrow) ? 1 : 0) - (Held(Key.A) || Held(Key.LeftArrow) ? 1 : 0);
        public static bool Crouch => Held(Key.S) || Held(Key.DownArrow);
        public static bool Jump => Pressed(Key.Space);
        public static bool Attack => Pressed(Key.J);
    }
}

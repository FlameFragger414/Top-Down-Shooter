using UnityEngine;
using UnityEngine.InputSystem;

namespace Neon
{
    /// Thin wrapper over the Input System so gameplay code never touches device objects.
    /// The project runs with the legacy input manager disabled, so UnityEngine.Input is off limits.
    public static class InputHub
    {
        static Keyboard K => Keyboard.current;
        static Mouse M => Mouse.current;

        public static Vector2 Move
        {
            get
            {
                if (K == null) return Vector2.zero;
                Vector2 v = Vector2.zero;
                if (K.wKey.isPressed || K.upArrowKey.isPressed) v.y += 1f;
                if (K.sKey.isPressed || K.downArrowKey.isPressed) v.y -= 1f;
                if (K.aKey.isPressed || K.leftArrowKey.isPressed) v.x -= 1f;
                if (K.dKey.isPressed || K.rightArrowKey.isPressed) v.x += 1f;
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        public static Vector2 MouseScreen => M != null ? M.position.ReadValue() : Vector2.zero;

        public static bool Attack => M != null && M.leftButton.isPressed;
        public static bool AttackDown => M != null && M.leftButton.wasPressedThisFrame;
        public static bool ThrowDown => K != null && K.fKey.wasPressedThisFrame;
        /// Held right mouse button: scope in with a scoped weapon.
        public static bool Aiming => M != null && M.rightButton.isPressed;
        public static bool PickupDown => K != null && K.eKey.wasPressedThisFrame;
        public static bool DashDown => (K != null && K.leftShiftKey.wasPressedThisFrame) ||
                                       (K != null && K.spaceKey.wasPressedThisFrame);
        public static bool RestartDown => K != null && K.rKey.wasPressedThisFrame;
        public static bool ConfirmDown => K != null &&
            (K.enterKey.wasPressedThisFrame || K.numpadEnterKey.wasPressedThisFrame ||
             K.spaceKey.wasPressedThisFrame);
        public static bool BackDown => K != null && K.escapeKey.wasPressedThisFrame;
        public static bool LeftDown => K != null && (K.aKey.wasPressedThisFrame || K.leftArrowKey.wasPressedThisFrame);
        public static bool RightDown => K != null && (K.dKey.wasPressedThisFrame || K.rightArrowKey.wasPressedThisFrame);
        public static bool AnyKeyDown => K != null && K.anyKey.wasPressedThisFrame;

        /// Number row 1..5 for direct slot selection. Returns -1 when nothing was pressed.
        public static int SlotDown
        {
            get
            {
                if (K == null) return -1;
                if (K.digit1Key.wasPressedThisFrame) return 0;
                if (K.digit2Key.wasPressedThisFrame) return 1;
                if (K.digit3Key.wasPressedThisFrame) return 2;
                if (K.digit4Key.wasPressedThisFrame) return 3;
                if (K.digit5Key.wasPressedThisFrame) return 4;
                return -1;
            }
        }

        /// +1 / -1 / 0 for one notch of the wheel.
        public static int ScrollStep
        {
            get
            {
                if (M == null) return 0;
                float y = M.scroll.ReadValue().y;
                if (y > 0.01f) return 1;
                if (y < -0.01f) return -1;
                return 0;
            }
        }

        public static bool DropDown => K != null && K.gKey.wasPressedThisFrame;
    }
}

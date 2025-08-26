using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class CursorManager
    {
        public static bool DefaultUnlocked
        {
            get => !Unlocked.Inverse;
            set => Unlocked.Inverse = !value;
        }

        public static readonly BooleanLock Unlocked = new();

        static CursorManager() => Unlocked.OnChanged += OnStateChanged;

        private static void OnStateChanged(bool state)
        {
            Cursor.visible = state;
            Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}

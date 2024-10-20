using System;

namespace SwiftKraft.Utils
{
    /// <summary>
    /// A fancy boolean that stays until something receives it.
    /// </summary>
    public class Trigger
    {
        public bool Triggered { get; private set; }

        public void SetTrigger(bool state = true) => Triggered = state;

        /// <summary>
        /// Auto resets the trigger.
        /// </summary>
        /// <returns>If the trigger is triggered.</returns>
        public bool GetTrigger()
        {
            if (Triggered)
            {
                SetTrigger(false);
                return true;
            }
            return false;
        }

        public static implicit operator bool(Trigger tr) => tr.Triggered;
    }
}

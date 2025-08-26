using System;

namespace SwiftKraft.Utils
{
    public class SingleAction
    {
        public bool Executed { get; private set; }
        public Action Action { get; set; }

        public void Execute()
        {
            if (Executed)
                return;

            Action?.Invoke();
            Executed = true;
        }

        public void Rearm() => Executed = false;
        public void Unarm() => Executed = true;

        public static implicit operator Action(SingleAction act) => act.Action;
        public static implicit operator SingleAction(Action act) => new() { Action = act };
    }
}

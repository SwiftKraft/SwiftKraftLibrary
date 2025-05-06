using SwiftKraft.Utils;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Inputs
{
    public static class InputManager
    {
        public static readonly Dictionary<string, ActionBase> Actions = new();

        public static T CreateAction<T>(string id, InputStyle style = InputStyle.Click, string name = "") where T : ActionBase, new()
        {
            if (Actions.ContainsKey(id))
                return null;

            T t = new()
            {
                ID = id,
                Name = name,
                Style = style,
            };

            Actions.Add(id, t);
            return t;
        }

        public static bool TryGetAction<T>(string id, out T t) where T : ActionBase
        {
            t = GetAction<T>(id);
            return t != null;
        }

        public static T GetAction<T>(string id) where T : ActionBase => !Actions.ContainsKey(id) || Actions[id] is not T t ? null : t;

        public static bool TryGetAction(string id, out ActionBase action) => TryGetAction(id, out action);

        public static ActionBase GetAction(string id) => GetAction<ActionBase>(id);

        public static bool HasAction(string id) => Actions.ContainsKey(id);

        public static bool GetInput(string id) => TryGetAction(id, out ActionBase act) && act.Get();

        public static bool TryGetInput(string id, out bool input)
        {
            if (TryGetAction(id, out ActionBase act))
            {
                input = act.Get();
                return true;
            }

            input = false;
            return false;
        }

        [Serializable]
        public abstract class ActionBase
        {
            public string ID;
            public string Name;

            public InputStyle Style;

            readonly Trigger input = new();

            bool status;
            bool resetted;

            public abstract bool ReceiveInput();

            public virtual bool Get()
            {
                if ((Style == InputStyle.Hold && ValidateInput()) || status)
                    input.SetTrigger();

                return input.GetTrigger();
            }

            public virtual void Update()
            {
                if (!ValidateInput())
                    return;

                input.SetTrigger();

                if (Style == InputStyle.Toggle)
                    status = !status;
            }

            public virtual bool ValidateInput()
            {
                bool keyed = ReceiveInput();

                if (Style == InputStyle.Hold)
                    return keyed;

                bool valid = resetted && keyed;

                if (keyed)
                    resetted = false;
                else if (!resetted && !keyed)
                    resetted = true;

                return valid;
            }

            public virtual void Performed() { }

            public static implicit operator bool(ActionBase b) => b.Get();
            public static implicit operator string(ActionBase b) => b.ID;
            public static implicit operator ActionBase(string id) => TryGetAction(id, out ActionBase act) ? act : null;
        }

        public enum InputStyle
        {
            Click,
            Hold,
            Toggle
        }
    }
}

using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons.Triggers
{
    [RequireComponent(typeof(WeaponBase))]
    public class WeaponTrigger : WeaponComponent
    {
        public Action[] Actions;

        public bool Enabled = true;

        private void Update()
        {
            if (!Enabled || InputBlocker.Blocked)
                return;

            foreach (Action a in Actions)
                a.Update();
        }

        private void FixedUpdate()
        {
            if (!Enabled || InputBlocker.Blocked)
                return;

            foreach (Action a in Actions)
            {
                bool input = a.GetInput();
                Parent.UpdateAction(a.ID, input);
                if (input && Parent.StartAction(a.ID))
                    a.Performed();
            }
        }

        [Serializable]
        public class Action
        {
            public string ID;
            public KeyCode Key;
            public State Style;
            public Timer Linger = new(0.05f);

            readonly Trigger input = new();

            bool status;
            bool resetted;

            public void Update()
            {
                Linger.Tick(Time.deltaTime);

                if (ValidateInput())
                {
                    input.SetTrigger();
                    Linger.Reset();

                    if (Style == State.Toggle)
                        status = !status;
                }
            }

            public bool ValidateInput()
            {
                bool keyed = Input.GetKey(Key);

                if (Style == State.Hold)
                    return keyed;

                bool valid = resetted && keyed;

                if (keyed)
                    resetted = false;
                else if (!resetted && !keyed)
                    resetted = true;

                return valid;
            }

            public bool GetInput()
            {
                if ((Style == State.Hold && ValidateInput()) || status)
                    input.SetTrigger();

                return input.GetTrigger() || !Linger.Ended;
            }

            public void Performed() => Linger.Tick(Linger.MaxValue);

            public enum State
            {
                Click,
                Hold,
                Toggle
            }
        }
    }
}

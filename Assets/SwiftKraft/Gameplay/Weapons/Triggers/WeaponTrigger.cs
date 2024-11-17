using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons.Triggers
{
    [RequireComponent(typeof(WeaponBase))]
    public class WeaponTrigger : MonoBehaviour
    {
        public Action[] Actions;

        public WeaponBase Weapon
        {
            get
            {
                if (_weapon == null)
                    _weapon = GetComponent<WeaponBase>();

                return _weapon;
            }
        }
        WeaponBase _weapon;

        private void Update()
        {
            foreach (Action a in Actions)
                a.Update();
        }

        private void FixedUpdate()
        {
            foreach (Action a in Actions)
                if (a.GetInput())
                    Weapon.StartAction(a.ID);
        }

        [Serializable]
        public class Action
        {
            public string ID;
            public KeyCode Key;
            public bool AllowHeld;

            readonly Trigger input = new();

            bool resetted;

            public void Update()
            {
                if (ValidateInput())
                    input.SetTrigger();
            }

            public bool ValidateInput()
            {
                bool keyed = Input.GetKey(Key);

                if (AllowHeld)
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
                if (AllowHeld && ValidateInput())
                    input.SetTrigger();

                return input.GetTrigger();
            }
        }
    }
}

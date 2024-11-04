using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons.Triggers
{
    public abstract class PlayerWeaponTrigger : WeaponTrigger
    {
        public bool AllowHeld;

        bool resetted = true;

        readonly Trigger input = new();

        public abstract bool GetKey();

        protected virtual void Update()
        {
            if (ValidateInput())
                input.SetTrigger();
        }

        public bool ValidateInput()
        {
            bool keyed = GetKey();

            if (AllowHeld)
                return keyed;

            bool valid = resetted && keyed;

            if (keyed)
                resetted = false;
            else if (!resetted && !keyed)
                resetted = true;

            return valid;
        }

        public override bool GetInput()
        {
            if (AllowHeld && ValidateInput())
                input.SetTrigger();

            return input.GetTrigger();
        }
    }
}

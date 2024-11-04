using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons.Triggers
{
    public class InputWeaponTrigger : PlayerWeaponTrigger
    {
        public KeyCode Key;

        public override bool GetKey() => Input.GetKey(Key);
    }
}

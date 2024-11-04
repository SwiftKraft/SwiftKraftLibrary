using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons.Triggers
{
    [RequireComponent(typeof(WeaponBase))]
    public abstract class WeaponTrigger : MonoBehaviour
    {
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

        public abstract bool GetInput();

        protected virtual void FixedUpdate()
        {
            if (GetInput())
                Weapon.Attack();
        }
    }
}

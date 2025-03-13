using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons.UI
{
    public abstract class WeaponAmmoCounterBase<T> : RequiredDependencyComponent<T> where T : Component
    {
        public WeaponAmmo Ammo { get; set; }

        protected virtual void Awake()
        {
            if (!TryGetComponent(out WeaponAmmo wa) && !this.TryGetComponentInParent(out wa) && !this.TryGetComponentInChildren(out wa))
                return;

            Ammo = wa;
            Ammo.OnAmmoUpdated += OnAmmoUpdated;
        }

        protected virtual void Start() => OnAmmoUpdated(Ammo.CurrentAmmo);

        protected virtual void OnDestroy() => Ammo.OnAmmoUpdated -= OnAmmoUpdated;

        protected abstract void OnAmmoUpdated(int amount);
    }
}

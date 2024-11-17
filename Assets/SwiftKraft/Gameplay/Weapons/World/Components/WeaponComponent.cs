using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [RequireComponent(typeof(WeaponBase))]
    public abstract class WeaponComponent : RequiredDependencyComponent<WeaponBase>
    {
        public WeaponBase Parent => Component;
    }
}

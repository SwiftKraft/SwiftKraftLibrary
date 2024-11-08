using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [RequireComponent(typeof(WeaponBase))]
    public abstract class WeaponComponent : MonoBehaviour
    {
        public WeaponBase Parent { get; private set; }

        protected virtual void Awake() => Parent = GetComponent<WeaponBase>();
    }
}

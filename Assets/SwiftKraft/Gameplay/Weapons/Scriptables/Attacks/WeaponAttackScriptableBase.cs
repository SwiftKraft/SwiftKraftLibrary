using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAttackScriptableBase : ScriptableObject
    {
        public abstract void Attack(Transform origin);

        public virtual void Tick() { }
    }
}

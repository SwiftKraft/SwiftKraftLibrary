using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAttackScriptableBase : ScriptableObject
    {
        public GameObject Prefab;

        public virtual void Attack(Transform origin) => Instantiate(Prefab, origin.position, origin.rotation);

        public virtual void Tick() { }
    }
}

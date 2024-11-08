using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Weapons/Attacks/Base", fileName = "New Attack", order = 0)]
    public class WeaponAttackScriptableBase : ScriptableObject
    {
        public GameObject Prefab;

        public virtual void Attack(Transform origin) => Instantiate(Prefab, origin.position, origin.rotation);

        public virtual void Tick() { }
    }
}

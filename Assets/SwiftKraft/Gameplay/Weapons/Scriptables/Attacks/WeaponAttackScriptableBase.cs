using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Weapons/Attacks/Base", fileName = "New Attack", order = 0)]
    public class WeaponAttackScriptableBase : ScriptableObject
    {
        [HideInInspector]
        public WeaponBase Parent;

        public GameObject Prefab;

        public float Spread;

        public virtual void Attack(Transform origin)
        {
            if (!Parent.CanAttack)
                return;

            GameObject go = Instantiate(Prefab, origin.position, origin.rotation * Quaternion.Euler(Random.insideUnitCircle * Spread));
            if (go.TryGetComponent(out IPet pet))
                pet.Owner = Parent.GetRootOwner();

            Parent.AttackEvent(go);
        }

        public virtual void Tick() { }
    }
}

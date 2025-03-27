using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Projectiles;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Weapons/Attacks/Base", fileName = "New Attack", order = 0)]
    public class WeaponAttackScriptableBase : ScriptableObject
    {
        [HideInInspector]
        public WeaponBase Parent;

        public GameObject Prefab;

        public virtual void Attack(Transform origin)
        {
            if (!Parent.CanAttack)
                return;

            Parent.PreAttackEvent();
            GameObject go = Instantiate(Prefab, origin.position, origin.rotation);
            Parent.AttackEvent(go);
            if (go.TryGetComponent(out ProjectileBase projectile))
                projectile.BaseDamage = Parent.Damage;

            if (go.TryGetComponent(out IPet pet))
                pet.Owner = Parent.GetRootOwner();
        }

        public virtual void Tick() { }
    }
}

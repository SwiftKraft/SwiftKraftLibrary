using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Projectiles;
using SwiftKraft.Utils;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftKraft.Gameplay.Weapons
{
    [Serializable]
    public abstract class WeaponAttackBase
    {
        public const string AttackEvent = "Attack";

        [HideInInspector]
        public WeaponBase Parent;

        public GameObject Prefab;
        public ModifiableStatistic Amount = new(1);

        public virtual GameObject[] Spawn(Transform transform)
        {
            int amount = (int)Amount;
            if (amount <= 0)
                amount = 1;

            GameObject[] res = new GameObject[amount];

            for (int i = 0; i < amount; i++)
            {
                Parent.PreSpawnEvent();
                GameObject go = Object.Instantiate(Prefab, transform.position, transform.rotation);

                if (go.TryGetComponent(out ProjectileBase projectile))
                    projectile.BaseDamage = Parent.Damage;

                if (go.TryGetComponent(out IPet pet))
                    pet.Owner = Parent.GetRootOwner();

                res[i] = go;
                Parent.SpawnEvent(go);
            }

            Parent.SendEvent(AttackEvent);

            return res;
        }

        public abstract void Attack(Transform origin);

        public virtual void Tick() { }
    }
}

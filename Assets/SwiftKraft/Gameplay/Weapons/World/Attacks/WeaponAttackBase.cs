using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Projectiles;
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
        public int Amount = 1;

        public virtual GameObject[] Spawn(Transform transform)
        {
            if (Amount <= 0)
                Amount = 1;

            GameObject[] res = new GameObject[Amount];

            for (int i = 0; i < Amount; i++)
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

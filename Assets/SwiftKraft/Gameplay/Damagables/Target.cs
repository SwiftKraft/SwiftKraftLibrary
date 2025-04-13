using SwiftKraft.Gameplay.Interfaces;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Damagables
{
    public class Target : MonoBehaviour, IDamagable
    {
        public static event Action<Target, DamageDataBase> OnTargetDamage;

        public UnityEvent<DamageDataBase> OnDamage;
        public UnityEvent<Vector3> OnHit;

        public void Damage(DamageDataBase data)
        {
            OnDamage?.Invoke(data);
            OnHit?.Invoke(data.HitPoint);
            OnTargetDamage?.Invoke(this, data);
        }
    }
}

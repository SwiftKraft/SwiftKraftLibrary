using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Damagables
{
    public class Target : MonoBehaviour, IDamagable
    {
        public UnityEvent<float> OnDamage;
        public UnityEvent<Vector3> OnHit;

        public void Damage(DamageDataBase data)
        {
            OnDamage?.Invoke(data.Damage);
            OnHit?.Invoke(data.HitPoint);
        }
    }
}

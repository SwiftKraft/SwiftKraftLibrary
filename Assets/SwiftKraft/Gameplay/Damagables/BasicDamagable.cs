using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Damagables
{
    public class BasicDamagable : MonoBehaviour, IHealth
    {
        [field: SerializeField]
        public float MaxHealth { get; set; }

        public float CurrentHealth { get; set; }

        public bool IsDead { get; set; }

        public bool DestroyOnDeath;

        public UnityEvent OnDeath;
        public UnityEvent<DamageDataBase> OnDamage;

        protected virtual void Awake() => CurrentHealth = MaxHealth;

        public void Damage(DamageDataBase data)
        {
            OnDamage?.Invoke(data);
            CurrentHealth -= data.Damage;
            if (CurrentHealth <= 0)
                Death();
        }

        public void Death()
        {
            if (IsDead)
                return;

            OnDeath?.Invoke();
            IsDead = true;

            if (DestroyOnDeath)
                Destroy(gameObject);
        }
    }
}

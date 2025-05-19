using SwiftKraft.Gameplay;
using SwiftKraft.Gameplay.Damagables;
using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.UI.HUD
{
    [RequireComponent(typeof(IPawn))]
    public class HitmarkerTriggerBase : MonoBehaviour
    {
        public IPawn Target { get; private set; }

        protected virtual void Awake()
        {
            Target = GetComponent<IPawn>();
            GameEvents.OnDamage += OnDamage;
        }

        protected virtual void OnDestroy() => GameEvents.OnDamage -= OnDamage;

        protected virtual void OnDamage(IDamagable target, DamageDataBase damage)
        {
            if (damage.Attacker != Target)
                return;

            Hitmarker.Trigger();
        }
    }
}

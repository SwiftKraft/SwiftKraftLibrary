using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public abstract class ProjectileBase : PetBehaviourBase
    {
        public Timer Lifetime;

        public bool Despawned { get; private set; }

        protected virtual void Awake() => Lifetime.Reset();

        protected virtual void FixedUpdate()
        {
            if (Despawned)
                return;

            Lifetime.Tick(Time.fixedDeltaTime);
            if (Lifetime.Ended)
            {
                Despawned = true;
                Despawn();
            }
        }

        public virtual void Despawn() => Destroy(gameObject);
    }
}

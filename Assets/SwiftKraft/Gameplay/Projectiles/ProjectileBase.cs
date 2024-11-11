using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public abstract class ProjectileBase : PetBehaviourBase
    {
        public Timer Lifetime;
        public float MinLifetime = 0.05f;

        protected virtual void Awake() => Lifetime.Reset();

        protected virtual void FixedUpdate()
        {
            Lifetime.Tick(Time.fixedDeltaTime);
            
        }
    }
}

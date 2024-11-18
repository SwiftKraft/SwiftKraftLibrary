using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Projectiles
{
    public abstract class ProjectileComponent<T> : RequiredDependencyComponent<T> where T : ProjectileBase
    {
        public T Projectile => Component;
    }
}

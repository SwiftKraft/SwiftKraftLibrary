using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Projectiles
{
    public abstract class ProjectileComponent : RequiredDependencyComponent<ProjectileBase>
    {
        public abstract void Init();
    }

    public abstract class ProjectileComponent<T> : ProjectileComponent where T : ProjectileBase
    {
        public T Projectile
        {
            get
            {
                if (_projectile == null)
                    _projectile = Component is T t ? t : null;

                return _projectile;
            }
        }
        T _projectile;
    }
}

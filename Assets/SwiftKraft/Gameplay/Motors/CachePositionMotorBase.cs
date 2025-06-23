using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public abstract class CachePositionMotorBase<T> : MotorBase<T> where T : Component
    {
        public override Vector3 WishMovePosition
        {
            get => base.WishMovePosition;
            set
            {
                base.WishMovePosition = value;
                CachedWishMovePosition = value;
            }
        }

        protected Vector3 CachedWishMovePosition { get; private set; }

        protected float CachedPositionDistance => Vector3.Distance(transform.position, CachedWishMovePosition);
    }
}

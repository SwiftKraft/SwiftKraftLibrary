using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public abstract class MotorBase<T> : MonoBehaviour where T : Component
    {
        public T Component
        {
            get
            {
                if (_component == null && !TryGetComponent(out _component))
                    _component = gameObject.AddComponent<T>();
                return _component;
            }
        }
        T _component;

        public Vector3 WishMoveDirection { get; set; }

        public Vector3 WishMovePosition
        {
            get => transform.position + WishMoveDirection;
            set => WishLookDirection = (value - transform.position).normalized;
        }

        public Vector3 CurrentMoveDirection { get; private set; }

        public Quaternion WishLookRotation { get; set; }

        public Vector3 WishLookDirection
        {
            get => WishLookRotation * Vector3.forward;
            set => WishLookRotation = Quaternion.LookRotation(value.normalized);
        }

        public Vector3 WishLookPosition
        {
            get => LookPoint.position + WishLookDirection;
            set => WishLookDirection = value - LookPoint.position;
        }

        public Quaternion CurrentLookRotation { get; private set; }

        [field: SerializeField]
        public Transform LookPoint { get; private set; }

        [field: SerializeField]
        public bool Enabled { get; set; } = true;
        [field: SerializeField]
        public bool LookInUpdate { get; set; } = true;

        protected virtual void Update()
        {
            if (!Enabled)
                return;

            if (LookInUpdate)
            {
                CurrentLookRotation = LookInterpolation();
                Look(CurrentLookRotation);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!Enabled)
                return;

            CurrentLookRotation = LookInterpolation();
            CurrentMoveDirection = MoveInterpolation();

            Look(CurrentLookRotation);
            Move(CurrentMoveDirection);
        }

        public virtual Quaternion LookInterpolation() => WishLookRotation;
        public virtual Vector3 MoveInterpolation() => WishMoveDirection;

        public abstract void Look(Quaternion rotation);
        public abstract void Move(Vector3 direction);
    }
}

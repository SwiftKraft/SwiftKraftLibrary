using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public abstract class MotorBase : MonoBehaviour
    {
        public Vector3 WishMoveDirection { get; set; }

        public Vector3 WishMovePosition
        {
            get => transform.position + WishMoveDirection;
            set => WishMoveDirection = (value - transform.position).normalized;
        }

        public Vector3 CurrentMoveDirection { get; protected set; }

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

        public virtual float LookPointHeight
        {
            get => LookPoint.localPosition.y;
            set => LookPoint.localPosition = Vector3.up * value;
        }

        public Quaternion CurrentLookRotation { get; protected set; }

        public int State { get; protected set; }

        [field: SerializeField]
        public Transform LookPoint { get; protected set; }

        [field: SerializeField]
        public bool Enabled { get; set; } = true;
        [field: SerializeField]
        public bool LookInUpdate { get; set; } = true;

        /// <summary>
        /// The the amount of time spent moving before stopping. 
        /// </summary>
        public float TimeMoving { get; protected set; }

        /// <summary>
        /// Whether or not the motor is currently moving.
        /// </summary>
        public bool Moving { get; protected set; }

        public MotorBase Vehicle { get; set; }

        protected virtual void Awake() { }

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
            Look(CurrentLookRotation);

            if (Vehicle == null)
            {
                CurrentMoveDirection = MoveInterpolation();
                Move(CurrentMoveDirection);

                Moving = CurrentMoveDirection != Vector3.zero;
                TimeMoving = Moving ? TimeMoving + Time.fixedDeltaTime : 0f;
            }
        }

        public virtual Quaternion LookInterpolation() => WishLookRotation;
        public virtual Vector3 MoveInterpolation() => WishMoveDirection;

        /// <summary>
        /// Changes the look rotation of the motor. Runs every FixedUpdate.
        /// </summary>
        /// <param name="rotation">Global rotation.</param>
        public abstract void Look(Quaternion rotation);
        /// <summary>
        /// Move the motor. Runs every FixedUpdate.
        /// </summary>
        /// <param name="direction">Global direction of the movement.</param>
        public abstract void Move(Vector3 direction);
    }

    public abstract class MotorBase<T> : MotorBase where T : Component
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
    }
}

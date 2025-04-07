using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public abstract class MotorBase : MonoBehaviour
    {
        public class DriverSlot
        {
            public MotorBase Parent { get; private set; }
            public MotorBase Reference
            {
                get => _reference;
                set
                {
                    if (_reference == value)
                        return;

                    if (_reference != null)
                    {
                        _reference.Vehicle = null;
                        _reference.transform.parent = prevParent;
                        if (ExitPoint != null)
                            _reference.transform.SetPositionAndRotation(ExitPoint.position, ExitPoint.rotation);
                    }

                    _reference = value;
                    _reference.Vehicle = Parent;
                    prevParent = _reference.transform.parent;
                    _reference.transform.parent = Point;
                    _reference.transform.SetLocalPositionAndRotation(default, Quaternion.identity);
                }
            }
            MotorBase _reference;
            public Transform Point;
            public Transform ExitPoint;
            public float Influence;

            Transform prevParent;

            public void Init(MotorBase parent) => Parent = parent;

            public Vector3 GetMove() => Reference == null ? Vector3.zero : Reference.WishMoveDirection * Influence;

            public Quaternion GetLook() => Reference == null ? Quaternion.identity : Quaternion.LerpUnclamped(Quaternion.identity, Reference.WishLookRotation, Influence);
        }

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

        public DriverSlot[] Drivers;

        public MotorBase Vehicle { get; set; }

        protected virtual void Awake()
        {
/*            foreach (DriverSlot slot in Drivers)
                slot.Init(this);*/
        }

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

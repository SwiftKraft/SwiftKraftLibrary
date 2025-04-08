using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public class MotorDriver : MonoBehaviour
    {
        [Serializable]
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
                        {
                            _reference.transform.SetPositionAndRotation(ExitPoint.position, ExitPoint.rotation);
                            _reference.WishLookRotation = ExitPoint.rotation;
                        }
                    }

                    _reference = value;

                    if (_reference == null)
                        return;

                    _reference.Vehicle = Parent;
                    prevParent = _reference.transform.parent;
                    _reference.transform.parent = Point;
                    _reference.transform.SetLocalPositionAndRotation(default, Quaternion.identity);
                    _reference.WishLookRotation = Quaternion.identity;
                }
            }
            MotorBase _reference;
            public Transform Point;
            public Transform ExitPoint;
            public float LookInfluence;
            public float MoveInfluence;

            Transform prevParent;

            public void Init(MotorBase parent) => Parent = parent;

            public Vector3 GetMove() => Reference == null ? Vector3.zero : Reference.WishMoveDirection * MoveInfluence;

            public Quaternion GetLook() => Reference == null ? Quaternion.identity : Quaternion.LerpUnclamped(Quaternion.identity, Reference.WishLookRotation, LookInfluence);
        }

        public DriverSlot[] Drivers;

        public MotorBase Parent { get; private set; }

        private void Awake()
        {
            Parent = GetComponent<MotorBase>();
            foreach (DriverSlot slot in Drivers)
                slot.Init(Parent);
        }

        private void FixedUpdate()
        {
            foreach (DriverSlot slot in Drivers)
            {
                Parent.WishMoveDirection = slot.GetMove();
                Parent.WishLookRotation = slot.GetLook();
            }
        }

        public bool TryGetDriver(int index, out DriverSlot driver)
        {
            driver = GetDriver(index);
            return driver != null;
        }

        public DriverSlot GetDriver(int index) => !Drivers.InRange(index) ? null : Drivers[index];

        public bool SetDriverSafe(MotorBase motor, int index)
        {
            if (!Drivers.InRange(index) || Drivers[index].Reference != null)
                return false;

            SetDriver(motor, index);
            return true;
        }

        public void SetDriver(MotorBase motor, int index)
        {
            if (!Drivers.InRange(index))
                return;

            Drivers[index].Reference = motor;
        }
    }
}

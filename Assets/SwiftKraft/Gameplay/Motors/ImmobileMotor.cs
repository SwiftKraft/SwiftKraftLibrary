using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public abstract class ImmobileMotor : MonoBehaviour, ILookable
    {
        public Transform Vertical;
        public Transform Horizontal;

        public Vector3 VerticalMultiplier = Vector3.right;
        public Vector3 HorizontalMultiplier = Vector3.up;

        public Quaternion WishLookRotation { get; set; }

        public Quaternion CurrentLookRotation { get; private set; }

        public Vector3 WishLookDirection
        {
            get => WishLookRotation * Vector3.forward;
            set
            {
                Vector3 target = Quaternion.LookRotation(value.normalized, transform.up).eulerAngles;
                target.x = -target.x;
                WishLookRotation = Quaternion.Euler(target);
            }
        }

        public Vector3 WishLookPosition
        {
            get => Vertical.position + WishLookDirection;
            set => WishLookDirection = value - Vertical.position;
        }

        public Transform LookPoint => Vertical;

        public bool LookInUpdate = false;

        protected virtual void Update()
        {
            if (LookInUpdate)
                UpdateRotation();
        }

        protected virtual void FixedUpdate()
        {
            UpdateRotation();

            CurrentLookRotation = Interpolation();
        }

        public abstract Quaternion Interpolation();

        public virtual void UpdateRotation()
        {
            float pitch = CurrentLookRotation.eulerAngles.x;
            float yaw = CurrentLookRotation.eulerAngles.y;

            if (Vertical != null)
                Vertical.localRotation = Quaternion.Euler(VerticalMultiplier * -pitch);
            if (Horizontal != null)
                Horizontal.localRotation = Quaternion.Euler(HorizontalMultiplier * -yaw);
        }
    }
}

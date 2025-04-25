using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public abstract class ImmobileMotor : MonoBehaviour, ILookable
    {
        public Transform Vertical;
        public Transform Horizontal;

        public Quaternion WishLookRotation { get; set; }

        public Quaternion CurrentLookRotation { get; private set; }

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
                Vertical.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            if (Horizontal != null)
                Horizontal.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}

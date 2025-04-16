using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors.Miscellaneous
{
    public class Bobbing : OptionalModifyTransformComponent
    {
        public MotorBase MotorBase { get; private set; }

        public Vector3Curve MovePosition;
        public Vector3Curve MoveRotation;

        public float Amplitude = 1f;
        public float Rate = 1f;
        public float DampTime = 0.1f;

        Vector3 pos;
        Vector3 rot;

        protected override void Awake()
        {
            base.Awake();
            MotorBase = GetComponentInParent<MotorBase>();
        }

        protected virtual void Update()
        {
            Position = Vector3.SmoothDamp(Position, MovePosition.Evaluate(MotorBase.MoveFactor * Rate) * Amplitude, ref pos, DampTime);
            Rotation = Rotation.SmoothDamp(Quaternion.Euler(MoveRotation.Evaluate(MotorBase.MoveFactor * Rate) * Amplitude), ref rot, DampTime);
        }
    }
}

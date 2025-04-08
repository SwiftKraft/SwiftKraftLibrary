using SwiftKraft.Gameplay.Motors;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS
{
    [RequireComponent(typeof(Rigidbody))]
    public class FPSMovementStateMotor : PlayerMotorBase<Rigidbody>
    {
        public override Vector2 GetInputLook() => new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        public override Vector2 GetInputMove() => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        public override void Look(Quaternion rotation)
        {
            
        }

        public override void Move(Vector3 direction)
        {
            
        }

        public abstract class MovementStateBase : ScriptableObject
        {
            public FPSMovementStateMotor Motor { get; set; }

            public abstract void Look(Quaternion rotation);

            public abstract void Move(Vector3 direction);
        }
    }
}

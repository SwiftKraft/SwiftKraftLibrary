using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public abstract class PlayerMotorBase<T> : MotorBase<T> where T : Component
    {
        public abstract Vector2 GetInputMove();
        public abstract Vector2 GetInputLook();
    }
}

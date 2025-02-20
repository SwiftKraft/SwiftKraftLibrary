using UnityEngine;

namespace Player.Movement
{
    public abstract class PlayerMovementStateBase : ScriptableObject
    {
        public virtual void StateStarted(PlayerMotor parent) { }
        public virtual void StateEnded(PlayerMotor parent) { }
        public virtual void GroundedChanged(PlayerMotor parent, bool value, bool prev) { }
        public abstract void InputUpdate(PlayerMotor parent);
        public abstract void TickUpdate(PlayerMotor parent);

        public virtual void ReceiveData<T>(T obj) { }
    }
}

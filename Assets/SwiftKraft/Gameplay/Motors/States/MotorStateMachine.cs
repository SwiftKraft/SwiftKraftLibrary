using UnityEngine;

namespace SwiftKraft.Gameplay.Motors.States
{
    public class MotorStateMachine : MonoBehaviour
    {
        /// <summary>
        /// Will use GetComponentInChildren when unassigned.
        /// </summary>
        [field: SerializeField]
        public MotorBase Motor { get; private set; }

        public MotorState DefaultState;

        public MotorState CurrentState
        {
            get => currentState;
            set
            {
                if (currentState == value)
                    return;

                if (currentState != null)
                {
                    currentState.End();
                    DestroyImmediate(currentState, false);
                }

                if (value != null)
                {
                    currentState = Instantiate(value);
                    currentState.Parent = this;
                    currentState.Begin();
                }
            }
        }

        MotorState currentState;

        protected virtual void Awake()
        {
            CurrentState = DefaultState;
            if (Motor == null)
                Motor = GetComponentInChildren<MotorBase>();
        }

        protected virtual void Update()
        {
            if (CurrentState == null)
                return;

            CurrentState.Frame();
        }

        protected virtual void FixedUpdate()
        {
            if (CurrentState == null)
                return;

            CurrentState.Tick();
        }

        protected virtual void OnDestroy() => CurrentState = null;
    }

    public abstract class MotorState : ScriptableObject
    {
        public MotorStateMachine Parent { get; set; }

        public MotorBase Motor => Parent.Motor;

        public virtual void Frame() { }
        public virtual void Tick() { }
        public virtual void Begin() { }
        public virtual void End() { }
    }
}

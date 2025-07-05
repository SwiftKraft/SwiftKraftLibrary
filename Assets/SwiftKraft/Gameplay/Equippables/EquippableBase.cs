using UnityEngine;

namespace SwiftKraft.Gameplay.Equippables
{
    public abstract class EquippableBase : MonoBehaviour
    {
        public EquippableStateBase CurrentState
        {
            get => _currentState;
            set
            {
                _currentState?.End();
                _currentState = value;
                _currentState ??= DefaultState;
                _currentState?.Begin();
            }
        }
        EquippableStateBase _currentState;

        /// <summary>
        /// Setting current state to null will get to this state.
        /// </summary>
        public abstract EquippableStateBase DefaultState { get; }

        /// <summary>
        /// This is the state the equippable will be in when activated.
        /// </summary>
        public abstract EquippableStateBase EntryState { get; }

        /// <summary>
        /// A place to cache all states.
        /// </summary>
        public abstract EquippableStateBase[] AllStates { get; }

        protected virtual void Start() => CurrentState = EntryState;

        protected virtual void FixedUpdate() => CurrentState?.Tick();
    }

    public abstract class EquippableStateBase
    {
        public virtual void Begin() { }
        public virtual void Tick() { }
        public virtual void End() { }
    }
}

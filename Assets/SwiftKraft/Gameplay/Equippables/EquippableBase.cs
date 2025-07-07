using Newtonsoft.Json;
using SwiftKraft.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Equippables
{
    public abstract class EquippableBase : MonoBehaviour
    {
        public readonly Dictionary<int, EquippableDataWrapper> Data;

        public EquippableStateBase CurrentState
        {
            get
            {
                if (_currentStateDirty)
                {
                    _currentState = AllStatesCache.InRange(CurrentStateIndex) && AllStatesCache.Length > 0 ? AllStatesCache[CurrentStateIndex] : null;
                    _currentStateDirty = false;
                }

                return _currentState;
            }
        }
        EquippableStateBase _currentState;

        public int CurrentStateIndex
        {
            get => _currentStateIndex;
            set
            {
                CurrentState?.End();

                _currentStateDirty = true;
                _currentStateIndex = value;

                if (CurrentState == null)
                {
                    _currentStateDirty = true;
                    _currentStateIndex = DefaultState;
                }

                CurrentState?.Begin();
            }
        }
        int _currentStateIndex;
        bool _currentStateDirty;

        /// <summary>
        /// Setting current state to null will get to this state.
        /// </summary>
        public abstract int DefaultState { get; }

        /// <summary>
        /// This is the state the equippable will be in when activated.
        /// </summary>
        public abstract int EntryState { get; }

        /// <summary>
        /// Define all states.
        /// </summary>
        public abstract EquippableStateBase[] AllStates { get; }

        /// <summary>
        /// Cache all states.
        /// </summary>
        public EquippableStateBase[] AllStatesCache { get; private set; }

        public T AddData<T>(int id) where T : EquippableDataBase, new()
        {
            T t = new();
            Data.Add(id, t);
            return t;
        }

        public bool RemoveData(int id) => Data.Remove(id);

        protected virtual void Awake() => AllStatesCache = AllStates;

        protected virtual void Start() => CurrentStateIndex = EntryState;

        protected virtual void FixedUpdate() => CurrentState?.Tick();
    }

    public class EquippableDataWrapper
    {
        public readonly EquippableDataBase Base;

        public EquippableDataWrapper(EquippableDataBase b) => Base = b;

        public static implicit operator EquippableDataWrapper(EquippableDataBase b) => new(b);
        public static implicit operator EquippableDataBase(EquippableDataWrapper w) => w.Base;
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class EquippableDataBase { }

    public abstract class EquippableStateBase
    {
        public readonly EquippableBase Parent;

        public EquippableStateBase(EquippableBase parent) => Parent = parent;

        public virtual void Begin() { }
        public virtual void Tick() { }
        public virtual void End() { }
    }
}

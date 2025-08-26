using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCCore : PawnBehaviourBase
    {
        public readonly SafeDictionary<string, NPCModuleBase> Modules = new();
        public readonly SafeDictionary<string, object> Values = new();

        public NPCStateBase CurrentState
        {
            get => _currentState;
            set
            {
                if (_currentState != null)
                {
                    _currentState.End();
                    DestroyImmediate(_currentState, false);
                }

                if (value == null)
                    return;

                _currentState = Instantiate(value);
                _currentState.Init(this);
                _currentState.Begin();
            }
        }
        NPCStateBase _currentState;

        [SerializeField]
        private NPCStateBase startingState;

        protected virtual void Awake() => NPCManager.Instance.NPCs.Add(this);

        protected virtual void Start() => CurrentState = startingState;

        public virtual void Tick()
        {
            if (CurrentState != null)
                CurrentState.Tick();

            foreach (NPCModuleBase mod in Modules.Values)
                if (mod.enabled)
                    mod.Tick();
        }

        protected virtual void OnDestroy() => NPCManager.Instance.NPCs.Remove(this);
    }

    [RequireComponent(typeof(NPCCore))]
    public abstract class NPCModuleBase : MonoBehaviour
    {
        public virtual string ID => "Module";

        public NPCCore Parent { get; private set; }

        protected virtual void Awake()
        {
            Parent = GetComponent<NPCCore>();
            Parent.Modules.Add(ID, this);
        }

        public virtual void Tick() { }

        protected virtual void OnDestroy()
        {
            if (Parent != null)
                Parent.Modules.Remove(ID);
        }
    }
}

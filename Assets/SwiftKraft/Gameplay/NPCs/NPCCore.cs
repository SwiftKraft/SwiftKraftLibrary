using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCCore : MonoBehaviour
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

        protected virtual void Start() => CurrentState = startingState;

        protected virtual void Update()
        {
            if (CurrentState != null)
                CurrentState.Update();
        }

        protected virtual void FixedUpdate()
        {
            if (CurrentState != null)
                CurrentState.Tick();
        }
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

        protected virtual void OnDestroy()
        {
            if (Parent != null)
                Parent.Modules.Remove(ID);
        }
    }
}

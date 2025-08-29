using UnityEngine;

namespace SwiftKraft.Utils
{
    public abstract class AnimatorCommunicator<T> : RequiredDependencyComponent<Animator> where T : Component
    {
        public Animator Animator => Component;

        public T ParentComponent
        {
            get
            {
                if (_parentComponent == null)
                    _parentComponent = GetComponentInParent<T>();

                return _parentComponent;
            }
        }
        T _parentComponent;

        protected virtual void Awake()
        {
            _parentComponent = GetComponentInParent<T>();
        }
    }
}

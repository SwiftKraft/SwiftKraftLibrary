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
                if (_component == null)
                    _component = GetComponentInParent<T>();

                return _component;
            }
        }
        T _component;
    }
}

using UnityEngine;

namespace SwiftKraft.Utils
{
    public abstract class RequiredDependencyComponent<T> : MonoBehaviour where T : Component
    {
        public T Component
        {
            get
            {
                if (_component == null)
                    _component = GetComponent<T>();

                return _component;
            }
        }
        T _component;
    }
}

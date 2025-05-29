using UnityEngine;

namespace SwiftKraft.Utils
{
    public abstract class RequiredDependencyComponent<T> : MonoBehaviour
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

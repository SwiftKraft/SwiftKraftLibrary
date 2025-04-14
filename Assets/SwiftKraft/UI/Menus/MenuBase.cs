using System;
using UnityEngine;

namespace SwiftKraft.UI.Menus
{
    public abstract class MenuBase : MonoBehaviour
    {
        public bool Active
        {
            get => _active;
            set
            {
                if (value == _active)
                    return;

                _active = value;
                gameObject.SetActive(value);
                OnActiveChanged?.Invoke();
            }
        }
        bool _active = true;

        public event Action OnActiveChanged;
    }
}

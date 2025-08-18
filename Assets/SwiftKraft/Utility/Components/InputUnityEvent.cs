using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Utils
{
    public class InputUnityEvent : MonoBehaviour
    {
        public KeyCode Key;

        public UnityEvent OnPress;
        public UnityEvent OnRelease;

        private void Update()
        {
            if (Input.GetKeyDown(Key))
                OnPress?.Invoke();
            else if (Input.GetKeyUp(Key))
                OnRelease?.Invoke();
        }
    }
}

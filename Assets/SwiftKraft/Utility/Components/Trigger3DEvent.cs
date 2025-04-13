using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Utils
{
    public class Trigger3DEvent : MonoBehaviour
    {
        public string Tag = "Player";

        public UnityEvent<Collider> OnTriggerEnterEvent;
        public UnityEvent<Collider> OnTriggerExitEvent;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(Tag))
                OnTriggerEnterEvent?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(Tag))
                OnTriggerExitEvent?.Invoke(other);
        }
    }
}
